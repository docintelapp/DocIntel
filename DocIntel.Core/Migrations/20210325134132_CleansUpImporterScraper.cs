using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class CleansUpImporterScraper : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderedImportRuleSet_IncomingFeeds_ImporterId",
                table: "OrderedImportRuleSet");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderedImportRuleSet_Scrapers_ScraperId",
                table: "OrderedImportRuleSet");

            migrationBuilder.DropTable(
                name: "ImporterError");

            migrationBuilder.DropIndex(
                name: "IX_Tags_URL",
                table: "Tags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderedImportRuleSet",
                table: "OrderedImportRuleSet");

            migrationBuilder.DropIndex(
                name: "IX_OrderedImportRuleSet_ImporterId",
                table: "OrderedImportRuleSet");

            migrationBuilder.DropIndex(
                name: "IX_OrderedImportRuleSet_ScraperId",
                table: "OrderedImportRuleSet");

            migrationBuilder.DropColumn(
                name: "IncomingFeedId",
                table: "OrderedImportRuleSet");

            migrationBuilder.DropColumn(
                name: "ImporterId",
                table: "OrderedImportRuleSet");

            migrationBuilder.AlterColumn<Guid>(
                name: "ScraperId",
                table: "OrderedImportRuleSet",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ImportRuleSets",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderedImportRuleSet",
                table: "OrderedImportRuleSet",
                columns: new[] { "ScraperId", "ImportRuleSetId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_URL",
                table: "Tags",
                column: "URL");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderedImportRuleSet_Scrapers_ScraperId",
                table: "OrderedImportRuleSet",
                column: "ScraperId",
                principalTable: "Scrapers",
                principalColumn: "ScraperId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderedImportRuleSet_Scrapers_ScraperId",
                table: "OrderedImportRuleSet");

            migrationBuilder.DropIndex(
                name: "IX_Tags_URL",
                table: "Tags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderedImportRuleSet",
                table: "OrderedImportRuleSet");

            migrationBuilder.AlterColumn<Guid>(
                name: "ScraperId",
                table: "OrderedImportRuleSet",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "IncomingFeedId",
                table: "OrderedImportRuleSet",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ImporterId",
                table: "OrderedImportRuleSet",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ImportRuleSets",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderedImportRuleSet",
                table: "OrderedImportRuleSet",
                columns: new[] { "IncomingFeedId", "ImportRuleSetId" });

            migrationBuilder.CreateTable(
                name: "ImporterError",
                columns: table => new
                {
                    ImporterErrorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ImporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImporterError", x => x.ImporterErrorId);
                    table.ForeignKey(
                        name: "FK_ImporterError_IncomingFeeds_ImporterId",
                        column: x => x.ImporterId,
                        principalTable: "IncomingFeeds",
                        principalColumn: "ImporterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_URL",
                table: "Tags",
                column: "URL",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderedImportRuleSet_ImporterId",
                table: "OrderedImportRuleSet",
                column: "ImporterId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderedImportRuleSet_ScraperId",
                table: "OrderedImportRuleSet",
                column: "ScraperId");

            migrationBuilder.CreateIndex(
                name: "IX_ImporterError_ImporterId",
                table: "ImporterError",
                column: "ImporterId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderedImportRuleSet_IncomingFeeds_ImporterId",
                table: "OrderedImportRuleSet",
                column: "ImporterId",
                principalTable: "IncomingFeeds",
                principalColumn: "ImporterId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderedImportRuleSet_Scrapers_ScraperId",
                table: "OrderedImportRuleSet",
                column: "ScraperId",
                principalTable: "Scrapers",
                principalColumn: "ScraperId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
