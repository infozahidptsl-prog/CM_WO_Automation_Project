using System;
using System.Linq;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var raw = new[]
        {
            new { WoId = 88194, ServiceName = "POS", Vendor = "Other", Status = "Closed", LmApprover = "Md Amirul Islam" },
            new { WoId = 88414, ServiceName = "One Sales App", Vendor = "Other", Status = "Closed", LmApprover = "Md Amirul Islam" },
            new { WoId = 99999, ServiceName = "Sales Force App And DMS", Vendor = "Other", Status = "Closed", LmApprover = "Md Amirul Islam" }
        };

        var allowedLMs = new[] { "md amirul islam" };
        var allowedVendors = new[] { "primetech bd" };
        var dbFunctionalUnitMappings = new[]
        {
            new { SourceValue = "POS", FunctionalUnitName = "POS" },
            new { SourceValue = "One Sales App", FunctionalUnitName = "Sales Force App / RSO App" },
            new { SourceValue = "Sales Force App", FunctionalUnitName = "Sales Force App / RSO App" },
            new { SourceValue = "DMS", FunctionalUnitName = "DMS" }
        };

        var filteredWorkOrders = raw.Where(wo => 
            string.Equals(wo.Status?.Trim(), "Closed", StringComparison.OrdinalIgnoreCase) &&
            allowedLMs.Contains(wo.LmApprover?.Trim().ToLower() ?? "") &&
            (string.IsNullOrWhiteSpace(wo.Vendor) || 
             string.Equals(wo.Vendor?.Trim(), "Other", StringComparison.OrdinalIgnoreCase) ||
             wo.Vendor.Split(new[] { ',', '&', '|' }, StringSplitOptions.RemoveEmptyEntries)
                      .Select(v => v.Trim().ToLower())
                      .Any(v => allowedVendors.Contains(v)))
        ).ToList();
        
        Console.WriteLine($"Filtered: {filteredWorkOrders.Count}");

        var reportItemsMap = new Dictionary<string, int>();

        foreach (var wo in filteredWorkOrders)
        {
            var matchedUnits = new HashSet<string>();
            void EvaluateService(string svc)
            {
                if (string.IsNullOrWhiteSpace(svc)) return;
                foreach (var mapping in dbFunctionalUnitMappings)
                {
                    if (svc.Contains(mapping.SourceValue, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedUnits.Add(mapping.FunctionalUnitName);
                    }
                }
            }

            var services = wo.ServiceName?.Split(new[] { ',', '&', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).ToList();

            foreach (var svc in services) EvaluateService(svc);
            if (matchedUnits.Count == 0) EvaluateService(wo.ServiceName);

            foreach (var unit in matchedUnits)
            {
                if (!reportItemsMap.ContainsKey(unit)) reportItemsMap[unit] = 0;
                reportItemsMap[unit]++;
            }
        }
        
        foreach (var kvp in reportItemsMap)
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value}");
        }
    }
}
