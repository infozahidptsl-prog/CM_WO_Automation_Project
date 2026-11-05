using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using System.Text.RegularExpressions;
using Tesseract;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Microsoft.AspNetCore.Hosting;
using WO_CM_Review_Automation.Application.Interfaces;
using WO_CM_Review_Automation.Application.ViewModels.CMReview;
using WO_CM_Review_Automation.Domain.Entities.CMReview;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using WO_CM_Review_Automation.Application.Reports;
using WO_CM_Review_Automation.Infrastructure.Data;

namespace WO_CM_Review_Automation.Web.Components.Pages.CMReview;

public partial class DocumentPreparation : ComponentBase, IDisposable
{
    [Inject] private ICMReviewDocumentService DocumentService { get; set; } = default!;
    [Inject] private ICMReviewWOReportService WOReportService { get; set; } = default!;
    [Inject] private IImageStorageService ImageStorageService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private IWebHostEnvironment WebHostEnvironment { get; set; } = default!;
    [Inject] private ICMReviewWordGenerator WordGenerator { get; set; } = default!;
    [Inject] private IServiceScopeFactory ServiceScopeFactory { get; set; } = default!;
    [Inject] private ICMReviewEvidenceImageGenerator EvidenceImageGenerator { get; set; } = default!;

    private DotNetObjectReference<DocumentPreparation>? _dotNetRef;
    private bool _pendingClipboardInit = false; // deferred to OnAfterRenderAsync

    // Currently active view model - single source of truth for UI
    public CMReviewDocumentViewModel DocumentModel { get; set; } = new();

    private string _lastSeenSubnet = null; // Used to track the last RDP/FileZilla subnet found for OCR fallbacks

    public bool IsLoading { get; set; }
    public bool IsDocumentLoaded { get; set; }
    public List<string> ActiveApplications { get; set; } = new();

    public string DisplayApp => string.Equals(DocumentModel?.Application, "Techvan", StringComparison.OrdinalIgnoreCase)
        ? "TechVan"
        : (DocumentModel?.Application ?? "");

    private bool _showSiemWarningDialog = false;
    private enum PendingSiemAction
    {
        None,
        SaveAndClose,
        Finalize
    }
    private PendingSiemAction _pendingSiemAction = PendingSiemAction.None;

    [Parameter]
    public Guid? DocumentId { get; set; }

    [SupplyParameterFromQuery(Name = "app")]
    public string? QueryApp { get; set; }

    [SupplyParameterFromQuery(Name = "month")]
    public int? QueryMonth { get; set; }

    [SupplyParameterFromQuery(Name = "year")]
    public int? QueryYear { get; set; }

    private readonly System.Threading.SemaphoreSlim _loadLock = new(1, 1);

