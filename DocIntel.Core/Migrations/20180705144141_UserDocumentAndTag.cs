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
    public partial class UserDocumentAndTag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_Documents_User_OwnerUserId",
                "Documents");

            migrationBuilder.DropTable(
                "User");

            migrationBuilder.DropIndex(
                "IX_Documents_OwnerUserId",
                "Documents");

            migrationBuilder.DropColumn(
                "OwnerUserId",
                "Documents");

            migrationBuilder.AddColumn<string>(
                "CreatedBy",
                "Tags",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "LastModifiedBy",
                "Tags",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "LastModifiedBy",
                "Documents",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "RegisteredBy",
                "Documents",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "CreatedBy",
                "Tags");

            migrationBuilder.DropColumn(
                "LastModifiedBy",
                "Tags");

            migrationBuilder.DropColumn(
                "LastModifiedBy",
                "Documents");

            migrationBuilder.DropColumn(
                "RegisteredBy",
                "Documents");

            migrationBuilder.AddColumn<int>(
                "OwnerUserId",
                "Documents",
                nullable: true);

            migrationBuilder.CreateTable(
                "User",
                table => new
                {
                    UserId = table.Column<int>(nullable: false),
                    // .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Email = table.Column<string>(nullable: true),
                    FirstName = table.Column<string>(nullable: true),
                    LastName = table.Column<string>(nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_User", x => x.UserId); });

            migrationBuilder.CreateIndex(
                "IX_Documents_OwnerUserId",
                "Documents",
                "OwnerUserId");

            migrationBuilder.AddForeignKey(
                "FK_Documents_User_OwnerUserId",
                "Documents",
                "OwnerUserId",
                "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}