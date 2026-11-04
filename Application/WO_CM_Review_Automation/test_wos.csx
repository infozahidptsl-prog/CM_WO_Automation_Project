using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using WO_CM_Review_Automation.Infrastructure.Data;
using WO_CM_Review_Automation.Infrastructure.Services;
using WO_CM_Review_Automation.Infrastructure.Services.CMReview;

var services = new ServiceCollection();
services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(\"Server=.\\\\SQLEXPRESS;Database=WO_CM_Review_Automation;Trusted_Connection=True;TrustServerCertificate=True;\"));
services.AddScoped<WO_CM_Review_Automation.Application.Interfaces.IWorkOrderFilterService, WorkOrderFilterService>();
services.AddScoped<WO_CM_Review_Automation.Application.Interfaces.ICMReviewWOReportService, CMReviewWOReportService>();
var provider = services.BuildServiceProvider();

var reportService = provider.GetRequiredService<WO_CM_Review_Automation.Application.Interfaces.ICMReviewWOReportService>();
var items = await reportService.GenerateWOReportItemsAsync(Guid.Empty, \"TECHVAN\", 2026, 7);

Console.WriteLine($\"Found {items.Count} items.\");
foreach(var item in items) {
    Console.WriteLine($\"WO: {item.WoId}, ServiceName: {item.ServiceName}\");
}
