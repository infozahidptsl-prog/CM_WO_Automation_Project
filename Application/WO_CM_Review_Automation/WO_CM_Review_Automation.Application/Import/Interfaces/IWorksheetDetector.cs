using ClosedXML.Excel;
using WO_CM_Review_Automation.Application.Import.Models;

namespace WO_CM_Review_Automation.Application.Import.Interfaces;

public interface IWorksheetDetector
{
    (IXLWorksheet Worksheet, int HeaderRowIndex, double Confidence) DetectBestWorksheet(IXLWorkbook workbook);
    (int HeaderRowIndex, double Confidence) DetectHeaderRow(IXLWorksheet worksheet);
}
