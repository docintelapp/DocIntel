using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class FineGrainedOverrideforImportersAndScrapers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OverrideClassification",
                table: "SubmittedDocuments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OverrideEyesOnly",
                table: "SubmittedDocuments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OverrideReleasableTo",
                table: "SubmittedDocuments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OverrideEyesOnly",
                table: "Scrapers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OverrideReleasableTo",
                table: "Scrapers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OverrideEyesOnly",
                table: "IncomingFeeds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OverrideReleasableTo",
                table: "IncomingFeeds",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OverrideClassification",
                table: "SubmittedDocuments");

            migrationBuilder.DropColumn(
                name: "OverrideEyesOnly",
                table: "SubmittedDocuments");

            migrationBuilder.DropColumn(
                name: "OverrideReleasableTo",
                table: "SubmittedDocuments");

            migrationBuilder.DropColumn(
                name: "OverrideEyesOnly",
                table: "Scrapers");

            migrationBuilder.DropColumn(
                name: "OverrideReleasableTo",
                table: "Scrapers");

            migrationBuilder.DropColumn(
                name: "OverrideEyesOnly",
                table: "IncomingFeeds");

            migrationBuilder.DropColumn(
                name: "OverrideReleasableTo",
                table: "IncomingFeeds");
        }
    }
}
