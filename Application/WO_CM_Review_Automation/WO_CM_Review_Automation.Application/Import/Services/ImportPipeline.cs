using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WO_CM_Review_Automation.Application.Import.Interfaces;
using WO_CM_Review_Automation.Application.Import.Models;

namespace WO_CM_Review_Automation.Application.Import.Services;

public class ImportPipeline<TEntity> : IImportPipeline<TEntity> where TEntity : class
{
    private readonly ILogger<ImportPipeline<TEntity>> _logger;
    private readonly IHeaderMappingService _headerMappingService;
    private readonly IWorksheetDetector _worksheetDetector;
    private readonly IEntityMapper<TEntity> _entityMapper;
    private readonly IRowValidator<TEntity> _rowValidator;
    private readonly ImportEngineOptions _options;

    // A callback or repository service should ideally handle the persistence
    // For this generic pipeline, we will just return the valid entities.
    // The caller will handle persistence.
    // However, to keep it generic without overcomplicating, we'll return a tuple or attach entities to the session.
    // Wait, let's just make ImportSession return the state, and expose a delegate/func for persistence?
    // The user said: "Stage 8 Database Save. Every stage should have its own responsibility".
    // I'll make this pipeline return (ImportSession, List<TEntity>).

    public ImportPipeline(
        ILogger<ImportPipeline<TEntity>> logger,
        IHeaderMappingService headerMappingService,
        IWorksheetDetector worksheetDetector,
        IEntityMapper<TEntity> entityMapper,
        IRowValidator<TEntity> rowValidator,
        IOptions<ImportEngineOptions> options)
    {
        _logger = logger;
        _headerMappingService = headerMappingService;
        _worksheetDetector = worksheetDetector;
        _entityMapper = entityMapper;
        _rowValidator = rowValidator;
        _options = options.Value;
    }

    public async Task<(ImportSession Session, List<TEntity> Entities)> ProcessAsync(Stream fileStream, string fileName, string uploadedBy)
    {
        var stopwatch = Stopwatch.StartNew();
        var session = new ImportSession
        {
            SessionId = Guid.NewGuid(),
            FileName = fileName,
            WorkbookName = fileName,
            FileSize = fileStream.Length,
            UploadedBy = uploadedBy,
            UploadedAt = DateTime.UtcNow,
            Status = "Started"
        };
        session.Diagnostics.WorkbookName = fileName;
        var entities = new List<TEntity>();

        _logger.LogInformation("START Import Pipeline for {FileName}. SessionId: {SessionId}", fileName, session.SessionId);

        try
        {
            // Stage 1: Workbook Loaded (Support CSV)
            session.Status = "Stage 1: Workbook Loaded";
            
            XLWorkbook LoadWorkbook()
            {
                if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("CSV file detected, parsing into in-memory OpenXML Workbook...");
                    var wb = new XLWorkbook();
                    var sheet = wb.AddWorksheet("WO List");
                    
                    using var reader = new StreamReader(fileStream, System.Text.Encoding.UTF8, leaveOpen: true);
                    var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture) { HasHeaderRecord = false };
                    using var csv = new CsvHelper.CsvReader(reader, config);
                    
                    int r = 1;
                    while (csv.Read())
                    {
                        for (int c = 0; c < csv.Parser.Count; c++)
                        {
                            sheet.Cell(r, c + 1).Value = csv.Parser.Record?[c];
                        }
                        r++;
                    }
                    return wb;
                }
                return new XLWorkbook(fileStream);
            }

            using var workbook = LoadWorkbook();

            session.Diagnostics.TotalWorksheets = workbook.Worksheets.Count;
            _logger.LogInformation("Stage 1 Complete. Found {Count} worksheets.", workbook.Worksheets.Count);

            // Stage 2 & 3: Worksheet Selection & Header Detection
            session.Status = "Stage 2: Header Detection";
            
            // Strictly target 'WO List' sheet as requested
            IXLWorksheet? worksheet = null;
            if (workbook.TryGetWorksheet("WO List", out var exactSheet))
            {
                worksheet = exactSheet;
            }
            else
            {
                // Fallback to detection if explicitly named sheet is not found
                var detectionResult = _worksheetDetector.DetectBestWorksheet(workbook);
                worksheet = detectionResult.Worksheet;
            }

            var (headerRowIndex, confidence) = worksheet != null 
                ? _worksheetDetector.DetectHeaderRow(worksheet) 
                : (0, 0);

            if (worksheet == null || headerRowIndex < 1)
            {
                session.Status = "Failed: No valid worksheet found";
                session.Diagnostics.WarningsAndErrors.Add(new ValidationMessage
                {
                    Level = ValidationLevel.Critical,
                    Message = "Could not find 'WO List' worksheet or a worksheet containing the required headers."
                });
                return (session, entities);
            }

            session.WorkbookName = fileName;
            session.WorksheetName = worksheet.Name;
            session.HeaderRow = headerRowIndex;
            session.Diagnostics.WorksheetUsed = worksheet.Name;
            session.Diagnostics.HeaderRowIndex = headerRowIndex;
            session.Diagnostics.HeaderConfidence = confidence;
            
