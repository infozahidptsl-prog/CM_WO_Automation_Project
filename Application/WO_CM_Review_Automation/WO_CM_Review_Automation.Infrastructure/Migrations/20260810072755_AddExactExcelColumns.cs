using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WO_CM_Review_Automation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExactExcelColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Assignee",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChangeType",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExecutionEnd",
                table: "WorkOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExecutionStart",
                table: "WorkOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IssueDateTime",
                table: "WorkOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NeCount",
                table: "WorkOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NeType",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProposedDateTime",
                table: "WorkOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QaKpiCheckedBy",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sla",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WoType",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Assignee",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "ChangeType",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "ExecutionEnd",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "ExecutionStart",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "IssueDateTime",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "NeCount",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "NeType",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "ProposedDateTime",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "QaKpiCheckedBy",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "Sla",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "Subject",
                table: "WorkOrders");

            migrationBuilder.DropColumn(
                name: "WoType",
                table: "WorkOrders");
        }
    }
}
