using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class UpdatesThumbnailDeleteDefault : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Files_ThumbnailId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ThumbnailId",
                table: "Documents");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ThumbnailId",
                table: "Documents",
                column: "ThumbnailId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Files_ThumbnailId",
                table: "Documents",
                column: "ThumbnailId",
                principalTable: "Files",
                principalColumn: "FileId",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Files_ThumbnailId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ThumbnailId",
                table: "Documents");

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
    }
}
