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
    public partial class SplitActivities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "Summary",
                "Activity");

            migrationBuilder.DropColumn(
                "TargetId",
                "Activity");

            migrationBuilder.DropColumn(
                "TargetType",
                "Activity");

            migrationBuilder.DropColumn(
                "Verb",
                "Activity");

            migrationBuilder.CreateTable(
                "Change",
                table => new
                {
                    ChangeId = table.Column<string>(nullable: false),
                    TargetId = table.Column<Guid>(nullable: true),
                    TargetType = table.Column<int>(nullable: false),
                    Verb = table.Column<int>(nullable: false),
                    ActivityId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Change", x => x.ChangeId);
                    table.ForeignKey(
                        "FK_Change_Activity_ActivityId",
                        x => x.ActivityId,
                        "Activity",
                        "ActivityId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                "IX_Change_ActivityId",
                "Change",
                "ActivityId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "Change");

            migrationBuilder.AddColumn<string>(
                "Summary",
                "Activity",
                "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                "TargetId",
                "Activity",
                "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                "TargetType",
                "Activity",
                "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                "Verb",
                "Activity",
                "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}