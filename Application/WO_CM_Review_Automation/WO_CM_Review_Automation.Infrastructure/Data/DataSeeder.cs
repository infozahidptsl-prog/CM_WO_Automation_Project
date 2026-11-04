using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using WO_CM_Review_Automation.Domain.Entities;
using WO_CM_Review_Automation.Infrastructure.Identity;

namespace WO_CM_Review_Automation.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DataSeeder");

        try
        {
            if (context.Database.IsSqlServer())
            {
                await context.Database.MigrateAsync();
            }

            // Roles
            var roles = new[] { "Administrator", "Operator", "Viewer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Admin User
            if (await userManager.FindByEmailAsync("admin@local.com") == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@local.com",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "ChangeMe123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Administrator");
                }
            }

            // Seed Configuration Tables
            if (!context.AllowedLMApprovers.Any())
            {
                context.AllowedLMApprovers.Add(new AllowedLMApprover { Name = "Md Amirul Islam", CreatedBy = "System", CreatedDate = DateTimeOffset.UtcNow });
            }

            if (!context.AllowedVendors.Any())
            {
                context.AllowedVendors.Add(new AllowedVendor { Name = "Primetech BD", CreatedBy = "System", CreatedDate = DateTimeOffset.UtcNow });
            }

            if (!context.AllowedIssuers.Any())
            {
                context.AllowedIssuers.AddRange(
                    new AllowedIssuer { Name = "Maksud Saifullah Pulak", CreatedBy = "System", CreatedDate = DateTimeOffset.UtcNow },
                    new AllowedIssuer { Name = "Golam Mostofa Shawon", CreatedBy = "System", CreatedDate = DateTimeOffset.UtcNow },
                    new AllowedIssuer { Name = "Md Aawlad Hossain", CreatedBy = "System", CreatedDate = DateTimeOffset.UtcNow }
                );
            }

            if (!context.AllowedIssuerOffices.Any())
            {
                context.AllowedIssuerOffices.Add(new AllowedIssuerOffice { Name = "BSS-CBAS-SC", CreatedBy = "System", CreatedDate = DateTimeOffset.UtcNow });
            }

            if (!context.AllowedDomains.Any())
            {
                context.AllowedDomains.Add(new AllowedDomain { Name = "BSS", CreatedBy = "System", CreatedDate = DateTimeOffset.UtcNow });
            }

            var desiredMappings = new[] { "POS", "DMS", "Sales Force App", "AMS", "Techvan", "LEA" };
            var currentMappings = context.ApplicationMappings.Select(x => x.NormalizedName).Distinct().ToList();
            var isCorrect = currentMappings.Count == 6 && desiredMappings.All(x => currentMappings.Contains(x));
            if (!isCorrect)
            {
                context.ApplicationMappings.RemoveRange(context.ApplicationMappings);
                await context.SaveChangesAsync();
                
                context.ApplicationMappings.AddRange(
                    new ApplicationMapping { AliasName = "POS", NormalizedName = "POS", DisplayOrder = 1, CreatedBy = "System", CreatedDate = DateTimeOffset.UtcNow },
                    new ApplicationMapping { AliasName = "DMS", NormalizedName = "DMS", DisplayOrder = 2, CreatedBy = "System", CreatedDate = DateTimeOffset.UtcNow },
                    new ApplicationMapping { AliasName = "Sales Force App", NormalizedName = "Sales Force App", DisplayOrder = 3, CreatedBy = "System", CreatedDate = DateTimeOffset.UtcNow },
                    new ApplicationMapping { AliasName = "OneSalesApp", NormalizedName = "Sales Force App", DisplayOrder = 3, CreatedBy = "System", CreatedDate = DateTimeOffset.UtcNow },
                    new ApplicationMapping { AliasName = "OneSales App", NormalizedName = "Sales Force App", DisplayOrder = 3, CreatedBy = "System", CreatedDate = DateTimeOffset.UtcNow },
                    new ApplicationMapping { AliasName = "RSO App", NormalizedName = "Sales Force App", DisplayOrder = 3, CreatedBy = "System", CreatedDate = DateTimeOffset.UtcNow },
                    new ApplicationMapping { AliasName = "AMS", NormalizedName = "AMS", DisplayOrder = 4, CreatedBy = "System", CreatedDate = DateTimeOffset.UtcNow },
                    new ApplicationMapping { AliasName = "Techvan", NormalizedName = "Techvan", DisplayOrder = 5, CreatedBy = "System", CreatedDate = DateTimeOffset.UtcNow },
                    new ApplicationMapping { AliasName = "LEA", NormalizedName = "LEA", DisplayOrder = 6, CreatedBy = "System", CreatedDate = DateTimeOffset.UtcNow }
                );
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }
}
