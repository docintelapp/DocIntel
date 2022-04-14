using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class UpdatesAlternateKeyToUniqueIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "AK_Tags_FacetId_Label",
                table: "Tags");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_FacetId_Label",
                table: "Tags",
                columns: new[] { "FacetId", "Label" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tags_FacetId_Label",
                table: "Tags");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Tags_FacetId_Label",
                table: "Tags",
                columns: new[] { "FacetId", "Label" });
        }
    }
}
