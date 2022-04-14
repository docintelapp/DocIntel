using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class AddAppPasswordsDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                "CreationDate",
                "AppPassword",
                nullable: false,
                defaultValue: DateTime.UtcNow);

            migrationBuilder.AddColumn<string>(
                "LastIP",
                "AppPassword",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                "LastUpdate",
                "AppPassword",
                nullable: false,
                defaultValue: DateTime.UtcNow);

            migrationBuilder.AddColumn<DateTime>(
                "LastUsage",
                "AppPassword",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "CreationDate",
                "AppPassword");

            migrationBuilder.DropColumn(
                "LastIP",
                "AppPassword");

            migrationBuilder.DropColumn(
                "LastUpdate",
                "AppPassword");

            migrationBuilder.DropColumn(
                "LastUsage",
                "AppPassword");
        }
    }
}