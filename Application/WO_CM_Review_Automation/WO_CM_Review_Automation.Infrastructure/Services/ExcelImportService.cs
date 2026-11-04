using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WO_CM_Review_Automation.Application.ExcelImport;
using WO_CM_Review_Automation.Application.Import.Interfaces;
using WO_CM_Review_Automation.Application.Import.Models;
using WO_CM_Review_Automation.Domain.Entities;
using WO_CM_Review_Automation.Infrastructure.Data;

namespace WO_CM_Review_Automation.Infrastructure.Services;

public class ExcelImportService : IExcelImportService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly IImportPipeline<WorkOrder> _importPipeline;

    public ExcelImportService(
        ApplicationDbContext context, 
        IMemoryCache cache,
        IImportPipeline<WorkOrder> importPipeline)
    {
        _context = context;
        _cache = cache;
        _importPipeline = importPipeline;
    }

    public async Task<ImportSession> GeneratePreviewAsync(Stream fileStream, string fileName, string uploadedBy)
    {
        var (session, entities) = await _importPipeline.ProcessAsync(fileStream, fileName, uploadedBy);

        // Check duplicates inside the batch
        var seenWoIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicatesInBatch = new List<WorkOrder>();
        var validWorkOrders = new List<WorkOrder>();

        foreach (var entity in entities)
        {
            if (string.IsNullOrWhiteSpace(entity.WoId)) continue;

            if (seenWoIds.Contains(entity.WoId))
            {
                session.DuplicateRows++;
                session.Diagnostics.WarningsAndErrors.Add(new ValidationMessage 
                { 
                    Level = ValidationLevel.Warning, 
                    Message = $"Duplicate WO ID '{entity.WoId}' found in this file.",
                    ColumnName = "WO ID"
                });
            }
            else
            {
                seenWoIds.Add(entity.WoId);
                validWorkOrders.Add(entity);
            }
        }

        // Cache the session AND the parsed entities
        if (!session.Diagnostics.HasCriticalErrors && session.Status != "Failed: Exception")
        {
            _cache.Set($"Session_{session.SessionId}", session, TimeSpan.FromMinutes(15));
            _cache.Set($"Entities_{session.SessionId}", validWorkOrders, TimeSpan.FromMinutes(15));
        }

        return session;
    }

    public async Task<ImportSummaryResult> ConfirmImportAsync(Guid previewId)
    {
        if (!_cache.TryGetValue($"Session_{previewId}", out ImportSession? session) || session == null ||
            !_cache.TryGetValue($"Entities_{previewId}", out List<WorkOrder>? validWorkOrders) || validWorkOrders == null)
        {
            throw new InvalidOperationException("Preview session expired or not found. Please upload again.");
        }

        var sw = Stopwatch.StartNew();

        var history = new UploadHistory
        {
            Id = Guid.NewGuid(),
            FileName = session.FileName,
            UploadDate = session.UploadedAt,
            UploadedBy = session.UploadedBy,
            ImportStarted = DateTimeOffset.UtcNow,
            TotalRows = session.TotalRows,
            MatchingRows = 0, // Used by processing engine later
            InvalidRows = session.InvalidRows,
            UploadStatus = "Completed",
            FileSize = 0
        };

        foreach (var wo in validWorkOrders)
        {
            wo.UploadHistoryId = history.Id;
            wo.Id = Guid.NewGuid();
        }

        history.ImportCompleted = DateTimeOffset.UtcNow;
        history.ProcessingTime = sw.Elapsed;

        _context.UploadHistories.Add(history);
        
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var chunkedWorkOrders = validWorkOrders.Chunk(1000);
            foreach (var chunk in chunkedWorkOrders)
            {
                await _context.WorkOrders.AddRangeAsync(chunk);
                await _context.SaveChangesAsync();
            }
            
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException($"Import failed during database transaction: {ex.Message}", ex);
        }

        _cache.Remove($"Session_{previewId}");
        _cache.Remove($"Entities_{previewId}");
        
        sw.Stop();

        return new ImportSummaryResult
        {
            UploadHistoryId = history.Id,
            FileName = history.FileName,
            ImportDate = history.ImportCompleted.Value,
            ImportedRecords = validWorkOrders.Count,
            InvalidRecords = session.InvalidRows,
            DuplicateRecords = session.DuplicateRows,
            ProcessingTime = sw.Elapsed
        };
    }

    public void DiscardPreview(Guid previewId)
    {
        _cache.Remove($"Session_{previewId}");
        _cache.Remove($"Entities_{previewId}");
    }
}
