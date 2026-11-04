using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WO_CM_Review_Automation.Infrastructure.Data;
using WO_CM_Review_Automation.Domain.Entities;

var services = new ServiceCollection();
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer("Server=.\\SQLEXPRESS;Database=WO_CM_Review_Db;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"));

var sp = services.BuildServiceProvider();
var db = sp.GetRequiredService<ApplicationDbContext>();

var lastUpload = await db.UploadHistories.OrderByDescending(u => u.UploadDate).FirstOrDefaultAsync();
if (lastUpload != null)
{
    var wos = await db.WorkOrders.Where(w => w.UploadHistoryId == lastUpload.Id).ToListAsync();
    Console.WriteLine($"Total WOs: {wos.Count}");
    
    var statuses = wos.GroupBy(w => w.Status).Select(g => new { Status = g.Key, Count = g.Count() });
    Console.WriteLine("Statuses:");
    foreach (var s in statuses)
    {
        Console.WriteLine($"  {s.Status}: {s.Count}");
    }
    
    var allowedLMs = await db.AllowedLMApprovers.Select(x => x.Name.ToLower()).ToListAsync();
    var allowedVendors = await db.AllowedVendors.Select(x => x.Name.ToLower()).ToListAsync();
    var allowedIssuers = await db.AllowedIssuers.Select(x => x.Name.ToLower()).ToListAsync();
    var allowedOffices = await db.AllowedIssuerOffices.Select(x => x.Name.ToLower()).ToListAsync();
    var allowedDomains = await db.AllowedDomains.Select(x => x.Name.ToLower()).ToListAsync();
    
    int passedAll = 0;
    foreach(var wo in wos)
    {
        bool pLM = allowedLMs.Contains(wo.LmApprover?.Trim().ToLower() ?? "");
        bool pVen = allowedVendors.Contains(wo.Vendor?.Trim().ToLower() ?? "");
        bool pIss = allowedIssuers.Contains(wo.Issuer?.Trim().ToLower() ?? "");
        bool pOff = allowedOffices.Contains(wo.IssuerOffice?.Trim().ToLower() ?? "");
        bool pDom = allowedDomains.Contains(wo.Domain?.Trim().ToLower() ?? "");
        bool pStatus = string.Equals(wo.Status?.Trim(), "Closed", StringComparison.OrdinalIgnoreCase);
        
        if (pLM && pVen && pIss && pOff && pDom && pStatus)
            passedAll++;
    }
    
    Console.WriteLine($"Passed all filters: {passedAll}");
    
    int passedWithoutStatus = 0;
    foreach(var wo in wos)
    {
        bool pLM = allowedLMs.Contains(wo.LmApprover?.Trim().ToLower() ?? "");
        bool pVen = allowedVendors.Contains(wo.Vendor?.Trim().ToLower() ?? "");
        bool pIss = allowedIssuers.Contains(wo.Issuer?.Trim().ToLower() ?? "");
        bool pOff = allowedOffices.Contains(wo.IssuerOffice?.Trim().ToLower() ?? "");
        bool pDom = allowedDomains.Contains(wo.Domain?.Trim().ToLower() ?? "");
        
        if (pLM && pVen && pIss && pOff && pDom)
            passedWithoutStatus++;
    }
    
    Console.WriteLine($"Passed without status filter: {passedWithoutStatus}");
    
    int unmappedCount = 0;
    var funcMappings = await db.FunctionalUnitMappings.ToListAsync();
    foreach(var wo in wos)
    {
        bool pLM = allowedLMs.Contains(wo.LmApprover?.Trim().ToLower() ?? "");
        bool pVen = allowedVendors.Contains(wo.Vendor?.Trim().ToLower() ?? "");
        bool pIss = allowedIssuers.Contains(wo.Issuer?.Trim().ToLower() ?? "");
        bool pOff = allowedOffices.Contains(wo.IssuerOffice?.Trim().ToLower() ?? "");
        bool pDom = allowedDomains.Contains(wo.Domain?.Trim().ToLower() ?? "");
        bool pStatus = string.Equals(wo.Status?.Trim(), "Closed", StringComparison.OrdinalIgnoreCase);
        
        if (pLM && pVen && pIss && pOff && pDom && pStatus)
        {
            var servicesList = wo.ServiceName?.Split(new[] { ',', '&', '|' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList() ?? new List<string>();
            bool matched = false;
            foreach(var svc in servicesList)
            {
                if (funcMappings.Any(m => svc.Contains(m.SourceValue, StringComparison.OrdinalIgnoreCase)))
                    matched = true;
            }
            if (!matched)
            {
                if (funcMappings.Any(m => (wo.ServiceName ?? "").Contains(m.SourceValue, StringComparison.OrdinalIgnoreCase)))
                    matched = true;
            }
            if (!matched)
                unmappedCount++;
        }
    }
    Console.WriteLine($"Passed all but unmapped: {unmappedCount}");
}
