using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WO_CM_Review_Automation.Application.Reports;
using WO_CM_Review_Automation.Infrastructure.Data;
using WO_CM_Review_Automation.Infrastructure.Identity;
using WO_CM_Review_Automation.Infrastructure.Services;

namespace WO_CM_Review_Automation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddMemoryCache();
        services.AddScoped<WO_CM_Review_Automation.Application.ExcelImport.IExcelImportService, WO_CM_Review_Automation.Infrastructure.Services.ExcelImportService>();
        services.AddScoped<WO_CM_Review_Automation.Application.Processing.IProcessingEngine, WO_CM_Review_Automation.Infrastructure.Services.ProcessingEngine>();
        services.AddScoped<WO_CM_Review_Automation.Application.Reports.IReportService, WO_CM_Review_Automation.Infrastructure.Services.ReportService>();
        services.AddScoped<IWorkOrderFilterService, WorkOrderFilterService>();
        services.AddScoped<IChangeManagementReportService, ChangeManagementReportService>();
        
        // CM Review Document Preparation Services
        services.AddScoped<WO_CM_Review_Automation.Application.Interfaces.ICMReviewDocumentService, WO_CM_Review_Automation.Infrastructure.Services.CMReview.CMReviewDocumentService>();
        services.AddScoped<WO_CM_Review_Automation.Application.Interfaces.IImageStorageService, WO_CM_Review_Automation.Infrastructure.Services.CMReview.ImageStorageService>();
        services.AddScoped<WO_CM_Review_Automation.Application.Interfaces.ICMReviewWOReportService, WO_CM_Review_Automation.Infrastructure.Services.CMReview.CMReviewWOReportService>();
        services.AddScoped<WO_CM_Review_Automation.Application.Interfaces.ICMReviewWordGenerator, WO_CM_Review_Automation.Infrastructure.Services.CMReview.CMReviewWordGenerator>();


        services.AddScoped<WO_CM_Review_Automation.Application.Mappings.Interfaces.IMappingService, WO_CM_Review_Automation.Infrastructure.Services.Mappings.MappingService>();

        return services;
    }
}
