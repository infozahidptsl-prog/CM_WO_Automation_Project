using System.Collections.Generic;
using WO_CM_Review_Automation.Application.Import.Models;

namespace WO_CM_Review_Automation.Application.Import.Interfaces;

public interface IHeaderMappingService
{
    string NormalizeHeader(string rawHeader);
    ColumnMappingResult MapColumn(string rawHeader);
    double CalculateConfidence(List<string> rawHeaders);
}
