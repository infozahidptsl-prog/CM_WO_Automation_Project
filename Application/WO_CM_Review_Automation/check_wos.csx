using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WO_CM_Review_Automation.Infrastructure.Data;

var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer(\"Server=(localdb)\\\\mssqllocaldb;Database=WO_CM_Review_Automation;Trusted_Connection=True;MultipleActiveResultSets=true\")
    .Options;

using var context = new ApplicationDbContext(options);
var wos = context.WorkOrders.Where(w => w.ApprovalDate != null && w.ApprovalDate.Value.Year == 2026 && w.ApprovalDate.Value.Month == 7).ToList();

Console.WriteLine($\"Found {wos.Count} WorkOrders for July 2026.\");
foreach (var wo in wos) {
    Console.WriteLine($\"WO: {wo.WoId}, ServiceName: {wo.ServiceName}\");
}
