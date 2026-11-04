using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WO_CM_Review_Automation.Application.Reports;

public class ChangeManagementReportItem
{
    public string Division { get; set; } = string.Empty;
    public string FunctionalUnit { get; set; } = string.Empty;
    public string TeamLeader { get; set; } = string.Empty;
    public int ActualCount { get; set; }
    public List<string> WorkOrderIds { get; set; } = new();
    public string? RowColor { get; set; }
}

public class ChangeManagementReportResult
{
    public Guid UploadHistoryId { get; set; }
    public string MonthYear { get; set; } = string.Empty;
    public List<ChangeManagementReportItem> Items { get; set; } = new();
    public int TotalWorkOrders { get; set; }
    public int TotalRawWorkOrders { get; set; }
    public List<WO_CM_Review_Automation.Domain.Entities.WorkOrder> FilteredWorkOrders { get; set; } = new();
}

public interface IChangeManagementReportService
{
    Task<ChangeManagementReportResult> GenerateReportAsync(Guid uploadHistoryId);
    Task SaveFinalizedReportAsync(Guid uploadHistoryId, ChangeManagementReportResult result);
    Task<byte[]> ExportExcelAsync(Guid uploadHistoryId);
    Task<byte[]> ExportCMReviewExcelAsync(Guid uploadHistoryId);
}
