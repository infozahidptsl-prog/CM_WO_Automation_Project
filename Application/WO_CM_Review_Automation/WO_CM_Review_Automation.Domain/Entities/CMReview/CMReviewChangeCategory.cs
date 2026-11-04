using System;
using System.Collections.Generic;

namespace WO_CM_Review_Automation.Domain.Entities.CMReview;

public class CMReviewChangeCategory : BaseAuditableEntity
{
    public Guid CMReviewDocumentId { get; set; }
    public CMReviewDocument CMReviewDocument { get; set; } = null!;

    public string CategoryName { get; set; } = string.Empty;
    public bool HasChanges { get; set; }
    public int DisplayOrder { get; set; }

    public ICollection<CMReviewChangeDetail> ChangeDetails { get; set; } = new List<CMReviewChangeDetail>();
}
