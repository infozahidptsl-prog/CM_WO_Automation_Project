using System;
using System.Collections.Generic;

namespace WO_CM_Review_Automation.Application.Processing;

public class ProcessingResult
{
    public MonthlySummaryReport MonthlySummary { get; set; } = new();
    public List<ProcessedWorkOrder> ProcessedWorkOrders { get; set; } = new();
    public ProcessingStatistics Statistics { get; set; } = new();
    public List<string> ValidationMessages { get; set; } = new();
}

public class ProcessingStatistics
{
    public int RawWorkOrders { get; set; }
    public int FilteredWorkOrders { get; set; }
    public int ProcessedApplications { get; set; }
    public int UnknownApplications { get; set; }
    public int DuplicateApplicationsRemoved { get; set; }
    public int SummaryRecords { get; set; }
}
