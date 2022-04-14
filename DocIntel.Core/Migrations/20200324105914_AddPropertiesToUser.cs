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
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class AddPropertiesToUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "DefaultTagColor",
                "AspNetUsers");

            migrationBuilder.AddColumn<bool>(
                "Enabled",
                "AspNetUsers",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                "LastActivity",
                "AspNetUsers",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<JsonDocument>(
                "Preferences",
                "AspNetUsers",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "Enabled",
                "AspNetUsers");

            migrationBuilder.DropColumn(
                "LastActivity",
                "AspNetUsers");

            migrationBuilder.DropColumn(
                "Preferences",
                "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                "DefaultTagColor",
                "AspNetUsers",
                "text",
                nullable: true);
        }
    }
}