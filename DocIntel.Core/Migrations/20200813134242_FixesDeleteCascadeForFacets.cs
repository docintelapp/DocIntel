using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class FixesDeleteCascadeForFacets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_Tags_Facets_FacetId",
                "Tags");

            migrationBuilder.AddForeignKey(
                "FK_Tags_Facets_FacetId",
                "Tags",
                "FacetId",
                "Facets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_Tags_Facets_FacetId",
                "Tags");

            migrationBuilder.AddForeignKey(
                "FK_Tags_Facets_FacetId",
                "Tags",
                "FacetId",
                "Facets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}