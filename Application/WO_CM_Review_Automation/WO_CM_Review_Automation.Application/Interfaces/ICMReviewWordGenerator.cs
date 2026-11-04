using System.Threading.Tasks;
using WO_CM_Review_Automation.Application.ViewModels.CMReview;

namespace WO_CM_Review_Automation.Application.Interfaces;

public interface ICMReviewWordGenerator
{
    Task<byte[]> GenerateWordDocumentAsync(CMReviewDocumentViewModel documentModel);
}
