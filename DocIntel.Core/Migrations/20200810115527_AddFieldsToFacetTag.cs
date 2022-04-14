using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class AddFieldsToFacetTag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                "CreatedById",
                "Facets",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                "CreationDate",
                "Facets",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                "LastModifiedById",
                "Facets",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                "ModificationDate",
                "Facets",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                "IX_Facets_CreatedById",
                "Facets",
                "CreatedById");

            migrationBuilder.CreateIndex(
                "IX_Facets_LastModifiedById",
                "Facets",
                "LastModifiedById");

            migrationBuilder.AddForeignKey(
                "FK_Facets_AspNetUsers_CreatedById",
                "Facets",
                "CreatedById",
                "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_Facets_AspNetUsers_LastModifiedById",
                "Facets",
                "LastModifiedById",
                "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_Facets_AspNetUsers_CreatedById",
                "Facets");

            migrationBuilder.DropForeignKey(
                "FK_Facets_AspNetUsers_LastModifiedById",
                "Facets");

            migrationBuilder.DropIndex(
                "IX_Facets_CreatedById",
                "Facets");

            migrationBuilder.DropIndex(
                "IX_Facets_LastModifiedById",
                "Facets");

            migrationBuilder.DropColumn(
                "CreatedById",
                "Facets");

            migrationBuilder.DropColumn(
                "CreationDate",
                "Facets");

            migrationBuilder.DropColumn(
                "LastModifiedById",
                "Facets");

            migrationBuilder.DropColumn(
                "ModificationDate",
                "Facets");
        }
    }
}