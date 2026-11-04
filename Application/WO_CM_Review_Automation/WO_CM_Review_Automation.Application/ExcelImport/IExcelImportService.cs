using System;
using System.IO;
using System.Threading.Tasks;
using WO_CM_Review_Automation.Application.Import.Models;

namespace WO_CM_Review_Automation.Application.ExcelImport;

public interface IExcelImportService
{
    Task<ImportSession> GeneratePreviewAsync(Stream fileStream, string fileName, string uploadedBy);
    Task<ImportSummaryResult> ConfirmImportAsync(Guid previewId);
    void DiscardPreview(Guid previewId);
}
