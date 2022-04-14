using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json.Linq;

namespace DocIntel.Core.Migrations
{
    public partial class AddMetadatatotagsandfacets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<JObject>(
                "MetaData",
                "Tags",
                "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<JObject>(
                "MetaData",
                "Facets",
                "jsonb",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "MetaData",
                "Tags");

            migrationBuilder.DropColumn(
                "MetaData",
                "Facets");
        }
    }
}