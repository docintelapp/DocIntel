using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class UpdatesClassificationOnDocs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClassificationId",
                table: "Documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ClassificationId",
                table: "Documents",
                column: "ClassificationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Classifications_ClassificationId",
                table: "Documents",
                column: "ClassificationId",
                principalTable: "Classifications",
                principalColumn: "ClassificationId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Classifications_ClassificationId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ClassificationId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ClassificationId",
                table: "Documents");
        }
    }
}
