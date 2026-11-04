using System;

namespace WO_CM_Review_Automation.Application.Processing;

public class ProcessedWorkOrder
{
    public Guid WorkOrderId { get; set; }
    public string WoId { get; set; } = string.Empty;
    public string NormalizedApplicationName { get; set; } = string.Empty;
    public string OriginalServiceName { get; set; } = string.Empty;
    public string? ApprovalDate { get; set; }
    public string Vendor { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string LmApprover { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
