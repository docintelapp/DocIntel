using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class RenamesAppPasswordtoAPIKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "AppPassword");

            migrationBuilder.CreateTable(
                "APIKeys",
                table => new
                {
                    APIKeyId = table.Column<Guid>(nullable: false),
                    UserId = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    Key = table.Column<string>(nullable: true),
                    CreationDate = table.Column<DateTime>(nullable: false),
                    ModificationDate = table.Column<DateTime>(nullable: false),
                    LastUsage = table.Column<DateTime>(nullable: true),
                    LastIP = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APIKeys", x => x.APIKeyId);
                    table.ForeignKey(
                        "FK_APIKeys_AspNetUsers_UserId",
                        x => x.UserId,
                        "AspNetUsers",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                "IX_APIKeys_UserId",
                "APIKeys",
                "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "APIKeys");

            migrationBuilder.CreateTable(
                "AppPassword",
                table => new
                {
                    AppPasswordId = table.Column<Guid>("uuid", nullable: false),
                    CreationDate = table.Column<DateTime>("timestamp without time zone", nullable: false),
                    Description = table.Column<string>("text", nullable: true),
                    LastIP = table.Column<string>("text", nullable: true),
                    LastUpdate = table.Column<DateTime>("timestamp without time zone", nullable: false),
                    LastUsage = table.Column<DateTime>("timestamp without time zone", nullable: true),
                    Name = table.Column<string>("text", nullable: true),
                    Password = table.Column<string>("text", nullable: true),
                    UserId = table.Column<string>("text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPassword", x => x.AppPasswordId);
                    table.ForeignKey(
                        "FK_AppPassword_AspNetUsers_UserId",
                        x => x.UserId,
                        "AspNetUsers",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                "IX_AppPassword_UserId",
                "AppPassword",
                "UserId");
        }
    }
}