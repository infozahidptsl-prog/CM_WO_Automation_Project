using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WO_CM_Review_Automation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNightActivityViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE VIEW vw_EmployeeSummary AS
                SELECT 
                    n.EmployeeId, 
                    e.EmployeeName, 
                    COUNT(*) AS TotalActivities,
                    SUM(CASE WHEN s.Name = 'Completed' THEN 1 ELSE 0 END) AS CompletedActivities,
                    CASE WHEN COUNT(*) = 0 THEN 0 ELSE CAST(SUM(CASE WHEN s.Name = 'Completed' THEN 1 ELSE 0 END) AS FLOAT) / COUNT(*) * 100 END AS CompletionRate
                FROM NightActivities n
                JOIN Employees e ON n.EmployeeId = e.Id
                JOIN ActivityStatuses s ON n.ActivityStatusId = s.Id
                WHERE n.IsDeleted = 0
                GROUP BY n.EmployeeId, e.EmployeeName
            ");

            migrationBuilder.Sql(@"
                CREATE VIEW vw_DailySummary AS
                SELECT 
                    CAST(n.ActivityDate AS DATE) AS ActivityDate,
                    COUNT(*) AS TotalActivities,
                    SUM(CASE WHEN s.Name = 'Completed' THEN 1 ELSE 0 END) AS CompletedActivities
                FROM NightActivities n
                JOIN ActivityStatuses s ON n.ActivityStatusId = s.Id
                WHERE n.IsDeleted = 0
                GROUP BY CAST(n.ActivityDate AS DATE)
            ");

            migrationBuilder.Sql(@"
                CREATE VIEW vw_MonthlySummary AS
                SELECT 
                    YEAR(n.ActivityDate) AS ActivityYear,
                    MONTH(n.ActivityDate) AS ActivityMonth,
                    COUNT(*) AS TotalActivities,
                    SUM(CASE WHEN s.Name = 'Completed' THEN 1 ELSE 0 END) AS CompletedActivities
                FROM NightActivities n
                JOIN ActivityStatuses s ON n.ActivityStatusId = s.Id
                WHERE n.IsDeleted = 0
                GROUP BY YEAR(n.ActivityDate), MONTH(n.ActivityDate)
            ");

            migrationBuilder.Sql(@"
                CREATE VIEW vw_StatusSummary AS
                SELECT 
                    s.Name AS StatusName,
                    s.DisplayColor,
                    COUNT(*) AS TotalCount
                FROM NightActivities n
                JOIN ActivityStatuses s ON n.ActivityStatusId = s.Id
                WHERE n.IsDeleted = 0
                GROUP BY s.Name, s.DisplayColor
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_EmployeeSummary");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_DailySummary");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_MonthlySummary");
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_StatusSummary");
        }
    }
}
