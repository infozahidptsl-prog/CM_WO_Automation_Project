using System;
using System.Threading.Tasks;

namespace WO_CM_Review_Automation.Application.Reports;

public interface IReportService
{
    Task<ReportViewModel> GetReportAsync(Guid uploadHistoryId);
    Task<byte[]> GenerateExcelAsync(Guid uploadHistoryId);
    Task<byte[]> GeneratePdfAsync(Guid uploadHistoryId);
}
