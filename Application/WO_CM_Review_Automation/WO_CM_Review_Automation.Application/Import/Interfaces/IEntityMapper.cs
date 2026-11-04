using ClosedXML.Excel;
using System.Collections.Generic;
using WO_CM_Review_Automation.Application.Import.Models;

namespace WO_CM_Review_Automation.Application.Import.Interfaces;

public interface IEntityMapper<TEntity> where TEntity : class
{
    TEntity MapRow(IXLRow row, Dictionary<string, int> columnMap, ImportSession session);
}
