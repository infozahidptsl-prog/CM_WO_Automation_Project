using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WO_CM_Review_Automation.Application.Import.Models;

namespace WO_CM_Review_Automation.Application.Import.Interfaces;

public interface IImportPipeline<TEntity> where TEntity : class
{
    Task<(ImportSession Session, List<TEntity> Entities)> ProcessAsync(Stream fileStream, string fileName, string uploadedBy);
}
