using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WO_CM_Review_Automation.Domain.Entities.CMReview;

namespace WO_CM_Review_Automation.Application.Interfaces;

public interface ICMReviewDocumentService
{
    Task<CMReviewDocument> CreateDocumentAsync(string application, int year, int month);
    Task<CMReviewDocument?> GetDocumentAsync(Guid documentId);
    Task<CMReviewDocument?> GetDocumentByPeriodAsync(string application, int year, int month);
    Task SaveDraftAsync(CMReviewDocument document);
    Task<CMReviewDocument> DuplicatePreviousMonthAsync(string application, int currentYear, int currentMonth);
    Task FinalizeDocumentAsync(Guid documentId, string user);
    Task<List<CMReviewDocument>> GetAllDocumentsAsync();
    void MarkNewEntityAsAdded(object entity);
}
