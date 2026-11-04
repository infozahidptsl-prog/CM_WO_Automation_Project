using System.Collections.Generic;
using WO_CM_Review_Automation.Application.Import.Models;

namespace WO_CM_Review_Automation.Application.Import.Interfaces;

public interface IRowValidator<TEntity> where TEntity : class
{
    List<ValidationMessage> Validate(TEntity entity, int rowIndex);
}
