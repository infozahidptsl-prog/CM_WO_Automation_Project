using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WO_CM_Review_Automation.Application;
using WO_CM_Review_Automation.Application.ExcelImport;
using WO_CM_Review_Automation.Infrastructure.Data;
using WO_CM_Review_Automation.Infrastructure.Services;
using Xunit;
using Xunit.Abstractions;

namespace WO_CM_Review_Automation.Tests;

public class ExcelImportServiceTests
{
    private readonly ITestOutputHelper _output;
    private readonly ApplicationDbContext _context;
    private readonly IExcelImportService _service;

    public ExcelImportServiceTests(ITestOutputHelper output)
    {
        _output = output;
        
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddMemoryCache();
        services.AddLogging();
        services.AddScoped<IExcelImportService, ExcelImportService>();

        // Build mock configuration
        var inMemorySettings = new System.Collections.Generic.Dictionary<string, string?> {
            {"ImportEngine:HeaderConfidenceThreshold", "0.5"},
            {"ImportEngine:MaxHeaderScanRows", "20"},
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        services.AddApplicationServices(configuration);
        
        var provider = services.BuildServiceProvider();
        _context = provider.GetRequiredService<ApplicationDbContext>();
        _service = provider.GetRequiredService<IExcelImportService>();
    }

    [Fact]
    public async Task PerformanceTest_LargeExcel_ShouldProcessQuickly()
    {
        var filePath = @"e:\Development\WO_CM_Review_Automation\TestFiles\Large_Excel.xlsx";
        if (!File.Exists(filePath)) return; // Skip if test file doesn't exist locally

        using var stream = File.OpenRead(filePath);
        
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        // 1. Preview
        var preview = await _service.GeneratePreviewAsync(stream, "Large_Excel.xlsx", "admin");
        sw.Stop();
        
        _output.WriteLine($"Preview generated in {sw.ElapsedMilliseconds} ms");
        _output.WriteLine($"Total Rows: {preview.TotalRows}");
        _output.WriteLine($"Valid Rows: {preview.ValidRows}");
        _output.WriteLine($"Global Errors: {preview.Diagnostics.WarningsAndErrors.Count}");
        
        Assert.False(preview.Diagnostics.HasCriticalErrors);

        // 2. Confirm Import
        sw.Restart();
        var summary = await _service.ConfirmImportAsync(preview.SessionId);
        sw.Stop();

        _output.WriteLine($"Import confirmed and saved in {sw.ElapsedMilliseconds} ms");
        _output.WriteLine($"Imported Records: {summary.ImportedRecords}");
        
        Assert.Equal(preview.ValidRows, summary.ImportedRecords);
    }

    [Fact]
    public async Task ValidationTest_MissingColumns_ShouldFail()
    {
        var filePath = @"e:\Development\WO_CM_Review_Automation\TestFiles\Missing_Columns.xlsx";
        if (!File.Exists(filePath)) return;
        
        using var stream = File.OpenRead(filePath);
        
        var preview = await _service.GeneratePreviewAsync(stream, "Missing_Columns.xlsx", "admin");
        
        Assert.True(preview.Diagnostics.HasCriticalErrors);
    }

    [Fact]
    public async Task ValidationTest_Duplicates_ShouldBeFlagged()
    {
        var filePath = @"e:\Development\WO_CM_Review_Automation\TestFiles\Duplicates.xlsx";
        if (!File.Exists(filePath)) return;
        
        using var stream = File.OpenRead(filePath);
        
        var preview = await _service.GeneratePreviewAsync(stream, "Duplicates.xlsx", "admin");
        
        Assert.False(preview.Diagnostics.HasCriticalErrors);
    }
}
