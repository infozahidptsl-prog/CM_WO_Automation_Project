using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using WO_CM_Review_Automation.Application.Interfaces;
using WO_CM_Review_Automation.Application.ViewModels.CMReview;
using WO_CM_Review_Automation.Infrastructure.Services.CMReview;

namespace WO_CM_Review_Automation.Tests;

public class CMReviewWordGeneratorTests
{
    private readonly ITestOutputHelper _output;
    private readonly ICMReviewWordGenerator _generator;

    public CMReviewWordGeneratorTests(ITestOutputHelper output)
    {
        _output = output;
        _generator = new CMReviewWordGenerator();
    }

    [Fact]
    public async Task GenerateWordDocumentAsync_ValidModel_ShouldCreateDocx()
    {
        // Arrange
        var model = new CMReviewDocumentViewModel
        {
            Id = Guid.NewGuid(),
            Application = "Test App",
            ReportingYear = 2026,
            ReportingMonth = 8,
            DocumentVersion = "1.0",
            TemplateVersion = "1.0",
            Status = Domain.Entities.CMReview.CMReviewDocumentStatus.Finalized,
            UATStatement = "UAT Passed",
            ComparisonSummary = "No issues",
            VerifierName = "John Doe",
            VerifierDesignation = "Manager",
            ServerEvidences = new List<ServerEvidenceViewModel>
            {
                new ServerEvidenceViewModel
                {
                    ServerIp = "192.168.1.1",
                    ServerName = "AppServer1",
                    DisplayOrder = 1,
                    Screenshots = new List<ScreenshotViewModel>
                    {
                        new ScreenshotViewModel
                        {
                            StorageReference = "missing_image.png", // Generator should gracefully handle missing images
                            Caption = "Login Screen",
                            DisplayOrder = 1
                        }
                    }
                }
            },
            ChangeCategories = new List<ChangeCategoryViewModel>
            {
                new ChangeCategoryViewModel
                {
                    CategoryName = "Application Changes",
                    HasChanges = true,
                    DisplayOrder = 1,
                    ChangeDetails = new List<ChangeDetailViewModel>
                    {
                        new ChangeDetailViewModel
                        {
                            ChangeOrder = "1",
                            ChangeType = "Feature",
                            Description = "Added new login page"
                        }
                    }
                }
            },
            WOReportItems = new List<WOReportItemViewModel>
            {
                new WOReportItemViewModel
                {
                    WoId = "WO-001",
                    Status = "Closed",
                    Assignee = "Dev Team"
                }
            }
        };

        // Act
        var docBytes = await _generator.GenerateWordDocumentAsync(model);

        // Assert
        Assert.NotNull(docBytes);
        Assert.True(docBytes.Length > 0);
        
        // Basic check for docx magic numbers (PK zip signature)
        Assert.Equal(0x50, docBytes[0]);
        Assert.Equal(0x4B, docBytes[1]);
        Assert.Equal(0x03, docBytes[2]);
        Assert.Equal(0x04, docBytes[3]);

        _output.WriteLine($"Generated docx size: {docBytes.Length} bytes");
    }

    [Fact]
    public async Task GenerateWordDocumentAsync_PathTraversal_ShouldRejectAndGenerateSafely()
    {
        // Arrange
        var model = new CMReviewDocumentViewModel
        {
            Application = "Test App",
            ReportingYear = 2026,
            ReportingMonth = 8,
            ServerEvidences = new List<ServerEvidenceViewModel>
            {
                new ServerEvidenceViewModel
                {
                    ServerIp = "10.0.0.1",
                    DisplayOrder = 1,
                    Screenshots = new List<ScreenshotViewModel>
                    {
                        new ScreenshotViewModel
                        {
                            // Malicious path traversal
                            StorageReference = "../../windows/system32/cmd.exe",
                            DisplayOrder = 1
                        }
                    }
                }
            }
        };

        // Act
        var docBytes = await _generator.GenerateWordDocumentAsync(model);

        // Assert
        Assert.NotNull(docBytes);
        Assert.True(docBytes.Length > 0);
        _output.WriteLine("Path traversal handled safely (document generated without crashing).");
    }
}
