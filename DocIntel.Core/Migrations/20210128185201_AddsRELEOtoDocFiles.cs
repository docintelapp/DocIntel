using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class AddsRELEOtoDocFiles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DocumentFileFileId",
                table: "ReleasableTo",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OverrideEyesOnly",
                table: "Files",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OverrideReleasableTo",
                table: "Files",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "DocumentFileFileId",
                table: "EyesOnly",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReleasableTo_DocumentFileFileId",
                table: "ReleasableTo",
                column: "DocumentFileFileId");

            migrationBuilder.CreateIndex(
                name: "IX_EyesOnly_DocumentFileFileId",
                table: "EyesOnly",
                column: "DocumentFileFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_EyesOnly_Files_DocumentFileFileId",
                table: "EyesOnly",
                column: "DocumentFileFileId",
                principalTable: "Files",
                principalColumn: "FileId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReleasableTo_Files_DocumentFileFileId",
                table: "ReleasableTo",
                column: "DocumentFileFileId",
                principalTable: "Files",
                principalColumn: "FileId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EyesOnly_Files_DocumentFileFileId",
                table: "EyesOnly");

            migrationBuilder.DropForeignKey(
                name: "FK_ReleasableTo_Files_DocumentFileFileId",
                table: "ReleasableTo");

            migrationBuilder.DropIndex(
                name: "IX_ReleasableTo_DocumentFileFileId",
                table: "ReleasableTo");

            migrationBuilder.DropIndex(
                name: "IX_EyesOnly_DocumentFileFileId",
                table: "EyesOnly");

            migrationBuilder.DropColumn(
                name: "DocumentFileFileId",
                table: "ReleasableTo");

            migrationBuilder.DropColumn(
                name: "OverrideEyesOnly",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "OverrideReleasableTo",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "DocumentFileFileId",
                table: "EyesOnly");
        }
    }
}
