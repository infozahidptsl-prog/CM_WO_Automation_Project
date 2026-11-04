using System;
using System.Collections.Generic;

namespace WO_CM_Review_Automation.Domain.Entities.CMReview;

public class CMReviewServerEvidence : BaseAuditableEntity
{
    public Guid CMReviewDocumentId { get; set; }
    public CMReviewDocument CMReviewDocument { get; set; } = null!;

    public string ServerIp { get; set; } = string.Empty;
    public string? ServerName { get; set; }
    public int DisplayOrder { get; set; }
    public string? Remarks { get; set; }

    public ICollection<CMReviewScreenshot> Screenshots { get; set; } = new List<CMReviewScreenshot>();
}
