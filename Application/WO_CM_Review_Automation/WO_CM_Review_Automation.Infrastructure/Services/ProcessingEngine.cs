using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WO_CM_Review_Automation.Application.Processing;
using WO_CM_Review_Automation.Domain.Entities;
using WO_CM_Review_Automation.Infrastructure.Data;

namespace WO_CM_Review_Automation.Infrastructure.Services;

public class ProcessingEngine : IProcessingEngine
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;

    public ProcessingEngine(ApplicationDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<ProcessingResult> ProcessBatchAsync(Guid uploadHistoryId)
    {
        var cacheKey = $"ProcessingResult_{uploadHistoryId}";
        if (_cache.TryGetValue(cacheKey, out ProcessingResult? cachedResult) && cachedResult != null)
        {
            return cachedResult;
        }

        var config = await LoadConfigurationAsync();
        var rawWorkOrders = await _context.WorkOrders
            .Where(w => w.UploadHistoryId == uploadHistoryId)
            .ToListAsync();

        var result = ProcessWorkOrders(rawWorkOrders, config);

        // Update database with statistics
        var history = await _context.UploadHistories.FindAsync(uploadHistoryId);
        if (history != null)
        {
            history.RawWorkOrders = result.Statistics.RawWorkOrders;
            history.FilteredWorkOrders = result.Statistics.FilteredWorkOrders;
            history.ProcessedApplications = result.Statistics.ProcessedApplications;
            history.UnknownApplications = result.Statistics.UnknownApplications;
            history.DuplicateApplicationsRemoved = result.Statistics.DuplicateApplicationsRemoved;
            history.SummaryGeneratedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
        }

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));

        return result;
    }

    public ProcessingResult ProcessWorkOrders(IEnumerable<WorkOrder> rawWorkOrders, ProcessingConfiguration config)
    {
        var result = new ProcessingResult();
        var stats = result.Statistics;

        // The delimiters: comma, semicolon, pipe, new line
        var delimiters = new[] { ',', ';', '|', '\n', '\r' };
        
        // Regex to collapse multiple spaces to a single space
        var spaceRegex = new Regex(@"\s+", RegexOptions.Compiled);

        foreach (var wo in rawWorkOrders)
        {
            stats.RawWorkOrders++;

            // 1. Business Rule Filtering (Case-insensitive)
            if (!config.AllowedLMApprovers.Contains(wo.LmApprover.Trim()) ||
                !config.AllowedVendors.Contains(wo.Vendor.Trim()) ||
                !config.AllowedIssuers.Contains(wo.Issuer.Trim()) ||
                !config.AllowedDomains.Contains(wo.Domain.Trim()) ||
                !config.AllowedIssuerOffices.Contains(wo.IssuerOffice.Trim()))
            {
                continue;
            }

            stats.FilteredWorkOrders++;

            // 2. Split Service Names
            var splitServices = wo.ServiceName
                .Split(delimiters, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            var seenAppsInWo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var service in splitServices)
            {
                // 3. Normalize Application Names (Trim, collapse spaces)
                var normalizedRaw = spaceRegex.Replace(service, " ").Trim();
                
                string finalAppName;
                int displayOrder = 9999;

                if (config.ApplicationMappings.TryGetValue(normalizedRaw, out var mapping))
                {
                    finalAppName = mapping.NormalizedName;
                    displayOrder = mapping.DisplayOrder;
                }
                else
                {
                    finalAppName = "Unknown Application";
                    stats.UnknownApplications++;
                    result.ValidationMessages.Add($"WorkOrder {wo.WoId}: Unknown Application '{service}' mapped to '{finalAppName}'");
                }

                // 4. Remove Duplicate Applications (within the SAME Work Order)
                if (seenAppsInWo.Contains(finalAppName))
                {
                    stats.DuplicateApplicationsRemoved++;
                    continue; // Skip duplicate app for this WO
                }

                seenAppsInWo.Add(finalAppName);
                stats.ProcessedApplications++;

                result.ProcessedWorkOrders.Add(new ProcessedWorkOrder
                {
                    WorkOrderId = wo.Id,
                    WoId = wo.WoId,
                    OriginalServiceName = service,
                    NormalizedApplicationName = finalAppName,
                    ApprovalDate = wo.ApprovalDate,
                    Vendor = wo.Vendor,
                    Issuer = wo.Issuer,
                    LmApprover = wo.LmApprover,
                    Status = wo.Status
                });
            }
        }

        // 5. Generate Monthly Summary
        var summaryGroups = result.ProcessedWorkOrders
            .GroupBy(p => p.NormalizedApplicationName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        // Include Active Applications with zero count
        foreach (var kvp in config.ActiveNormalizedApps)
        {
            var appName = kvp.Key;
            var displayOrder = kvp.Value;
            
            var count = summaryGroups.ContainsKey(appName) ? summaryGroups[appName] : 0;
            
            result.MonthlySummary.Items.Add(new ApplicationSummaryItem
            {
                ApplicationName = appName,
                Count = count,
                DisplayOrder = displayOrder
            });
            
            summaryGroups.Remove(appName); // Mark as processed
        }

        // Add any remaining items (like Unknown Application)
        foreach (var kvp in summaryGroups)
        {
            result.MonthlySummary.Items.Add(new ApplicationSummaryItem
            {
                ApplicationName = kvp.Key,
                Count = kvp.Value,
                DisplayOrder = 9999
            });
        }

        // Sort by DisplayOrder, then Alphabetical as fallback
        result.MonthlySummary.Items = result.MonthlySummary.Items
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.ApplicationName)
            .ToList();

        stats.SummaryRecords = result.MonthlySummary.Items.Count;

        return result;
    }

    private async Task<ProcessingConfiguration> LoadConfigurationAsync()
    {
        var config = new ProcessingConfiguration();

        var approvers = await _context.AllowedLMApprovers.Where(x => x.IsActive).Select(x => x.Name).ToListAsync();
        config.AllowedLMApprovers = new HashSet<string>(approvers.Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);

        var vendors = await _context.AllowedVendors.Where(x => x.IsActive).Select(x => x.Name).ToListAsync();
        config.AllowedVendors = new HashSet<string>(vendors.Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);

        var issuers = await _context.AllowedIssuers.Where(x => x.IsActive).Select(x => x.Name).ToListAsync();
        config.AllowedIssuers = new HashSet<string>(issuers.Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);

        var domains = await _context.AllowedDomains.Where(x => x.IsActive).Select(x => x.Name).ToListAsync();
        config.AllowedDomains = new HashSet<string>(domains.Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);

        var offices = await _context.AllowedIssuerOffices.Where(x => x.IsActive).Select(x => x.Name).ToListAsync();
        config.AllowedIssuerOffices = new HashSet<string>(offices.Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);

        var mappings = await _context.ApplicationMappings.Where(x => x.IsActive).ToListAsync();
        foreach (var m in mappings)
        {
            var normalizedAlias = Regex.Replace(m.AliasName.Trim(), @"\s+", " ");
            config.ApplicationMappings[normalizedAlias] = (m.NormalizedName, m.DisplayOrder);
            
            // Keep track of active normalized apps for zero-count display
            if (!config.ActiveNormalizedApps.ContainsKey(m.NormalizedName))
            {
                config.ActiveNormalizedApps[m.NormalizedName] = m.DisplayOrder;
            }
            else
            {
                // In case of multiple display orders for the same normalized name, keep the minimum (highest priority)
                config.ActiveNormalizedApps[m.NormalizedName] = Math.Min(config.ActiveNormalizedApps[m.NormalizedName], m.DisplayOrder);
            }
        }

        return config;
    }
}
