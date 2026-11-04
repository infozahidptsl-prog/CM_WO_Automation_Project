using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WO_CM_Review_Automation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExcelScreenshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExcelScreenshotStorageReference",
                table: "CMReviewDocuments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExcelScreenshotStorageReference",
                table: "CMReviewDocuments");
        }
    }
}
