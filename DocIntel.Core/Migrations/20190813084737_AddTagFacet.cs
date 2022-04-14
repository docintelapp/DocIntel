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
    public partial class AddTagFacet : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                "FacetId",
                "Tags",
                nullable: true);

            migrationBuilder.CreateTable(
                "Facets",
                table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    Prefix = table.Column<string>(nullable: true)
                },
                constraints: table => { table.PrimaryKey("PK_Facets", x => x.Id); });

            migrationBuilder.CreateIndex(
                "IX_Tags_FacetId",
                "Tags",
                "FacetId");

            migrationBuilder.AddForeignKey(
                "FK_Tags_Facets_FacetId",
                "Tags",
                "FacetId",
                "Facets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_Tags_Facets_FacetId",
                "Tags");

            migrationBuilder.DropTable(
                "Facets");

            migrationBuilder.DropIndex(
                "IX_Tags_FacetId",
                "Tags");

            migrationBuilder.DropColumn(
                "FacetId",
                "Tags");
        }
    }
}