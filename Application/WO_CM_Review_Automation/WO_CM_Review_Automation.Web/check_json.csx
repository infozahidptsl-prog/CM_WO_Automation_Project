
using System;
using System.IO;
using System.Data.SqlClient;
using System.Text.Json;
using System.Collections.Generic;

string connStr = "Server=.\\SQLEXPRESS;Database=WO_CM_Review_Automation;Trusted_Connection=True;TrustServerCertificate=True;";
using var conn = new SqlConnection(connStr);
conn.Open();

using var cmd = new SqlCommand("SELECT TOP 1 FinalizedReportJson FROM UploadHistories ORDER BY UploadDate DESC", conn);
var json = cmd.ExecuteScalar()?.ToString();

if (string.IsNullOrEmpty(json)) {
    Console.WriteLine("No json found.");
    return;
}

var obj = JsonDocument.Parse(json);
var root = obj.RootElement;
var items = root.GetProperty("Items");

Console.WriteLine("Items in JSON:");
foreach(var item in items.EnumerateArray()) {
    var fu = item.GetProperty("FunctionalUnit").GetString();
    var wos = item.GetProperty("WorkOrderIds");
    Console.WriteLine($"FU: {fu}, WOs: {wos.GetArrayLength()}");
    foreach(var wo in wos.EnumerateArray()) {
        if (wo.GetString() == "101773") {
            Console.WriteLine("  ==> FOUND 101773 in FU: " + fu);
        }
    }
}

var filtered = root.GetProperty("FilteredWorkOrders");
Console.WriteLine($"FilteredWorkOrders length: {filtered.GetArrayLength()}");
foreach(var f in filtered.EnumerateArray()) {
    if (f.GetProperty("WoId").GetString() == "101773") {
        Console.WriteLine("  ==> FOUND 101773 in FilteredWorkOrders");
    }
}

