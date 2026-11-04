using System;
using System.Collections.Generic;

namespace WO_CM_Review_Automation.Application.Import.Models;

public class ColumnMappingResult
{
    public string RawHeader { get; set; } = string.Empty;
    public string NormalizedHeader { get; set; } = string.Empty;
    public string CanonicalHeader { get; set; } = string.Empty;
    public int ColumnIndex { get; set; }
    
    // Status can be: "Matched", "Alias Match", "Missing", "Unused"
    public string MatchStatus { get; set; } = string.Empty;
    public ColumnClassification Classification { get; set; }
}

public class ValidationDiagnosticReport
{
    public string WorkbookName { get; set; } = string.Empty;
    public string WorksheetUsed { get; set; } = string.Empty;
    public int TotalWorksheets { get; set; }
    public int HeaderRowIndex { get; set; }
    public int TotalColumns { get; set; }
    public int TotalRows { get; set; }
    public double HeaderConfidence { get; set; }
    
    public List<ColumnMappingResult> MappingTable { get; set; } = new();
    public List<string> MissingRequiredColumns { get; set; } = new();
    public List<string> DuplicateColumns { get; set; } = new();
    public List<ValidationMessage> WarningsAndErrors { get; set; } = new();
    
    public bool HasCriticalErrors => WarningsAndErrors.Exists(x => x.Level == ValidationLevel.Critical);
}

public class ImportSession
{
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    public string WorkbookName { get; set; } = string.Empty;
    public string WorksheetName { get; set; } = string.Empty;
    public int HeaderRow { get; set; }
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public int DuplicateRows { get; set; }
    public long ProcessingTimeMs { get; set; }
    
    // Status can be: "Uploaded", "Scanning", "Validating", "PreviewReady", "Importing", "Completed", "Failed"
    public string Status { get; set; } = "Uploaded";
    
    public ValidationDiagnosticReport Diagnostics { get; set; } = new();
}