            _logger.LogInformation("Stage 2 & 3 Complete. Selected '{Worksheet}' with header at row {Row}. Confidence: {Confidence}", worksheet.Name, headerRowIndex, confidence);

            if (confidence < _options.HeaderConfidenceThreshold)
            {
                session.Status = "Failed: Low Confidence";
                session.Diagnostics.WarningsAndErrors.Add(new ValidationMessage
                {
                    Level = ValidationLevel.Critical,
                    Message = $"Header detection confidence ({confidence:P0}) is below the required threshold ({_options.HeaderConfidenceThreshold:P0})."
                });
                return (session, entities);
            }

            // Stage 4: Column Mapping
            session.Status = "Stage 4: Column Mapping";
            var headerRow = worksheet.Row(headerRowIndex);
            var columnMap = new Dictionary<string, int>(); // Canonical -> Column Index
            
            int firstCol = headerRow.FirstCellUsed()?.Address.ColumnNumber ?? 1;
            int lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 1;
            session.Diagnostics.TotalColumns = lastCol - firstCol + 1;

            for (int c = firstCol; c <= lastCol; c++)
            {
                string rawHeader = headerRow.Cell(c).GetString();
                if (string.IsNullOrWhiteSpace(rawHeader)) continue;

                var mappingResult = _headerMappingService.MapColumn(rawHeader);
                mappingResult.ColumnIndex = c;
                
                session.Diagnostics.MappingTable.Add(mappingResult);

                if (mappingResult.Classification != ColumnClassification.Unknown)
                {
                    if (!columnMap.ContainsKey(mappingResult.CanonicalHeader))
                    {
                        columnMap.Add(mappingResult.CanonicalHeader, c);
                    }
                    else
                    {
                        session.Diagnostics.DuplicateColumns.Add(rawHeader);
                    }
                }
            }

            // Check for missing required columns
            var requiredColumns = _options.Mappings.Where(m => m.Classification == ColumnClassification.Required).Select(m => m.CanonicalName);
            foreach (var req in requiredColumns)
            {
                if (!columnMap.ContainsKey(req))
                {
                    session.Diagnostics.MissingRequiredColumns.Add(req);
                    session.Diagnostics.WarningsAndErrors.Add(new ValidationMessage
                    {
                        Level = ValidationLevel.Critical,
                        Message = $"Missing required column: {req}"
                    });
                }
            }

            if (session.Diagnostics.HasCriticalErrors)
            {
                session.Status = "Failed: Missing Columns";
                _logger.LogError("Stage 4 Failed. Missing critical columns in Session {SessionId}", session.SessionId);
                return (session, entities);
            }

            _logger.LogInformation("Stage 4 Complete. Columns successfully mapped.");

            // Stage 5 & 6: Data Transformation & Validation
            session.Status = "Stage 5 & 6: Data Transformation";
            int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRowIndex;
            session.TotalRows = lastRow - headerRowIndex;
            session.Diagnostics.TotalRows = session.TotalRows;

            for (int r = headerRowIndex + 1; r <= lastRow; r++)
            {
                var row = worksheet.Row(r);
                if (row.IsEmpty()) continue;

                try
                {
                    // Map Entity
                    var entity = _entityMapper.MapRow(row, columnMap, session);
                    if (entity == null) continue; // Skipped empty row

                    // Validate
                    var validationMessages = _rowValidator.Validate(entity, r);
                    
                    if (validationMessages.Any(m => m.Level == ValidationLevel.Critical || m.Level == ValidationLevel.Error))
                    {
                        session.InvalidRows++;
                        session.Diagnostics.WarningsAndErrors.AddRange(validationMessages);
                    }
                    else
                    {
                        if (validationMessages.Any())
                        {
                            session.Diagnostics.WarningsAndErrors.AddRange(validationMessages);
                        }
                        session.ValidRows++;
                        entities.Add(entity);
                    }
                }
                catch (Exception ex)
                {
                    session.InvalidRows++;
                    session.Diagnostics.WarningsAndErrors.Add(new ValidationMessage
                    {
                        Level = ValidationLevel.Error,
                        RowIndex = r,
                        Message = $"Exception during transformation: {ex.Message}"
                    });
                }
            }

            _logger.LogInformation("Stage 5 & 6 Complete. Valid: {Valid}, Invalid: {Invalid}", session.ValidRows, session.InvalidRows);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FATAL ERROR in Import Pipeline for SessionId {SessionId}", session.SessionId);
            session.Status = "Failed: Exception";
            session.Diagnostics.WarningsAndErrors.Add(new ValidationMessage
            {
                Level = ValidationLevel.Critical,
                Message = $"Fatal error processing file: {ex.Message}"
            });
        }
        finally
        {
            stopwatch.Stop();
            session.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            if (session.Status != "Failed: Exception" && !session.Diagnostics.HasCriticalErrors)
            {
                session.Status = "Preview Ready";
            }
            _logger.LogInformation("Import Pipeline Finished in {ElapsedMs}ms. Status: {Status}", stopwatch.ElapsedMilliseconds, session.Status);
        }

        return (session, entities);
    }
}
