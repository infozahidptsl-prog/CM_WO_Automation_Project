using System;
using System.Collections.Generic;

namespace WO_CM_Review_Automation.Domain.Entities;

public class UploadHistory : BaseAuditableEntity
{
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTimeOffset UploadDate { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public DateTimeOffset? ImportStarted { get; set; }
    public DateTimeOffset? ImportCompleted { get; set; }
    public TimeSpan? ProcessingTime { get; set; }
    public int TotalRows { get; set; }
    public int MatchingRows { get; set; }
    public int InvalidRows { get; set; }
    public string UploadStatus { get; set; } = string.Empty;
    public string? Remarks { get; set; }

    // Processing Statistics
    public int RawWorkOrders { get; set; }
    public int FilteredWorkOrders { get; set; }
    public int ProcessedApplications { get; set; }
    public int UnknownApplications { get; set; }
    public int DuplicateApplicationsRemoved { get; set; }
    public DateTimeOffset? SummaryGeneratedAt { get; set; }
    public string? FinalizedReportJson { get; set; }

    public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}
