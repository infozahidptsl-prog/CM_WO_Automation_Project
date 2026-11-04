using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using WO_CM_Review_Automation.Application.Import.Interfaces;
using WO_CM_Review_Automation.Application.Import.Models;
using WO_CM_Review_Automation.Domain.Entities;

namespace WO_CM_Review_Automation.Application.Import.WorkOrderImpl;

public class WorkOrderEntityMapper : IEntityMapper<WorkOrder>
{
    public WorkOrder MapRow(IXLRow row, Dictionary<string, int> columnMap, ImportSession session)
    {
        var wo = new WorkOrder();

        wo.WoId = GetStringValue(row, columnMap, "WO ID");
        wo.ServiceName = GetStringValue(row, columnMap, "Service Name");
        wo.Vendor = GetStringValue(row, columnMap, "Vendor");
        wo.Issuer = GetStringValue(row, columnMap, "Issuer");
        wo.IssuerOffice = GetStringValue(row, columnMap, "Issuer Office");
        wo.LmApprover = GetStringValue(row, columnMap, "LM Approver");
        wo.Status = GetStringValue(row, columnMap, "Status");
        wo.Domain = GetStringValue(row, columnMap, "Domain");
        
        wo.Subject = GetStringValue(row, columnMap, "Subject");
        wo.WoType = GetStringValue(row, columnMap, "WO Type");
        wo.ChangeType = GetStringValue(row, columnMap, "Change Type");
        wo.NeType = GetStringValue(row, columnMap, "NE Type");
        
        var neCountVal = GetCellValue(row, columnMap, "NE Count");
        if (neCountVal.IsNumber) wo.NeCount = (int)neCountVal.GetNumber();
        else if (neCountVal.IsText && int.TryParse(neCountVal.GetText(), out var neCount)) wo.NeCount = neCount;

        wo.QaKpiCheckedBy = GetStringValue(row, columnMap, "QA/KPI Checked By");
        wo.Assignee = GetStringValue(row, columnMap, "Assignee");
        wo.Sla = GetStringValue(row, columnMap, "SLA");

        wo.IssueDateTime = GetStringValue(row, columnMap, "Issue Date/Time");
        wo.ProposedDateTime = GetStringValue(row, columnMap, "Proposed Date/Time");
        wo.ExecutionStart = GetStringValue(row, columnMap, "Execution Start");
        wo.ExecutionEnd = GetStringValue(row, columnMap, "Execution End");
        
        // Handle dates properly
        wo.ApprovalDate = GetStringValue(row, columnMap, "Approval Date");

        return wo;
    }

    private DateTime? ParseDateSafe(XLCellValue dateVal)
    {
        if (dateVal.IsDateTime)
        {
            return dateVal.GetDateTime();
        }
        else if (dateVal.IsNumber)
        {
            return DateTime.FromOADate(dateVal.GetNumber());
        }
        else if (dateVal.IsText)
        {
            if (DateTime.TryParse(dateVal.GetText(), out var dt))
            {
                return dt;
            }
        }
        return null;
    }

    private string GetStringValue(IXLRow row, Dictionary<string, int> columnMap, string canonicalName)
    {
        if (columnMap.TryGetValue(canonicalName, out int colIndex))
        {
            return row.Cell(colIndex).GetFormattedString().Trim();
        }
        return string.Empty;
    }

    private XLCellValue GetCellValue(IXLRow row, Dictionary<string, int> columnMap, string canonicalName)
    {
        if (columnMap.TryGetValue(canonicalName, out int colIndex))
        {
            return row.Cell(colIndex).Value;
        }
        return Blank.Value;
    }
}
