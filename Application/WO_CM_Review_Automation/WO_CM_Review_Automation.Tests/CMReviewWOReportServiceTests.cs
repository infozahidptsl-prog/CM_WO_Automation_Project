using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using WO_CM_Review_Automation.Domain.Entities;
using WO_CM_Review_Automation.Infrastructure.Data;
using WO_CM_Review_Automation.Infrastructure.Services.CMReview;
using WO_CM_Review_Automation.Infrastructure.Services;
using WO_CM_Review_Automation.Application.Interfaces;

namespace WO_CM_Review_Automation.Tests;

public class CMReviewWOReportServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly ICMReviewWOReportService _service;

    public CMReviewWOReportServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _context = new ApplicationDbContext(options);
        
        // Mock the IWorkOrderFilterService logic by instantiating the real one if we can
        var filterService = new WorkOrderFilterService(_context); 
        
        _service = new CMReviewWOReportService(_context, filterService);
    }

    [Fact]
    public async Task GetWOReportItemsAsync_ShouldReturnFilteredWOs_MappingCorrectly()
    {
        // Arrange
        var appName = "Test Application";
        var year = 2026;
        var month = 8;
        var historyId = Guid.NewGuid();

        _context.AllowedLMApprovers.Add(new AllowedLMApprover { Id = Guid.NewGuid(), Name = "Approved by LM" });
        _context.AllowedVendors.Add(new AllowedVendor { Id = Guid.NewGuid(), Name = "Vendor A" });
        _context.ApplicationMappings.Add(new ApplicationMapping { Id = Guid.NewGuid(), NormalizedName = appName, AliasName = "Test App Alias", IsActive = true });
        
        _context.UploadHistories.Add(new UploadHistory { Id = historyId, FileName = "test.xlsx", UploadedBy = "tester", UploadDate = DateTime.Now });

        _context.WorkOrders.AddRange(new List<WorkOrder>
        {
            new WorkOrder
            {
                Id = Guid.NewGuid(),
                UploadHistoryId = historyId,
                WoId = "WO-001",
                ServiceName = appName,
                ApprovalDate = new DateTime(year, month, 5).ToString("yyyy-MM-dd"),
                Status = "Closed",
                Domain = "Infrastructure",
                Issuer = "John",
                LmApprover = "Approved by LM",
                Vendor = "Vendor A"
            },
            new WorkOrder
            {
                Id = Guid.NewGuid(),
                UploadHistoryId = historyId,
                WoId = "WO-002",
                ServiceName = appName,
                ApprovalDate = new DateTime(year, month, 15).ToString("yyyy-MM-dd"),
                Status = "Open", // Should be filtered out by WorkOrderFilterService logic
                Domain = "Infra",
                Issuer = "Jane"
            },
            new WorkOrder
            {
                Id = Guid.NewGuid(),
                UploadHistoryId = historyId,
                WoId = "WO-003",
                ServiceName = "Other App", // Wrong app
                ApprovalDate = new DateTime(year, month, 5).ToString("yyyy-MM-dd"),
                Status = "Closed",
                Domain = "Infra",
                Issuer = "Bob",
                LmApprover = "Approved by LM"
            }
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GenerateWOReportItemsAsync(Guid.NewGuid(), appName, year, month);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result); // Only WO-001 should pass filters (Closed, correct App, correct Date)
        
        var item = result.First();
        Assert.Equal("WO-001", item.WoId);
        Assert.Equal("Closed", item.Status);
        Assert.Equal("Approved by LM", item.Assignee);
    }
}
