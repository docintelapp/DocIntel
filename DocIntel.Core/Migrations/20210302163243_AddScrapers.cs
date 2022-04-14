using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json.Linq;

namespace DocIntel.Core.Migrations
{
    public partial class AddScrapers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ScraperId",
                table: "OrderedImportRuleSet",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastCollection",
                table: "IncomingFeeds",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.CreateTable(
                name: "Scrapers",
                columns: table => new
                {
                    ScraperId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Settings = table.Column<JObject>(type: "jsonb", nullable: true),
                    ReferenceClass = table.Column<string>(type: "text", nullable: true),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkipInbox = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scrapers", x => x.ScraperId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderedImportRuleSet_ScraperId",
                table: "OrderedImportRuleSet",
                column: "ScraperId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderedImportRuleSet_Scrapers_ScraperId",
                table: "OrderedImportRuleSet",
                column: "ScraperId",
                principalTable: "Scrapers",
                principalColumn: "ScraperId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderedImportRuleSet_Scrapers_ScraperId",
                table: "OrderedImportRuleSet");

            migrationBuilder.DropTable(
                name: "Scrapers");

            migrationBuilder.DropIndex(
                name: "IX_OrderedImportRuleSet_ScraperId",
                table: "OrderedImportRuleSet");

            migrationBuilder.DropColumn(
                name: "ScraperId",
                table: "OrderedImportRuleSet");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastCollection",
                table: "IncomingFeeds",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);
        }
    }
}
