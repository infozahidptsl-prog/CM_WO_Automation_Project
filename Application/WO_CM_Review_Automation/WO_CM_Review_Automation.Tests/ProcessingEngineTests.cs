using System;
using System.Collections.Generic;
using System.Linq;
using WO_CM_Review_Automation.Application.Processing;
using WO_CM_Review_Automation.Domain.Entities;
using WO_CM_Review_Automation.Infrastructure.Data;
using WO_CM_Review_Automation.Infrastructure.Services;
using Xunit;

namespace WO_CM_Review_Automation.Tests;

public class ProcessingEngineTests
{
    private ProcessingConfiguration GetTestConfig()
    {
        var config = new ProcessingConfiguration();
        config.AllowedLMApprovers.Add("Approver A");
        config.AllowedVendors.Add("Vendor V");
        config.AllowedIssuers.Add("Issuer I");
        config.AllowedDomains.Add("Domain D");
        config.AllowedIssuerOffices.Add("Office O");

        config.ApplicationMappings["RSO App"] = ("Sales Force App / RSO App", 1);
        config.ApplicationMappings["RSOApp"] = ("Sales Force App / RSO App", 1);
        config.ApplicationMappings["Sales Force App"] = ("Sales Force App / RSO App", 1);
        config.ApplicationMappings["SalesForceApp"] = ("Sales Force App / RSO App", 1);
        
        config.ApplicationMappings["LEA"] = ("LEA", 2);
        config.ApplicationMappings["LEA (Call Details Application)"] = ("LEA", 2);
        
        config.ApplicationMappings["DMS"] = ("DMS", 3);

        config.ActiveNormalizedApps["Sales Force App / RSO App"] = 1;
        config.ActiveNormalizedApps["LEA"] = 2;
        config.ActiveNormalizedApps["DMS"] = 3;
        config.ActiveNormalizedApps["POS"] = 4; // Zero-count test

        return config;
    }

    private WorkOrder CreateValidWO(string serviceName)
    {
        return new WorkOrder
        {
            Id = Guid.NewGuid(),
            WoId = "WO-" + Guid.NewGuid().ToString().Substring(0, 4),
            ServiceName = serviceName,
            LmApprover = "Approver A",
            Vendor = "Vendor V",
            Issuer = "Issuer I",
            Domain = "Domain D",
            IssuerOffice = "Office O"
        };
    }

    [Fact]
    public void BusinessRuleFiltering_ShouldDropInvalidRows()
    {
        var engine = new ProcessingEngine(null!, null!); // context not needed for ProcessWorkOrders memory overload
        var config = GetTestConfig();

        var valid = CreateValidWO("RSO App");
        
        var invalid = CreateValidWO("RSO App");
        invalid.LmApprover = "Invalid Approver"; // Should be filtered out

        var result = engine.ProcessWorkOrders(new[] { valid, invalid }, config);

        Assert.Equal(2, result.Statistics.RawWorkOrders);
        Assert.Equal(1, result.Statistics.FilteredWorkOrders);
        Assert.Single(result.ProcessedWorkOrders);
    }

    [Fact]
    public void Normalization_ShouldMapCorrectly_AndTrimWhitespace()
    {
        var engine = new ProcessingEngine(null!, null!);
        var config = GetTestConfig();

        // Testing various spacings and cases
        var wo = CreateValidWO(" RSO   App  ");
        var result = engine.ProcessWorkOrders(new[] { wo }, config);

        Assert.Equal("Sales Force App / RSO App", result.ProcessedWorkOrders.First().NormalizedApplicationName);
    }

    [Fact]
    public void MultiServiceParsing_And_DuplicateRemoval_ShouldWork()
    {
        var engine = new ProcessingEngine(null!, null!);
        var config = GetTestConfig();

        // "RSO App", "RSOApp", and "SalesForceApp" all map to the SAME normalized app.
        // So within one WO, they should be deduplicated to a single count.
        // "LEA" is a different app.
        var wo = CreateValidWO("RSO App, RSOApp; SalesForceApp | LEA \n DMS");
        
        var result = engine.ProcessWorkOrders(new[] { wo }, config);

        // Expected unique normalized apps: "Sales Force App / RSO App", "LEA", "DMS"
        Assert.Equal(3, result.ProcessedWorkOrders.Count);
        
        var normalizedNames = result.ProcessedWorkOrders.Select(x => x.NormalizedApplicationName).ToList();
        Assert.Contains("Sales Force App / RSO App", normalizedNames);
        Assert.Contains("LEA", normalizedNames);
        Assert.Contains("DMS", normalizedNames);
        
        Assert.Equal(2, result.Statistics.DuplicateApplicationsRemoved); // RSOApp and SalesForceApp were dupes
    }

    [Fact]
    public void UnknownApplication_ShouldBeFlagged()
    {
        var engine = new ProcessingEngine(null!, null!);
        var config = GetTestConfig();

        var wo = CreateValidWO("Weird Unknown App");
        var result = engine.ProcessWorkOrders(new[] { wo }, config);

        Assert.Equal("Unknown Application", result.ProcessedWorkOrders.First().NormalizedApplicationName);
        Assert.Equal(1, result.Statistics.UnknownApplications);
        Assert.Single(result.ValidationMessages);
    }

    [Fact]
    public void SummaryGeneration_ShouldSortByDisplayOrder_AndIncludeZeroCounts()
    {
        var engine = new ProcessingEngine(null!, null!);
        var config = GetTestConfig();

        var wo1 = CreateValidWO("DMS"); // Display Order 3
        var wo2 = CreateValidWO("LEA"); // Display Order 2
        var wo3 = CreateValidWO("Weird App"); // Unknown App, Display Order 9999

        var result = engine.ProcessWorkOrders(new[] { wo1, wo2, wo3 }, config);

        var items = result.MonthlySummary.Items;
        
        // Expected Order: 
        // 1. Sales Force App / RSO App (Count 0)
        // 2. LEA (Count 1)
        // 3. DMS (Count 1)
        // 4. POS (Count 0)
        // 9999. Unknown Application (Count 1)

        Assert.Equal(5, items.Count);
        
        Assert.Equal("Sales Force App / RSO App", items[0].ApplicationName);
        Assert.Equal(0, items[0].Count);

        Assert.Equal("LEA", items[1].ApplicationName);
        Assert.Equal(1, items[1].Count);

        Assert.Equal("DMS", items[2].ApplicationName);
        Assert.Equal(1, items[2].Count);

        Assert.Equal("POS", items[3].ApplicationName);
        Assert.Equal(0, items[3].Count);

        Assert.Equal("Unknown Application", items[4].ApplicationName);
        Assert.Equal(1, items[4].Count);
    }

    [Fact]
    public void DrillDown_ShouldMaintainOriginalReferences()
    {
        var engine = new ProcessingEngine(null!, null!);
        var config = GetTestConfig();

        var wo = CreateValidWO("RSO App");
        var result = engine.ProcessWorkOrders(new[] { wo }, config);

        var processed = result.ProcessedWorkOrders.First();
        Assert.Equal(wo.Id, processed.WorkOrderId);
        Assert.Equal(wo.WoId, processed.WoId);
        Assert.Equal("RSO App", processed.OriginalServiceName);
    }
}
