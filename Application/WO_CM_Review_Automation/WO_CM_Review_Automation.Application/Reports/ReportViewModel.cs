using System;
using WO_CM_Review_Automation.Application.Processing;

namespace WO_CM_Review_Automation.Application.Reports;

public class ReportViewModel
{
    public MonthlySummaryReport MonthlySummary { get; set; } = new();
    public string MonthName { get; set; } = string.Empty;
    public string GeneratedBy { get; set; } = string.Empty;
    public DateTimeOffset GeneratedDate { get; set; }
    public string SourceFile { get; set; } = string.Empty;
    public DateTimeOffset ImportDate { get; set; }
    public int TotalWorkOrders { get; set; }
}
