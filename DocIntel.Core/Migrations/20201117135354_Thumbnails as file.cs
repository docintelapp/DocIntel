using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class Thumbnailsasfile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                "Thumbnail",
                "Files",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                "Visible",
                "Files",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                "Title",
                "Facets",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "Thumbnail",
                "Files");

            migrationBuilder.DropColumn(
                "Visible",
                "Files");

            migrationBuilder.AlterColumn<string>(
                "Title",
                "Facets",
                "text",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);
        }
    }
}