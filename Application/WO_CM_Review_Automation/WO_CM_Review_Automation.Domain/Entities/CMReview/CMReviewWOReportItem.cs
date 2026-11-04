using System;

namespace WO_CM_Review_Automation.Domain.Entities.CMReview;

public class CMReviewWOReportItem : BaseAuditableEntity
{
    public Guid CMReviewDocumentId { get; set; }
    public CMReviewDocument CMReviewDocument { get; set; } = null!;

    public string WoId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string AdvanceTemplate { get; set; } = string.Empty;
    public string WOType { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string NEType { get; set; } = string.Empty;
    public int NECount { get; set; }
    public string Vendors { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string IssuerOffice { get; set; } = string.Empty;
    public string CollaboratedBy { get; set; } = string.Empty;
    public string KPICheckedBy { get; set; } = string.Empty;
    public string AssigneeOffice { get; set; } = string.Empty;
    public string Assignee { get; set; } = string.Empty;
    public string SLAPercentage { get; set; } = string.Empty;
    public string IssueDateTime { get; set; } = string.Empty;
    public string ProposedExecutionWindows { get; set; } = string.Empty;
    public string ExecutionStart { get; set; } = string.Empty;
    public string ExecutionEnd { get; set; } = string.Empty;
    public string ExecutionEndDate { get; set; } = string.Empty;
}
