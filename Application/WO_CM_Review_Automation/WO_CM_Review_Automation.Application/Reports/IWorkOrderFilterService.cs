using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WO_CM_Review_Automation.Domain.Entities;

namespace WO_CM_Review_Automation.Application.Reports;

public interface IWorkOrderFilterService
{
    Task<List<WorkOrder>> GetFilteredWorkOrdersAsync(Guid uploadHistoryId);
}
