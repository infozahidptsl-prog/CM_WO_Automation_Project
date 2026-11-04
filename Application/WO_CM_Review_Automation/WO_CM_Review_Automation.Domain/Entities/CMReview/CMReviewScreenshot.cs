using System;

namespace WO_CM_Review_Automation.Domain.Entities.CMReview;

public class CMReviewScreenshot : BaseAuditableEntity
{
    public Guid? CMReviewServerEvidenceId { get; set; }
    public CMReviewServerEvidence? ServerEvidence { get; set; }

    public Guid? CMReviewDeploymentEvidenceId { get; set; }
    public CMReviewDeploymentEvidence? DeploymentEvidence { get; set; }

    public Guid? CMReviewChangeDetailId { get; set; }
    public CMReviewChangeDetail? ChangeDetail { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string StorageReference { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsBeforeDeployment { get; set; } // Used when attached to DeploymentEvidence
}
