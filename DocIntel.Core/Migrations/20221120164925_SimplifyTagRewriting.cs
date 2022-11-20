using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocIntel.Core.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyTagRewriting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderedImportRuleSet");

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "ImportRuleSets",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Position",
                table: "ImportRuleSets");

            migrationBuilder.CreateTable(
                name: "OrderedImportRuleSet",
                columns: table => new
                {
                    ScraperId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportRuleSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderedImportRuleSet", x => new { x.ScraperId, x.ImportRuleSetId });
                    table.ForeignKey(
                        name: "FK_OrderedImportRuleSet_ImportRuleSets_ImportRuleSetId",
                        column: x => x.ImportRuleSetId,
                        principalTable: "ImportRuleSets",
                        principalColumn: "ImportRuleSetId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderedImportRuleSet_Scrapers_ScraperId",
                        column: x => x.ScraperId,
                        principalTable: "Scrapers",
                        principalColumn: "ScraperId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderedImportRuleSet_ImportRuleSetId",
                table: "OrderedImportRuleSet",
                column: "ImportRuleSetId");
        }
    }
}
