using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class Thumbnailsasfile2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "Thumbnail",
                "Files");

            migrationBuilder.AddColumn<Guid>(
                "ThumbnailId",
                "Files",
                nullable: true);

            migrationBuilder.CreateIndex(
                "IX_Files_ThumbnailId",
                "Files",
                "ThumbnailId");

            migrationBuilder.AddForeignKey(
                "FK_Files_Files_ThumbnailId",
                "Files",
                "ThumbnailId",
                "Files",
                principalColumn: "FileId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_Files_Files_ThumbnailId",
                "Files");

            migrationBuilder.DropIndex(
                "IX_Files_ThumbnailId",
                "Files");

            migrationBuilder.DropColumn(
                "ThumbnailId",
                "Files");

            migrationBuilder.AddColumn<bool>(
                "Thumbnail",
                "Files",
                "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}