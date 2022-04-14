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
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "Tags",
                table => new
                {
                    TagId = table.Column<int>(nullable: false),
                    // .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    BackgroundColor = table.Column<string>(nullable: true),
                    Label = table.Column<string>(nullable: true),
                    TextColor = table.Column<string>(nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_Tags", x => x.TagId); });

            migrationBuilder.CreateTable(
                "Users",
                table => new
                {
                    UserId = table.Column<int>(nullable: false),
                    // .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Email = table.Column<string>(nullable: true),
                    FirstName = table.Column<string>(nullable: true),
                    LastName = table.Column<string>(nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_Users", x => x.UserId); });

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
                        "FK_Collections_Users_AuthorUserId",
                        x => x.AuthorUserId,
                        "Users",
                        "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                "Documents",
                table => new
                {
                    DocumentId = table.Column<int>(nullable: false),
                    // .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Classification = table.Column<int>(nullable: false),
                    DocumentDate = table.Column<DateTime>(nullable: false),
                    Filepath = table.Column<string>(nullable: false),
                    MispEvents = table.Column<string>(nullable: true),
                    ModificationDate = table.Column<DateTime>(nullable: false),
                    Note = table.Column<string>(nullable: true),
                    OwnerUserId = table.Column<int>(nullable: true),
                    Reference = table.Column<string>(nullable: false),
                    RegistrationDate = table.Column<DateTime>(nullable: false),
                    Sha256Hash = table.Column<string>(nullable: false),
                    ShortDescription = table.Column<string>(nullable: true),
                    Source = table.Column<string>(nullable: false),
                    SourceUrl = table.Column<string>(nullable: true),
                    Title = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.DocumentId);
                    table.UniqueConstraint("Unique_SourceURL", x => x.SourceUrl);
                    table.UniqueConstraint("Unique_Hash", x => x.Sha256Hash);
                    table.ForeignKey(
                        "FK_Documents_Users_OwnerUserId",
                        x => x.OwnerUserId,
                        "Users",
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

            migrationBuilder.CreateTable(
                "DocumentTag",
                table => new
                {
                    DocumentId = table.Column<int>(nullable: false),
                    TagId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTag", x => new {x.DocumentId, x.TagId});
                    table.ForeignKey(
                        "FK_DocumentTag_Documents_DocumentId",
                        x => x.DocumentId,
                        "Documents",
                        "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_DocumentTag_Tags_TagId",
                        x => x.TagId,
                        "Tags",
                        "TagId",
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

            migrationBuilder.CreateIndex(
                "IX_Documents_OwnerUserId",
                "Documents",
                "OwnerUserId");

            migrationBuilder.CreateIndex(
                "IX_DocumentTag_TagId",
                "DocumentTag",
                "TagId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "CollectionTag");

            migrationBuilder.DropTable(
                "DocumentCollection");

            migrationBuilder.DropTable(
                "DocumentTag");

            migrationBuilder.DropTable(
                "Collections");

            migrationBuilder.DropTable(
                "Documents");

            migrationBuilder.DropTable(
                "Tags");

            migrationBuilder.DropTable(
                "Users");
        }
    }
}