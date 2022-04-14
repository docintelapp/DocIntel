using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class AddSourceOverride2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "SourceId",
                table: "Scrapers",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_Scrapers_SourceId",
                table: "Scrapers",
                column: "SourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Scrapers_Sources_SourceId",
                table: "Scrapers",
                column: "SourceId",
                principalTable: "Sources",
                principalColumn: "SourceId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Scrapers_Sources_SourceId",
                table: "Scrapers");

            migrationBuilder.DropIndex(
                name: "IX_Scrapers_SourceId",
                table: "Scrapers");

            migrationBuilder.AlterColumn<Guid>(
                name: "SourceId",
                table: "Scrapers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
