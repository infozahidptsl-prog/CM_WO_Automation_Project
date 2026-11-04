using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WO_CM_Review_Automation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingRsoMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "FunctionalUnitMappings",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "FunctionalUnitName", "IsActive", "ModifiedBy", "ModifiedDate", "SourceValue" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000011"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Sales Force App / RSO App", true, null, null, "RSO_App" },
                    { new Guid("10000000-0000-0000-0000-000000000012"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Sales Force App / RSO App", true, null, null, "Sales Force App" },
                    { new Guid("10000000-0000-0000-0000-000000000013"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Sales Force App / RSO App", true, null, null, "SFA" }
                });

            migrationBuilder.InsertData(
                table: "TeamLeaderMappings",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsActive", "ModifiedBy", "ModifiedDate", "SourceValue", "TeamLeaderName" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000011"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, null, null, "RSO_App", "Maksud Saifullah Pulak" },
                    { new Guid("20000000-0000-0000-0000-000000000012"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, null, null, "Sales Force App", "Maksud Saifullah Pulak" },
                    { new Guid("20000000-0000-0000-0000-000000000013"), "System", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, null, null, "SFA", "Maksud Saifullah Pulak" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "FunctionalUnitMappings",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "FunctionalUnitMappings",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "FunctionalUnitMappings",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                table: "TeamLeaderMappings",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "TeamLeaderMappings",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "TeamLeaderMappings",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000013"));
        }
    }
}
