using System;

namespace WO_CM_Review_Automation.Domain.Entities;

public class TeamLeaderMapping : BaseAuditableEntity
{
    public string TeamLeaderName { get; set; } = string.Empty;
    public string SourceValue { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
