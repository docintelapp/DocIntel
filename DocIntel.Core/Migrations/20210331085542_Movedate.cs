using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class Movedate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Date",
                table: "SubmittedDocuments");

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmissionDate",
                table: "SubmittedDocuments",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmissionDate",
                table: "SubmittedDocuments");

            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "SubmittedDocuments",
                type: "timestamp without time zone",
                nullable: true);
        }
    }
}
