using System;
using System.Text.Json.Serialization;

namespace WO_CM_Review_Automation.Domain.Entities;

public class WorkOrder : BaseAuditableEntity
{
    public Guid UploadHistoryId { get; set; }
    [JsonIgnore]
    public UploadHistory UploadHistory { get; set; } = null!;

    public int ExcelRowIndex { get; set; }
    public string WoId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string IssuerOffice { get; set; } = string.Empty;
    public string LmApprover { get; set; } = string.Empty;
    public string? ApprovalDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    
    public int ApplicationCount { get; set; }
    public string? ProcessingRemarks { get; set; }

    public string? Subject { get; set; }
    public string? WoType { get; set; }
    public string? ChangeType { get; set; }
    public string? NeType { get; set; }
    public int? NeCount { get; set; }
    public string? QaKpiCheckedBy { get; set; }
    public string? Assignee { get; set; }
    public string? Sla { get; set; }
    public string? IssueDateTime { get; set; }
    public string? ProposedDateTime { get; set; }
    public string? ExecutionStart { get; set; }
    public string? ExecutionEnd { get; set; }
    public string? RowColor { get; set; }
}
