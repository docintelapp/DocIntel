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
    public partial class FixesUserRelations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "LastModifiedBy",
                "Tags");

            migrationBuilder.DropColumn(
                "CreatedBy",
                "Tags");

            migrationBuilder.DropColumn(
                "RegisteredBy",
                "Documents");

            migrationBuilder.DropColumn(
                "LastModifiedBy",
                "Documents");

            migrationBuilder.AddColumn<string>(
                "LastModifiedById",
                "Tags",
                nullable: true); // for legacy reasons...

            migrationBuilder.AddColumn<string>(
                "CreatedById",
                "Tags",
                nullable: true); // for legacy reasons...

            migrationBuilder.AddColumn<string>(
                "RegisteredById",
                "Documents",
                nullable: true); // for legacy reasons...

            migrationBuilder.AddColumn<string>(
                "LastModifiedById",
                "Documents",
                nullable: true); // for legacy reasons...

            migrationBuilder.CreateIndex(
                "IX_Tags_CreatedById",
                "Tags",
                "CreatedById");

            migrationBuilder.CreateIndex(
                "IX_Tags_LastModifiedById",
                "Tags",
                "LastModifiedById");

            migrationBuilder.CreateIndex(
                "IX_Documents_LastModifiedById",
                "Documents",
                "LastModifiedById");

            migrationBuilder.CreateIndex(
                "IX_Documents_RegisteredById",
                "Documents",
                "RegisteredById");

            migrationBuilder.AddForeignKey(
                "FK_Documents_AspNetUsers_LastModifiedById",
                "Documents",
                "LastModifiedById",
                "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_Documents_AspNetUsers_RegisteredById",
                "Documents",
                "RegisteredById",
                "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_Tags_AspNetUsers_CreatedById",
                "Tags",
                "CreatedById",
                "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_Tags_AspNetUsers_LastModifiedById",
                "Tags",
                "LastModifiedById",
                "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_Documents_AspNetUsers_LastModifiedById",
                "Documents");

            migrationBuilder.DropForeignKey(
                "FK_Documents_AspNetUsers_RegisteredById",
                "Documents");

            migrationBuilder.DropForeignKey(
                "FK_Tags_AspNetUsers_CreatedById",
                "Tags");

            migrationBuilder.DropForeignKey(
                "FK_Tags_AspNetUsers_LastModifiedById",
                "Tags");

            migrationBuilder.DropIndex(
                "IX_Tags_CreatedById",
                "Tags");

            migrationBuilder.DropIndex(
                "IX_Tags_LastModifiedById",
                "Tags");

            migrationBuilder.DropIndex(
                "IX_Documents_LastModifiedById",
                "Documents");

            migrationBuilder.DropIndex(
                "IX_Documents_RegisteredById",
                "Documents");

            migrationBuilder.RenameColumn(
                "LastModifiedById",
                "Tags",
                "LastModifiedBy");

            migrationBuilder.RenameColumn(
                "CreatedById",
                "Tags",
                "CreatedBy");

            migrationBuilder.RenameColumn(
                "RegisteredById",
                "Documents",
                "RegisteredBy");

            migrationBuilder.RenameColumn(
                "LastModifiedById",
                "Documents",
                "LastModifiedBy");
        }
    }
}