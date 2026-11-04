using System.Collections.Generic;

namespace WO_CM_Review_Automation.Application.Import.Models;

public class HeaderMapping
{
    public string CanonicalName { get; set; } = string.Empty;
    public ColumnClassification Classification { get; set; }
    public List<string> Aliases { get; set; } = new();
}

public class ImportEngineOptions
{
    public const string SectionName = "ImportEngine";

    public double HeaderConfidenceThreshold { get; set; } = 0.8;
    public int MaxHeaderScanRows { get; set; } = 20;
    public long MaxUploadSizeBytes { get; set; } = 104857600; // 100MB
    public List<HeaderMapping> Mappings { get; set; } = new();
}
