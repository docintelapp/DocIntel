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
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace DocIntel.Core.Migrations
{
    public partial class RemovesObservables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "Observables");

            migrationBuilder.DropTable(
                "WhiteListObservables");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "Observables",
                table => new
                {
                    ObservableId = table.Column<Guid>("uuid", nullable: false),
                    DocumentId = table.Column<Guid>("uuid", nullable: false),
                    Type = table.Column<string>("text", nullable: false),
                    Value = table.Column<string>("text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Observables", x => x.ObservableId);
                    table.ForeignKey(
                        "FK_Observables_Documents_DocumentId",
                        x => x.DocumentId,
                        "Documents",
                        "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "WhiteListObservables",
                table => new
                {
                    WhiteListObservableId = table.Column<int>("integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<string>("text", nullable: false),
                    Value = table.Column<string>("text", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_WhiteListObservables", x => x.WhiteListObservableId); });

            migrationBuilder.CreateIndex(
                "IX_Observables_DocumentId",
                "Observables",
                "DocumentId");
        }
    }
}