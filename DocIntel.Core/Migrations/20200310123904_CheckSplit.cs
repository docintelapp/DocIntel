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

using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class CheckSplit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_Change_Activity_ActivityId",
                "Change");

            migrationBuilder.DropPrimaryKey(
                "PK_Change",
                "Change");

            migrationBuilder.RenameTable(
                "Change",
                newName: "Changes");

            migrationBuilder.RenameIndex(
                "IX_Change_ActivityId",
                table: "Changes",
                newName: "IX_Changes_ActivityId");

            migrationBuilder.AddPrimaryKey(
                "PK_Changes",
                "Changes",
                "ChangeId");

            migrationBuilder.AddForeignKey(
                "FK_Changes_Activity_ActivityId",
                "Changes",
                "ActivityId",
                "Activity",
                principalColumn: "ActivityId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_Changes_Activity_ActivityId",
                "Changes");

            migrationBuilder.DropPrimaryKey(
                "PK_Changes",
                "Changes");

            migrationBuilder.RenameTable(
                "Changes",
                newName: "Change");

            migrationBuilder.RenameIndex(
                "IX_Changes_ActivityId",
                table: "Change",
                newName: "IX_Change_ActivityId");

            migrationBuilder.AddPrimaryKey(
                "PK_Change",
                "Change",
                "ChangeId");

            migrationBuilder.AddForeignKey(
                "FK_Change_Activity_ActivityId",
                "Change",
                "ActivityId",
                "Activity",
                principalColumn: "ActivityId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}