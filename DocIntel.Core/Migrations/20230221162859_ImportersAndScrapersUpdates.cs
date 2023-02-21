using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocIntel.Core.Migrations
{
    /// <inheritdoc />
    public partial class ImportersAndScrapersUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubmittedDocuments_IncomingFeeds_ImporterId",
                table: "SubmittedDocuments");

            migrationBuilder.AddColumn<bool>(
                name: "OverrideSource",
                table: "SubmittedDocuments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SkipInbox",
                table: "SubmittedDocuments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OverrideSource",
                table: "IncomingFeeds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SkipInbox",
                table: "IncomingFeeds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ImporterTags",
                columns: table => new
                {
                    ImporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImporterTags", x => new { x.ImporterId, x.TagId });
                    table.ForeignKey(
                        name: "FK_ImporterTags_IncomingFeeds_ImporterId",
                        column: x => x.ImporterId,
                        principalTable: "IncomingFeeds",
                        principalColumn: "ImporterId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImporterTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScraperTags",
                columns: table => new
                {
                    ScraperId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScraperTags", x => new { x.ScraperId, x.TagId });
                    table.ForeignKey(
                        name: "FK_ScraperTags_Scrapers_ScraperId",
                        column: x => x.ScraperId,
                        principalTable: "Scrapers",
                        principalColumn: "ScraperId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScraperTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImporterTags_TagId",
                table: "ImporterTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_ScraperTags_TagId",
                table: "ScraperTags",
                column: "TagId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubmittedDocuments_IncomingFeeds_ImporterId",
                table: "SubmittedDocuments",
                column: "ImporterId",
                principalTable: "IncomingFeeds",
                principalColumn: "ImporterId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubmittedDocuments_IncomingFeeds_ImporterId",
                table: "SubmittedDocuments");

            migrationBuilder.DropTable(
                name: "ImporterTags");

            migrationBuilder.DropTable(
                name: "ScraperTags");

            migrationBuilder.DropColumn(
                name: "OverrideSource",
                table: "SubmittedDocuments");

            migrationBuilder.DropColumn(
                name: "SkipInbox",
                table: "SubmittedDocuments");

            migrationBuilder.DropColumn(
                name: "OverrideSource",
                table: "IncomingFeeds");

            migrationBuilder.DropColumn(
                name: "SkipInbox",
                table: "IncomingFeeds");

            migrationBuilder.AddForeignKey(
                name: "FK_SubmittedDocuments_IncomingFeeds_ImporterId",
                table: "SubmittedDocuments",
                column: "ImporterId",
                principalTable: "IncomingFeeds",
                principalColumn: "ImporterId");
        }
    }
}
