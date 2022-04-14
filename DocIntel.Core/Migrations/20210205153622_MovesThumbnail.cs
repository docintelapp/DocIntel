using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class MovesThumbnail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Files_ThumbnailId",
                table: "Files");

            migrationBuilder.DropIndex(
                name: "IX_Files_ThumbnailId",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "ThumbnailId",
                table: "Files");

            migrationBuilder.AddColumn<Guid>(
                name: "ThumbnailId",
                table: "Documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ThumbnailId",
                table: "Documents",
                column: "ThumbnailId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Files_ThumbnailId",
                table: "Documents",
                column: "ThumbnailId",
                principalTable: "Files",
                principalColumn: "FileId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Files_ThumbnailId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ThumbnailId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ThumbnailId",
                table: "Documents");

            migrationBuilder.AddColumn<Guid>(
                name: "ThumbnailId",
                table: "Files",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Files_ThumbnailId",
                table: "Files",
                column: "ThumbnailId");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Files_ThumbnailId",
                table: "Files",
                column: "ThumbnailId",
                principalTable: "Files",
                principalColumn: "FileId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
