using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WO_CM_Review_Automation.Application.Interfaces;
using WO_CM_Review_Automation.Domain.Entities.CMReview;
using WO_CM_Review_Automation.Infrastructure.Data;

namespace WO_CM_Review_Automation.Infrastructure.Services.CMReview;

public class CMReviewDocumentService : ICMReviewDocumentService
{
    private readonly ApplicationDbContext _context;

    public CMReviewDocumentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CMReviewDocument> CreateDocumentAsync(string application, int year, int month)
    {
        var existing = await GetDocumentByPeriodAsync(application, year, month);
        if (existing != null)
        {
            return existing;
        }

        var doc = new CMReviewDocument
        {
            Application = application,
            ReportingYear = year,
            ReportingMonth = month,
            Status = CMReviewDocumentStatus.Draft,
            VerifierName = "Md. Amirul Islam",
            VerifierDesignation = "Core Business Application and Service Senior Manager",
            VerifierDepartment = "Business Support System, Technology",
            VerifierOrganization = "Banglalink"
        };

        string[] categoryNames;
        
        if (application.Equals("Sales Force App", StringComparison.OrdinalIgnoreCase))
        {
            categoryNames = new string[]
            {
                "Re-factoring existing application for performance, scalability or other issues",
                "Apply ad-hoc enhancement request in existing features",
                "Implementation of Change Request- OneSalesApp",
                "Fixation of bug on OneSalesApp",
                "Modification of Application Existing Features- OneSalesApp",
                "Deploy new application/module",
                "Standard Change",
                "Business Logic -OneSalesApp"
            };
        }
        else
        {
            categoryNames = new string[]
            {
                "Re-factoring existing application for performance, scalability or other issues",
                "Apply ad-hoc enhancement request in existing features",
                "Bug Fixing",
                "Standard Change",
                "Implementation of Change Request",
                "Data reconciliation for user or system data",
                "Add new features/module in existing application",
                "Deploy new application / Modules",
                "Software install/remove/configure in server"
            };
        }

        for(int i = 0; i < categoryNames.Length; i++)
        {
            doc.ChangeCategories.Add(new CMReviewChangeCategory
            {
                CategoryName = categoryNames[i],
                DisplayOrder = i,
                HasChanges = false // Default to no changes as requested
            });
        }

        _context.CMReviewDocuments.Add(doc);
        await _context.SaveChangesAsync();
        return doc;
    }

    public async Task<CMReviewDocument?> GetDocumentAsync(Guid documentId)
    {
        return await _context.CMReviewDocuments
            .Include(d => d.ServerEvidences)
                .ThenInclude(se => se.Screenshots)
            .Include(d => d.ChangeCategories)
                .ThenInclude(cc => cc.ChangeDetails)
            .Include(d => d.DeploymentEvidences)
                .ThenInclude(de => de.Screenshots)
            .Include(d => d.WOReportItems)
            .FirstOrDefaultAsync(d => d.Id == documentId);
    }

    public async Task<CMReviewDocument?> GetDocumentByPeriodAsync(string application, int year, int month)
    {
        return await _context.CMReviewDocuments
            .Include(d => d.ServerEvidences)
                .ThenInclude(se => se.Screenshots)
            .Include(d => d.ChangeCategories)
                .ThenInclude(cc => cc.ChangeDetails)
            .Include(d => d.DeploymentEvidences)
                .ThenInclude(de => de.Screenshots)
            .Include(d => d.WOReportItems)
            .FirstOrDefaultAsync(d => d.Application == application && d.ReportingYear == year && d.ReportingMonth == month);
    }

    public async Task SaveDraftAsync(CMReviewDocument document)
    {
        var existing = await _context.CMReviewDocuments.FindAsync(document.Id);
        if (existing == null) throw new InvalidOperationException("Document not found");

        if (existing.Status == CMReviewDocumentStatus.Finalized)
        {
            throw new InvalidOperationException("Cannot modify a finalized document.");
        }

        // Just calling SaveChanges because EF tracks the object graph assuming it was retrieved via context.
        await _context.SaveChangesAsync();
    }

    public async Task<CMReviewDocument> DuplicatePreviousMonthAsync(string application, int currentYear, int currentMonth)
    {
        // For duplication, calculate previous month
        var prevMonth = currentMonth == 1 ? 12 : currentMonth - 1;
        var prevYear = currentMonth == 1 ? currentYear - 1 : currentYear;

        var prevDoc = await GetDocumentByPeriodAsync(application, prevYear, prevMonth);
        if (prevDoc == null)
        {
            throw new InvalidOperationException("No document found for previous month to duplicate.");
        }

        var newDoc = await CreateDocumentAsync(application, currentYear, currentMonth);

        // Copy Server structures
        foreach(var server in prevDoc.ServerEvidences)
        {
            newDoc.ServerEvidences.Add(new CMReviewServerEvidence
            {
                ServerIp = server.ServerIp,
                ServerName = server.ServerName,
                DisplayOrder = server.DisplayOrder,
                Remarks = server.Remarks
                // No screenshots copied
            });
        }

        // Copy Categories
        foreach(var cat in prevDoc.ChangeCategories)
        {
            newDoc.ChangeCategories.Add(new CMReviewChangeCategory
            {
                CategoryName = cat.CategoryName,
                DisplayOrder = cat.DisplayOrder,
                HasChanges = false // Reset
            });
        }

        newDoc.VerifierName = prevDoc.VerifierName;
        newDoc.VerifierDepartment = prevDoc.VerifierDepartment;
        newDoc.VerifierDesignation = prevDoc.VerifierDesignation;
        newDoc.VerifierOrganization = prevDoc.VerifierOrganization;

        await _context.SaveChangesAsync();
        return newDoc;
    }

    public async Task FinalizeDocumentAsync(Guid documentId, string user)
    {
        var existing = await _context.CMReviewDocuments.FindAsync(documentId);
        if (existing == null) throw new InvalidOperationException("Document not found");

        existing.Status = CMReviewDocumentStatus.Finalized;
        existing.FinalizedBy = user;
        existing.FinalizedDate = DateTimeOffset.UtcNow;
        
        await _context.SaveChangesAsync();
    }

    public async Task<List<CMReviewDocument>> GetAllDocumentsAsync()
    {
        return await _context.CMReviewDocuments
            .OrderByDescending(d => d.ReportingYear)
            .ThenByDescending(d => d.ReportingMonth)
            .ToListAsync();
    }

    public void MarkNewEntityAsAdded(object entity)
    {
        _context.Add(entity);
    }
}
