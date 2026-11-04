namespace WO_CM_Review_Automation.Application.Import.Models;

public class ValidationMessage
{
    public ValidationLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? RowIndex { get; set; }
    public string ColumnName { get; set; } = string.Empty;
}
