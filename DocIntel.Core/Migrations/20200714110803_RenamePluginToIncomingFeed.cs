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
    public partial class RenamePluginToIncomingFeed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_OrderedImportRuleSet_Plugins_PluginId",
                "OrderedImportRuleSet");

            migrationBuilder.DropPrimaryKey(
                "PK_OrderedImportRuleSet",
                "OrderedImportRuleSet");

            migrationBuilder.DropPrimaryKey(
                "PK_Plugins",
                "Plugins");

            migrationBuilder.RenameTable(
                "Plugins",
                newName: "IncomingFeeds");

            migrationBuilder.RenameColumn(
                "PluginId",
                "IncomingFeeds",
                "IncomingFeedId");

            migrationBuilder.RenameColumn(
                "PluginId",
                "OrderedImportRuleSet",
                "IncomingFeedId");

            migrationBuilder.AddPrimaryKey(
                "PK_OrderedImportRuleSet",
                "OrderedImportRuleSet",
                new[] {"IncomingFeedId", "ImportRuleSetId"});

            migrationBuilder.AddPrimaryKey(
                "PK_IncomingFeeds",
                "IncomingFeeds",
                new[] {"IncomingFeedId"});

            migrationBuilder.AddForeignKey(
                "FK_OrderedImportRuleSet_IncomingFeeds_IncomingFeedId",
                "OrderedImportRuleSet",
                "IncomingFeedId",
                "IncomingFeeds",
                principalColumn: "IncomingFeedId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotImplementedException();
        }
    }
}