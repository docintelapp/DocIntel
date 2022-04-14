using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class AddFieldsToSubmissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DocumentId",
                table: "SubmittedDocuments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ImporterId",
                table: "SubmittedDocuments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IngestionDate",
                table: "SubmittedDocuments",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedDocuments_DocumentId",
                table: "SubmittedDocuments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedDocuments_ImporterId",
                table: "SubmittedDocuments",
                column: "ImporterId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubmittedDocuments_Documents_DocumentId",
                table: "SubmittedDocuments",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubmittedDocuments_IncomingFeeds_ImporterId",
                table: "SubmittedDocuments",
                column: "ImporterId",
                principalTable: "IncomingFeeds",
                principalColumn: "ImporterId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubmittedDocuments_Documents_DocumentId",
                table: "SubmittedDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_SubmittedDocuments_IncomingFeeds_ImporterId",
                table: "SubmittedDocuments");

            migrationBuilder.DropIndex(
                name: "IX_SubmittedDocuments_DocumentId",
                table: "SubmittedDocuments");

            migrationBuilder.DropIndex(
                name: "IX_SubmittedDocuments_ImporterId",
                table: "SubmittedDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentId",
                table: "SubmittedDocuments");

            migrationBuilder.DropColumn(
                name: "ImporterId",
                table: "SubmittedDocuments");

            migrationBuilder.DropColumn(
                name: "IngestionDate",
                table: "SubmittedDocuments");
        }
    }
}
