using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WO_CM_Review_Automation.Application.Reports;
using WO_CM_Review_Automation.Domain.Entities;
using WO_CM_Review_Automation.Infrastructure.Data;

namespace WO_CM_Review_Automation.Infrastructure.Services;

public class ChangeManagementReportService : IChangeManagementReportService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkOrderFilterService _workOrderFilterService;

    public ChangeManagementReportService(ApplicationDbContext context, IWorkOrderFilterService workOrderFilterService)
    {
        _context = context;
        _workOrderFilterService = workOrderFilterService;
    }

    public async Task<ChangeManagementReportResult> GenerateReportAsync(Guid uploadHistoryId)
    {
        var history = await _context.UploadHistories
            .FirstOrDefaultAsync(h => h.Id == uploadHistoryId);

        if (history == null)
            throw new Exception("Upload history not found.");

        if (!string.IsNullOrEmpty(history.FinalizedReportJson))
        {
            try
            {
                var jsonOptions = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles };
                var savedReport = JsonSerializer.Deserialize<ChangeManagementReportResult>(history.FinalizedReportJson, jsonOptions);
                if (savedReport != null)
                {
                    // Backward compatibility for legacy JSON that didn't have FilteredWorkOrders
                    if (!history.FinalizedReportJson.Contains("\"FilteredWorkOrders\""))
                    {
                        savedReport.FilteredWorkOrders = await _workOrderFilterService.GetFilteredWorkOrdersAsync(uploadHistoryId);
                    }
                    return savedReport;
                }
            }
            catch
            {
                // If deserialization fails, fallback to recalculation
            }
        }

        // 1. Get filtered work orders using the shared filter service
        var filteredWorkOrders = await _workOrderFilterService.GetFilteredWorkOrdersAsync(uploadHistoryId);

        var dbFunctionalUnitMappings = await _context.FunctionalUnitMappings.ToListAsync();
        var dbTeamLeaderMappings = await _context.TeamLeaderMappings.ToListAsync();

        // 2. Functional Unit Mapping
        var reportItemsMap = new Dictionary<string, ChangeManagementReportItem>(); // Key: FunctionalUnit

        // Pre-populate with all known Functional Units so they always appear
        var allFunctionalUnits = dbFunctionalUnitMappings.Select(x => x.FunctionalUnitName).Distinct().ToList();
        
        foreach(var fu in allFunctionalUnits)
        {
            var relatedSourceValues = dbFunctionalUnitMappings
                .Where(f => f.FunctionalUnitName == fu)
                .Select(f => f.SourceValue)
                .ToList();

            var pocRule = dbTeamLeaderMappings
                .FirstOrDefault(t => relatedSourceValues.Contains(t.SourceValue));
                
            string poc = pocRule?.TeamLeaderName ?? "Unknown";

            reportItemsMap[fu] = new ChangeManagementReportItem
            {
                Division = "Business Support Service and Operations Support Service",
                FunctionalUnit = fu,
                TeamLeader = poc,
                ActualCount = 0,
                WorkOrderIds = new List<string>()
            };
        }

        foreach (var wo in filteredWorkOrders)
        {
            if (string.IsNullOrWhiteSpace(wo.ServiceName)) continue;

            var matchedUnits = new HashSet<string>();

            // Helper to map and assign based on DB rules
            void EvaluateService(string? svc)
            {
                if (string.IsNullOrWhiteSpace(svc)) return;
                
                foreach (var mapping in dbFunctionalUnitMappings)
                {
                    if (svc.Contains(mapping.SourceValue, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedUnits.Add(mapping.FunctionalUnitName);
                    }
                }
            }

            var services = wo.ServiceName?.Split(new[] { ',', '&', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList() ?? new List<string>();

            foreach (var svc in services)
            {
                EvaluateService(svc);
            }

            if (matchedUnits.Count == 0)
            {
                EvaluateService(wo.ServiceName);
            }

            foreach (var unit in matchedUnits)
            {
                if (!reportItemsMap.ContainsKey(unit))
                {
                    // Look up POC from DB mappings
                    var pocRule = dbTeamLeaderMappings.FirstOrDefault(x => string.Equals(x.SourceValue, unit, StringComparison.OrdinalIgnoreCase));
                    string poc = pocRule?.TeamLeaderName ?? "Unknown";

                    reportItemsMap[unit] = new ChangeManagementReportItem
                    {
                        Division = "Business Support Service and Operations Support Service",
                        FunctionalUnit = unit,
                        TeamLeader = poc,
                        ActualCount = 0,
                        WorkOrderIds = new List<string>()
                    };
                }

                var item = reportItemsMap[unit];
                if (!item.WorkOrderIds.Contains(wo.WoId))
                {
                    item.WorkOrderIds.Add(wo.WoId);
                    item.ActualCount++;
                }
            }
        }

        // Sort WO IDs
        foreach (var item in reportItemsMap.Values)
        {
            item.WorkOrderIds.Sort();
        }

        string monthYear = history.UploadDate.ToString("MMMM yyyy"); // Fallback
        
        var validDates = new List<DateTime>();
        foreach (var w in filteredWorkOrders)
        {
            string? dateToParse = !string.IsNullOrWhiteSpace(w.ApprovalDate) ? w.ApprovalDate : w.IssueDateTime;
            if (!string.IsNullOrWhiteSpace(dateToParse) && DateTime.TryParse(dateToParse, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt))
            {
                validDates.Add(dt);
            }
        }
        if (validDates.Any())
        {
            // Get the most common month/year from the data
            monthYear = validDates
                .GroupBy(d => new { d.Year, d.Month })
                .OrderByDescending(g => g.Count())
                .First()
                .First()
                .ToString("MMMM yyyy");
        }

        var result = new ChangeManagementReportResult
        {
            UploadHistoryId = uploadHistoryId,
            MonthYear = monthYear,
            TotalWorkOrders = reportItemsMap.Values.Sum(x => x.ActualCount),
            TotalRawWorkOrders = history.RawWorkOrders,
            Items = reportItemsMap.Values.OrderBy(x => x.FunctionalUnit).ToList(),
            FilteredWorkOrders = filteredWorkOrders
        };
        return result;
    }

    public async Task SaveFinalizedReportAsync(Guid uploadHistoryId, ChangeManagementReportResult result)
    {
        var history = await _context.UploadHistories.FirstOrDefaultAsync(h => h.Id == uploadHistoryId);
        if (history != null)
        {
            var jsonOptions = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles };
            history.FinalizedReportJson = JsonSerializer.Serialize(result, jsonOptions);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<byte[]> ExportExcelAsync(Guid uploadHistoryId)
    {
        var result = await GenerateReportAsync(uploadHistoryId);
        
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var ws = workbook.Worksheets.Add("CM Summary");

        ws.Cell(1, 1).Value = $"Change Management Summary {result.MonthYear}";
        ws.Range(1, 1, 1, 5).Merge();
        ws.Cell(1, 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#F4B084");
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(1, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
        ws.Cell(1, 1).Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
        ws.Row(1).Height = 25;

        ws.Cell(2, 1).Value = "Total Change :";
        ws.Cell(2, 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#FFE699");
        ws.Cell(2, 1).Style.Font.Bold = true;

        ws.Cell(2, 2).Value = result.TotalRawWorkOrders;
        ws.Range(2, 2, 2, 5).Merge();
        ws.Cell(2, 2).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#FFE699");
        ws.Cell(2, 2).Style.Font.Bold = true;

        ws.Cell(3, 1).Value = "Division";
        ws.Cell(3, 2).Value = "Functional Unit";
        ws.Cell(3, 3).Value = "Team Leader/POC";
        ws.Cell(3, 4).Value = "Actual Count";
        ws.Cell(3, 5).Value = "WO No.";

        var headerRange = ws.Range(3, 1, 3, 5);
        headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#595959");
        headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
        ws.Row(3).Height = 20;

        int row = 4;
        bool isFirst = true;
        int itemCount = result.Items.Count;

        foreach (var item in result.Items)
        {
            if (isFirst && itemCount > 0)
            {
                ws.Cell(row, 1).Value = "Business Support\r\nService and Operations\r\nSupport Service";
                ws.Cell(row, 1).Style.Alignment.WrapText = true;
                ws.Cell(row, 1).Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                ws.Cell(row, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                ws.Range(row, 1, row + itemCount - 1, 1).Merge();
                isFirst = false;
            }

            ws.Cell(row, 2).Value = item.FunctionalUnit;
            ws.Cell(row, 3).Value = item.TeamLeader;
            ws.Cell(row, 4).Value = item.ActualCount;
            ws.Cell(row, 4).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            
            ws.Cell(row, 5).Value = item.ActualCount == 0 ? "N/A" : string.Join(", ", item.WorkOrderIds);
            ws.Cell(row, 5).Style.Alignment.WrapText = true;
            ws.Cell(row, 5).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            row++;
        }

        ws.Cell(row, 1).Value = "Total";
        ws.Range(row, 1, row, 3).Merge();
        ws.Cell(row, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;
        ws.Cell(row, 1).Style.Font.Bold = true;

        ws.Cell(row, 4).Value = result.TotalWorkOrders;
        ws.Cell(row, 4).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#E2EFDA");
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Cell(row, 4).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

        // Apply borders
        var dataRange = ws.Range(1, 1, row, 5);
        dataRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

        // Adjust column widths
        ws.Column(1).Width = 25;
        ws.Column(2).Width = 35;
        ws.Column(3).Width = 30;
        ws.Column(4).Width = 15;
        ws.Column(5).Width = 40;

        using var ms = new System.IO.MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportCMReviewExcelAsync(Guid uploadHistoryId)
    {
        var result = await GenerateReportAsync(uploadHistoryId);
        
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var ws = workbook.Worksheets.Add("CM Review Summary");

        ws.Cell(1, 1).Value = $"Change Management Summary {result.MonthYear}";
        ws.Range(1, 1, 1, 6).Merge();
        ws.Cell(1, 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#F4B084");
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(1, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
        ws.Cell(1, 1).Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
        ws.Row(1).Height = 25;

        ws.Cell(2, 1).Value = "Total Change :";
        ws.Cell(2, 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#FFE699");
        ws.Cell(2, 1).Style.Font.Bold = true;

        ws.Cell(2, 2).Value = result.TotalRawWorkOrders;
        ws.Range(2, 2, 2, 6).Merge();
        ws.Cell(2, 2).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#FFE699");
        ws.Cell(2, 2).Style.Font.Bold = true;
        ws.Cell(2, 2).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;

        ws.Cell(3, 1).Value = "Division";
        ws.Cell(3, 2).Value = "Functional Unit";
        ws.Cell(3, 3).Value = "Team Leader/POC";
        ws.Cell(3, 4).Value = "Implemented Change";
        ws.Cell(3, 5).Value = "Implemented Change with backdated CM";
        ws.Cell(3, 6).Value = "Total";

        var headerRange = ws.Range(3, 1, 3, 6);
        headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#8EAADB");
        headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.Black;
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
        ws.Row(3).Height = 35;
        headerRange.Style.Alignment.WrapText = true;

        int row = 4;
        bool isFirst = true;
        int itemCount = result.Items.Count;

        foreach (var item in result.Items)
        {
            if (isFirst && itemCount > 0)
            {
                ws.Cell(row, 1).Value = "Business Support\r\nService and Operations\r\nSupport Service";
                ws.Cell(row, 1).Style.Alignment.WrapText = true;
                ws.Cell(row, 1).Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                ws.Cell(row, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                ws.Range(row, 1, row + itemCount - 1, 1).Merge();
                isFirst = false;
            }

            ws.Cell(row, 2).Value = item.FunctionalUnit;
            ws.Cell(row, 3).Value = item.TeamLeader;
            ws.Cell(row, 4).Value = item.ActualCount;
            ws.Cell(row, 5).Value = ""; // Empty string for backdated CM
            ws.Cell(row, 6).Value = item.ActualCount;

            ws.Cell(row, 4).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 6).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            row++;
        }

        // Apply borders
        var dataRange = ws.Range(1, 1, row - 1, 6);
        dataRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

        // Adjust column widths
        ws.Column(1).Width = 25;
        ws.Column(2).Width = 35;
        ws.Column(3).Width = 30;
        ws.Column(4).Width = 15;
        ws.Column(5).Width = 25;
        ws.Column(6).Width = 12;

        using var ms = new System.IO.MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
