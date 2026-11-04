using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WO_CM_Review_Automation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRowColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RowColor",
                table: "WorkOrders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowColor",
                table: "WorkOrders");
        }
    }
}
