using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using WO_CM_Review_Automation.Application.Import.Interfaces;
using WO_CM_Review_Automation.Application.Import.Models;
using WO_CM_Review_Automation.Application.Import.Services;
using WO_CM_Review_Automation.Application.Import.WorkOrderImpl;
using WO_CM_Review_Automation.Domain.Entities;

namespace WO_CM_Review_Automation.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        // Import Engine Configuration
        services.Configure<ImportEngineOptions>(configuration.GetSection(ImportEngineOptions.SectionName));

        // Generic Import Pipeline Services
        services.AddScoped<IHeaderMappingService, HeaderMappingService>();
        services.AddScoped<IWorksheetDetector, WorksheetDetector>();
        services.AddScoped(typeof(IImportPipeline<>), typeof(ImportPipeline<>));

        // Work Order Specific Implementations
        services.AddScoped<IEntityMapper<WorkOrder>, WorkOrderEntityMapper>();
        services.AddScoped<IRowValidator<WorkOrder>, WorkOrderRowValidator>();

        return services;
    }
}
