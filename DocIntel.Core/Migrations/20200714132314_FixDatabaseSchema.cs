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
    public partial class FixDatabaseSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                "ImportRuleSetId",
                "IncomingFeeds",
                nullable: true);

            migrationBuilder.CreateIndex(
                "IX_IncomingFeeds_ImportRuleSetId",
                "IncomingFeeds",
                "ImportRuleSetId");

            migrationBuilder.AddForeignKey(
                "FK_IncomingFeeds_ImportRuleSets_ImportRuleSetId",
                "IncomingFeeds",
                "ImportRuleSetId",
                "ImportRuleSets",
                principalColumn: "ImportRuleSetId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_IncomingFeeds_ImportRuleSets_ImportRuleSetId",
                "IncomingFeeds");

            migrationBuilder.DropIndex(
                "IX_IncomingFeeds_ImportRuleSetId",
                "IncomingFeeds");

            migrationBuilder.DropColumn(
                "ImportRuleSetId",
                "IncomingFeeds");
        }
    }
}