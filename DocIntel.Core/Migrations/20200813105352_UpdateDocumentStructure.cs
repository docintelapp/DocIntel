using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class UpdateDocumentStructure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "Starred",
                "Documents");


            migrationBuilder.AddColumn<string>(
                "URL",
                "Documents",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "URL",
                "Documents");

            migrationBuilder.AddColumn<bool>(
                "Starred",
                "Documents",
                "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}