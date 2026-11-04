using System.Collections.Generic;
using WO_CM_Review_Automation.Application.Reports;
using WO_CM_Review_Automation.Domain.Entities;

namespace WO_CM_Review_Automation.Application.Interfaces;

public interface ICMReviewEvidenceImageGenerator
{
    byte[] GenerateSummaryTableImage(string monthYear, int totalRawWorkOrders, List<ChangeManagementReportItem> items, string highlightedApp);
    byte[] GenerateFilteredExcelImage(List<WorkOrder> filteredWorkOrders, HashSet<string> appWorkOrderIds);
}
