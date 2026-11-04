using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WO_CM_Review_Automation.Application.Interfaces;
using WO_CM_Review_Automation.Application.Reports;
using WO_CM_Review_Automation.Domain.Entities.CMReview;
using WO_CM_Review_Automation.Infrastructure.Data;

namespace WO_CM_Review_Automation.Infrastructure.Services.CMReview;

public class CMReviewWOReportService : ICMReviewWOReportService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkOrderFilterService _workOrderFilterService;

    public CMReviewWOReportService(ApplicationDbContext context, IWorkOrderFilterService workOrderFilterService)
    {
        _context = context;
        _workOrderFilterService = workOrderFilterService;
    }

    private bool TryParseFlexibleDate(string dateStr, out DateTime result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(dateStr)) return false;
        
        if (DateTime.TryParseExact(dateStr, new[] { "yyyy-MM-dd HH:mm:ss.fffffff", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result))
            return true;
            
        if (DateTime.TryParseExact(dateStr, new[] { "dd-MM-yy H:mm", "dd-MM-yy HH:mm", "d-M-yy H:mm", "dd-MM-yy", "dd-MM-yyyy HH:mm:ss", "dd-MM-yyyy H:mm:ss", "dd-MM-yyyy" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result))
            return true;
            
        if (DateTime.TryParse(dateStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result))
            return true;
            
        if (DateTime.TryParse(dateStr, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None, out result))
            return true;
            
        return false;
    }

    public async Task<List<CMReviewWOReportItem>> GenerateWOReportItemsAsync(Guid documentId, string application, int year, int month)
    {
        var allWOs = await _context.WorkOrders.ToListAsync();
        
        // 1. Identify appropriate WorkOrder data for the requested Month/Year using flexible date parsing.
        var targetWOs = allWOs
            .Where(w => !string.IsNullOrWhiteSpace(w.ApprovalDate) && TryParseFlexibleDate(w.ApprovalDate, out var dt) && dt.Year == year && dt.Month == month)
            .ToList();

        // 2. Group targetWOs by WoId to find the LATEST record for each WorkOrder
        var latestTargetWOs = targetWOs
            .GroupBy(w => w.WoId)
            .Select(g => g.OrderByDescending(w => w.CreatedDate).First())
            .ToList();

        // 3. We only care about the UploadHistoryIds of these latest records
        var relevantHistoryIds = latestTargetWOs.Select(w => w.UploadHistoryId).Distinct().ToList();

        var allFilteredWOs = new List<WO_CM_Review_Automation.Domain.Entities.WorkOrder>();

        // 4. Apply the shared WorkOrder eligibility filtering per UploadHistory
        foreach (var historyId in relevantHistoryIds)
        {
            var history = await _context.UploadHistories.FindAsync(historyId);
            bool usedSavedFilteredWOs = false;
            var filteredForHistory = new List<WO_CM_Review_Automation.Domain.Entities.WorkOrder>();

            if (history != null && !string.IsNullOrWhiteSpace(history.FinalizedReportJson))
            {
                try
                {
                    var jsonOptions = new System.Text.Json.JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles };
                    var savedReport = System.Text.Json.JsonSerializer.Deserialize<ChangeManagementReportResult>(history.FinalizedReportJson, jsonOptions);
                    
                    if (savedReport != null && history.FinalizedReportJson.Contains("\"FilteredWorkOrders\""))
                    {
                        filteredForHistory = savedReport.FilteredWorkOrders ?? new List<WO_CM_Review_Automation.Domain.Entities.WorkOrder>();
                        Console.WriteLine($"[DEBUG-FILTER] history {historyId} initially has {filteredForHistory.Count} filtered WOs");
                        
                        if (savedReport.Items != null && savedReport.Items.Any())
                        {
                            var activeWoIds = new HashSet<string>(savedReport.Items.SelectMany(i => i.WorkOrderIds ?? new List<string>()), StringComparer.OrdinalIgnoreCase);
                            Console.WriteLine($"[DEBUG-FILTER] history {historyId} activeWoIds count: {activeWoIds.Count}");
                            Console.WriteLine($"[DEBUG-FILTER] is 99714 in activeWoIds? {activeWoIds.Contains("99714")}");
                            filteredForHistory = filteredForHistory.Where(w => activeWoIds.Contains(w.WoId)).ToList();
                            Console.WriteLine($"[DEBUG-FILTER] after activeWoIds filter: {filteredForHistory.Count} WOs");
                        }
                        usedSavedFilteredWOs = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG-ERROR] JSON parse failed for {historyId}: {ex.Message}");
                }
            }

            if (!usedSavedFilteredWOs)
            {
                filteredForHistory = await _workOrderFilterService.GetFilteredWorkOrdersAsync(historyId);
            }

            // Only add WOs that are IN our latestTargetWOs list AND are in the filtered (selected) list
            var selectedWoIdsInThisHistory = new HashSet<string>(filteredForHistory.Select(w => w.WoId), StringComparer.OrdinalIgnoreCase);
            
            var matchingLatestWOs = latestTargetWOs
                .Where(w => w.UploadHistoryId == historyId && selectedWoIdsInThisHistory.Contains(w.WoId))
                .ToList();
                
            allFilteredWOs.AddRange(matchingLatestWOs);
        }

        // 5. Apply the required Application mapping/filtering
        var appMappings = await _context.ApplicationMappings
            .Where(a => a.IsActive && a.NormalizedName.ToLower() == application.ToLower())
            .ToListAsync();

        var fuMappings = await _context.FunctionalUnitMappings
            .Where(f => f.IsActive && f.FunctionalUnitName.ToLower() == application.ToLower())
            .ToListAsync();

        var validAliases = appMappings.Select(a => a.AliasName.ToLower().Trim()).ToList();
        var validFuSources = fuMappings.Select(f => f.SourceValue.ToLower().Trim()).ToList();
        
        var normalizedAppName = application.ToLower().Trim();
        var delimiters = new[] { ',', ';', '|', '\n', '\r' };
        var finalWOs = new List<WO_CM_Review_Automation.Domain.Entities.WorkOrder>();

        Console.WriteLine($"[DEBUG] application: {application}, year: {year}, month: {month}");
        Console.WriteLine($"[DEBUG] targetWOs count: {targetWOs.Count}, allFilteredWOs count: {allFilteredWOs.Count}");

        foreach (var wo in allFilteredWOs.OrderByDescending(w => w.CreatedDate)) 
        {
            Console.WriteLine($"[DEBUG] Checking WO: {wo.WoId}, ServiceName: {wo.ServiceName}");
            if (string.IsNullOrWhiteSpace(wo.ServiceName)) continue;

            var splitServices = wo.ServiceName
                .Split(delimiters, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().ToLower())
                .ToList();

            // Match if any split service matches an alias or the normalized name itself
            bool matchesApp = splitServices.Any(s => validAliases.Contains(s) || s == normalizedAppName);

            // Match if ServiceName contains any Functional Unit Source Value
            bool matchesFu = validFuSources.Any(fu => wo.ServiceName.Contains(fu, StringComparison.OrdinalIgnoreCase));

            Console.WriteLine($"[DEBUG] matchesApp: {matchesApp}, matchesFu: {matchesFu}");

            if (matchesApp || matchesFu)
            {
                // Enrich missing ChangeType from latest historical upload if necessary
                if (string.IsNullOrWhiteSpace(wo.ChangeType))
                {
                    var latestWo = allWOs.Where(w => w.WoId == wo.WoId && !string.IsNullOrWhiteSpace(w.ChangeType))
                                         .OrderByDescending(w => w.CreatedDate)
                                         .FirstOrDefault();
                    if (latestWo != null)
                    {
                        wo.ChangeType = latestWo.ChangeType;
                    }
                }

                if (string.IsNullOrWhiteSpace(wo.Subject))
                {
                    var latestWo = allWOs.Where(w => w.WoId == wo.WoId && !string.IsNullOrWhiteSpace(w.Subject))
                                         .OrderByDescending(w => w.CreatedDate)
                                         .FirstOrDefault();
                    if (latestWo != null)
                    {
                        wo.Subject = latestWo.Subject;
                    }
                }
                
                if (string.IsNullOrWhiteSpace(wo.ExecutionEnd) || wo.ExecutionEnd == "--")
                {
                    var latestWo = allWOs.Where(w => w.WoId == wo.WoId && !string.IsNullOrWhiteSpace(w.ExecutionEnd) && w.ExecutionEnd != "--")
                                         .OrderByDescending(w => w.CreatedDate)
                                         .FirstOrDefault();
                    if (latestWo != null)
                    {
                        wo.ExecutionEnd = latestWo.ExecutionEnd;
                    }
                    else
                    {
                        // Fallback to ExecutionStart if ExecutionEnd is entirely missing
                        wo.ExecutionEnd = wo.ExecutionStart;
                    }
                }
                
                finalWOs.Add(wo);
            }
        }
        Console.WriteLine($"[DEBUG] finalWOs count: {finalWOs.Count}");

        // 4. Transform the resulting records into CMReviewWOReportItem
        var reportItems = finalWOs.Select(wo => new CMReviewWOReportItem
        {
            CMReviewDocumentId = documentId,
            WoId = wo.WoId,
            Status = wo.Status ?? "",
            Subject = wo.Subject ?? "",
            AdvanceTemplate = "", 
            WOType = "CM", // Default assumption, adjust if needed
            ChangeType = string.IsNullOrWhiteSpace(wo.ChangeType) ? "Normal" : wo.ChangeType,
            ServiceName = wo.ServiceName ?? "",
            Domain = wo.Domain ?? "",
            NEType = "",
            NECount = 0,
            Vendors = wo.Vendor ?? "",
            Issuer = wo.Issuer ?? "",
            IssuerOffice = wo.IssuerOffice ?? "",
            CollaboratedBy = "",
            KPICheckedBy = "",
            AssigneeOffice = "",
            Assignee = wo.LmApprover ?? "",
            SLAPercentage = "",
            IssueDateTime = wo.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
            ProposedExecutionWindows = "",
            ExecutionStart = wo.ApprovalDate ?? "",
            ExecutionEnd = wo.ExecutionEnd ?? "",
            ExecutionEndDate = wo.ApprovalDate ?? ""
        }).OrderBy(x => x.WoId).ToList();

        return reportItems;
    }
}
