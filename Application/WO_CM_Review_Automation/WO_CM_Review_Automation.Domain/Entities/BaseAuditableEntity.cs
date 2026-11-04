using System;

namespace WO_CM_Review_Automation.Domain.Entities;

public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTimeOffset CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
}