    protected override async Task OnParametersSetAsync()
    {
        if (IsDocumentLoaded) return;

        await _loadLock.WaitAsync();
        try
        {
            if (IsDocumentLoaded) return;

            using var scope = ServiceScopeFactory.CreateScope();
            var mapService = scope.ServiceProvider.GetRequiredService<WO_CM_Review_Automation.Application.Mappings.Interfaces.IMappingService>();
            var docService = scope.ServiceProvider.GetRequiredService<ICMReviewDocumentService>();

            if (!ActiveApplications.Any())
            {
                var mappings = await mapService.GetApplicationMappingsAsync();
                ActiveApplications = mappings
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.DisplayOrder)
                    .Select(m => m.NormalizedName)
                    .Distinct()
                    .ToList();

                if (!ActiveApplications.Any())
                {
                    ActiveApplications = new List<string> { "POS", "DMS", "Sales Force App", "AMS", "Techvan", "LEA" };
                }
            }

            if (DocumentId.HasValue)
            {
                var dbDoc = await docService.GetDocumentAsync(DocumentId.Value);
                if (dbDoc != null)
                {
                    DocumentModel.Application = dbDoc.Application;
                    DocumentModel.ReportingYear = dbDoc.ReportingYear;
                    DocumentModel.ReportingMonth = dbDoc.ReportingMonth;
                    await LoadDocumentDataInternalAsync();
                }
            }
            else if (!string.IsNullOrWhiteSpace(QueryApp))
            {
                DocumentModel.Application = QueryApp;
                if (QueryMonth.HasValue && QueryMonth.Value >= 1 && QueryMonth.Value <= 12)
                    DocumentModel.ReportingMonth = QueryMonth.Value;
                if (QueryYear.HasValue && QueryYear.Value >= 2000)
                    DocumentModel.ReportingYear = QueryYear.Value;
                await LoadDocumentDataInternalAsync();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(DocumentModel.Application))
                {
                    DocumentModel.Application = ActiveApplications.FirstOrDefault() ?? "POS";
                }
                await LoadDocumentDataInternalAsync();
            }
        }
        finally
        {
            _loadLock.Release();
        }
    }

    protected override Task OnInitializedAsync()
    {
        _dotNetRef = DotNetObjectReference.Create(this);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _dotNetRef?.Dispose();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && IsDocumentLoaded)
        {
            await InitializeSummaryClipboardAsync();
            foreach (var server in DocumentModel.ServerEvidences)
            {
                await InitializeClipboardAsync(server.Id);
            }
        }
        
        if (_pendingClipboardInit)
        {
            _pendingClipboardInit = false;
            await Task.Delay(100);
            await InitializeSummaryClipboardAsync();
            await InitializeExcelClipboardAsync();
            foreach (var server in DocumentModel.ServerEvidences)
                await InitializeClipboardAsync(server.Id);
            foreach (var cat in DocumentModel.ChangeCategories)
                foreach (var detail in cat.ChangeDetails)
                    await InitializeClipboardAsync(detail.Id);
        }
    }

    private async Task LoadDocumentDataAsync()
    {
        if (IsLoading) return;

        await _loadLock.WaitAsync();
        try
        {
            await LoadDocumentDataInternalAsync();
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task LoadDocumentDataInternalAsync()
    {
        if (IsLoading) return;

        if (string.IsNullOrWhiteSpace(DocumentModel.Application))
        {
            Snackbar.Add("Please select an application", Severity.Warning);
            return;
        }

        IsLoading = true;
        try
        {
            using var scope = ServiceScopeFactory.CreateScope();
            var docService = scope.ServiceProvider.GetRequiredService<ICMReviewDocumentService>();
            var woService = scope.ServiceProvider.GetRequiredService<ICMReviewWOReportService>();
            var mapService = scope.ServiceProvider.GetRequiredService<WO_CM_Review_Automation.Application.Mappings.Interfaces.IMappingService>();

            var dbDoc = await docService.GetDocumentByPeriodAsync(DocumentModel.Application, DocumentModel.ReportingYear, DocumentModel.ReportingMonth);
            
            if (dbDoc == null)
            {
                // Create new
                dbDoc = await docService.CreateDocumentAsync(DocumentModel.Application, DocumentModel.ReportingYear, DocumentModel.ReportingMonth);
            }

            // Map DB entity to ViewModel
            MapToViewModel(dbDoc);

            // Reload WO Report items based on shared filter
            var reportItems = await woService.GenerateWOReportItemsAsync(dbDoc.Id, DocumentModel.Application, DocumentModel.ReportingYear, DocumentModel.ReportingMonth);
            
            DocumentModel.WOReportItems = reportItems.Select(r => new WOReportItemViewModel
            {
                Id = r.Id,
                WoId = r.WoId,
                Status = r.Status,
                Assignee = r.Assignee,
                ExecutionStart = r.ExecutionStart,
                ExecutionEnd = r.ExecutionEnd,
                ServiceName = r.ServiceName,
                Subject = r.Subject,
                ChangeType = r.ChangeType
                // map other required fields...
            }).ToList();

            // Auto-detect and populate Changes from Work Orders
            if (DocumentModel.WOReportItems.Any())
            {
                bool isTechvan = DocumentModel.Application.Equals("Techvan", StringComparison.OrdinalIgnoreCase);
                bool isSalesForce = DocumentModel.Application.Equals("Sales Force App", StringComparison.OrdinalIgnoreCase);
                
                // === Auto-migrate: replace old generic categories with correct Sales Force App categories ===
                if (isSalesForce)
                {
                    var correctSfaCategories = new[]
                    {
                        "Re-factoring existing application for performance, scalability or other issues",
                        "Apply ad-hoc enhancement request in existing features",
                        "Implementation of Change Request- OneSalesApp",
                        "Fixation of bug on OneSalesApp",
                        "Modification of Application Existing Features- OneSalesApp",
                        "Deploy new application/module",
                        "Standard Change",
                        "Business Logic -OneSalesApp"
                    };
                    
                    // Remove any categories that are NOT in the correct SFA list (old generic ones)
                    var toRemove = DocumentModel.ChangeCategories
                        .Where(c => !correctSfaCategories.Any(name =>
                            name.Equals(c.CategoryName, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                    foreach (var cat in toRemove)
                        DocumentModel.ChangeCategories.Remove(cat);
                    
                    // Add any missing categories in the correct order
                    for (int i = 0; i < correctSfaCategories.Length; i++)
                    {
                        string catName = correctSfaCategories[i];
                        bool exists = DocumentModel.ChangeCategories.Any(c =>
                            c.CategoryName.Equals(catName, StringComparison.OrdinalIgnoreCase));
                        if (!exists)
                        {
                            DocumentModel.ChangeCategories.Add(new ChangeCategoryViewModel
                            {
                                Id = Guid.NewGuid(),
                                CategoryName = catName,
                                DisplayOrder = i,
                                HasChanges = false
                            });
                        }
                        else
                        {
                            // Correct the display order
                            var existing = DocumentModel.ChangeCategories.First(c =>
                                c.CategoryName.Equals(catName, StringComparison.OrdinalIgnoreCase));
                            existing.DisplayOrder = i;
                        }
                    }
                }

                var standardChangeCat = DocumentModel.ChangeCategories.FirstOrDefault(c => c.CategoryName.Equals("Standard Change", StringComparison.OrdinalIgnoreCase));
                
                // For Techvan: deployment category is "Apply ad-hoc enhancement request in existing features"
                // For all others: "Implementation of Change Request" (or variations)
                var implChangeCat = isTechvan
                    ? DocumentModel.ChangeCategories.FirstOrDefault(c => c.CategoryName.StartsWith("Apply ad-hoc enhancement request", StringComparison.OrdinalIgnoreCase))
                    : DocumentModel.ChangeCategories.FirstOrDefault(c => c.CategoryName.StartsWith("Implementation of Change Request", StringComparison.OrdinalIgnoreCase));
                
                // Clear any existing details in all categories
                foreach (var cat in DocumentModel.ChangeCategories)
                {
                    cat.ChangeDetails.Clear();
                    cat.HasChanges = false;
                }
                
                // Load dynamic routing rules
                var allMappings = await mapService.GetChangeCategoryMappingsAsync();
                var appMappings = allMappings
                    .Where(m => m.ApplicationName.Equals(DocumentModel.Application, StringComparison.OrdinalIgnoreCase) && m.IsActive)
                    .ToList();
                
                foreach (var wo in DocumentModel.WOReportItems)
                {
                    DateTimeOffset? parsedDate = null;
                    if (DateTime.TryParse(wo.ExecutionEnd, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dateResult))
                    {
                        parsedDate = new DateTimeOffset(dateResult);
                    }
                    
                    // Use Subject for Change Description
                    string description = wo.Subject;
                    
                    var detail = new ChangeDetailViewModel
                    {
                        ChangeOrder = $"WO {wo.WoId}",
                        Description = description,
                        ScheduleDate = parsedDate
                    };

                    var customMapping = appMappings.FirstOrDefault(m => wo.ChangeType.Contains(m.ChangeType, StringComparison.OrdinalIgnoreCase));

                    if (customMapping != null)
                    {
                        var targetCat = DocumentModel.ChangeCategories.FirstOrDefault(c => c.CategoryName.Equals(customMapping.CategoryName, StringComparison.OrdinalIgnoreCase));
                        if (targetCat != null)
                        {
                            targetCat.HasChanges = true;
                            targetCat.ChangeDetails.Add(detail);
                        }
                        else if (implChangeCat != null)
                        {
                            implChangeCat.HasChanges = true;
                            implChangeCat.ChangeDetails.Add(detail);
                        }
                    }
                    else if (wo.ChangeType.Contains("Standard", StringComparison.OrdinalIgnoreCase))
                    {
                        if (standardChangeCat != null)
                        {
                            standardChangeCat.HasChanges = true;
                            standardChangeCat.ChangeDetails.Add(detail);
                        }
                    }
                    else
                    {
                        if (implChangeCat != null)
                        {
                            implChangeCat.HasChanges = true;
                            implChangeCat.ChangeDetails.Add(detail);
                        }
                    }
                }
                
                // Ensure HasChanges flag is correct
                foreach (var cat in DocumentModel.ChangeCategories)
                {
                    if (!cat.ChangeDetails.Any())
                        cat.HasChanges = false;
                }
            }

            // Auto-generate Evidence Screenshots:
            // 1. "WOs for CM review" table (Figure 1) with current application highlighted yellow (for ALL applications)
            // 2. "Filtered Excel" table (Figure 2) with current application's WOs highlighted yellow (ONLY if application has changes)
            await AutoGenerateEvidenceScreenshotsAsync();

            IsDocumentLoaded = true;
            Snackbar.Add("Data loaded successfully", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading data: {ex.Message}", Severity.Error);
        }
        finally
        {
            IsLoading = false;
        }

        // Signal OnAfterRenderAsync to run clipboard initializations safely
        _pendingClipboardInit = true;
        StateHasChanged();
    }

    private Guid? _selectedServerId;

    private string GetMonthName(int month)
    {
        if (month < 1 || month > 12) return month.ToString();
        return System.Globalization.DateTimeFormatInfo.InvariantInfo.GetMonthName(month);
    }

    private void ToggleCategoryHasChanges(ChangeCategoryViewModel cat, bool val)
    {
        cat.HasChanges = val;
        if (val && !cat.ChangeDetails.Any())
        {
            cat.ChangeDetails.Add(new ChangeDetailViewModel());
        }
    }

    private static string FormatWoInput(string? rawOrder)
    {
        if (string.IsNullOrWhiteSpace(rawOrder)) return string.Empty;
        return System.Text.RegularExpressions.Regex.Replace(rawOrder, @"^WO[\s\-_]*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
    }

    private static void UpdateChangeOrder(ChangeDetailViewModel detail, string? val)
    {
        if (string.IsNullOrWhiteSpace(val))
        {
            detail.ChangeOrder = string.Empty;
            return;
        }
        var trimmed = val.Trim();
        detail.ChangeOrder = trimmed.StartsWith("WO ", StringComparison.OrdinalIgnoreCase) ? trimmed : $"WO {trimmed}";
    }

    private void SelectServer(Guid serverId)
    {
        _selectedServerId = _selectedServerId == serverId ? null : serverId;
    }

    private void AddServerAndSelect()
    {
        var newServer = new ServerEvidenceViewModel
        {
            DisplayOrder = DocumentModel.ServerEvidences.Count
        };
        DocumentModel.ServerEvidences.Add(newServer);
        _selectedServerId = newServer.Id;
        _pendingClipboardInit = true;
        StateHasChanged();
    }

    private void RemoveServerAndAdjustSelection(ServerEvidenceViewModel server)
    {
        RemoveServerEvidence(server);
        if (_selectedServerId == server.Id)
        {
            _selectedServerId = DocumentModel.ServerEvidences.FirstOrDefault()?.Id;
        }
        StateHasChanged();
    }

    private void MoveServer(ServerEvidenceViewModel server, int direction)
    {
        var currentIndex = DocumentModel.ServerEvidences.IndexOf(server);
        if (currentIndex < 0) return;

        var newIndex = currentIndex + direction;
        if (newIndex < 0 || newIndex >= DocumentModel.ServerEvidences.Count) return;

        DocumentModel.ServerEvidences.RemoveAt(currentIndex);
        DocumentModel.ServerEvidences.Insert(newIndex, server);

        for (int i = 0; i < DocumentModel.ServerEvidences.Count; i++)
        {
            DocumentModel.ServerEvidences[i].DisplayOrder = i;
        }

        _selectedServerId = server.Id;
        StateHasChanged();
    }

    private void AddServerEvidence()
    {
        AddServerAndSelect();
    }

    private void RemoveServerEvidence(ServerEvidenceViewModel server)
    {
        DocumentModel.ServerEvidences.Remove(server);
    }

    private void AddChangeCategory()
    {
        DocumentModel.ChangeCategories.Add(new ChangeCategoryViewModel());
    }

    private void RemoveChangeCategory(ChangeCategoryViewModel category)
    {
        DocumentModel.ChangeCategories.Remove(category);
    }

    private void AddChangeDetail(ChangeCategoryViewModel category)
    {
        category.ChangeDetails.Add(new ChangeDetailViewModel());
    }

    private void RemoveChangeDetail(ChangeCategoryViewModel category, ChangeDetailViewModel detail)
    {
        category.ChangeDetails.Remove(detail);
    }

    private async Task InitializeClipboardAsync(Guid targetId)
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("cmClipboard.initialize", _dotNetRef, $"paste-area-{targetId}", targetId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing clipboard: {ex.Message}");
        }
    }

    private async Task InitializeSummaryClipboardAsync()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("cmClipboard.initialize", _dotNetRef, "paste-area-summary", Guid.Empty);
        }
        catch (Exception)
        {
            // Ignore if JS not fully ready yet
        }
    }



        private async Task UploadSiemScreenshotAsync(InputFileChangeEventArgs e)
        {
            var file = e.File;
            if (file != null)
            {
                using var stream = file.OpenReadStream(maxAllowedSize: 100 * 1024 * 1024);
                using var memoryStream = new System.IO.MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();
                var base64 = Convert.ToBase64String(imageBytes);
                var dataUrl = $"data:{file.ContentType};base64,{base64}";
                
                DocumentModel.SiemScreenshot = new ScreenshotViewModel
                {
                    Id = Guid.NewGuid(),
                    Base64Data = dataUrl,
                    ContentType = file.ContentType,
                    FileName = file.Name
                };
                StateHasChanged();
            }
        }

        private void RemoveSiemScreenshot()
        {
            DocumentModel.SiemScreenshot = null;
            StateHasChanged();
        }

        private async Task InitializeExcelClipboardAsync()
        {
            if (_dotNetRef != null)
            {
                // We use a specific Guid for Excel clipboard to differentiate from servers
                await JSRuntime.InvokeVoidAsync("cmClipboard.initialize", _dotNetRef, "paste-area-excel", Guid.Parse("11111111-1111-1111-1111-111111111111"));
            }
        }

        private async Task InitializeSiemClipboardAsync()
        {
            if (_dotNetRef != null)
            {
                // We use a specific Guid for SIEM clipboard
                await JSRuntime.InvokeVoidAsync("cmClipboard.initialize", _dotNetRef, "paste-area-siem", Guid.Parse("22222222-2222-2222-2222-222222222222"));
            }
        }

    [JSInvokable]
    public async Task HandleImagePaste(string base64Data, string contentType, Guid targetId)
    {
        // Find the target server evidence
        var server = DocumentModel.ServerEvidences.FirstOrDefault(s => s.Id == targetId);
        if (server != null)
        {
            // We use JS interop string and just store it in view model for UI.
            // When saving, we use IImageStorageService to write it.
            server.Screenshots.Add(new ScreenshotViewModel
            {
                Base64Data = base64Data,
                ContentType = contentType,
                FileName = $"screenshot_{DateTime.Now:yyyyMMddHHmmss}.png",
                DisplayOrder = server.Screenshots.Count
            });
            StateHasChanged();
        }
        else if (targetId == Guid.Empty)
        {
            // Convert base64 data back to bytes for processing
            var commaIndex = base64Data.IndexOf(',');
            var base64Part = commaIndex >= 0 ? base64Data.Substring(commaIndex + 1) : base64Data;
            var imageBytes = Convert.FromBase64String(base64Part);
            
            var processedDataUrl = await ProcessSummaryImageAsync(imageBytes, contentType);
            
            // It's the summary table screenshot
            DocumentModel.SummaryTableScreenshot = new ScreenshotViewModel
            {
                Base64Data = processedDataUrl,
                ContentType = contentType,
                FileName = $"summary_{DateTime.Now:yyyyMMddHHmmss}.png"
            };
            StateHasChanged();
        }
        else if (targetId == Guid.Parse("11111111-1111-1111-1111-111111111111"))
        {
            // Excel Screenshot
            DocumentModel.ExcelScreenshot = new ScreenshotViewModel
            {
                Base64Data = base64Data,
                ContentType = contentType,
                FileName = $"excel_{DateTime.Now:yyyyMMddHHmmss}.png"
            };
            StateHasChanged();
        }
        else if (targetId == Guid.Parse("22222222-2222-2222-2222-222222222222"))
        {
            // SIEM Screenshot
            DocumentModel.SiemScreenshot = new ScreenshotViewModel
            {
                Base64Data = base64Data,
                ContentType = contentType,
                FileName = $"siem_{DateTime.Now:yyyyMMddHHmmss}.png"
            };
            StateHasChanged();
        }
        else
        {
            // Try to find if it's a change detail target
            var changeDetail = DocumentModel.ChangeCategories
                .SelectMany(c => c.ChangeDetails)
                .FirstOrDefault(d => d.Id == targetId);

            if (changeDetail != null)
            {
                changeDetail.DeploymentScreenshots.Add(new ScreenshotViewModel
                {
                    Base64Data = base64Data,
                    ContentType = contentType,
                    FileName = $"deploy_{DateTime.Now:yyyyMMddHHmmss}.png",
                    DisplayOrder = changeDetail.DeploymentScreenshots.Count
                });
                StateHasChanged();
            }
        }
    }

    private async Task UploadScreenshotsAsync(InputFileChangeEventArgs e, Guid targetId)
    {
        var server = DocumentModel.ServerEvidences.FirstOrDefault(s => s.Id == targetId);
        if (server == null) return;

        foreach (var file in e.GetMultipleFiles(maximumFileCount: 100))
        {
            if (file.Size > 5 * 1024 * 1024)
            {
                Snackbar.Add($"File {file.Name} is too large (max 5MB)", Severity.Error);
                continue;
            }

            using var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
            using var memoryStream = new System.IO.MemoryStream();
            await stream.CopyToAsync(memoryStream);
            
            var imageBytes = memoryStream.ToArray();
            var base64 = Convert.ToBase64String(imageBytes);
            var dataUrl = $"data:{file.ContentType};base64,{base64}";
            
            // Add image to UI immediately - no waiting for OCR
            var newShot = new ScreenshotViewModel
            {
                Base64Data = dataUrl,
                ContentType = file.ContentType,
                FileName = file.Name,
                DisplayOrder = server.Screenshots.Count
            };
            server.Screenshots.Add(newShot);
            StateHasChanged();
            
            // Run OCR in background to auto-fill the server IP
            var capturedFile = file;
            var capturedBytes = imageBytes;
            var capturedServer = server;
            _ = Task.Run(async () =>
            {
                string extractedIp = null;
                try
                {
                    var match = Regex.Match(capturedFile.Name, @"_(\d+)\.(png|jpg|jpeg)$", RegexOptions.IgnoreCase);
                    string targetEnding = match.Success ? match.Groups[1].Value : null;
                    // Fallback: also try prefix pattern (e.g. "79_1-sep-26.png" → "79")
                    if (string.IsNullOrEmpty(targetEnding))
                    {
                        var prefixMatch = Regex.Match(capturedFile.Name, @"^(\d{2,3})[_\.]", RegexOptions.IgnoreCase);
                        if (prefixMatch.Success) targetEnding = prefixMatch.Groups[1].Value;
                    }
                    
                    var tessDataPath = System.IO.Path.Combine(WebHostEnvironment.ContentRootPath, "tessdata");
                    if (System.IO.Directory.Exists(tessDataPath))
                    {
                        var leftCropBytes = CropHalf(capturedBytes, true);
                        var rightCropBytes = CropHalf(capturedBytes, false);
                        var leftInverted = PreprocessImageForOcr(leftCropBytes, true);
                        var rightInverted = PreprocessImageForOcr(rightCropBytes, true);
                        
                        string text = "";
                        using var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
                        
                        // === STEP 1: Extract IP from Top Crop (title bar) FIRST - most reliable ===
                        string extractedServerName = null;
                        string topText = "";
                        try
                        {
                            var topCropBytes = CropTopBar(capturedBytes);
                            var topCropInvertedBytes = PreprocessImageForOcr(topCropBytes, true);
                            using (var imgTop = Pix.LoadFromMemory(topCropBytes))
                            using (var pageTop = engine.Process(imgTop, PageSegMode.SparseText))
                            {
                                topText = pageTop.GetText();
                            }
                            using (var imgTopInv = Pix.LoadFromMemory(topCropInvertedBytes))
                            using (var pageTopInv = engine.Process(imgTopInv, PageSegMode.SparseText))
                            {
                                topText += " " + pageTopInv.GetText();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Top Crop OCR failed: {ex.Message}");
                        }

                        Console.WriteLine($"----- OCR OUTPUT FOR {capturedFile.Name} (TOP CROP) -----");
                        Console.WriteLine(topText);
                        Console.WriteLine("---------------------------------------------");

                        bool found = false;
                        var gatewayOctets = new HashSet<string> { "1", "254", "253", "255", "0" };
                        string rdpDiscardedSubnet = null; // saved when Priority 1 is thrown out due to filename conflict

                        // Priority 1: RDP/SSH title bar pattern: @IP@MDMUSER or IP@user (most authoritative)
                        var rdpTitleMatch = Regex.Match(topText,
                            @"(?:^|\s|@)(\d{1,3}[\.\s]\d{1,3}[\.\s]\d{1,3}[\.\s]\d{1,3})\s*@",
                            RegexOptions.IgnoreCase);
                        if (rdpTitleMatch.Success)
                        {
                            var rdpIp = Regex.Replace(rdpTitleMatch.Groups[1].Value, @"\s+", ".");
                            // Cross-check: if filename encodes the last octet (e.g. _162.png),
                            // and OCR gave a different last octet, OCR likely mis-read it — skip
                            bool rdpIpConflictsWithFilename = false;
                            if (!string.IsNullOrEmpty(targetEnding))
                            {
                                var rdpLastOctet = rdpIp.Split('.').LastOrDefault() ?? "";
                                rdpIpConflictsWithFilename = !rdpLastOctet.Equals(targetEnding, StringComparison.OrdinalIgnoreCase);
                            }
                            if (!rdpIpConflictsWithFilename)
                            {
                                Console.WriteLine($"RDP Title bar IP: {rdpIp}");
                                extractedIp = rdpIp;
                                found = true;
                                
                                var rdpOctets = rdpIp.Split('.');
                                if (rdpOctets.Length == 4)
                                    _lastSeenSubnet = string.Join(".", rdpOctets[0], rdpOctets[1], rdpOctets[2]);
                            }
                            else
                            {
                                // Save the subnet from the discarded RDP IP so we can reconstruct later
                                var rdpOctets = rdpIp.Split('.');
                                if (rdpOctets.Length == 4)
                                {
                                    rdpDiscardedSubnet = string.Join(".", rdpOctets[0], rdpOctets[1], rdpOctets[2]);
                                    _lastSeenSubnet = rdpDiscardedSubnet;
                                }
                                Console.WriteLine($"RDP Title bar IP {rdpIp} conflicts with filename suffix _{targetEnding} — discarding OCR result.");
                            }
                        }

                        // Priority 2: sftp://user@IP - FileZilla title bar
                        if (!found)
                        {
                            var titleIpMatch = Regex.Match(topText,
                                @"sftp://[^\s@]+@(\d{1,3}[\.\s]\d{1,3}[\.\s]\d{1,3}[\.\s]\d{1,3})",
                                RegexOptions.IgnoreCase);
                            if (titleIpMatch.Success)
                            {
                                var titleIp = Regex.Replace(titleIpMatch.Groups[1].Value, @"\s+", ".");
                                Console.WriteLine($"FileZilla Title bar IP: {titleIp}");
                                extractedIp = titleIp;
                                found = true;
                            }
                        }

                        // Priority 3: Any IP in the top crop bar text
                        if (!found)
                        {
                            var topIpMatches = Regex.Matches(topText, @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b");
                            foreach (Match m in topIpMatches)
                            {
                                var last = m.Groups[1].Value.Split('.').LastOrDefault();
                                if (last != null && !gatewayOctets.Contains(last))
                                {
                                    extractedIp = m.Groups[1].Value;
                                    found = true;
                                    Console.WriteLine($"Top crop IP: {extractedIp}");
                                    break;
                                }
                            }
                        }

                        // === STEP 2: Full body OCR — always run, to pick up ipconfig IPv4 and other body IPs ===
                        {
                            // Also scan full image (for CMD window ipconfig output)
                            using (var imgFull = Pix.LoadFromMemory(capturedBytes))
                            using (var pageFull = engine.Process(imgFull, PageSegMode.SparseText)) { text += " " + pageFull.GetText(); }
                            
                            var fullInvertedBytes = PreprocessImageForOcr(capturedBytes, true);
                            using (var imgFullInv = Pix.LoadFromMemory(fullInvertedBytes))
                            using (var pageFullInv = engine.Process(imgFullInv, PageSegMode.SparseText)) { text += " " + pageFullInv.GetText(); }
                            
                            using (var img = Pix.LoadFromMemory(leftCropBytes))
                            using (var page = engine.Process(img, PageSegMode.SparseText)) { text += " " + page.GetText(); }
                            using (var img = Pix.LoadFromMemory(leftInverted))
                            using (var page = engine.Process(img, PageSegMode.SparseText)) { text += " " + page.GetText(); }
                            using (var img = Pix.LoadFromMemory(rightCropBytes))
                            using (var page = engine.Process(img, PageSegMode.SparseText)) { text += " " + page.GetText(); }
                            using (var img = Pix.LoadFromMemory(rightInverted))
                            using (var page = engine.Process(img, PageSegMode.SparseText)) { text += " " + page.GetText(); }

                            Console.WriteLine($"----- OCR OUTPUT FOR {capturedFile.Name} (BODY) -----");
                            Console.WriteLine(text);
                            Console.WriteLine("---------------------------------------------");

                            var compactText = Regex.Replace(text, @"\s+", " ");
                            var noSpaceText = Regex.Replace(text, @"\s", "");
                            var combinedText = compactText + " " + noSpaceText;

                            // Priority A: ipconfig IPv4 Address line (most authoritative for CMD window images)
                            // Matches: "IPv4 Address. . . . : 172.16.8.79"
                            if (!found)
                            {
                                var ipv4LineMatch = Regex.Match(compactText,
                                    @"IPv4\s+Address[\s\.]{1,60}:?\s*(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})",
                                    RegexOptions.IgnoreCase);
                                if (ipv4LineMatch.Success)
                                {
                                    extractedIp = ipv4LineMatch.Groups[1].Value;
                                    found = true;
                                    Console.WriteLine($"IPv4 Address (ipconfig) IP: {extractedIp}");
                                }
                            }

                            // Priority B: IP directly after "IPv4" keyword (handles OCR noise in dots)
                            if (!found)
                            {
                                var ipv4NoisyMatch = Regex.Match(compactText,
                                    @"IPv4[^\d]{0,60}?(?:[:\.]|\s)\s*(\d{1,3}[\.\s]\d{1,3}[\.\s]\d{1,3}[\.\s]\d{1,3})",
                                    RegexOptions.IgnoreCase);
                                if (ipv4NoisyMatch.Success)
                                {
                                    extractedIp = Regex.Replace(ipv4NoisyMatch.Groups[1].Value, @"\s+", ".");
                                    found = true;
                                    Console.WriteLine($"IPv4 noisy match IP: {extractedIp}");
                                }
                            }

                            // Priority B2: OCR-garbled IPv4 variants (IPva, 1Pv4, IPV, etc.)
                            // Tesseract often misreads small CMD text — catch any IP after 3-5 char word resembling IPv4
                            if (!found)
                            {
                                // Look for pattern: word-like-IPv4 followed shortly by an IP number
                                var garbledIpv4Match = Regex.Match(compactText,
                                    @"\b[I1l][Pp][Vv][a4A]\b[^\d]{0,60}?(?:[:\.]|\s)\s*(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})",
                                    RegexOptions.IgnoreCase);
                                if (garbledIpv4Match.Success)
                                {
                                    extractedIp = garbledIpv4Match.Groups[1].Value;
                                    found = true;
                                    Console.WriteLine($"Garbled IPv4 match IP: {extractedIp}");
                                }
                            }

                            // Priority B3: "AQQ" or "AQd" is OCR noise for the IP address itself — look for IP after Ethernet context
                            if (!found)
                            {
                                // Pattern: "Ethernet" context then IP on same/next line area
                                var ethernetCtx = Regex.Match(compactText,
                                    @"Ethernet[^\.]{0,80}(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})",
                                    RegexOptions.IgnoreCase);
                                if (ethernetCtx.Success)
                                {
                                    var last = ethernetCtx.Groups[1].Value.Split('.').LastOrDefault();
                                    if (last != null && !gatewayOctets.Contains(last))
                                    {
                                        extractedIp = ethernetCtx.Groups[1].Value;
                                        found = true;
                                        Console.WriteLine($"Ethernet context IP: {extractedIp}");
                                    }
                                }
                            }

                            // Priority C: Filename-based target ending match
                            var candidateIps = new List<string>();
                            var robustMatches = Regex.Matches(compactText, @"\b(?:10|172|192)[\.\s]+\d{1,3}[\.\s]+\d{1,3}[\.\s]+\d{1,3}\b");
                            foreach (Match rm in robustMatches)
                                candidateIps.Add(Regex.Replace(rm.Value, @"\s+", "."));

                            var ipMatches = Regex.Matches(combinedText, @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b");
                            foreach (Match im in ipMatches)
                                candidateIps.Add(im.Value);

                            if (!found && !string.IsNullOrEmpty(targetEnding))
                            {
                                var matchedTarget = candidateIps.FirstOrDefault(ip => ip.EndsWith("." + targetEnding));
                                if (matchedTarget == null)
                                {
                                    // Fallback: Check if the IP is already in ServerEvidences
                                    matchedTarget = DocumentModel.ServerEvidences
                                        .Select(s => s.ServerIp)
                                        .FirstOrDefault(ip => !string.IsNullOrWhiteSpace(ip) && ip.EndsWith("." + targetEnding));
                                }
                                // Last resort: reconstruct IP from discarded RDP subnet, last seen, or App default + filename octet
                                // e.g. discarded '172.16.11.17' subnet='172.16.11' + '162' → '172.16.11.162'
                                if (matchedTarget == null)
                                {
                                    string guessedSubnet = rdpDiscardedSubnet ?? _lastSeenSubnet;
                                    if (string.IsNullOrEmpty(guessedSubnet))
                                    {
                                        guessedSubnet = DocumentModel.Application.Equals("LEA", StringComparison.OrdinalIgnoreCase) ? "172.16.11" : "172.16.8";
                                    }
                                    matchedTarget = guessedSubnet + "." + targetEnding;
                                    Console.WriteLine($"Reconstructed IP from guessed subnet ({guessedSubnet}): {matchedTarget}");
                                }

                                if (matchedTarget != null) { extractedIp = matchedTarget; found = true; }
                            }

                            // Priority D: Non-gateway IP fallback
                            if (!found)
                            {
                                var matchedNonGateway = candidateIps.FirstOrDefault(ip => {
                                    var last = ip.Split('.').LastOrDefault();
                                    return last != null && !gatewayOctets.Contains(last);
                                });
                                if (matchedNonGateway != null) { extractedIp = matchedNonGateway; found = true; }
                            }
                        }

                        // === Extract Server Name from top crop text ===
                        var searchMatch = Regex.Match(topText, @"Search\s+([a-zA-Z0-9_\-]+)", RegexOptions.IgnoreCase);
                        if (searchMatch.Success && searchMatch.Groups[1].Value.Trim().Length > 2)
                        {
                            extractedServerName = searchMatch.Groups[1].Value.Trim();
                        }
                        else
                        {
                            var pathMatches = Regex.Matches(topText, @"[>\\/]\s*([a-zA-Z0-9_\-\s]+)(?=\s*[>\\/]|\s*$|Search|Type here)", RegexOptions.IgnoreCase);
                            if (pathMatches.Count > 0)
                            {
                                var lastMatch = pathMatches.Cast<Match>().LastOrDefault(m => m.Groups[1].Value.Trim().Length > 2);
                                if (lastMatch != null)
                                    extractedServerName = Regex.Replace(lastMatch.Groups[1].Value, @"[^a-zA-Z0-9_\-]", "").Trim();
                            }
                        }

                        if (!string.IsNullOrEmpty(extractedServerName))
                        {
                            newShot.ServerName = extractedServerName;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Background OCR failed: {ex.Message}");
                }
                
                // Update UI on the main thread with OCR result
                await InvokeAsync(() =>
                {
                    if (!string.IsNullOrEmpty(extractedIp))
                    {
                        if (string.IsNullOrWhiteSpace(capturedServer.ServerIp))
                        {
                            capturedServer.ServerIp = extractedIp;
                        }
                        else if (capturedServer.ServerIp != extractedIp)
                        {
                            var existingServer = DocumentModel.ServerEvidences.FirstOrDefault(s => s.ServerIp == extractedIp);
                            if (existingServer != null)
                            {
                                // Move the screenshot to the correct server block
                                capturedServer.Screenshots.Remove(newShot);
                                newShot.DisplayOrder = existingServer.Screenshots.Count;
                                existingServer.Screenshots.Add(newShot);
                            }
                            else
                            {
                                var newServer = new ServerEvidenceViewModel
                                {
                                    Id = Guid.NewGuid(),
                                    ServerIp = extractedIp,
                                    DisplayOrder = DocumentModel.ServerEvidences.Count
                                };
                                capturedServer.Screenshots.Remove(newShot);
                                newShot.DisplayOrder = newServer.Screenshots.Count;
                                newServer.Screenshots.Add(newShot);
                                DocumentModel.ServerEvidences.Add(newServer);
                                _ = InvokeAsync(async () => {
                                    await Task.Delay(100);
                                    await InitializeClipboardAsync(newServer.Id);
                                });
                            }
                        }
                    }
                    else if (string.IsNullOrWhiteSpace(capturedServer.ServerIp))
                    {
                        Snackbar.Add($"Could not auto-detect IP from '{capturedFile.Name}'. Please type the Server IP in the box above.", Severity.Info);
                    }
                    StateHasChanged();
                });
            });
        }
    }

    private void RemoveScreenshot(ServerEvidenceViewModel server, ScreenshotViewModel shot)
    {
        server.Screenshots.Remove(shot);
    }

    private async Task UploadDeploymentScreenshotsAsync(InputFileChangeEventArgs e, Guid targetDetailId)
    {
        var changeDetail = DocumentModel.ChangeCategories
            .SelectMany(c => c.ChangeDetails)
            .FirstOrDefault(d => d.Id == targetDetailId);
            
        if (changeDetail == null) return;

        foreach (var file in e.GetMultipleFiles(maximumFileCount: 100))
        {
            if (file.Size > 100 * 1024 * 1024)
            {
                Snackbar.Add($"File {file.Name} is too large (max 100MB)", Severity.Error);
                continue;
            }

            using var stream = file.OpenReadStream(maxAllowedSize: 100 * 1024 * 1024);
            using var memoryStream = new System.IO.MemoryStream();
            await stream.CopyToAsync(memoryStream);
            
            if (file.Name.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    memoryStream.Position = 0;
                    using var wordDoc = WordprocessingDocument.Open(memoryStream, false);
                    var body = wordDoc.MainDocumentPart?.Document?.Body;
                    if (body != null)
                    {
                        string currentHeading = "Screenshot";
                        string currentIp = "";
                        
                        foreach (var p in body.Descendants<Paragraph>())
                        {
                            if (true) // Keep block to preserve brace matching
                            {
                                string text = p.InnerText.Trim();
                                
                                // Check for Heading updates
                                if (text.Contains("Before", StringComparison.OrdinalIgnoreCase)) currentHeading = "Before Deployment Screenshot";
                                if (text.Contains("After", StringComparison.OrdinalIgnoreCase)) currentHeading = "After Deployment Screenshot";
                                
                                // Check for IP
                                var ipMatch = Regex.Match(text, @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b");
                                if (ipMatch.Success)
                                {
                                    currentIp = ipMatch.Value;
                                }
                                
                                // Look for Drawings (Images) in this paragraph
                                var drawings = p.Descendants<DocumentFormat.OpenXml.Wordprocessing.Drawing>().ToList();
                                foreach (var drawing in drawings)
                                {
                                    var blip = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().FirstOrDefault();
                                    if (blip != null && !string.IsNullOrEmpty(blip.Embed?.Value))
                                    {
                                        var imagePartId = blip.Embed.Value;
                                        var imagePart = wordDoc.MainDocumentPart.GetPartById(imagePartId) as ImagePart;
                                        if (imagePart != null)
                                        {
                                            using var imageStream = imagePart.GetStream();
                                            using var msImg = new System.IO.MemoryStream();
                                            await imageStream.CopyToAsync(msImg);
                                            var imgBytes = msImg.ToArray();
                                            
                                            string ocrIp = null;
                                            string ocrServerName = null;
                                            var tessDataPath = System.IO.Path.Combine(WebHostEnvironment.ContentRootPath, "tessdata");
                                            if (System.IO.Directory.Exists(tessDataPath))
                                            {
                                                try
                                                {
                                                    var processedBytes = PreprocessImageForOcr(imgBytes, false);
                                                    var processedBytesInverted = PreprocessImageForOcr(imgBytes, true);
                                                    
                                                    string imgText = "";
                                                    using var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
                                                    
                                                    using (var pixImg = Pix.LoadFromMemory(processedBytes))
                                                    using (var page = engine.Process(pixImg, PageSegMode.SparseText))
                                                    {
                                                        imgText = page.GetText();
                                                    }
                                                    
                                                    using (var pixImgInverted = Pix.LoadFromMemory(processedBytesInverted))
                                                    using (var pageInverted = engine.Process(pixImgInverted, PageSegMode.SparseText))
                                                    {
                                                        imgText += " " + pageInverted.GetText();
                                                    }
                                                    
                                                    var compactText = Regex.Replace(imgText, @"\s+", " ");
                                                    var noSpaceText = Regex.Replace(imgText, @"\s", "");
                                                    var combinedText = compactText + " " + noSpaceText;
                                                    
                                                    Console.WriteLine($"----- DOCX OCR OUTPUT -----");
                                                    Console.WriteLine(imgText);
                                                    Console.WriteLine($"---------------------------");
                                                    
                                                    var gatewayOctets = new HashSet<string> { "1", "254", "253", "255", "0" };
                                                    
                                                    // 1. Try to find the Host field (active server in FileZilla)
                                                    var hostMatch = Regex.Match(compactText, @"Host\s*[:\|\]]*\s*(172[\.\s,]+(?:19|16)[\.\s,]+\d{1,3}[\.\s,]+\d{1,3})", RegexOptions.IgnoreCase);
                                                    if (hostMatch.Success)
                                                    {
                                                        ocrIp = Regex.Replace(hostMatch.Groups[1].Value, @"[\s,]+", ".");
                                                    }
                                                    
                                                    // 2. Fallback to candidate list
                                                    if (string.IsNullOrEmpty(ocrIp))
                                                    {
                                                        var candidateIps = new List<string>();
                                                        
                                                        var robustMatches = Regex.Matches(compactText, @"\b(?:10|172|192)[\.\s,]+\d{1,3}[\.\s,]+\d{1,3}[\.\s,]+\d{1,3}\b");
                                                        foreach (Match rm in robustMatches) candidateIps.Add(Regex.Replace(rm.Value, @"[\s,]+", "."));
                                                        
                                                        var ipMatches = Regex.Matches(combinedText, @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b");
                                                        foreach (Match im in ipMatches) candidateIps.Add(im.Value);
                                                        
                                                        ocrIp = candidateIps.FirstOrDefault(ip => {
                                                            var last = ip.Split('.').LastOrDefault();
                                                            return last != null && !gatewayOctets.Contains(last);
                                                        });
                                                        
                                                        if (string.IsNullOrEmpty(ocrIp)) {
                                                            ocrIp = candidateIps.FirstOrDefault();
                                                        }
                                                    }
                                                    
                                                    Console.WriteLine($"DOCX OCR Extracted IP: {ocrIp}");
                                                }
                                                catch (Exception ex)
                                                {
                                                    Console.WriteLine($"OCR failed on docx image: {ex.Message}");
                                                }
                                                
                                                try
                                                {
                                                    var topCropBytes = CropTopBar(imgBytes);
                                                    var topCropInvertedBytes = PreprocessImageForOcr(topCropBytes, true);
                                                    string topText = "";
                                                    using var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
                                                    using (var imgTop = Pix.LoadFromMemory(topCropBytes))
                                                    using (var pageTop = engine.Process(imgTop, PageSegMode.SparseText))
                                                    {
                                                        topText = pageTop.GetText();
                                                    }
                                                    using (var imgTopInv = Pix.LoadFromMemory(topCropInvertedBytes))
                                                    using (var pageTopInv = engine.Process(imgTopInv, PageSegMode.SparseText))
                                                    {
                                                        topText += " " + pageTopInv.GetText();
                                                    }
                                                    
                                                    Console.WriteLine($"TOP CROP TEXT: {topText}");
                                                    
                                                    // === PRIORITY: Extract IP from FileZilla title bar ===
                                                    // Title bar shows active tab: "sftp://user@10.4.40.142 - FileZilla"
                                                    var titleIpMatch = Regex.Match(topText, 
                                                        @"sftp://[^\s@]+@((?:10|172|192)[\.\s]*\d{1,3}[\.\s]*\d{1,3}[\.\s]*\d{1,3})\s*-\s*FileZilla",
                                                        RegexOptions.IgnoreCase);
                                                    if (titleIpMatch.Success)
                                                    {
                                                        var titleIp = Regex.Replace(titleIpMatch.Groups[1].Value, @"\s+", ".");
                                                        Console.WriteLine($"Title bar IP: {titleIp}");
                                                        ocrIp = titleIp; // Title bar is most authoritative
                                                    }
                                                    else
                                                    {
                                                        // Also try without FileZilla suffix (some crops cut it off)
                                                        var titleIpMatch2 = Regex.Match(topText,
                                                            @"sftp://[^\s@]+@(172[\.\s]*(?:19|16)[\.\s]*\d{1,3}[\.\s]*\d{1,3})",
                                                            RegexOptions.IgnoreCase);
                                                        if (titleIpMatch2.Success)
                                                        {
                                                            var titleIp2 = Regex.Replace(titleIpMatch2.Groups[1].Value, @"\s+", ".");
                                                            Console.WriteLine($"Title bar IP (no suffix): {titleIp2}");
                                                            ocrIp = titleIp2;
                                                        }
                                                    }
                                                    
                                                    // === Extract server folder name from path ===
                                                    var pathMatches = Regex.Matches(topText, @"[>\\/]\s*([a-zA-Z0-9_\-\s]+)(?=\s*[>\\/]|\s*$|Search|Type here)", RegexOptions.IgnoreCase);
                                                    if (pathMatches.Count > 0)
                                                    {
                                                        var lastMatch = pathMatches.Cast<Match>().LastOrDefault(m => m.Groups[1].Value.Trim().Length > 2);
                                                        if (lastMatch != null)
                                                        {
                                                            string extractedName = Regex.Replace(lastMatch.Groups[1].Value, @"[^a-zA-Z0-9_\-]", "").Trim();
                                                            if (!string.IsNullOrEmpty(extractedName))
                                                            {
                                                                ocrServerName = extractedName;
                                                            }
                                                        }
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    Console.WriteLine($"Top crop OCR failed: {ex.Message}");
                                                }
                                            }
                                            
                                            // For the final IP: prefer OCR (most accurate), fallback to document text IP
                                            string finalIp = !string.IsNullOrEmpty(ocrIp) ? ocrIp : currentIp;
                                            
                                            var base64 = Convert.ToBase64String(imgBytes);
                                            var contentType = imagePart.ContentType;
                                            var dataUrl = $"data:{contentType};base64,{base64}";
                                            
                                            string caption = currentHeading;
                                            changeDetail.DeploymentScreenshots.Add(new ScreenshotViewModel
                                            {
                                                Id = Guid.NewGuid(),
                                                Base64Data = dataUrl,
                                                ContentType = contentType,
                                                FileName = $"Extracted_{imagePartId}.png",
                                                DisplayOrder = changeDetail.DeploymentScreenshots.Count,
                                                Heading = currentHeading,
                                                ServerIp = finalIp,
                                                ServerName = ocrServerName,
                                                Caption = null // caption not needed - heading and IP shown as group header
                                            });
                                        }
                                    }
                                }
                            }
                        }
                        Snackbar.Add($"Successfully extracted images from {file.Name}", Severity.Success);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing docx: {ex.Message}");
                    Snackbar.Add($"Error reading {file.Name}: {ex.Message}", Severity.Error);
                }
            }
            else
            {
                var imageBytes = memoryStream.ToArray();
                var base64 = Convert.ToBase64String(imageBytes);
                var dataUrl = $"data:{file.ContentType};base64,{base64}";
                
                var newShot = new ScreenshotViewModel
                {
                    Id = Guid.NewGuid(),
                    Base64Data = dataUrl,
                    ContentType = file.ContentType,
                    FileName = file.Name,
                    DisplayOrder = changeDetail.DeploymentScreenshots.Count,
                    Heading = "Screenshot" // Default heading
                };
                
                changeDetail.DeploymentScreenshots.Add(newShot);
                
                var capturedFile = file;
                var capturedBytes = imageBytes;
                _ = Task.Run(async () => 
                {
                    string extractedIp = null;
                    string extractedServerName = null;
                    try
                    {
                        var fileMatch = Regex.Match(capturedFile.Name, @"_(\d+)\.(png|jpg|jpeg)$", RegexOptions.IgnoreCase);
                        string targetEndingPaste = fileMatch.Success ? fileMatch.Groups[1].Value : null;
                        if (string.IsNullOrEmpty(targetEndingPaste))
                        {
                            var prefixMatchP = Regex.Match(capturedFile.Name, @"^(\d{2,3})[_\.]", RegexOptions.IgnoreCase);
                            if (prefixMatchP.Success) targetEndingPaste = prefixMatchP.Groups[1].Value;
                        }
                        var tessDataPath = System.IO.Path.Combine(WebHostEnvironment.ContentRootPath, "tessdata");
                        if (System.IO.Directory.Exists(tessDataPath))
                        {
                            using var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
                            
                            var leftCropBytes = CropHalf(capturedBytes, true);
                            var rightCropBytes = CropHalf(capturedBytes, false);
                            var leftInverted = PreprocessImageForOcr(leftCropBytes, true);
                            var rightInverted = PreprocessImageForOcr(rightCropBytes, true);
                            
                            // === STEP 1: Extract IP from Top Crop (title bar) FIRST - most reliable ===
                            string topText = "";
                            try
                            {
                                var topCropBytes = CropTopBar(capturedBytes);
                                var topCropInvertedBytes = PreprocessImageForOcr(topCropBytes, true);
                                using (var imgTop = Pix.LoadFromMemory(topCropBytes))
                                using (var pageTop = engine.Process(imgTop, PageSegMode.SparseText))
                                {
                                    topText = pageTop.GetText();
                                }
                                using (var imgTopInv = Pix.LoadFromMemory(topCropInvertedBytes))
                                using (var pageTopInv = engine.Process(imgTopInv, PageSegMode.SparseText))
                                {
                                    topText += " " + pageTopInv.GetText();
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Top Crop OCR failed: {ex.Message}");
                            }

                            bool found = false;
                            var gatewayOctets = new HashSet<string> { "1", "254", "253", "255", "0" };
                            string rdpDiscardedSubnetP = null;

                            // Priority 1: RDP/SSH title bar pattern: @IP@MDMUSER or IP@user
                            var rdpTitleMatch = Regex.Match(topText,
                                @"(?:^|\s|@)(\d{1,3}[\.\s]\d{1,3}[\.\s]\d{1,3}[\.\s]\d{1,3})\s*@",
                                RegexOptions.IgnoreCase);
                            if (rdpTitleMatch.Success)
                            {
                                var rdpIp = Regex.Replace(rdpTitleMatch.Groups[1].Value, @"\s+", ".");
                                bool rdpConflicts = false;
                                if (!string.IsNullOrEmpty(targetEndingPaste))
                                {
                                    var rdpLastOctet = rdpIp.Split('.').LastOrDefault() ?? "";
                                    rdpConflicts = !rdpLastOctet.Equals(targetEndingPaste, StringComparison.OrdinalIgnoreCase);
                                }
                                if (!rdpConflicts)
                                {
                                    extractedIp = rdpIp;
                                    found = true;
                                    
                                    var rdpOctetsP = rdpIp.Split('.');
                                    if (rdpOctetsP.Length == 4)
                                        _lastSeenSubnet = string.Join(".", rdpOctetsP[0], rdpOctetsP[1], rdpOctetsP[2]);
                                }
                                else
                                {
                                    var rdpOctetsP = rdpIp.Split('.');
                                    if (rdpOctetsP.Length == 4)
                                    {
                                        rdpDiscardedSubnetP = string.Join(".", rdpOctetsP[0], rdpOctetsP[1], rdpOctetsP[2]);
                                        _lastSeenSubnet = rdpDiscardedSubnetP;
                                    }
                                    Console.WriteLine($"[Paste] RDP Title bar IP {rdpIp} conflicts with filename suffix _{targetEndingPaste} — discarding.");
                                }
                            }

                            // Priority 2: sftp://user@IP - FileZilla title bar
                            if (!found)
                            {
                                var titleIpMatch = Regex.Match(topText,
                                    @"sftp://[^\s@]+@(\d{1,3}[\.\s]\d{1,3}[\.\s]\d{1,3}[\.\s]\d{1,3})",
                                    RegexOptions.IgnoreCase);
                                if (titleIpMatch.Success)
                                {
                                    extractedIp = Regex.Replace(titleIpMatch.Groups[1].Value, @"\s+", ".");
                                    found = true;
                                }
                            }

                            // Priority 3: Any IP in the top crop bar text
                            if (!found)
                            {
                                var topIpMatches = Regex.Matches(topText, @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b");
                                foreach (Match m in topIpMatches)
                                {
                                    var last = m.Groups[1].Value.Split('.').LastOrDefault();
                                    if (last != null && !gatewayOctets.Contains(last))
                                    {
                                        extractedIp = m.Groups[1].Value;
                                        found = true;
                                        break;
                                    }
                                }
                            }

                            // === STEP 2: Full body OCR ===
                            string text = "";
                            using (var imgFull = Pix.LoadFromMemory(capturedBytes))
                            using (var pageFull = engine.Process(imgFull, PageSegMode.SparseText)) { text += " " + pageFull.GetText(); }
                            
                            var fullInvertedBytes = PreprocessImageForOcr(capturedBytes, true);
                            using (var imgFullInv = Pix.LoadFromMemory(fullInvertedBytes))
                            using (var pageFullInv = engine.Process(imgFullInv, PageSegMode.SparseText)) { text += " " + pageFullInv.GetText(); }
                            
                            using (var img = Pix.LoadFromMemory(leftCropBytes))
                            using (var page = engine.Process(img, PageSegMode.SparseText)) { text += " " + page.GetText(); }
                            using (var img = Pix.LoadFromMemory(rightCropBytes))
                            using (var page = engine.Process(img, PageSegMode.SparseText)) { text += " " + page.GetText(); }
                            using (var img = Pix.LoadFromMemory(leftInverted))
                            using (var page = engine.Process(img, PageSegMode.SparseText)) { text += " " + page.GetText(); }
                            using (var img = Pix.LoadFromMemory(rightInverted))
                            using (var page = engine.Process(img, PageSegMode.SparseText)) { text += " " + page.GetText(); }
                            
                            var compactText = Regex.Replace(text, @"\s+", " ");
                            var noSpaceText = Regex.Replace(text, @"\s", "");
                            var combinedText = compactText + " " + noSpaceText;
                            
                            // Priority A: ipconfig IPv4 Address line
                            if (!found)
                            {
                                var ipv4LineMatch = Regex.Match(compactText,
                                    @"IPv4\s+Address[\s\.]+:?\s*(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})",
                                    RegexOptions.IgnoreCase);
                                if (ipv4LineMatch.Success) { extractedIp = ipv4LineMatch.Groups[1].Value; found = true; }
                            }

                            // Priority B: IP directly after "IPv4" keyword
                            if (!found)
                            {
                                var ipv4NoisyMatch = Regex.Match(compactText,
                                    @"IPv4[^:]{0,30}:\s*(\d{1,3}[\.\s]\d{1,3}[\.\s]\d{1,3}[\.\s]\d{1,3})",
                                    RegexOptions.IgnoreCase);
                                if (ipv4NoisyMatch.Success) { extractedIp = Regex.Replace(ipv4NoisyMatch.Groups[1].Value, @"\s+", "."); found = true; }
                            }

                            // Priority B2: OCR-garbled IPv4 variants (IPva, 1Pv4)
                            if (!found)
                            {
                                var garbledIpv4Match = Regex.Match(compactText,
                                    @"\b[I1][Pp][Vv][a4A]\b[^:]{0,20}:?\s*(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})",
                                    RegexOptions.IgnoreCase);
                                if (garbledIpv4Match.Success) { extractedIp = garbledIpv4Match.Groups[1].Value; found = true; }
                            }

                            // Priority B3: IP after Ethernet context
                            if (!found)
                            {
                                var ethernetCtx = Regex.Match(compactText,
                                    @"Ethernet[^\.]{0,80}(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})",
                                    RegexOptions.IgnoreCase);
                                if (ethernetCtx.Success)
                                {
                                    var last = ethernetCtx.Groups[1].Value.Split('.').LastOrDefault();
                                    if (last != null && !gatewayOctets.Contains(last)) { extractedIp = ethernetCtx.Groups[1].Value; found = true; }
                                }
                            }

                            var candidateIps = new List<string>();
                            var robustMatches = Regex.Matches(compactText, @"\b(?:10|172|192)[\.\s]+\d{1,3}[\.\s]+\d{1,3}[\.\s]+\d{1,3}\b");
                            foreach (Match rm in robustMatches) candidateIps.Add(Regex.Replace(rm.Value, @"\s+", "."));
                            
                            var ipMatches = Regex.Matches(combinedText, @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b");
                            foreach (Match im in ipMatches) candidateIps.Add(im.Value);
                            
                            // Try filename match
                            var fileNameMatch2 = Regex.Match(capturedFile.Name, @"_(\d+)\.(png|jpg|jpeg)$", RegexOptions.IgnoreCase);
                            string targetEnding = fileNameMatch2.Success ? fileNameMatch2.Groups[1].Value : null;
                            if (!found && !string.IsNullOrEmpty(targetEnding))
                            {
                                var matchedTarget = candidateIps.FirstOrDefault(ip => ip.EndsWith("." + targetEnding));
                                if (matchedTarget == null)
                                {
                                    matchedTarget = DocumentModel.ServerEvidences
                                        .Select(s => s.ServerIp)
                                        .FirstOrDefault(ip => !string.IsNullOrWhiteSpace(ip) && ip.EndsWith("." + targetEnding));
                                }
                                // Last resort: reconstruct IP from discarded RDP subnet, last seen, or App default + filename octet
                                if (matchedTarget == null)
                                {
                                    string guessedSubnetP = rdpDiscardedSubnetP ?? _lastSeenSubnet;
                                    if (string.IsNullOrEmpty(guessedSubnetP))
                                    {
                                        guessedSubnetP = DocumentModel.Application.Equals("LEA", StringComparison.OrdinalIgnoreCase) ? "172.16.11" : "172.16.8";
                                    }
                                    matchedTarget = guessedSubnetP + "." + targetEnding;
                                    Console.WriteLine($"[Paste] Reconstructed IP from guessed subnet ({guessedSubnetP}): {matchedTarget}");
                                }
                                if (matchedTarget != null) { extractedIp = matchedTarget; found = true; }
                            }

                            if (!found)
                            {
                                extractedIp = candidateIps.FirstOrDefault(ip => {
                                    var last = ip.Split('.').LastOrDefault();
                                    return last != null && !gatewayOctets.Contains(last);
                                });
                            }
                            
                            if (!string.IsNullOrEmpty(extractedIp) && extractedIp.StartsWith("172.16."))
                            {
                                await InvokeAsync(() => {
                                    changeDetail.DeploymentScreenshots.Remove(newShot);
                                    StateHasChanged();
                                });
                                return; // Stop processing this 172.16 image
                            }
                            
                            // First try to extract from the "Search <FolderName>" box
                            var searchMatch = Regex.Match(topText, @"Search\s+([a-zA-Z0-9_\-]+)", RegexOptions.IgnoreCase);
                            if (searchMatch.Success && searchMatch.Groups[1].Value.Trim().Length > 2)
                            {
                                extractedServerName = searchMatch.Groups[1].Value.Trim();
                            }
                            else
                            {
                                // Fallback to parsing the path
                                var pathMatches = Regex.Matches(topText, @"[>\\/]\s*([a-zA-Z0-9_\-\s]+)(?=\s*[>\\/]|\s*$|Search|Type here)", RegexOptions.IgnoreCase);
                                if (pathMatches.Count > 0)
                                {
                                    var lastMatch = pathMatches.Cast<Match>().LastOrDefault(m => m.Groups[1].Value.Trim().Length > 2);
                                    if (lastMatch != null) extractedServerName = Regex.Replace(lastMatch.Groups[1].Value, @"[^a-zA-Z0-9_\-]", "").Trim();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Background OCR failed on pasted deployment screenshot: {ex.Message}");
                    }
                    
                    await InvokeAsync(() =>
                    {
                        if (!string.IsNullOrEmpty(extractedIp)) newShot.ServerIp = extractedIp;
                        if (!string.IsNullOrEmpty(extractedServerName)) newShot.ServerName = extractedServerName;
                        StateHasChanged();
                    });
                });
            }
            
            StateHasChanged();
        }
    }

    private void RemoveDeploymentScreenshot(ChangeDetailViewModel detail, ScreenshotViewModel shot)
    {
        detail.DeploymentScreenshots.Remove(shot);
    }

    private async Task UploadSummaryScreenshotAsync(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file != null)
        {
            if (file.Size > 100 * 1024 * 1024)
            {
                Snackbar.Add("File too large (max 100MB)", Severity.Error);
                return;
            }

            using var stream = file.OpenReadStream(maxAllowedSize: 100 * 1024 * 1024);
            using var memoryStream = new System.IO.MemoryStream();
            await stream.CopyToAsync(memoryStream);
            
            var imageBytes = memoryStream.ToArray();
            var processedDataUrl = await ProcessSummaryImageAsync(imageBytes, file.ContentType);
            
            DocumentModel.SummaryTableScreenshot = new ScreenshotViewModel
            {
                Base64Data = processedDataUrl,
                ContentType = file.ContentType,
                FileName = file.Name
            };
        }
    }

    private async Task<string> ProcessSummaryImageAsync(byte[] imageBytes, string contentType)
    {
        var tessDataPath = System.IO.Path.Combine(WebHostEnvironment.ContentRootPath, "tessdata");
        if (!System.IO.Directory.Exists(tessDataPath) || string.IsNullOrWhiteSpace(DocumentModel.Application))
        {
            return $"data:{contentType};base64,{Convert.ToBase64String(imageBytes)}";
        }

        string appName = DocumentModel.Application;
        bool foundMatch = false;
        int targetY = -1;
        int targetHeight = -1;
        int targetX = 0;
        
        try
        {
            using var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
            using var img = Pix.LoadFromMemory(imageBytes);
            using var page = engine.Process(img);
            
            using var iter = page.GetIterator();
            iter.Begin();
            do
            {
                string text = iter.GetText(PageIteratorLevel.TextLine);
                if (!string.IsNullOrWhiteSpace(text) && text.Contains(appName, StringComparison.OrdinalIgnoreCase))
                {
                    if (iter.TryGetBoundingBox(PageIteratorLevel.TextLine, out var rect))
                    {
                        targetY = rect.Y1;
                        targetHeight = rect.Height;
                        foundMatch = true;
                        
                        var numbers = Regex.Matches(text, @"\d+");
                        if (numbers.Count >= 1)
                        {
                            bool hasChangesInImage = numbers.Cast<Match>().Any(m => int.TryParse(m.Value, out int v) && v > 0);
                            bool uiHasChanges = DocumentModel.ChangeCategories.Any(c => c.HasChanges);
                            
                            if (hasChangesInImage != uiHasChanges)
                            {
                                Snackbar.Add($"Warning: Summary Image shows {(hasChangesInImage ? "changes" : "NO changes")} for {appName}, but UI is set to {(uiHasChanges ? "changes" : "NO changes")}.", Severity.Warning);
                            }
                            else
                            {
                                Snackbar.Add($"Success: Summary Image matches UI status for {appName}.", Severity.Success);
                            }
                        }
                        break;
                    }
                }
            } while (iter.Next(PageIteratorLevel.TextLine));
            
            // Image manipulation (yellow highlight) removed as requested.
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OCR Validation failed: {ex.Message}");
        }

        return $"data:{contentType};base64,{Convert.ToBase64String(imageBytes)}";
    }

    private void RemoveSummaryScreenshot()
    {
        DocumentModel.SummaryTableScreenshot = null;
    }

    private async Task AutoGenerateEvidenceScreenshotsAsync()
    {
        try
        {
            using var scope = ServiceScopeFactory.CreateScope();
            var reportService = scope.ServiceProvider.GetRequiredService<IChangeManagementReportService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Identify upload for DocumentModel.ReportingYear and DocumentModel.ReportingMonth
            Guid? targetUploadId = null;

            // Strategy A: Find upload associated with work orders of that month/year
            var allWos = await dbContext.WorkOrders
                .Select(w => new { w.UploadHistoryId, w.ApprovalDate })
                .ToListAsync();

            var matchingUploadIds = allWos
                .Where(w => !string.IsNullOrWhiteSpace(w.ApprovalDate) &&
                            DateTime.TryParse(w.ApprovalDate, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt) &&
                            dt.Year == DocumentModel.ReportingYear && dt.Month == DocumentModel.ReportingMonth)
                .GroupBy(w => w.UploadHistoryId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .ToList();

            if (matchingUploadIds.Any())
            {
                targetUploadId = matchingUploadIds.First();
            }

            // Strategy B: If none, look at UploadHistories by filename or date
            if (!targetUploadId.HasValue)
            {
                var monthName = System.Globalization.DateTimeFormatInfo.InvariantInfo.GetMonthName(DocumentModel.ReportingMonth);
                var shortMonth = monthName.Length > 3 ? monthName.Substring(0, 3) : monthName;

                var uploads = await dbContext.UploadHistories
                    .OrderByDescending(u => u.UploadDate)
                    .ToListAsync();

                var matchedUpload = uploads.FirstOrDefault(u =>
                    (u.FileName.Contains(monthName, StringComparison.OrdinalIgnoreCase) ||
                     u.FileName.Contains(shortMonth, StringComparison.OrdinalIgnoreCase)) &&
                    u.FileName.Contains(DocumentModel.ReportingYear.ToString()));

                if (matchedUpload == null)
                {
                    matchedUpload = uploads.FirstOrDefault(u =>
                        u.UploadDate.Year == DocumentModel.ReportingYear &&
                        u.UploadDate.Month == DocumentModel.ReportingMonth);
                }

                if (matchedUpload != null)
                {
                    targetUploadId = matchedUpload.Id;
                }
            }

            ChangeManagementReportResult? cmReport = null;
            if (targetUploadId.HasValue)
            {
                try
                {
                    cmReport = await reportService.GenerateReportAsync(targetUploadId.Value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AutoEvidence] GenerateReportAsync error: {ex.Message}");
                }
            }

            string displayMonthYear = cmReport != null && !string.IsNullOrWhiteSpace(cmReport.MonthYear)
                ? cmReport.MonthYear
                : $"{System.Globalization.DateTimeFormatInfo.InvariantInfo.GetMonthName(DocumentModel.ReportingMonth)} {DocumentModel.ReportingYear}";

            int rawWos = cmReport?.TotalRawWorkOrders ?? 0;
            var reportItems = cmReport?.Items ?? new List<ChangeManagementReportItem>();

            // Auto-generate Figure 1 ("WOs for CM review") for ALL applications
            var fig1Bytes = EvidenceImageGenerator.GenerateSummaryTableImage(
                displayMonthYear,
                rawWos,
                reportItems,
                DocumentModel.Application);

            DocumentModel.SummaryTableScreenshot = new ScreenshotViewModel
            {
                Id = Guid.NewGuid(),
                Base64Data = "data:image/png;base64," + Convert.ToBase64String(fig1Bytes),
                ContentType = "image/png",
                FileName = $"Summary_{DocumentModel.Application}_{DocumentModel.ReportingMonth}_{DocumentModel.ReportingYear}.png"
            };

            // Auto-generate Figure 2 ("Filtered Excel"):
            // "If the application has change, then next excel screenshot will appear with yellow marks."
            // "If the application has No change, then no excel screenshot."
            bool hasAnyChanges = DocumentModel.ChangeCategories.Any(c => c.HasChanges) || DocumentModel.WOReportItems.Any();

            if (hasAnyChanges && cmReport?.FilteredWorkOrders != null && cmReport.FilteredWorkOrders.Any())
            {
                var appWoIds = new HashSet<string>(DocumentModel.WOReportItems.Select(w => w.WoId), StringComparer.OrdinalIgnoreCase);
                var fig2Bytes = EvidenceImageGenerator.GenerateFilteredExcelImage(cmReport.FilteredWorkOrders, appWoIds);

                DocumentModel.ExcelScreenshot = new ScreenshotViewModel
                {
                    Id = Guid.NewGuid(),
                    Base64Data = "data:image/png;base64," + Convert.ToBase64String(fig2Bytes),
                    ContentType = "image/png",
                    FileName = $"Excel_{DocumentModel.Application}_{DocumentModel.ReportingMonth}_{DocumentModel.ReportingYear}.png"
                };
            }
            else
            {
                DocumentModel.ExcelScreenshot = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AutoEvidence] Failed to auto-generate evidence screenshots: {ex.Message}");
        }
    }

    private async Task UploadExcelScreenshotAsync(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file != null)
        {
            if (file.Size > 100 * 1024 * 1024)
            {
                Snackbar.Add("File too large (max 100MB)", Severity.Error);
                return;
            }

            using var stream = file.OpenReadStream(maxAllowedSize: 100 * 1024 * 1024);
            using var memoryStream = new System.IO.MemoryStream();
            await stream.CopyToAsync(memoryStream);
            
            if (file.Name.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    memoryStream.Position = 0;
                    using var wordDoc = WordprocessingDocument.Open(memoryStream, false);
                    var body = wordDoc.MainDocumentPart?.Document?.Body;
                    if (body != null)
                    {
                        var drawings = body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Drawing>().ToList();
                        foreach (var drawing in drawings)
                        {
                            var blip = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().FirstOrDefault();
                            if (blip != null && !string.IsNullOrEmpty(blip.Embed?.Value))
                            {
                                var imagePartId = blip.Embed.Value;
                                var imagePart = wordDoc.MainDocumentPart.GetPartById(imagePartId) as ImagePart;
                                if (imagePart != null)
                                {
                                    using var imageStream = imagePart.GetStream();
                                    using var msImg = new System.IO.MemoryStream();
                                    await imageStream.CopyToAsync(msImg);
                                    var imgBytes = msImg.ToArray();
                                    var base64 = Convert.ToBase64String(imgBytes);
                                    var contentType = imagePart.ContentType;
                                    var dataUrl = $"data:{contentType};base64,{base64}";
                                    
                                    DocumentModel.ExcelScreenshot = new ScreenshotViewModel
                                    {
                                        Base64Data = dataUrl,
                                        ContentType = contentType,
                                        FileName = $"Extracted_Excel_{imagePartId}.png"
                                    };
                                    break; // Take the first image found
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing docx for Excel: {ex.Message}");
                    Snackbar.Add($"Error reading {file.Name}: {ex.Message}", Severity.Error);
                }
            }
            else
            {
                var imageBytes = memoryStream.ToArray();
                var base64 = Convert.ToBase64String(imageBytes);
                var dataUrl = $"data:{file.ContentType};base64,{base64}";
                
                DocumentModel.ExcelScreenshot = new ScreenshotViewModel
                {
                    Base64Data = dataUrl,
                    ContentType = file.ContentType,
                    FileName = file.Name
                };
            }
        }
    }

    private void RemoveExcelScreenshot()
    {
        DocumentModel.ExcelScreenshot = null;
    }

    private async Task<bool> SaveDraftInternalAsync()
    {
        IsLoading = true;
        try
        {
            using var scope = ServiceScopeFactory.CreateScope();
            var docService = scope.ServiceProvider.GetRequiredService<ICMReviewDocumentService>();
            var dbDoc = await docService.GetDocumentAsync(DocumentModel.Id);
            if(dbDoc != null)
            {
                await MapToEntityAsync(dbDoc, docService);
                await docService.SaveDraftAsync(dbDoc);

                // Clear transient data only after successful save
                foreach (var serverVm in DocumentModel.ServerEvidences)
                {
                    foreach (var shotVm in serverVm.Screenshots)
                    {
                        shotVm.Base64Data = null;
                    }
                }
                if (DocumentModel.SummaryTableScreenshot != null)
                {
                    DocumentModel.SummaryTableScreenshot.Base64Data = null;
                }
                if (DocumentModel.ExcelScreenshot != null)
                {
                    DocumentModel.ExcelScreenshot.Base64Data = null;
                }

                Snackbar.Add("Draft saved", Severity.Success);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error saving draft: {ex.Message}", Severity.Error);
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool IsPosMissingSiem()
    {
        if (DocumentModel == null) return false;
        if (!string.Equals(DocumentModel.Application, "POS", StringComparison.OrdinalIgnoreCase))
            return false;

        bool hasSiem = DocumentModel.SiemScreenshot != null &&
            (!string.IsNullOrWhiteSpace(DocumentModel.SiemScreenshot.Base64Data) ||
             !string.IsNullOrWhiteSpace(DocumentModel.SiemScreenshot.StorageReference));

        return !hasSiem;
    }

    private void OnSiemWarningOk()
    {
        _showSiemWarningDialog = false;
        _pendingSiemAction = PendingSiemAction.None;
    }

    private async Task OnSiemWarningUploadLaterAsync()
    {
        _showSiemWarningDialog = false;
        var action = _pendingSiemAction;
        _pendingSiemAction = PendingSiemAction.None;

        if (action == PendingSiemAction.SaveAndClose)
        {
            await ExecuteSaveAndCloseAsync();
        }
        else if (action == PendingSiemAction.Finalize)
        {
            await ExecuteFinalizeDocumentAsync();
        }
    }

    private async Task SaveDraftAsync()
    {
        await SaveDraftInternalAsync();
    }

    private async Task SaveAndCloseAsync()
    {
        if (IsPosMissingSiem())
        {
            _pendingSiemAction = PendingSiemAction.SaveAndClose;
            _showSiemWarningDialog = true;
            return;
        }

        await ExecuteSaveAndCloseAsync();
    }

    private async Task ExecuteSaveAndCloseAsync()
    {
        var success = await SaveDraftInternalAsync();
        if (success)
        {
            NavigationManager.NavigateTo("/cm-reviews");
        }
    }

    private async Task MapToEntityAsync(CMReviewDocument dbDoc, ICMReviewDocumentService docService)
    {
        dbDoc.DocumentVersion = DocumentModel.DocumentVersion;
        dbDoc.TemplateVersion = DocumentModel.TemplateVersion;
        dbDoc.UATStatement = DocumentModel.UATStatement;
        dbDoc.ComparisonSummary = DocumentModel.ComparisonSummary;
        dbDoc.VerifierName = DocumentModel.VerifierName;
        dbDoc.VerifierDesignation = DocumentModel.VerifierDesignation;

        var newlySavedImages = new List<string>();

        try
        {
            // Ensure server DisplayOrder strictly matches current list order
            for (int i = 0; i < DocumentModel.ServerEvidences.Count; i++)
            {
                DocumentModel.ServerEvidences[i].DisplayOrder = i;
            }

            // Server Evidences
            var serverIds = DocumentModel.ServerEvidences.Select(s => s.Id).ToList();
            var serversToRemove = dbDoc.ServerEvidences.Where(s => !serverIds.Contains(s.Id)).ToList();
            foreach (var toRemove in serversToRemove) dbDoc.ServerEvidences.Remove(toRemove);

            foreach (var serverVm in DocumentModel.ServerEvidences)
            {
                var serverEntity = dbDoc.ServerEvidences.FirstOrDefault(s => s.Id == serverVm.Id);
                if (serverEntity == null)
                {
                    serverEntity = new CMReviewServerEvidence { Id = serverVm.Id, Screenshots = new List<CMReviewScreenshot>() };
                    docService.MarkNewEntityAsAdded(serverEntity);
                    dbDoc.ServerEvidences.Add(serverEntity);
                }

                serverEntity.ServerIp = serverVm.ServerIp;
                serverEntity.ServerName = serverVm.ServerName;
                serverEntity.DisplayOrder = serverVm.DisplayOrder;
                serverEntity.Remarks = serverVm.Remarks;
                if (serverEntity.Screenshots == null) serverEntity.Screenshots = new List<CMReviewScreenshot>();

                var shotIds = serverVm.Screenshots.Select(s => s.Id).ToList();
                var shotsToRemove = serverEntity.Screenshots.Where(s => !shotIds.Contains(s.Id)).ToList();
                foreach (var toRemove in shotsToRemove) serverEntity.Screenshots.Remove(toRemove);

                foreach (var shotVm in serverVm.Screenshots)
                {
                    // Persist new base64 screenshots
                    if (!string.IsNullOrWhiteSpace(shotVm.Base64Data))
                    {
                        var base64Part = shotVm.Base64Data;
                        if (base64Part.Contains(","))
                        {
                            base64Part = base64Part.Split(',')[1];
                        }
                        var path = await ImageStorageService.SaveImageAsync(base64Part, DocumentModel.Application, DocumentModel.ReportingYear, DocumentModel.ReportingMonth, DocumentModel.Id, "Screenshots", ".png");
                        newlySavedImages.Add(path);
                        shotVm.StorageReference = path;
                        // We will clear Base64Data after the DB save succeeds
                    }

                    var shotEntity = serverEntity.Screenshots.FirstOrDefault(s => s.Id == shotVm.Id);
                    if (shotEntity == null)
                    {
                        shotEntity = new CMReviewScreenshot { Id = shotVm.Id };
                        docService.MarkNewEntityAsAdded(shotEntity);
                        serverEntity.Screenshots.Add(shotEntity);
                    }

                    shotEntity.StorageReference = shotVm.StorageReference;
                    shotEntity.Caption = shotVm.Caption;
                    shotEntity.DisplayOrder = shotVm.DisplayOrder;
                    shotEntity.ContentType = shotVm.ContentType;
                    shotEntity.FileName = shotVm.FileName;
                }
            }

            // Summary Table Screenshot
            if (DocumentModel.SummaryTableScreenshot != null)
            {
                if (!string.IsNullOrWhiteSpace(DocumentModel.SummaryTableScreenshot.Base64Data))
                {
                    var base64Part = DocumentModel.SummaryTableScreenshot.Base64Data;
                    if (base64Part.Contains(","))
                    {
                        base64Part = base64Part.Split(',')[1];
                    }
                    var path = await ImageStorageService.SaveImageAsync(base64Part, DocumentModel.Application, DocumentModel.ReportingYear, DocumentModel.ReportingMonth, DocumentModel.Id, "Screenshots", ".png");
                    newlySavedImages.Add(path);
                    DocumentModel.SummaryTableScreenshot.StorageReference = path;
                }
                dbDoc.SummaryTableStorageReference = DocumentModel.SummaryTableScreenshot.StorageReference;
            }
            else
            {
                dbDoc.SummaryTableStorageReference = null;
            }

            // Excel Screenshot
            if (DocumentModel.ExcelScreenshot != null)
            {
                if (!string.IsNullOrWhiteSpace(DocumentModel.ExcelScreenshot.Base64Data))
                {
                    var base64Part = DocumentModel.ExcelScreenshot.Base64Data;
                    if (base64Part.Contains(","))
                    {
                        base64Part = base64Part.Split(',')[1];
                    }
                    var path = await ImageStorageService.SaveImageAsync(base64Part, DocumentModel.Application, DocumentModel.ReportingYear, DocumentModel.ReportingMonth, DocumentModel.Id, "Screenshots", ".png");
                    newlySavedImages.Add(path);
                    DocumentModel.ExcelScreenshot.StorageReference = path;
                }
                dbDoc.ExcelScreenshotStorageReference = DocumentModel.ExcelScreenshot.StorageReference;
            }
            else
            {
                dbDoc.ExcelScreenshotStorageReference = null;
            }

            // Siem Screenshot
            if (DocumentModel.SiemScreenshot != null)
            {
                if (!string.IsNullOrWhiteSpace(DocumentModel.SiemScreenshot.Base64Data))
                {
                    var base64Part = DocumentModel.SiemScreenshot.Base64Data;
                    if (base64Part.Contains(","))
                    {
                        base64Part = base64Part.Split(',')[1];
                    }
                    var path = await ImageStorageService.SaveImageAsync(base64Part, DocumentModel.Application, DocumentModel.ReportingYear, DocumentModel.ReportingMonth, DocumentModel.Id, "Screenshots", ".png");
                    newlySavedImages.Add(path);
                    DocumentModel.SiemScreenshot.StorageReference = path;
                }
                dbDoc.SiemScreenshotStorageReference = DocumentModel.SiemScreenshot.StorageReference;
            }
            else
            {
                dbDoc.SiemScreenshotStorageReference = null;
            }

            // Change Categories
            var catIds = DocumentModel.ChangeCategories.Select(c => c.Id).ToList();
            var catsToRemove = dbDoc.ChangeCategories.Where(c => !catIds.Contains(c.Id)).ToList();
            foreach (var toRemove in catsToRemove) dbDoc.ChangeCategories.Remove(toRemove);
            foreach (var catVm in DocumentModel.ChangeCategories)
            {
                var catEntity = dbDoc.ChangeCategories.FirstOrDefault(c => c.Id == catVm.Id);
                if (catEntity == null)
                {
                    catEntity = new CMReviewChangeCategory { Id = catVm.Id, ChangeDetails = new List<CMReviewChangeDetail>() };
                    docService.MarkNewEntityAsAdded(catEntity);
                    dbDoc.ChangeCategories.Add(catEntity);
                }

                catEntity.CategoryName = catVm.CategoryName;
                catEntity.HasChanges = catVm.HasChanges;
                catEntity.DisplayOrder = catVm.DisplayOrder;
                if (catEntity.ChangeDetails == null) catEntity.ChangeDetails = new List<CMReviewChangeDetail>();

                var detailIds = catVm.ChangeDetails.Select(d => d.Id).ToList();
                var detailsToRemove = catEntity.ChangeDetails.Where(d => !detailIds.Contains(d.Id)).ToList();
                foreach (var toRemove in detailsToRemove) catEntity.ChangeDetails.Remove(toRemove);

                foreach (var detailVm in catVm.ChangeDetails)
                {
                    var detailEntity = catEntity.ChangeDetails.FirstOrDefault(d => d.Id == detailVm.Id);
                    if (detailEntity == null)
                    {
                        detailEntity = new CMReviewChangeDetail { Id = detailVm.Id, DeploymentScreenshots = new List<CMReviewScreenshot>() };
                        docService.MarkNewEntityAsAdded(detailEntity);
                        catEntity.ChangeDetails.Add(detailEntity);
                    }

                    detailEntity.ChangeOrder = detailVm.ChangeOrder;
                    detailEntity.ChangeType = detailVm.ChangeType;
                    detailEntity.Description = detailVm.Description;
                    detailEntity.ScheduleDate = detailVm.ScheduleDate;
                    detailEntity.ServerInfo = detailVm.ServerInfo;
                    detailEntity.Remarks = detailVm.Remarks;
                    if (detailEntity.DeploymentScreenshots == null) detailEntity.DeploymentScreenshots = new List<CMReviewScreenshot>();

                    var dShotIds = detailVm.DeploymentScreenshots.Select(s => s.Id).ToList();
                    var dShotsToRemove = detailEntity.DeploymentScreenshots.Where(s => !dShotIds.Contains(s.Id)).ToList();
                    foreach (var toRemove in dShotsToRemove) detailEntity.DeploymentScreenshots.Remove(toRemove);

                    foreach (var shotVm in detailVm.DeploymentScreenshots)
                    {
                        if (!string.IsNullOrWhiteSpace(shotVm.Base64Data))
                        {
                            var base64Part = shotVm.Base64Data;
                            if (base64Part.Contains(","))
                            {
                                base64Part = base64Part.Split(',')[1];
                            }
                            var path = await ImageStorageService.SaveImageAsync(base64Part, DocumentModel.Application, DocumentModel.ReportingYear, DocumentModel.ReportingMonth, DocumentModel.Id, "Screenshots", ".png");
                            newlySavedImages.Add(path);
                            shotVm.StorageReference = path;
                        }

                        var shotEntity = detailEntity.DeploymentScreenshots.FirstOrDefault(s => s.Id == shotVm.Id);
                        if (shotEntity == null)
                        {
                            shotEntity = new CMReviewScreenshot { Id = shotVm.Id };
                            docService.MarkNewEntityAsAdded(shotEntity);
                            detailEntity.DeploymentScreenshots.Add(shotEntity);
                        }

                        shotEntity.StorageReference = shotVm.StorageReference;
                        shotEntity.Caption = shotVm.Caption;
                        shotEntity.DisplayOrder = shotVm.DisplayOrder;
                        shotEntity.ContentType = shotVm.ContentType;
                        shotEntity.FileName = shotVm.FileName;
                    }
                }
            }
        }
        catch
        {
            // Rollback files if mapping fails
            foreach (var savedPath in newlySavedImages)
            {
                try
                {
                    await ImageStorageService.DeleteImageAsync(savedPath);
                }
                catch { /* Ignore cleanup errors during rollback */ }
            }
            throw new InvalidOperationException("Failed to persist one or more screenshots. Save aborted.");
        }
    }

    private async Task ExportDocumentAsync()
    {
        try
        {
            IsLoading = true;
            
            // Ensure any unsaved draft data is mapped back if needed, or we just pass DocumentModel
            // Run the synchronous CPU-bound Word generation on a background thread
            // to avoid blocking the Blazor SignalR circuit and causing "Rejoining the server..." timeouts
            var fileBytes = await Task.Run(() => WordGenerator.GenerateWordDocumentAsync(DocumentModel));
            var fileName = $"CM_Review_{DocumentModel.Application}_{DocumentModel.ReportingYear}_{DocumentModel.ReportingMonth}.docx";
            // Use streaming to avoid SignalR disconnects, and increase timeout to 15 minutes for large files
            using var streamRef = new DotNetStreamReference(stream: new MemoryStream(fileBytes));
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
            await JSRuntime.InvokeVoidAsync("downloadFileFromStream", cts.Token, fileName, streamRef);
            
            Snackbar.Add("Exported Document to DOCX", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error exporting document: {ex.Message}", Severity.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task FinalizeDocumentAsync()
    {
        if (IsPosMissingSiem())
        {
            _pendingSiemAction = PendingSiemAction.Finalize;
            _showSiemWarningDialog = true;
            return;
        }

        await ExecuteFinalizeDocumentAsync();
    }

    private async Task ExecuteFinalizeDocumentAsync()
    {
        var saveSuccess = await SaveDraftInternalAsync();
        if (!saveSuccess)
        {
            return;
        }

        IsLoading = true;
        try
        {
            using var scope = ServiceScopeFactory.CreateScope();
            var docService = scope.ServiceProvider.GetRequiredService<ICMReviewDocumentService>();
            await docService.FinalizeDocumentAsync(DocumentModel.Id, "SystemUser");
            DocumentModel.Status = CMReviewDocumentStatus.Finalized;
            Snackbar.Add("Document finalized", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error finalising: {ex.Message}", Severity.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void MapToViewModel(CMReviewDocument dbDoc)
    {
        DocumentModel.Id = dbDoc.Id;
        DocumentModel.Application = dbDoc.Application;
        DocumentModel.ReportingYear = dbDoc.ReportingYear;
        DocumentModel.ReportingMonth = dbDoc.ReportingMonth;
        DocumentModel.DocumentVersion = dbDoc.DocumentVersion;
        DocumentModel.TemplateVersion = dbDoc.TemplateVersion;
        DocumentModel.Status = dbDoc.Status;

        DocumentModel.ServerEvidences = dbDoc.ServerEvidences
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new ServerEvidenceViewModel
            {
                Id = s.Id,
                ServerIp = s.ServerIp,
                ServerName = s.ServerName,
                DisplayOrder = s.DisplayOrder,
                Remarks = s.Remarks,
                Screenshots = s.Screenshots != null ? s.Screenshots.OrderBy(sc => sc.DisplayOrder).Select(sc => new ScreenshotViewModel
                {
                    Id = sc.Id,
                    StorageReference = sc.StorageReference,
                    Caption = sc.Caption,
                    DisplayOrder = sc.DisplayOrder,
                    ContentType = sc.ContentType,
                    FileName = sc.FileName
                }).ToList() : new List<ScreenshotViewModel>()
            }).ToList();

        for (int i = 0; i < DocumentModel.ServerEvidences.Count; i++)
        {
            DocumentModel.ServerEvidences[i].DisplayOrder = i;
        }

        if (_selectedServerId == null || !DocumentModel.ServerEvidences.Any(s => s.Id == _selectedServerId))
        {
            _selectedServerId = DocumentModel.ServerEvidences.FirstOrDefault()?.Id;
        }
        
        if (!string.IsNullOrWhiteSpace(dbDoc.SummaryTableStorageReference))
        {
            DocumentModel.SummaryTableScreenshot = new ScreenshotViewModel
            {
                StorageReference = dbDoc.SummaryTableStorageReference
            };
        }
        else
        {
            DocumentModel.SummaryTableScreenshot = null;
        }

        if (!string.IsNullOrWhiteSpace(dbDoc.ExcelScreenshotStorageReference))
        {
            DocumentModel.ExcelScreenshot = new ScreenshotViewModel
            {
                StorageReference = dbDoc.ExcelScreenshotStorageReference
            };
        }
        else
        {
            DocumentModel.ExcelScreenshot = null;
        }

        if (!string.IsNullOrWhiteSpace(dbDoc.SiemScreenshotStorageReference))
        {
            DocumentModel.SiemScreenshot = new ScreenshotViewModel
            {
                StorageReference = dbDoc.SiemScreenshotStorageReference
            };
        }
        else
        {
            DocumentModel.SiemScreenshot = null;
        }
        DocumentModel.VerifierName = string.IsNullOrWhiteSpace(dbDoc.VerifierName) ? "Md. Amirul Islam" : dbDoc.VerifierName;
        DocumentModel.VerifierDesignation = string.IsNullOrWhiteSpace(dbDoc.VerifierDesignation) ? "Core Business Application and Service Senior Manager" : dbDoc.VerifierDesignation;
        DocumentModel.VerifierDepartment = string.IsNullOrWhiteSpace(dbDoc.VerifierDepartment) ? "Business Support System, Technology" : dbDoc.VerifierDepartment;
        DocumentModel.VerifierOrganization = string.IsNullOrWhiteSpace(dbDoc.VerifierOrganization) ? "Banglalink" : dbDoc.VerifierOrganization;
        DocumentModel.ComparisonSummary = dbDoc.ComparisonSummary;
        DocumentModel.UATStatement = dbDoc.UATStatement;

        DocumentModel.ChangeCategories = dbDoc.ChangeCategories
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new ChangeCategoryViewModel
            {
                Id = c.Id,
                CategoryName = c.CategoryName,
                HasChanges = c.HasChanges,
                DisplayOrder = c.DisplayOrder,
                ChangeDetails = c.ChangeDetails != null ? c.ChangeDetails.OrderBy(d => d.ChangeOrder).Select(d => new ChangeDetailViewModel
                {
                    Id = d.Id,
                    ChangeOrder = d.ChangeOrder,
                    ChangeType = d.ChangeType,
                    Description = d.Description,
                    ScheduleDate = d.ScheduleDate,
                    ServerInfo = d.ServerInfo,
                    Remarks = d.Remarks,
                    DeploymentScreenshots = d.DeploymentScreenshots != null ? d.DeploymentScreenshots.OrderBy(s => s.DisplayOrder).Select(s => new ScreenshotViewModel
                    {
                        Id = s.Id,
                        StorageReference = s.StorageReference,
                        Caption = s.Caption,
                        DisplayOrder = s.DisplayOrder,
                        ContentType = s.ContentType,
                        FileName = s.FileName
                    }).ToList() : new List<ScreenshotViewModel>()
                }).ToList() : new List<ChangeDetailViewModel>()
            }).ToList();

        // Fallback for old drafts that have 0 categories
        if (!DocumentModel.ChangeCategories.Any())
        {
            string[] categoryNames;
            
            if (DocumentModel.Application.Equals("Sales Force App", StringComparison.OrdinalIgnoreCase))
            {
                categoryNames = new string[] 
                {
                    "Re-factoring existing application for performance, scalability or other issues",
                    "Apply ad-hoc enhancement request in existing features",
                    "Implementation of Change Request- OneSalesApp",
                    "Fixation of bug on OneSalesApp",
                    "Modification of Application Existing Features- OneSalesApp",
                    "Deploy new application/module",
                    "Standard Change",
                    "Business Logic -OneSalesApp"
                };
            }
            else
            {
                categoryNames = new string[] 
                {
                    "Re-factoring existing application for performance, scalability or other issues",
                    "Apply ad-hoc enhancement request in existing features",
                    "Bug Fixing",
                    "Standard Change",
                    "Implementation of Change Request",
                    "Data reconciliation for user or system data",
                    "Add new features/module in existing application",
                    "Deploy new application / Modules",
                    "Software install/remove/configure in server"
                };
            }

            for(int i = 0; i < categoryNames.Length; i++)
            {
                DocumentModel.ChangeCategories.Add(new ChangeCategoryViewModel
                {
                    Id = Guid.NewGuid(),
                    CategoryName = categoryNames[i],
                    DisplayOrder = i,
                    HasChanges = false
                });
            }
        }
    }

    private byte[] PreprocessImageForOcr(byte[] imageBytes, bool invert = false)
    {
        try
        {
            using var ms = new System.IO.MemoryStream(imageBytes);
            using var original = System.Drawing.Image.FromStream(ms);
            
            // Upscale by 2.5x to help OCR read very small text (like CMD window ipconfig)
            int newWidth = (int)(original.Width * 2.5);
            int newHeight = (int)(original.Height * 2.5);
            
            using var resized = new Bitmap(newWidth, newHeight);
            using (var graphics = Graphics.FromImage(resized))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                
                if (invert)
                {
                    System.Drawing.Imaging.ColorMatrix colorMatrix = new System.Drawing.Imaging.ColorMatrix(
                        new float[][]
                        {
                            new float[] {-1, 0,  0,  0,  0},
                            new float[] {0, -1,  0,  0,  0},
                            new float[] {0,  0, -1,  0,  0},
                            new float[] {0,  0,  0,  1,  0},
                            new float[] {1,  1,  1,  0,  1}
                        });
                    
                    using var attributes = new System.Drawing.Imaging.ImageAttributes();
                    attributes.SetColorMatrix(colorMatrix);
                    
                    graphics.DrawImage(original, new Rectangle(0, 0, newWidth, newHeight), 0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
                }
                else
                {
                    graphics.DrawImage(original, 0, 0, newWidth, newHeight);
                }
            }
            
            using var msOut = new System.IO.MemoryStream();
            resized.Save(msOut, System.Drawing.Imaging.ImageFormat.Png);
            return msOut.ToArray();
        }
        catch
        {
            // If anything fails, fallback to original bytes
            return imageBytes;
        }
    }
    
    private byte[] CropTopBar(byte[] imageBytes)
    {
        try
        {
            using var ms = new System.IO.MemoryStream(imageBytes);
            using var original = System.Drawing.Image.FromStream(ms);
            
            // Crop the top 15% where the file explorer path bar is usually located
            int cropHeight = (int)(original.Height * 0.15);
            
            using var cropped = new Bitmap(original.Width, cropHeight);
            using (var g = Graphics.FromImage(cropped))
            {
                g.DrawImage(original, new Rectangle(0, 0, original.Width, cropHeight),
                    new Rectangle(0, 0, original.Width, cropHeight), GraphicsUnit.Pixel);
            }
            
            int newWidth = original.Width * 2;
            int newHeight = cropHeight * 2;
            using var upscaled = new Bitmap(newWidth, newHeight);
            using (var g = Graphics.FromImage(upscaled))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(cropped, 0, 0, newWidth, newHeight);
            }
            
            using var msOut = new System.IO.MemoryStream();
            upscaled.Save(msOut, System.Drawing.Imaging.ImageFormat.Png);
            return msOut.ToArray();
        }
        catch
        {
            return imageBytes;
        }
    }
    
    private byte[] CropHalf(byte[] imageBytes, bool isLeft)
    {
        try
        {
            using var ms = new System.IO.MemoryStream(imageBytes);
            using var original = System.Drawing.Image.FromStream(ms);
            
            // Crop to the left or right 60% of the image (20% overlap in middle)
            int cropWidth = (int)(original.Width * 0.6);
            int cropX = isLeft ? 0 : original.Width - cropWidth;
            int cropHeight = original.Height;
            
            using var cropped = new Bitmap(cropWidth, cropHeight);
            using (var g = Graphics.FromImage(cropped))
            {
                g.DrawImage(original, new Rectangle(0, 0, cropWidth, cropHeight),
                    new Rectangle(cropX, 0, cropWidth, cropHeight), GraphicsUnit.Pixel);
            }
            
            // Also upscale 2x for better OCR readability
            int newWidth = cropWidth * 2;
            int newHeight = cropHeight * 2;
            using var upscaled = new Bitmap(newWidth, newHeight);
            using (var g = Graphics.FromImage(upscaled))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(cropped, 0, 0, newWidth, newHeight);
            }
            
            using var msOut = new System.IO.MemoryStream();
            upscaled.Save(msOut, System.Drawing.Imaging.ImageFormat.Png);
            return msOut.ToArray();
        }
        catch
        {
            return imageBytes;
        }
    }
}
