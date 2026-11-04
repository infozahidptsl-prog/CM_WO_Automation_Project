using System;

namespace WO_CM_Review_Automation.Domain.Entities;

public abstract class SoftDeletableEntity : BaseAuditableEntity
{
    public bool IsDeleted { get; set; } = false;
    public DateTimeOffset? DeletedDate { get; set; }
    public string? DeletedBy { get; set; }
}
