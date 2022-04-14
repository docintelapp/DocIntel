/*
 * DocIntel
 * Copyright (C) 2018-2021 Belgian Defense, Antoine Cailliau
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class AddImportRules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "ImportRuleSets",
                table => new
                {
                    ImportRuleSetId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_ImportRuleSets", x => x.ImportRuleSetId); });

            migrationBuilder.CreateTable(
                "ImportRules",
                table => new
                {
                    ImportRuleId = table.Column<Guid>(nullable: false),
                    Position = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    SearchPattern = table.Column<string>(nullable: true),
                    Replacement = table.Column<string>(nullable: true),
                    ImportRuleSetId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportRules", x => x.ImportRuleId);
                    table.ForeignKey(
                        "FK_ImportRules_ImportRuleSets_ImportRuleSetId",
                        x => x.ImportRuleSetId,
                        "ImportRuleSets",
                        "ImportRuleSetId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                "OrderedImportRuleSet",
                table => new
                {
                    PluginId = table.Column<Guid>(nullable: false),
                    ImportRuleSetId = table.Column<Guid>(nullable: false),
                    Position = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderedImportRuleSet", x => new {x.PluginId, x.ImportRuleSetId});
                    table.ForeignKey(
                        "FK_OrderedImportRuleSet_ImportRuleSets_ImportRuleSetId",
                        x => x.ImportRuleSetId,
                        "ImportRuleSets",
                        "ImportRuleSetId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_OrderedImportRuleSet_Plugins_PluginId",
                        x => x.PluginId,
                        "Plugins",
                        "PluginId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_ImportRules_ImportRuleSetId",
                "ImportRules",
                "ImportRuleSetId");

            migrationBuilder.CreateIndex(
                "IX_OrderedImportRuleSet_ImportRuleSetId",
                "OrderedImportRuleSet",
                "ImportRuleSetId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "ImportRules");

            migrationBuilder.DropTable(
                "OrderedImportRuleSet");

            migrationBuilder.DropTable(
                "ImportRuleSets");
        }
    }
}