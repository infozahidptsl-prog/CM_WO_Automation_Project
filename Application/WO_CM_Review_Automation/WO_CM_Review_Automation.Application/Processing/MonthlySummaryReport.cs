using System.Collections.Generic;

namespace WO_CM_Review_Automation.Application.Processing;

public class MonthlySummaryReport
{
    public List<ApplicationSummaryItem> Items { get; set; } = new();
}

public class ApplicationSummaryItem
{
    public string ApplicationName { get; set; } = string.Empty;
    public int Count { get; set; }
    public int DisplayOrder { get; set; }
}
