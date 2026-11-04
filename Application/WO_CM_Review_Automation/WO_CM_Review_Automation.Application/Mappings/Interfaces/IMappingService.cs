using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WO_CM_Review_Automation.Domain.Entities;

namespace WO_CM_Review_Automation.Application.Mappings.Interfaces;

public interface IMappingService
{
    // Functional Unit Mappings
    Task<List<FunctionalUnitMapping>> GetFunctionalUnitMappingsAsync();
    Task<FunctionalUnitMapping> AddFunctionalUnitMappingAsync(FunctionalUnitMapping mapping);
    Task<FunctionalUnitMapping> UpdateFunctionalUnitMappingAsync(FunctionalUnitMapping mapping);
    Task DeleteFunctionalUnitMappingAsync(Guid id);

    // Team Leader Mappings
    Task<List<TeamLeaderMapping>> GetTeamLeaderMappingsAsync();
    Task<TeamLeaderMapping> AddTeamLeaderMappingAsync(TeamLeaderMapping mapping);
    Task<TeamLeaderMapping> UpdateTeamLeaderMappingAsync(TeamLeaderMapping mapping);
    Task DeleteTeamLeaderMappingAsync(Guid id);

    // Application Mappings
    Task<List<ApplicationMapping>> GetApplicationMappingsAsync();
    Task<ApplicationMapping> AddApplicationMappingAsync(ApplicationMapping mapping);
    Task<ApplicationMapping> UpdateApplicationMappingAsync(ApplicationMapping mapping);
    Task DeleteApplicationMappingAsync(Guid id);

    // Change Category Mappings
    Task<List<ChangeCategoryMapping>> GetChangeCategoryMappingsAsync();
    Task<ChangeCategoryMapping> AddChangeCategoryMappingAsync(ChangeCategoryMapping mapping);
    Task<ChangeCategoryMapping> UpdateChangeCategoryMappingAsync(ChangeCategoryMapping mapping);
    Task DeleteChangeCategoryMappingAsync(Guid id);
}
