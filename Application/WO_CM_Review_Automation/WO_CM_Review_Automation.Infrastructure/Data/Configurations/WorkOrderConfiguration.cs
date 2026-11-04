using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WO_CM_Review_Automation.Domain.Entities;

namespace WO_CM_Review_Automation.Infrastructure.Data.Configurations;

public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.HasIndex(x => new { x.UploadHistoryId, x.WoId }).IsUnique();
        builder.HasIndex(x => x.WoId);
        builder.HasIndex(x => x.ApprovalDate);
        builder.HasIndex(x => x.Vendor);
        builder.HasIndex(x => x.Issuer);
        builder.HasIndex(x => x.LmApprover);
        builder.HasIndex(x => x.Domain);
        
        builder.HasOne(x => x.UploadHistory)
               .WithMany(x => x.WorkOrders)
               .HasForeignKey(x => x.UploadHistoryId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
