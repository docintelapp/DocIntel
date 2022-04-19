using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocIntel.Core.Migrations
{
    public partial class RenameFacetId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Facets",
                newName: "FacetId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FacetId",
                table: "Facets",
                newName: "Id");
        }
    }
}
