using Serilog;
using MudBlazor.Services;
using WO_CM_Review_Automation.Web.Components;
using WO_CM_Review_Automation.Application;
using WO_CM_Review_Automation.Infrastructure;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddSingleton<WO_CM_Review_Automation.Web.Services.UserSessionService>();

// Add Clean Architecture Layers
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await WO_CM_Review_Automation.Infrastructure.Data.DataSeeder.SeedAsync(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

var evidencePath = Path.Combine(builder.Environment.ContentRootPath, "CMReviewEvidence");
if (!Directory.Exists(evidencePath))
{
    Directory.CreateDirectory(evidencePath);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(evidencePath),
    RequestPath = "/CMReviewEvidence"
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
