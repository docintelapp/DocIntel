using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json.Linq;

namespace DocIntel.Core.Migrations
{
    public partial class metadataandurls : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                "URL",
                "Tags",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<JObject>(
                "MetaData",
                "Sources",
                "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "URL",
                "Sources",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<JObject>(
                "MetaData",
                "Files",
                "jsonb",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                "URL",
                "Documents",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<JObject>(
                "MetaData",
                "Documents",
                "jsonb",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                "AK_Tags_URL",
                "Tags",
                "URL");

            migrationBuilder.AddUniqueConstraint(
                "AK_Sources_URL",
                "Sources",
                "URL");

            migrationBuilder.AddUniqueConstraint(
                "AK_Documents_URL",
                "Documents",
                "URL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                "AK_Tags_URL",
                "Tags");

            migrationBuilder.DropUniqueConstraint(
                "AK_Sources_URL",
                "Sources");

            migrationBuilder.DropUniqueConstraint(
                "AK_Documents_URL",
                "Documents");

            migrationBuilder.DropColumn(
                "URL",
                "Tags");

            migrationBuilder.DropColumn(
                "MetaData",
                "Sources");

            migrationBuilder.DropColumn(
                "URL",
                "Sources");

            migrationBuilder.DropColumn(
                "MetaData",
                "Files");

            migrationBuilder.DropColumn(
                "MetaData",
                "Documents");

            migrationBuilder.AlterColumn<string>(
                "URL",
                "Documents",
                "text",
                nullable: true,
                oldClrType: typeof(string));
        }
    }
}