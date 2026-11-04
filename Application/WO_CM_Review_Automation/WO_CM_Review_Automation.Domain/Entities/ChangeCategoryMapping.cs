using System;

namespace WO_CM_Review_Automation.Domain.Entities
{
    public class ChangeCategoryMapping : BaseAuditableEntity
    {
        public string ApplicationName { get; set; } = string.Empty;
        public string ChangeType { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
