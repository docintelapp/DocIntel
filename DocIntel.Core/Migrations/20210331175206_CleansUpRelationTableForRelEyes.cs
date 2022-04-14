using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class CleansUpRelationTableForRelEyes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Groups_IncomingFeeds_ImporterId",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_Groups_IncomingFeeds_ImporterId1",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Scrapers_ScraperId",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Scrapers_ScraperId1",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_Groups_SubmittedDocuments_SubmittedDocumentId",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_Groups_SubmittedDocuments_SubmittedDocumentId1",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_ImporterId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_ImporterId1",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_ScraperId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_ScraperId1",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_SubmittedDocumentId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_SubmittedDocumentId1",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "ImporterId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "ImporterId1",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "ScraperId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "ScraperId1",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "SubmittedDocumentId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "SubmittedDocumentId1",
                table: "Groups");

            migrationBuilder.CreateTable(
                name: "ImporterGroupEyesOnly",
                columns: table => new
                {
                    EyesOnlyGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImporterEyesOnlyImporterId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImporterGroupEyesOnly", x => new { x.EyesOnlyGroupId, x.ImporterEyesOnlyImporterId });
                    table.ForeignKey(
                        name: "FK_ImporterGroupEyesOnly_Groups_EyesOnlyGroupId",
                        column: x => x.EyesOnlyGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImporterGroupEyesOnly_IncomingFeeds_ImporterEyesOnlyImporte~",
                        column: x => x.ImporterEyesOnlyImporterId,
                        principalTable: "IncomingFeeds",
                        principalColumn: "ImporterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImporterRelToGroup",
                columns: table => new
                {
                    ImporterReleasableToImporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleasableToGroupId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImporterRelToGroup", x => new { x.ImporterReleasableToImporterId, x.ReleasableToGroupId });
                    table.ForeignKey(
                        name: "FK_ImporterRelToGroup_Groups_ReleasableToGroupId",
                        column: x => x.ReleasableToGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImporterRelToGroup_IncomingFeeds_ImporterReleasableToImport~",
                        column: x => x.ImporterReleasableToImporterId,
                        principalTable: "IncomingFeeds",
                        principalColumn: "ImporterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScraperGroupEyesOnly",
                columns: table => new
                {
                    EyesOnlyGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScraperEyesOnlyScraperId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScraperGroupEyesOnly", x => new { x.EyesOnlyGroupId, x.ScraperEyesOnlyScraperId });
                    table.ForeignKey(
                        name: "FK_ScraperGroupEyesOnly_Groups_EyesOnlyGroupId",
                        column: x => x.EyesOnlyGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScraperGroupEyesOnly_Scrapers_ScraperEyesOnlyScraperId",
                        column: x => x.ScraperEyesOnlyScraperId,
                        principalTable: "Scrapers",
                        principalColumn: "ScraperId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScraperRelToGroup",
                columns: table => new
                {
                    ReleasableToGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScraperReleasableToScraperId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScraperRelToGroup", x => new { x.ReleasableToGroupId, x.ScraperReleasableToScraperId });
                    table.ForeignKey(
                        name: "FK_ScraperRelToGroup_Groups_ReleasableToGroupId",
                        column: x => x.ReleasableToGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScraperRelToGroup_Scrapers_ScraperReleasableToScraperId",
                        column: x => x.ScraperReleasableToScraperId,
                        principalTable: "Scrapers",
                        principalColumn: "ScraperId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionGroupEyesOnly",
                columns: table => new
                {
                    EyesOnlyGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedDocumentEyesOnlySubmittedDocumentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionGroupEyesOnly", x => new { x.EyesOnlyGroupId, x.SubmittedDocumentEyesOnlySubmittedDocumentId });
                    table.ForeignKey(
                        name: "FK_SubmissionGroupEyesOnly_Groups_EyesOnlyGroupId",
                        column: x => x.EyesOnlyGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubmissionGroupEyesOnly_SubmittedDocuments_SubmittedDocumen~",
                        column: x => x.SubmittedDocumentEyesOnlySubmittedDocumentId,
                        principalTable: "SubmittedDocuments",
                        principalColumn: "SubmittedDocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionRelToGroup",
                columns: table => new
                {
                    ReleasableToGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedDocumentReleasableToSubmittedDocumentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionRelToGroup", x => new { x.ReleasableToGroupId, x.SubmittedDocumentReleasableToSubmittedDocumentId });
                    table.ForeignKey(
                        name: "FK_SubmissionRelToGroup_Groups_ReleasableToGroupId",
                        column: x => x.ReleasableToGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubmissionRelToGroup_SubmittedDocuments_SubmittedDocumentRe~",
                        column: x => x.SubmittedDocumentReleasableToSubmittedDocumentId,
                        principalTable: "SubmittedDocuments",
                        principalColumn: "SubmittedDocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImporterGroupEyesOnly_ImporterEyesOnlyImporterId",
                table: "ImporterGroupEyesOnly",
                column: "ImporterEyesOnlyImporterId");

            migrationBuilder.CreateIndex(
                name: "IX_ImporterRelToGroup_ReleasableToGroupId",
                table: "ImporterRelToGroup",
                column: "ReleasableToGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ScraperGroupEyesOnly_ScraperEyesOnlyScraperId",
                table: "ScraperGroupEyesOnly",
                column: "ScraperEyesOnlyScraperId");

            migrationBuilder.CreateIndex(
                name: "IX_ScraperRelToGroup_ScraperReleasableToScraperId",
                table: "ScraperRelToGroup",
                column: "ScraperReleasableToScraperId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionGroupEyesOnly_SubmittedDocumentEyesOnlySubmittedD~",
                table: "SubmissionGroupEyesOnly",
                column: "SubmittedDocumentEyesOnlySubmittedDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionRelToGroup_SubmittedDocumentReleasableToSubmitted~",
                table: "SubmissionRelToGroup",
                column: "SubmittedDocumentReleasableToSubmittedDocumentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImporterGroupEyesOnly");

            migrationBuilder.DropTable(
                name: "ImporterRelToGroup");

            migrationBuilder.DropTable(
                name: "ScraperGroupEyesOnly");

            migrationBuilder.DropTable(
                name: "ScraperRelToGroup");

            migrationBuilder.DropTable(
                name: "SubmissionGroupEyesOnly");

            migrationBuilder.DropTable(
                name: "SubmissionRelToGroup");

            migrationBuilder.AddColumn<Guid>(
                name: "ImporterId",
                table: "Groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ImporterId1",
                table: "Groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ScraperId",
                table: "Groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ScraperId1",
                table: "Groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedDocumentId",
                table: "Groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedDocumentId1",
                table: "Groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_ImporterId",
                table: "Groups",
                column: "ImporterId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_ImporterId1",
                table: "Groups",
                column: "ImporterId1");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_ScraperId",
                table: "Groups",
                column: "ScraperId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_ScraperId1",
                table: "Groups",
                column: "ScraperId1");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_SubmittedDocumentId",
                table: "Groups",
                column: "SubmittedDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_SubmittedDocumentId1",
                table: "Groups",
                column: "SubmittedDocumentId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_IncomingFeeds_ImporterId",
                table: "Groups",
                column: "ImporterId",
                principalTable: "IncomingFeeds",
                principalColumn: "ImporterId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_IncomingFeeds_ImporterId1",
                table: "Groups",
                column: "ImporterId1",
                principalTable: "IncomingFeeds",
                principalColumn: "ImporterId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Scrapers_ScraperId",
                table: "Groups",
                column: "ScraperId",
                principalTable: "Scrapers",
                principalColumn: "ScraperId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Scrapers_ScraperId1",
                table: "Groups",
                column: "ScraperId1",
                principalTable: "Scrapers",
                principalColumn: "ScraperId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_SubmittedDocuments_SubmittedDocumentId",
                table: "Groups",
                column: "SubmittedDocumentId",
                principalTable: "SubmittedDocuments",
                principalColumn: "SubmittedDocumentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_SubmittedDocuments_SubmittedDocumentId1",
                table: "Groups",
                column: "SubmittedDocumentId1",
                principalTable: "SubmittedDocuments",
                principalColumn: "SubmittedDocumentId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
