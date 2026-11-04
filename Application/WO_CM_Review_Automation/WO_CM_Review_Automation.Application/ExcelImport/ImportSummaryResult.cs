using System;

namespace WO_CM_Review_Automation.Application.ExcelImport;

public class ImportSummaryResult
{
    public Guid UploadHistoryId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTimeOffset ImportDate { get; set; }
    public int ImportedRecords { get; set; }
    public int InvalidRecords { get; set; }
    public int DuplicateRecords { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}
