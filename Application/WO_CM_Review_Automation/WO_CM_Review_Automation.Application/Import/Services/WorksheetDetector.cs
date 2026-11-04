using ClosedXML.Excel;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using WO_CM_Review_Automation.Application.Import.Interfaces;
using WO_CM_Review_Automation.Application.Import.Models;

namespace WO_CM_Review_Automation.Application.Import.Services;

public class WorksheetDetector : IWorksheetDetector
{
    private readonly IHeaderMappingService _headerMappingService;
    private readonly ImportEngineOptions _options;

    public WorksheetDetector(IHeaderMappingService headerMappingService, IOptions<ImportEngineOptions> options)
    {
        _headerMappingService = headerMappingService;
        _options = options.Value;
    }

    public (IXLWorksheet Worksheet, int HeaderRowIndex, double Confidence) DetectBestWorksheet(IXLWorkbook workbook)
    {
        IXLWorksheet bestWorksheet = null;
        int bestHeaderRowIndex = -1;
        double bestConfidence = -1.0;

        foreach (var worksheet in workbook.Worksheets)
        {
            var (headerRow, confidence) = DetectHeaderRow(worksheet);
            if (confidence > bestConfidence)
            {
                bestConfidence = confidence;
                bestHeaderRowIndex = headerRow;
                bestWorksheet = worksheet;
            }
        }

        return (bestWorksheet, bestHeaderRowIndex, bestConfidence);
    }

    public (int HeaderRowIndex, double Confidence) DetectHeaderRow(IXLWorksheet worksheet)
    {
        int bestHeaderRowIndex = -1;
        double bestConfidence = -1.0;

        // Skip empty worksheets
        if (worksheet.LastRowUsed() == null) return (-1, -1.0);

        int maxScanRows = _options.MaxHeaderScanRows;
        
        for (int r = 1; r <= maxScanRows; r++)
        {
            var row = worksheet.Row(r);
            if (row.IsEmpty()) continue;

            var rawHeaders = new List<string>();
            int firstCol = row.FirstCellUsed()?.Address.ColumnNumber ?? 1;
            int lastCol = row.LastCellUsed()?.Address.ColumnNumber ?? 1;
            
            for (int c = firstCol; c <= lastCol; c++)
            {
                rawHeaders.Add(row.Cell(c).GetString());
            }

            double confidence = _headerMappingService.CalculateConfidence(rawHeaders);

            // Give slight bonus for having more populated rows below it (heuristic)
            int rowsPopulated = worksheet.LastRowUsed().RowNumber() - r;
            if (rowsPopulated > 0)
            {
                // Add tiny fraction so if two sheets have equal confidence, the one with more data wins
                confidence += Math.Min(0.05, rowsPopulated * 0.0001); 
            }
            
            confidence = Math.Min(1.0, confidence); // Cap at 100%

            if (confidence > bestConfidence)
            {
                bestConfidence = confidence;
                bestHeaderRowIndex = r;
            }
        }

        return (bestHeaderRowIndex, bestConfidence);
    }
}
