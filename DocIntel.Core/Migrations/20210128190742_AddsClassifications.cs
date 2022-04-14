using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class AddsClassifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Classification",
                table: "Inbox");

            migrationBuilder.DropColumn(
                name: "Classification",
                table: "Files");

            migrationBuilder.AddColumn<Guid>(
                name: "ClassificationId",
                table: "Inbox",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClassificationId",
                table: "Files",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OverrideClassification",
                table: "Files",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Classifications",
                columns: table => new
                {
                    ClassificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Subtitle = table.Column<string>(type: "text", nullable: true),
                    Color = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ParentClassificationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classifications", x => x.ClassificationId);
                    table.ForeignKey(
                        name: "FK_Classifications_Classifications_ParentClassificationId",
                        column: x => x.ParentClassificationId,
                        principalTable: "Classifications",
                        principalColumn: "ClassificationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inbox_ClassificationId",
                table: "Inbox",
                column: "ClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_ClassificationId",
                table: "Files",
                column: "ClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Classifications_ParentClassificationId",
                table: "Classifications",
                column: "ParentClassificationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Classifications_ClassificationId",
                table: "Files",
                column: "ClassificationId",
                principalTable: "Classifications",
                principalColumn: "ClassificationId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inbox_Classifications_ClassificationId",
                table: "Inbox",
                column: "ClassificationId",
                principalTable: "Classifications",
                principalColumn: "ClassificationId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Classifications_ClassificationId",
                table: "Files");

            migrationBuilder.DropForeignKey(
                name: "FK_Inbox_Classifications_ClassificationId",
                table: "Inbox");

            migrationBuilder.DropTable(
                name: "Classifications");

            migrationBuilder.DropIndex(
                name: "IX_Inbox_ClassificationId",
                table: "Inbox");

            migrationBuilder.DropIndex(
                name: "IX_Files_ClassificationId",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "ClassificationId",
                table: "Inbox");

            migrationBuilder.DropColumn(
                name: "ClassificationId",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "OverrideClassification",
                table: "Files");

            migrationBuilder.AddColumn<int>(
                name: "Classification",
                table: "Inbox",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Classification",
                table: "Files",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
