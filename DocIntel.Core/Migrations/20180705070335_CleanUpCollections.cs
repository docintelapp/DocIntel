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
    public partial class CleanUpCollections : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "CollectionTag");

            migrationBuilder.DropTable(
                "DocumentCollection");

            migrationBuilder.DropTable(
                "Collections");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "Collections",
                table => new
                {
                    CollectionId = table.Column<int>(nullable: false),
                    // .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    AuthorUserId = table.Column<int>(nullable: true),
                    Body = table.Column<string>(nullable: true),
                    CreationDate = table.Column<DateTime>(nullable: false),
                    LastUpdate = table.Column<DateTime>(nullable: false),
                    Title = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collections", x => x.CollectionId);
                    table.ForeignKey(
                        "FK_Collections_User_AuthorUserId",
                        x => x.AuthorUserId,
                        "User",
                        "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                "CollectionTag",
                table => new
                {
                    CollectionId = table.Column<int>(nullable: false),
                    TagId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionTag", x => new {x.CollectionId, x.TagId});
                    table.ForeignKey(
                        "FK_CollectionTag_Collections_CollectionId",
                        x => x.CollectionId,
                        "Collections",
                        "CollectionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_CollectionTag_Tags_TagId",
                        x => x.TagId,
                        "Tags",
                        "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "DocumentCollection",
                table => new
                {
                    DocumentId = table.Column<int>(nullable: false),
                    CollectionId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentCollection", x => new {x.DocumentId, x.CollectionId});
                    table.ForeignKey(
                        "FK_DocumentCollection_Collections_CollectionId",
                        x => x.CollectionId,
                        "Collections",
                        "CollectionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_DocumentCollection_Documents_DocumentId",
                        x => x.DocumentId,
                        "Documents",
                        "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_Collections_AuthorUserId",
                "Collections",
                "AuthorUserId");

            migrationBuilder.CreateIndex(
                "IX_CollectionTag_TagId",
                "CollectionTag",
                "TagId");

            migrationBuilder.CreateIndex(
                "IX_DocumentCollection_CollectionId",
                "DocumentCollection",
                "CollectionId");
        }
    }
}