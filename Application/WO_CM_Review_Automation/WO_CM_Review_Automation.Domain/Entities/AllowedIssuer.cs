using System;

namespace WO_CM_Review_Automation.Domain.Entities;

public class AllowedIssuer : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? Remarks { get; set; }
}
