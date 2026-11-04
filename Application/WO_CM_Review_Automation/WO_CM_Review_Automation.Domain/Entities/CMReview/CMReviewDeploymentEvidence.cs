using System;
using System.Collections.Generic;

namespace WO_CM_Review_Automation.Domain.Entities.CMReview;

public class CMReviewDeploymentEvidence : BaseAuditableEntity
{
    public Guid CMReviewDocumentId { get; set; }
    public CMReviewDocument CMReviewDocument { get; set; } = null!;

    public string ServerIp { get; set; } = string.Empty;
    public string? Note { get; set; }
    public int DisplayOrder { get; set; }

    // Navigation property for Before/After screenshots
    public ICollection<CMReviewScreenshot> Screenshots { get; set; } = new List<CMReviewScreenshot>();
}
