
using System;
using System.Linq;
using System.IO;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WO_CM_Review_Automation.Infrastructure.Data;

var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=WO_CM_Review_Automation;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;");
using var db = new ApplicationDbContext(optionsBuilder.Options);

var allWorkOrders = db.WorkOrders.ToList();
Console.WriteLine($"Total WOs in DB: {allWorkOrders.Count}");

var allowedLMs = db.AllowedLMApprovers.Select(x => x.Name.ToLower()).ToList();
var allowedVendors = db.AllowedVendors.Select(x => x.Name.ToLower()).ToList();
var allowedIssuers = db.AllowedIssuers.Select(x => x.Name.ToLower()).ToList();
var allowedOffices = db.AllowedIssuerOffices.Select(x => x.Name.ToLower()).ToList();
var allowedDomains = db.AllowedDomains.Select(x => x.Name.ToLower()).ToList();

var filtered = allWorkOrders.Where(wo => 
    string.Equals(wo.Status?.Trim(), "Closed", StringComparison.OrdinalIgnoreCase) &&
    allowedLMs.Contains(wo.LmApprover?.Trim().ToLower() ?? "") &&
    (string.IsNullOrWhiteSpace(wo.Vendor) || 
     string.Equals(wo.Vendor.Trim(), "Other", StringComparison.OrdinalIgnoreCase) ||
     wo.Vendor.Split(new[] { ",", "&", "|" }, StringSplitOptions.RemoveEmptyEntries)
              .Select(v => v.Trim().ToLower())
              .Any(v => allowedVendors.Contains(v)))
).ToList();

Console.WriteLine($"Filtered WOs: {filtered.Count}");
foreach(var w in filtered) {
    Console.WriteLine($"WO: {w.WoId}, ServiceName: {w.ServiceName}, ChangeType: {w.ChangeType}");
}

