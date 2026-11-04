using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WO_CM_Review_Automation.Domain.Entities;

namespace WO_CM_Review_Automation.Application.Processing;

public interface IProcessingEngine
{
    Task<ProcessingResult> ProcessBatchAsync(Guid uploadHistoryId);
    ProcessingResult ProcessWorkOrders(IEnumerable<WorkOrder> rawWorkOrders, ProcessingConfiguration config);
}
