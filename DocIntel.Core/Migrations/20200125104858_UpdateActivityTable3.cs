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
    public partial class UpdateActivityTable3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                "TargetGuid",
                "Activity",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                "ObjectGuid",
                "Activity",
                nullable: true);

            migrationBuilder.DropColumn("TargetId", "Activity");
            migrationBuilder.DropColumn("ObjectId", "Activity");

            migrationBuilder.RenameColumn("TargetGuid", "Activity", "TargetId");
            migrationBuilder.RenameColumn("ObjectGuid", "Activity", "ObjectId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                "TargetId",
                "Activity",
                "text",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AlterColumn<string>(
                "ObjectId",
                "Activity",
                "text",
                nullable: true,
                oldClrType: typeof(Guid));
        }
    }
}