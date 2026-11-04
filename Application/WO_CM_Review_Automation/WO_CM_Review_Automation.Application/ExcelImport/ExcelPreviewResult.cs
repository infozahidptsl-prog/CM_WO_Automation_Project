using System;
using System.Collections.Generic;

namespace WO_CM_Review_Automation.Application.ExcelImport;

public class ExcelPreviewResult
{
    public Guid PreviewId { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public DateTimeOffset UploadDate { get; set; } = DateTimeOffset.UtcNow;
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public int DuplicateRows { get; set; }
    public List<ParsedWorkOrderRow> Rows { get; set; } = new();
    public List<string> GlobalErrors { get; set; } = new();
    public bool IsSuccess => GlobalErrors.Count == 0;
}
