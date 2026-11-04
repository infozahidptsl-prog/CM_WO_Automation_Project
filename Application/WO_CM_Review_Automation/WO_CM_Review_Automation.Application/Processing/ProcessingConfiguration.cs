using System;
using System.Collections.Generic;

namespace WO_CM_Review_Automation.Application.Processing;

public class ProcessingConfiguration
{
    public HashSet<string> AllowedLMApprovers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> AllowedVendors { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> AllowedIssuers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> AllowedDomains { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> AllowedIssuerOffices { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    
    // Key: Alias, Value: (NormalizedName, DisplayOrder)
    public Dictionary<string, (string NormalizedName, int DisplayOrder)> ApplicationMappings { get; set; } 
        = new(StringComparer.OrdinalIgnoreCase);
        
    // List of active normalized apps (for zero-count inclusion)
    public Dictionary<string, int> ActiveNormalizedApps { get; set; } 
        = new(StringComparer.OrdinalIgnoreCase);
}
