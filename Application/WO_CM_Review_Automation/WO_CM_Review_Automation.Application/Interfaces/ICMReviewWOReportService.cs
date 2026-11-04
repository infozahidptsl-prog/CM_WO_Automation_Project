using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WO_CM_Review_Automation.Domain.Entities.CMReview;

namespace WO_CM_Review_Automation.Application.Interfaces;

public interface ICMReviewWOReportService
{
    Task<List<CMReviewWOReportItem>> GenerateWOReportItemsAsync(Guid documentId, string application, int year, int month);
}
