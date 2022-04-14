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
    public partial class MigratesSourceIdToGUID : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex("IX_Documents_SourceId");
            migrationBuilder.DropForeignKey("FK_Documents_Sources_SourceId", "Documents");
            migrationBuilder.DropPrimaryKey("PK_Sources", "Sources");

            migrationBuilder.AddColumn<Guid>(
                "SourceIdTemporary",
                "Sources");
            // .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AddColumn<Guid>(
                "SourceIdTemporary",
                "Documents",
                nullable: true);
            // .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            // migrationBuilder.Sql("create extension \"uuid-ossp\";");

            // Generate new identifier for the sources
            migrationBuilder.Sql(
                @"UPDATE ""Sources""
                  SET ""SourceIdTemporary"" = uuid_generate_v4();"
            );

            // Migrate from the integer identifier to the GUID
            migrationBuilder.Sql(
                @"UPDATE ""Documents""
                  SET ""SourceIdTemporary"" = (
                      SELECT ""SourceIdTemporary"" from ""Sources""
                      WHERE ""Documents"".""SourceId"" = ""Sources"".""SourceId""
                  );"
            );

            migrationBuilder.DropColumn("SourceId", "Sources");
            migrationBuilder.DropColumn("SourceId", "Documents");
            migrationBuilder.RenameColumn("SourceIdTemporary", "Sources", "SourceId");
            migrationBuilder.RenameColumn("SourceIdTemporary", "Documents", "SourceId");

            migrationBuilder.AddPrimaryKey("PK_Sources", "Sources", "SourceId");

            migrationBuilder.CreateIndex(
                "IX_Documents_SourceId",
                "Documents",
                "SourceId");

            migrationBuilder.AddForeignKey(
                "FK_Documents_Sources_SourceId",
                "Documents",
                "SourceId",
                "Sources",
                principalColumn: "SourceId",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // TODO
        }
    }
}