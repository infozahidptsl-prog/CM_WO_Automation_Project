using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WO_CM_Review_Automation.Application.Reports;
using WO_CM_Review_Automation.Domain.Entities;
using WO_CM_Review_Automation.Infrastructure.Data;

namespace WO_CM_Review_Automation.Infrastructure.Services;

public class WorkOrderFilterService : IWorkOrderFilterService
{
    private readonly ApplicationDbContext _context;

    public WorkOrderFilterService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<WorkOrder>> GetFilteredWorkOrdersAsync(Guid uploadHistoryId)
    {
        var allWorkOrders = await _context.WorkOrders
            .Where(w => w.UploadHistoryId == uploadHistoryId)
            .ToListAsync();

        var allowedLMs = await _context.AllowedLMApprovers.Select(x => x.Name.ToLower()).ToListAsync();
        var allowedVendors = await _context.AllowedVendors.Select(x => x.Name.ToLower()).ToListAsync();
        var allowedIssuers = await _context.AllowedIssuers.Select(x => x.Name.ToLower()).ToListAsync();
        var allowedOffices = await _context.AllowedIssuerOffices.Select(x => x.Name.ToLower()).ToListAsync();
        var allowedDomains = await _context.AllowedDomains.Select(x => x.Name.ToLower()).ToListAsync();

        var filteredWorkOrders = allWorkOrders.Where(wo => 
            string.Equals(wo.Status?.Trim(), "Closed", StringComparison.OrdinalIgnoreCase) &&
            allowedLMs.Contains(wo.LmApprover?.Trim().ToLower() ?? "") &&
            (string.IsNullOrWhiteSpace(wo.Vendor) || 
             string.Equals(wo.Vendor.Trim(), "Other", StringComparison.OrdinalIgnoreCase) ||
             wo.Vendor.Split(new[] { ',', '&', '|' }, StringSplitOptions.RemoveEmptyEntries)
                      .Select(v => v.Trim().ToLower())
                      .Any(v => allowedVendors.Contains(v)))
        ).ToList();

        return filteredWorkOrders;
    }
}
