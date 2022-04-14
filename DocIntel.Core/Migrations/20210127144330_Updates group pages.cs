using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class Updatesgrouppages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Groups_AspNetUsers_AppUserId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_AppUserId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "Groups");
            
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Groups",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Hidden",
                table: "Groups",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hidden",
                table: "Groups");
            
            migrationBuilder.AddColumn<string>(
                name: "AppUserId",
                table: "Groups",
                nullable: true);
            
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Groups");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_AppUserId",
                table: "Groups",
                column: "AppUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_AspNetUsers_AppUserId",
                table: "Groups",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
