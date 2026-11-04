using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WO_CM_Review_Automation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCMReviewModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CMReviewDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Application = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportingMonth = table.Column<int>(type: "int", nullable: false),
                    ReportingYear = table.Column<int>(type: "int", nullable: false),
                    DocumentVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TemplateVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UATStatement = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComparisonSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerifierName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerifierDesignation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerifierDepartment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerifierOrganization = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FinalizedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FinalizedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CMReviewDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CMReviewChangeCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CMReviewDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HasChanges = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CMReviewChangeCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CMReviewChangeCategories_CMReviewDocuments_CMReviewDocumentId",
                        column: x => x.CMReviewDocumentId,
                        principalTable: "CMReviewDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CMReviewDeploymentEvidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CMReviewDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServerIp = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CMReviewDeploymentEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CMReviewDeploymentEvidences_CMReviewDocuments_CMReviewDocumentId",
                        column: x => x.CMReviewDocumentId,
                        principalTable: "CMReviewDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CMReviewServerEvidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CMReviewDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServerIp = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ServerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CMReviewServerEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CMReviewServerEvidences_CMReviewDocuments_CMReviewDocumentId",
                        column: x => x.CMReviewDocumentId,
                        principalTable: "CMReviewDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CMReviewWOReportItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CMReviewDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WoId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdvanceTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WOType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NEType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NECount = table.Column<int>(type: "int", nullable: false),
                    Vendors = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Issuer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IssuerOffice = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CollaboratedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KPICheckedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssigneeOffice = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Assignee = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SLAPercentage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IssueDateTime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProposedExecutionWindows = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExecutionStart = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExecutionEndDate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CMReviewWOReportItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CMReviewWOReportItems_CMReviewDocuments_CMReviewDocumentId",
                        column: x => x.CMReviewDocumentId,
                        principalTable: "CMReviewDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CMReviewChangeDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CMReviewChangeCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeOrder = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScheduleDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ServerInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CMReviewChangeDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CMReviewChangeDetails_CMReviewChangeCategories_CMReviewChangeCategoryId",
                        column: x => x.CMReviewChangeCategoryId,
                        principalTable: "CMReviewChangeCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CMReviewScreenshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CMReviewServerEvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CMReviewDeploymentEvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StorageReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsBeforeDeployment = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CMReviewScreenshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CMReviewScreenshots_CMReviewDeploymentEvidences_CMReviewDeploymentEvidenceId",
                        column: x => x.CMReviewDeploymentEvidenceId,
                        principalTable: "CMReviewDeploymentEvidences",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CMReviewScreenshots_CMReviewServerEvidences_CMReviewServerEvidenceId",
                        column: x => x.CMReviewServerEvidenceId,
                        principalTable: "CMReviewServerEvidences",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CMReviewChangeCategories_CMReviewDocumentId",
                table: "CMReviewChangeCategories",
                column: "CMReviewDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_CMReviewChangeDetails_CMReviewChangeCategoryId",
                table: "CMReviewChangeDetails",
                column: "CMReviewChangeCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CMReviewDeploymentEvidences_CMReviewDocumentId",
                table: "CMReviewDeploymentEvidences",
                column: "CMReviewDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_CMReviewScreenshots_CMReviewDeploymentEvidenceId",
                table: "CMReviewScreenshots",
                column: "CMReviewDeploymentEvidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_CMReviewScreenshots_CMReviewServerEvidenceId",
                table: "CMReviewScreenshots",
                column: "CMReviewServerEvidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_CMReviewServerEvidences_CMReviewDocumentId",
                table: "CMReviewServerEvidences",
                column: "CMReviewDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_CMReviewWOReportItems_CMReviewDocumentId",
                table: "CMReviewWOReportItems",
                column: "CMReviewDocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CMReviewChangeDetails");

            migrationBuilder.DropTable(
                name: "CMReviewScreenshots");

            migrationBuilder.DropTable(
                name: "CMReviewWOReportItems");

            migrationBuilder.DropTable(
                name: "CMReviewChangeCategories");

            migrationBuilder.DropTable(
                name: "CMReviewDeploymentEvidences");

            migrationBuilder.DropTable(
                name: "CMReviewServerEvidences");

            migrationBuilder.DropTable(
                name: "CMReviewDocuments");
        }
    }
}
