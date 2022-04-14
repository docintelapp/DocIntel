using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class AddUniqueConstraint3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                "Label",
                "Tags",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                "URL",
                "Documents",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                "AK_Tags_FacetId_Label",
                "Tags",
                new[] {"FacetId", "Label"});

            migrationBuilder.AddUniqueConstraint(
                "AK_Documents_URL",
                "Documents",
                "URL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                "AK_Tags_FacetId_Label",
                "Tags");

            migrationBuilder.DropUniqueConstraint(
                "AK_Documents_URL",
                "Documents");

            migrationBuilder.AlterColumn<string>(
                "Label",
                "Tags",
                "text",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                "URL",
                "Documents",
                "text",
                nullable: true,
                oldClrType: typeof(string));
        }
    }
}