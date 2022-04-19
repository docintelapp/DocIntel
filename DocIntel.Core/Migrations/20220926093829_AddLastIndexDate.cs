using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocIntel.Core.Migrations
{
    public partial class AddLastIndexDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastIndexDate",
                table: "Tags",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastIndexDate",
                table: "Sources",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastIndexDate",
                table: "Facets",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastIndexDate",
                table: "Documents",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastIndexDate",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "LastIndexDate",
                table: "Sources");

            migrationBuilder.DropColumn(
                name: "LastIndexDate",
                table: "Facets");

            migrationBuilder.DropColumn(
                name: "LastIndexDate",
                table: "Documents");
        }
    }
}
