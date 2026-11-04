using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WO_CM_Review_Automation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChangeDetailScreenshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CMReviewChangeDetailId",
                table: "CMReviewScreenshots",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CMReviewScreenshots_CMReviewChangeDetailId",
                table: "CMReviewScreenshots",
                column: "CMReviewChangeDetailId");

            migrationBuilder.AddForeignKey(
                name: "FK_CMReviewScreenshots_CMReviewChangeDetails_CMReviewChangeDetailId",
                table: "CMReviewScreenshots",
                column: "CMReviewChangeDetailId",
                principalTable: "CMReviewChangeDetails",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CMReviewScreenshots_CMReviewChangeDetails_CMReviewChangeDetailId",
                table: "CMReviewScreenshots");

            migrationBuilder.DropIndex(
                name: "IX_CMReviewScreenshots_CMReviewChangeDetailId",
                table: "CMReviewScreenshots");

            migrationBuilder.DropColumn(
                name: "CMReviewChangeDetailId",
                table: "CMReviewScreenshots");
        }
    }
}
