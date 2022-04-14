using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class AddUniqueConstraint2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                "IX_Facets_Prefix",
                "Facets");

            migrationBuilder.Sql("UPDATE \"Facets\" SET \"Prefix\" = \'other\' WHERE \"Prefix\" = NULL;");
            
            migrationBuilder.AlterColumn<string>(
                "Prefix",
                "Facets",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                "AK_Facets_Prefix",
                "Facets",
                "Prefix");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                "AK_Facets_Prefix",
                "Facets");

            migrationBuilder.AlterColumn<string>(
                "Prefix",
                "Facets",
                "text",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.CreateIndex(
                "IX_Facets_Prefix",
                "Facets",
                "Prefix",
                unique: true);
        }
    }
}