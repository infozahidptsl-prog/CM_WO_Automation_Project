using System;
using System.Collections.Generic;

namespace WO_CM_Review_Automation.Domain.Entities.CMReview;

public enum CMReviewDocumentStatus
{
    Draft,
    InPreparation,
    ReadyForReview,
    Finalized
}

public class CMReviewDocument : BaseAuditableEntity
{
    public string Application { get; set; } = string.Empty;
    public int ReportingMonth { get; set; }
    public int ReportingYear { get; set; }
    public string DocumentVersion { get; set; } = "1.0";
    public string TemplateVersion { get; set; } = "1.0";
    public CMReviewDocumentStatus Status { get; set; } = CMReviewDocumentStatus.Draft;
    
    public string? UATStatement { get; set; }
    public string? ComparisonSummary { get; set; }
    public string? SummaryTableStorageReference { get; set; }
    public string? ExcelScreenshotStorageReference { get; set; }
    public string? SiemScreenshotStorageReference { get; set; }
    
    public string? VerifierName { get; set; }
    public string? VerifierDesignation { get; set; }
    public string? VerifierDepartment { get; set; }
    public string? VerifierOrganization { get; set; }

    public string? FinalizedBy { get; set; }
    public DateTimeOffset? FinalizedDate { get; set; }

    public ICollection<CMReviewServerEvidence> ServerEvidences { get; set; } = new List<CMReviewServerEvidence>();
    public ICollection<CMReviewChangeCategory> ChangeCategories { get; set; } = new List<CMReviewChangeCategory>();
    public ICollection<CMReviewDeploymentEvidence> DeploymentEvidences { get; set; } = new List<CMReviewDeploymentEvidence>();
    public ICollection<CMReviewWOReportItem> WOReportItems { get; set; } = new List<CMReviewWOReportItem>();
}
