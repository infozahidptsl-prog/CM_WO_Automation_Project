using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WO_CM_Review_Automation.Domain.Entities.CMReview;

namespace WO_CM_Review_Automation.Application.ViewModels.CMReview;

public class CMReviewDocumentViewModel
{
    public Guid Id { get; set; }
    
    [Required(ErrorMessage = "Application is required")]
    public string Application { get; set; } = string.Empty;

    [Required]
    [Range(2000, 2100)]
    public int ReportingYear { get; set; } = DateTime.Now.AddMonths(-1).Year;

    [Required]
    [Range(1, 12)]
    public int ReportingMonth { get; set; } = DateTime.Now.AddMonths(-1).Month;

    public string DocumentVersion { get; set; } = "1.0";
    public string TemplateVersion { get; set; } = "1.0";
    public CMReviewDocumentStatus Status { get; set; } = CMReviewDocumentStatus.Draft;

    public string? UATStatement { get; set; }
    public string? ComparisonSummary { get; set; }

    public string? VerifierName { get; set; }
    public string? VerifierDesignation { get; set; }
    public string? VerifierDepartment { get; set; }
    public string? VerifierOrganization { get; set; }

    public string? FinalizedBy { get; set; }
    public DateTimeOffset? FinalizedDate { get; set; }

    public List<ServerEvidenceViewModel> ServerEvidences { get; set; } = new();
    public List<ChangeCategoryViewModel> ChangeCategories { get; set; } = new();
    public List<DeploymentEvidenceViewModel> DeploymentEvidences { get; set; } = new();
    public List<WOReportItemViewModel> WOReportItems { get; set; } = new();
    
    public ScreenshotViewModel? SummaryTableScreenshot { get; set; }
    public ScreenshotViewModel? ExcelScreenshot { get; set; }
    public ScreenshotViewModel? SiemScreenshot { get; set; }
}

public class ServerEvidenceViewModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required(ErrorMessage = "Server IP is required")]
    public string ServerIp { get; set; } = string.Empty;
    public string? ServerName { get; set; }
    public int DisplayOrder { get; set; }
    public string? Remarks { get; set; }

    public List<ScreenshotViewModel> Screenshots { get; set; } = new();
}

public class ChangeCategoryViewModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required(ErrorMessage = "Category Name is required")]
    public string CategoryName { get; set; } = string.Empty;
    public bool HasChanges { get; set; }
    public int DisplayOrder { get; set; }

    public List<ChangeDetailViewModel> ChangeDetails { get; set; } = new();
}

public class ChangeDetailViewModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string ChangeOrder { get; set; } = string.Empty;
    public string? ChangeType { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? ScheduleDate { get; set; }
    public string? ServerInfo { get; set; }
    public string? Remarks { get; set; }

    public List<ScreenshotViewModel> DeploymentScreenshots { get; set; } = new();
}

public class DeploymentEvidenceViewModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string ServerIp { get; set; } = string.Empty;
    public string? Note { get; set; }
    public int DisplayOrder { get; set; }

    public List<ScreenshotViewModel> Screenshots { get; set; } = new();
}

public class ScreenshotViewModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    
    // Relative path where the image is stored
    public string StorageReference { get; set; } = string.Empty;
    
    // Temporary base64 string for immediate UI display/upload
    public string? Base64Data { get; set; }
    
    public string? Caption { get; set; }
    public string? Heading { get; set; }  // e.g. "Before Deployment Screenshot" or "After Deployment Screenshot"
    public string? ServerIp { get; set; } // The server IP this screenshot belongs to
    public string? ServerName { get; set; } // e.g. "SMSSchedulerLive" - extracted from explorer path bar
    public int DisplayOrder { get; set; }
    public bool IsBeforeDeployment { get; set; }
}

public class WOReportItemViewModel
{
    public Guid Id { get; set; }
    public string WoId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string AdvanceTemplate { get; set; } = string.Empty;
    public string WOType { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public string ExecutionEnd { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string NEType { get; set; } = string.Empty;
    public int NECount { get; set; }
    public string Vendors { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string IssuerOffice { get; set; } = string.Empty;
    public string CollaboratedBy { get; set; } = string.Empty;
    public string KPICheckedBy { get; set; } = string.Empty;
    public string AssigneeOffice { get; set; } = string.Empty;
    public string Assignee { get; set; } = string.Empty;
    public string SLAPercentage { get; set; } = string.Empty;
    public string IssueDateTime { get; set; } = string.Empty;
    public string ProposedExecutionWindows { get; set; } = string.Empty;
    public string ExecutionStart { get; set; } = string.Empty;
    public string ExecutionEndDate { get; set; } = string.Empty;
}
