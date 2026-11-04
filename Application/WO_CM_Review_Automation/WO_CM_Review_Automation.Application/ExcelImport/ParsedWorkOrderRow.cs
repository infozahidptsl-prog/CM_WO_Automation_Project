using System;
using WO_CM_Review_Automation.Domain.Entities;

namespace WO_CM_Review_Automation.Application.ExcelImport;

public class ParsedWorkOrderRow
{
    public int ExcelRowIndex { get; set; }
    public WorkOrder WorkOrder { get; set; } = new();
    public bool IsValid { get; set; } = true;
    public bool IsDuplicate { get; set; }
    public string? ErrorMessage { get; set; }
}
