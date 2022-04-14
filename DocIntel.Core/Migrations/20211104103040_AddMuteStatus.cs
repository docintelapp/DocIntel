using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class AddMuteStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BiasedWording",
                table: "Sources");

            migrationBuilder.DropColumn(
                name: "Factual",
                table: "Sources");

            migrationBuilder.DropColumn(
                name: "PoliticalAffiliation",
                table: "Sources");

            migrationBuilder.DropColumn(
                name: "PoliticalSpectrum",
                table: "Sources");

            migrationBuilder.DropColumn(
                name: "StoryChoice",
                table: "Sources");

            migrationBuilder.AddColumn<bool>(
                name: "Muted",
                table: "UserTagSubscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Subscribed",
                table: "UserTagSubscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Muted",
                table: "UserTagSubscriptions");

            migrationBuilder.DropColumn(
                name: "Subscribed",
                table: "UserTagSubscriptions");

            migrationBuilder.AddColumn<int>(
                name: "BiasedWording",
                table: "Sources",
                type: "integer",
                nullable: false,
                defaultValue: -1);

            migrationBuilder.AddColumn<int>(
                name: "Factual",
                table: "Sources",
                type: "integer",
                nullable: false,
                defaultValue: -1);

            migrationBuilder.AddColumn<int>(
                name: "PoliticalAffiliation",
                table: "Sources",
                type: "integer",
                nullable: false,
                defaultValue: -1);

            migrationBuilder.AddColumn<int>(
                name: "PoliticalSpectrum",
                table: "Sources",
                type: "integer",
                nullable: false,
                defaultValue: -1);

            migrationBuilder.AddColumn<int>(
                name: "StoryChoice",
                table: "Sources",
                type: "integer",
                nullable: false,
                defaultValue: -1);
        }
    }
}
