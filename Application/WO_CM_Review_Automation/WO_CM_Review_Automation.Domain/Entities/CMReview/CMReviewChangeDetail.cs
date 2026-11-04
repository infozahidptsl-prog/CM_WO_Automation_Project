using System;

namespace WO_CM_Review_Automation.Domain.Entities.CMReview;

public class CMReviewChangeDetail : BaseAuditableEntity
{
    public Guid CMReviewChangeCategoryId { get; set; }
    public CMReviewChangeCategory CMReviewChangeCategory { get; set; } = null!;

    public string ChangeOrder { get; set; } = string.Empty;
    public string? ChangeType { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? ScheduleDate { get; set; }
    public string? ServerInfo { get; set; }
    public string? Remarks { get; set; }
    
    public ICollection<CMReviewScreenshot> DeploymentScreenshots { get; set; } = new List<CMReviewScreenshot>();
}
