using System;

namespace WO_CM_Review_Automation.Domain.Entities;

public class FunctionalUnitMapping : BaseAuditableEntity
{
    public string FunctionalUnitName { get; set; } = string.Empty;
    public string SourceValue { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
