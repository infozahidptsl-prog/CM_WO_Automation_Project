using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WO_CM_Review_Automation.Domain.Entities;
using WO_CM_Review_Automation.Domain.Entities.CMReview;
using WO_CM_Review_Automation.Infrastructure.Identity;

namespace WO_CM_Review_Automation.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<UploadHistory> UploadHistories => Set<UploadHistory>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<ApplicationMapping> ApplicationMappings => Set<ApplicationMapping>();
    public DbSet<AllowedLMApprover> AllowedLMApprovers => Set<AllowedLMApprover>();
    public DbSet<AllowedVendor> AllowedVendors => Set<AllowedVendor>();
    public DbSet<AllowedIssuer> AllowedIssuers => Set<AllowedIssuer>();
    public DbSet<AllowedDomain> AllowedDomains => Set<AllowedDomain>();
    public DbSet<AllowedIssuerOffice> AllowedIssuerOffices => Set<AllowedIssuerOffice>();

    public DbSet<DivisionMapping> DivisionMappings => Set<DivisionMapping>();
    public DbSet<FunctionalUnitMapping> FunctionalUnitMappings => Set<FunctionalUnitMapping>();
    public DbSet<TeamLeaderMapping> TeamLeaderMappings => Set<TeamLeaderMapping>();
    public DbSet<ChangeCategoryMapping> ChangeCategoryMappings => Set<ChangeCategoryMapping>();

    // CM Review Document Preparation
    public DbSet<CMReviewDocument> CMReviewDocuments => Set<CMReviewDocument>();
    public DbSet<CMReviewServerEvidence> CMReviewServerEvidences => Set<CMReviewServerEvidence>();
    public DbSet<CMReviewScreenshot> CMReviewScreenshots => Set<CMReviewScreenshot>();
    public DbSet<CMReviewChangeCategory> CMReviewChangeCategories => Set<CMReviewChangeCategory>();
    public DbSet<CMReviewChangeDetail> CMReviewChangeDetails => Set<CMReviewChangeDetail>();
    public DbSet<CMReviewDeploymentEvidence> CMReviewDeploymentEvidences => Set<CMReviewDeploymentEvidence>();
    public DbSet<CMReviewWOReportItem> CMReviewWOReportItems => Set<CMReviewWOReportItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        builder.SeedData();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedDate = DateTimeOffset.UtcNow;
                    // CreatedBy will be populated by a separate Interceptor or service in real scenario
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedDate = DateTimeOffset.UtcNow;
                    break;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
