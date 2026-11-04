using System;
using Microsoft.EntityFrameworkCore;
using WO_CM_Review_Automation.Domain.Entities;

namespace WO_CM_Review_Automation.Infrastructure.Data;

public static class ModelBuilderExtensions
{
    public static void SeedData(this ModelBuilder builder)
    {
        // 1. POS: Keyword "POS" in Service Name | POC: "Md Maruf Ul Alam"
        // 2. LEA: Keyword "LEA" in Service Name | POC: "Minhaz Uddin Ahmed"
        // 3. DMS: Keyword "DMS" in Service Name | POC: "Md Maruf Ul Alam"
        // 4. Sales Force App / RSO App: Keywords ["One_Sales_App", "OneSalesApp", "OneSales App", "RSO App"] | POC: "Maksud Saifullah Pulak"
        // 5. AMS: Keyword "AMS" in Service Name | POC: "Maksud Saifullah Pulak"
        // 6. TechVan: Keywords ["Techvan", "TechVan"] | POC: "Md Amirul Islam"

        var posId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var leaId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var dmsId = Guid.Parse("10000000-0000-0000-0000-000000000003");
        var rso1Id = Guid.Parse("10000000-0000-0000-0000-000000000004");
        var rso2Id = Guid.Parse("10000000-0000-0000-0000-000000000005");
        var rso3Id = Guid.Parse("10000000-0000-0000-0000-000000000006");
        var rso4Id = Guid.Parse("10000000-0000-0000-0000-000000000007");
        var amsId = Guid.Parse("10000000-0000-0000-0000-000000000008");
        var techvan1Id = Guid.Parse("10000000-0000-0000-0000-000000000009");
        var techvan2Id = Guid.Parse("10000000-0000-0000-0000-000000000010");
        var rso5Id = Guid.Parse("10000000-0000-0000-0000-000000000011");
        var rso6Id = Guid.Parse("10000000-0000-0000-0000-000000000012");
        var rso7Id = Guid.Parse("10000000-0000-0000-0000-000000000013");

        var dt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        builder.Entity<FunctionalUnitMapping>().HasData(
            new FunctionalUnitMapping { Id = posId, FunctionalUnitName = "POS", SourceValue = "POS", CreatedDate = dt, CreatedBy = "System", IsActive = true },
            new FunctionalUnitMapping { Id = leaId, FunctionalUnitName = "LEA", SourceValue = "LEA", CreatedDate = dt, CreatedBy = "System", IsActive = true },
            new FunctionalUnitMapping { Id = dmsId, FunctionalUnitName = "DMS", SourceValue = "DMS", CreatedDate = dt, CreatedBy = "System", IsActive = true },
            new FunctionalUnitMapping { Id = rso1Id, FunctionalUnitName = "Sales Force App / RSO App", SourceValue = "One_Sales_App", CreatedDate = dt, CreatedBy = "System", IsActive = true },
            new FunctionalUnitMapping { Id = rso2Id, FunctionalUnitName = "Sales Force App / RSO App", SourceValue = "OneSalesApp", CreatedDate = dt, CreatedBy = "System", IsActive = true },
            new FunctionalUnitMapping { Id = rso3Id, FunctionalUnitName = "Sales Force App / RSO App", SourceValue = "OneSales App", CreatedDate = dt, CreatedBy = "System", IsActive = true },
            new FunctionalUnitMapping { Id = rso4Id, FunctionalUnitName = "Sales Force App / RSO App", SourceValue = "RSO App", CreatedDate = dt, CreatedBy = "System", IsActive = true },
            new FunctionalUnitMapping { Id = rso5Id, FunctionalUnitName = "Sales Force App / RSO App", SourceValue = "RSO_App", CreatedDate = dt, CreatedBy = "System", IsActive = true },
            new FunctionalUnitMapping { Id = rso6Id, FunctionalUnitName = "Sales Force App / RSO App", SourceValue = "Sales Force App", CreatedDate = dt, CreatedBy = "System", IsActive = true },
            new FunctionalUnitMapping { Id = rso7Id, FunctionalUnitName = "Sales Force App / RSO App", SourceValue = "SFA", CreatedDate = dt, CreatedBy = "System", IsActive = true },
            new FunctionalUnitMapping { Id = amsId, FunctionalUnitName = "AMS", SourceValue = "AMS", CreatedDate = dt, CreatedBy = "System", IsActive = true },
            new FunctionalUnitMapping { Id = techvan1Id, FunctionalUnitName = "TechVan", SourceValue = "Techvan", CreatedDate = dt, CreatedBy = "System", IsActive = true },
            new FunctionalUnitMapping { Id = techvan2Id, FunctionalUnitName = "TechVan", SourceValue = "TechVan", CreatedDate = dt, CreatedBy = "System", IsActive = true }
        );

        var tl1Id = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var tl2Id = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var tl3Id = Guid.Parse("20000000-0000-0000-0000-000000000003");

        builder.Entity<TeamLeaderMapping>().HasData(
            new TeamLeaderMapping { Id = tl1Id, TeamLeaderName = "Md Maruf Ul Alam", SourceValue = "POS", CreatedDate = dt, CreatedBy = "System", IsActive = true },
            new TeamLeaderMapping { Id = Guid.Parse("20000000-0000-0000-0000-000000000004"), TeamLeaderName = "Md Maruf Ul Alam", SourceValue = "DMS", CreatedDate = dt, CreatedBy = "System", IsActive = true },
            
            new TeamLeaderMapping { Id = tl2Id, TeamLeaderName = "Minhaz Uddin Ahmed", SourceValue = "LEA", CreatedDate = dt, CreatedBy = "System", IsActive = true },
            
            new TeamLeaderMapping { Id = tl3Id, TeamLeaderName = "Maksud Saifullah Pulak", SourceValue = "One_Sales_App", CreatedDate = dt, CreatedBy = "System", IsActive = true },
            new TeamLeaderMapping { Id = Guid.Parse("20000000-0000-0000-0000-000000000005"), TeamLeaderName = "Maksud Saifullah Pulak", SourceValue = "OneSalesApp", CreatedDate = dt, CreatedBy = "System", IsActive = true },
            new TeamLeaderMapping { Id = Guid.Parse("20000000-0000-0000-0000-000000000006"), TeamLeaderName = "Maksud Saifullah Pulak", SourceValue = "OneSales App", CreatedDate = dt, CreatedBy = "System", IsActive = true },
            new TeamLeaderMapping { Id = Guid.Parse("20000000-0000-0000-0000-000000000007"), TeamLeaderName = "Maksud Saifullah Pulak", SourceValue = "RSO App", CreatedDate = dt, CreatedBy = "System", IsActive = true },
            new TeamLeaderMapping { Id = Guid.Parse("20000000-0000-0000-0000-000000000011"), TeamLeaderName = "Maksud Saifullah Pulak", SourceValue = "RSO_App", CreatedDate = dt, CreatedBy = "System", IsActive = true },
            new TeamLeaderMapping { Id = Guid.Parse("20000000-0000-0000-0000-000000000012"), TeamLeaderName = "Maksud Saifullah Pulak", SourceValue = "Sales Force App", CreatedDate = dt, CreatedBy = "System", IsActive = true },
            new TeamLeaderMapping { Id = Guid.Parse("20000000-0000-0000-0000-000000000013"), TeamLeaderName = "Maksud Saifullah Pulak", SourceValue = "SFA", CreatedDate = dt, CreatedBy = "System", IsActive = true },
            new TeamLeaderMapping { Id = Guid.Parse("20000000-0000-0000-0000-000000000008"), TeamLeaderName = "Maksud Saifullah Pulak", SourceValue = "AMS", CreatedDate = dt, CreatedBy = "System", IsActive = true },

            new TeamLeaderMapping { Id = Guid.Parse("20000000-0000-0000-0000-000000000009"), TeamLeaderName = "Md Amirul Islam", SourceValue = "Techvan", CreatedDate = dt, CreatedBy = "System", IsActive = true },
            new TeamLeaderMapping { Id = Guid.Parse("20000000-0000-0000-0000-000000000010"), TeamLeaderName = "Md Amirul Islam", SourceValue = "TechVan", CreatedDate = dt, CreatedBy = "System", IsActive = true }
        );
    }
}
