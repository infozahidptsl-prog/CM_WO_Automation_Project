using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WO_CM_Review_Automation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedMappingData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "FunctionalUnitMappings",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "FunctionalUnitName", "IsActive", "ModifiedBy", "ModifiedDate", "SourceValue" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "POS", true, null, null, "POS" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "LEA", true, null, null, "LEA" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "DMS", true, null, null, "DMS" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Sales Force App / RSO App", true, null, null, "One_Sales_App" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Sales Force App / RSO App", true, null, null, "OneSalesApp" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Sales Force App / RSO App", true, null, null, "OneSales App" },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Sales Force App / RSO App", true, null, null, "RSO App" },
                    { new Guid("10000000-0000-0000-0000-000000000008"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AMS", true, null, null, "AMS" },
                    { new Guid("10000000-0000-0000-0000-000000000009"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "TechVan", true, null, null, "Techvan" },
                    { new Guid("10000000-0000-0000-0000-000000000010"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "TechVan", true, null, null, "TechVan" }
                });

            migrationBuilder.InsertData(
                table: "TeamLeaderMappings",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsActive", "ModifiedBy", "ModifiedDate", "SourceValue", "TeamLeaderName" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, null, null, "POS", "Md Maruf Ul Alam" },
                    { new Guid("20000000-0000-0000-0000-000000000002"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, null, null, "LEA", "Minhaz Uddin Ahmed" },
                    { new Guid("20000000-0000-0000-0000-000000000003"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, null, null, "One_Sales_App", "Maksud Saifullah Pulak" },
                    { new Guid("20000000-0000-0000-0000-000000000004"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, null, null, "DMS", "Md Maruf Ul Alam" },
                    { new Guid("20000000-0000-0000-0000-000000000005"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, null, null, "OneSalesApp", "Maksud Saifullah Pulak" },
                    { new Guid("20000000-0000-0000-0000-000000000006"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, null, null, "OneSales App", "Maksud Saifullah Pulak" },
                    { new Guid("20000000-0000-0000-0000-000000000007"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, null, null, "RSO App", "Maksud Saifullah Pulak" },
                    { new Guid("20000000-0000-0000-0000-000000000008"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, null, null, "AMS", "Maksud Saifullah Pulak" },
                    { new Guid("20000000-0000-0000-0000-000000000009"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, null, null, "Techvan", "Md Amirul Islam" },
                    { new Guid("20000000-0000-0000-0000-000000000010"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, null, null, "TechVan", "Md Amirul Islam" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "FunctionalUnitMappings",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "FunctionalUnitMappings",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "FunctionalUnitMappings",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "FunctionalUnitMappings",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "FunctionalUnitMappings",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "FunctionalUnitMappings",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "FunctionalUnitMappings",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "FunctionalUnitMappings",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "FunctionalUnitMappings",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "FunctionalUnitMappings",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "TeamLeaderMappings",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "TeamLeaderMappings",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "TeamLeaderMappings",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "TeamLeaderMappings",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "TeamLeaderMappings",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "TeamLeaderMappings",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "TeamLeaderMappings",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "TeamLeaderMappings",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "TeamLeaderMappings",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "TeamLeaderMappings",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000010"));
        }
    }
}
