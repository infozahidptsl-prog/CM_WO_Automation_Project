using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WO_CM_Review_Automation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessingStatsToUploadHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DuplicateApplicationsRemoved",
                table: "UploadHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FilteredWorkOrders",
                table: "UploadHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProcessedApplications",
                table: "UploadHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RawWorkOrders",
                table: "UploadHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SummaryGeneratedAt",
                table: "UploadHistories",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnknownApplications",
                table: "UploadHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DuplicateApplicationsRemoved",
                table: "UploadHistories");

            migrationBuilder.DropColumn(
                name: "FilteredWorkOrders",
                table: "UploadHistories");

            migrationBuilder.DropColumn(
                name: "ProcessedApplications",
                table: "UploadHistories");

            migrationBuilder.DropColumn(
                name: "RawWorkOrders",
                table: "UploadHistories");

            migrationBuilder.DropColumn(
                name: "SummaryGeneratedAt",
                table: "UploadHistories");

            migrationBuilder.DropColumn(
                name: "UnknownApplications",
                table: "UploadHistories");
        }
    }
}
