using System.Collections.Generic;
using WO_CM_Review_Automation.Application.Import.Interfaces;
using WO_CM_Review_Automation.Application.Import.Models;
using WO_CM_Review_Automation.Domain.Entities;

namespace WO_CM_Review_Automation.Application.Import.WorkOrderImpl;

public class WorkOrderRowValidator : IRowValidator<WorkOrder>
{
    public List<ValidationMessage> Validate(WorkOrder entity, int rowIndex)
    {
        var messages = new List<ValidationMessage>();

        if (string.IsNullOrWhiteSpace(entity.WoId))
        {
            messages.Add(new ValidationMessage { Level = ValidationLevel.Error, RowIndex = rowIndex, ColumnName = "WO ID", Message = "WO ID is empty" });
        }

        if (string.IsNullOrWhiteSpace(entity.Vendor))
        {
            messages.Add(new ValidationMessage { Level = ValidationLevel.Error, RowIndex = rowIndex, ColumnName = "Vendor", Message = "Vendor is empty" });
        }

        if (entity.ApprovalDate == default)
        {
            messages.Add(new ValidationMessage { Level = ValidationLevel.Error, RowIndex = rowIndex, ColumnName = "Approval Date", Message = "Approval Date is invalid or empty" });
        }

        // Additional domain validations could be added here
        
        return messages;
    }
}
