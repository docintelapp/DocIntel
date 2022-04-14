using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class updatesdbconstraints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                "AK_Tags_URL",
                "Tags");

            migrationBuilder.DropUniqueConstraint(
                "AK_Sources_URL",
                "Sources");

            migrationBuilder.DropUniqueConstraint(
                "AK_Facets_Prefix",
                "Facets");

            migrationBuilder.DropUniqueConstraint(
                "AK_Documents_URL",
                "Documents");

            migrationBuilder.DropColumn(
                "TextColor",
                "Tags");

            migrationBuilder.AlterColumn<string>(
                "Title",
                "Facets",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                "Reference",
                "Documents",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                "IX_Tags_URL",
                "Tags",
                "URL",
                unique: true);

            migrationBuilder.CreateIndex(
                "IX_Sources_URL",
                "Sources",
                "URL",
                unique: true);

            migrationBuilder.CreateIndex(
                "IX_Facets_Prefix",
                "Facets",
                "Prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                "IX_Documents_URL",
                "Documents",
                "URL",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                "IX_Tags_URL",
                "Tags");

            migrationBuilder.DropIndex(
                "IX_Sources_URL",
                "Sources");

            migrationBuilder.DropIndex(
                "IX_Facets_Prefix",
                "Facets");

            migrationBuilder.DropIndex(
                "IX_Documents_URL",
                "Documents");

            migrationBuilder.AddColumn<string>(
                "TextColor",
                "Tags",
                "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                "Title",
                "Facets",
                "text",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                "Reference",
                "Documents",
                "text",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AddUniqueConstraint(
                "AK_Tags_URL",
                "Tags",
                "URL");

            migrationBuilder.AddUniqueConstraint(
                "AK_Sources_URL",
                "Sources",
                "URL");

            migrationBuilder.AddUniqueConstraint(
                "AK_Facets_Prefix",
                "Facets",
                "Prefix");

            migrationBuilder.AddUniqueConstraint(
                "AK_Documents_URL",
                "Documents",
                "URL");
        }
    }
}