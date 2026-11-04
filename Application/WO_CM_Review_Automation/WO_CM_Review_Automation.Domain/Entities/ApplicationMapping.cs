using System;

namespace WO_CM_Review_Automation.Domain.Entities;

public class ApplicationMapping : BaseAuditableEntity
{
    public string AliasName { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Remarks { get; set; }
}
