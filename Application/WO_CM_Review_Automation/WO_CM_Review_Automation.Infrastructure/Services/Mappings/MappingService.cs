using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WO_CM_Review_Automation.Application.Mappings.Interfaces;
using WO_CM_Review_Automation.Domain.Entities;
using WO_CM_Review_Automation.Infrastructure.Data;

namespace WO_CM_Review_Automation.Infrastructure.Services.Mappings;

public class MappingService : IMappingService
{
    private readonly ApplicationDbContext _context;

    public MappingService(ApplicationDbContext context)
    {
        _context = context;
    }

    // --- Functional Unit Mappings ---

    public async Task<List<FunctionalUnitMapping>> GetFunctionalUnitMappingsAsync()
    {
        return await _context.FunctionalUnitMappings.ToListAsync();
    }

    public async Task<FunctionalUnitMapping> AddFunctionalUnitMappingAsync(FunctionalUnitMapping mapping)
    {
        mapping.Id = Guid.NewGuid();
        _context.FunctionalUnitMappings.Add(mapping);
        await _context.SaveChangesAsync();
        return mapping;
    }

    public async Task<FunctionalUnitMapping> UpdateFunctionalUnitMappingAsync(FunctionalUnitMapping mapping)
    {
        _context.FunctionalUnitMappings.Update(mapping);
        await _context.SaveChangesAsync();
        return mapping;
    }

    public async Task DeleteFunctionalUnitMappingAsync(Guid id)
    {
        var mapping = await _context.FunctionalUnitMappings.FindAsync(id);
        if (mapping != null)
        {
            _context.FunctionalUnitMappings.Remove(mapping);
            await _context.SaveChangesAsync();
        }
    }

    // --- Team Leader Mappings ---

    public async Task<List<TeamLeaderMapping>> GetTeamLeaderMappingsAsync()
    {
        return await _context.TeamLeaderMappings.ToListAsync();
    }

    public async Task<TeamLeaderMapping> AddTeamLeaderMappingAsync(TeamLeaderMapping mapping)
    {
        mapping.Id = Guid.NewGuid();
        _context.TeamLeaderMappings.Add(mapping);
        await _context.SaveChangesAsync();
        return mapping;
    }

    public async Task<TeamLeaderMapping> UpdateTeamLeaderMappingAsync(TeamLeaderMapping mapping)
    {
        _context.TeamLeaderMappings.Update(mapping);
        await _context.SaveChangesAsync();
        return mapping;
    }

    public async Task DeleteTeamLeaderMappingAsync(Guid id)
    {
        var mapping = await _context.TeamLeaderMappings.FindAsync(id);
        if (mapping != null)
        {
            _context.TeamLeaderMappings.Remove(mapping);
            await _context.SaveChangesAsync();
        }
    }

    // --- Application Mappings ---

    public async Task<List<ApplicationMapping>> GetApplicationMappingsAsync()
    {
        return await _context.ApplicationMappings.ToListAsync();
    }

    public async Task<ApplicationMapping> AddApplicationMappingAsync(ApplicationMapping mapping)
    {
        mapping.Id = Guid.NewGuid();
        _context.ApplicationMappings.Add(mapping);
        await _context.SaveChangesAsync();
        return mapping;
    }

    public async Task<ApplicationMapping> UpdateApplicationMappingAsync(ApplicationMapping mapping)
    {
        _context.ApplicationMappings.Update(mapping);
        await _context.SaveChangesAsync();
        return mapping;
    }

    public async Task DeleteApplicationMappingAsync(Guid id)
    {
        var mapping = await _context.ApplicationMappings.FindAsync(id);
        if (mapping != null)
        {
            _context.ApplicationMappings.Remove(mapping);
            await _context.SaveChangesAsync();
        }
    }

    // --- Change Category Mappings ---

    public async Task<List<ChangeCategoryMapping>> GetChangeCategoryMappingsAsync()
    {
        return await _context.ChangeCategoryMappings.ToListAsync();
    }

    public async Task<ChangeCategoryMapping> AddChangeCategoryMappingAsync(ChangeCategoryMapping mapping)
    {
        mapping.Id = Guid.NewGuid();
        _context.ChangeCategoryMappings.Add(mapping);
        await _context.SaveChangesAsync();
        return mapping;
    }

    public async Task<ChangeCategoryMapping> UpdateChangeCategoryMappingAsync(ChangeCategoryMapping mapping)
    {
        _context.ChangeCategoryMappings.Update(mapping);
        await _context.SaveChangesAsync();
        return mapping;
    }

    public async Task DeleteChangeCategoryMappingAsync(Guid id)
    {
        var mapping = await _context.ChangeCategoryMappings.FindAsync(id);
        if (mapping != null)
        {
            _context.ChangeCategoryMappings.Remove(mapping);
            await _context.SaveChangesAsync();
        }
    }
}
