using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WO_CM_Review_Automation.Application.Import.Interfaces;
using WO_CM_Review_Automation.Application.Import.Models;

namespace WO_CM_Review_Automation.Application.Import.Services;

public class HeaderMappingService : IHeaderMappingService
{
    private readonly ImportEngineOptions _options;
    private readonly Dictionary<string, HeaderMapping> _canonicalMap;
    private readonly Dictionary<string, string> _aliasToCanonicalMap;

    public HeaderMappingService(IOptions<ImportEngineOptions> options)
    {
        _options = options.Value;
        
        _canonicalMap = _options.Mappings.ToDictionary(m => NormalizeHeader(m.CanonicalName), m => m);
        _aliasToCanonicalMap = new Dictionary<string, string>();
        
        foreach (var mapping in _options.Mappings)
        {
            string normalizedCanonical = NormalizeHeader(mapping.CanonicalName);
            _aliasToCanonicalMap[normalizedCanonical] = normalizedCanonical;
            
            foreach (var alias in mapping.Aliases)
            {
                _aliasToCanonicalMap[NormalizeHeader(alias)] = normalizedCanonical;
            }
        }
    }

    public string NormalizeHeader(string rawHeader)
    {
        if (string.IsNullOrWhiteSpace(rawHeader)) return string.Empty;
        
        string result = rawHeader.ToLowerInvariant();
        result = Regex.Replace(result, @"[^a-z0-9]", "");
        return result;
    }

    public ColumnMappingResult MapColumn(string rawHeader)
    {
        string normalized = NormalizeHeader(rawHeader);
        var result = new ColumnMappingResult
        {
            RawHeader = rawHeader,
            NormalizedHeader = normalized
        };

        if (_aliasToCanonicalMap.TryGetValue(normalized, out var canonical))
        {
            var mapping = _canonicalMap[canonical];
            result.CanonicalHeader = mapping.CanonicalName;
            result.Classification = mapping.Classification;
            
            if (normalized == NormalizeHeader(mapping.CanonicalName))
            {
                result.MatchStatus = "Matched";
            }
            else
            {
                result.MatchStatus = "Alias Match";
            }
        }
        else
        {
            if (normalized.Contains("sla"))
            {
                var canonicalKey = NormalizeHeader("SLA");
                if (_canonicalMap.TryGetValue(canonicalKey, out var mapping))
                {
                    result.CanonicalHeader = mapping.CanonicalName;
                    result.Classification = mapping.Classification;
                    result.MatchStatus = "Fuzzy Match";
                    return result;
                }
            }
            else if (normalized.Contains("proposed"))
            {
                var canonicalKey = NormalizeHeader("Proposed Date/Time");
                if (_canonicalMap.TryGetValue(canonicalKey, out var mapping))
                {
                    result.CanonicalHeader = mapping.CanonicalName;
                    result.Classification = mapping.Classification;
                    result.MatchStatus = "Fuzzy Match";
                    return result;
                }
            }
            
            result.MatchStatus = "Unused";
            result.Classification = ColumnClassification.Unknown;
        }

        return result;
    }

    public double CalculateConfidence(List<string> rawHeaders)
    {
        if (rawHeaders == null || rawHeaders.Count == 0) return 0;
        
        int totalRequired = _options.Mappings.Count(m => m.Classification == ColumnClassification.Required);
        if (totalRequired == 0) return 1.0;

        int matchedRequired = 0;
        var foundCanonical = new HashSet<string>();

        foreach (var header in rawHeaders)
        {
            if (string.IsNullOrWhiteSpace(header)) continue;
            var mapping = MapColumn(header);
            
            if (mapping.Classification == ColumnClassification.Required && !foundCanonical.Contains(mapping.CanonicalHeader))
            {
                matchedRequired++;
                foundCanonical.Add(mapping.CanonicalHeader);
            }
        }

        return (double)matchedRequired / totalRequired;
    }
}
