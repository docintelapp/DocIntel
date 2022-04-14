using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class AddClassificationsToImporters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClassificationId",
                table: "SubmittedDocuments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "SubmittedDocuments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ClassificationId",
                table: "Scrapers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OverrideClassification",
                table: "Scrapers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ClassificationId",
                table: "IncomingFeeds",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OverrideClassification",
                table: "IncomingFeeds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "IncomingFeeds",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ImporterId",
                table: "Groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ImporterId1",
                table: "Groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ScraperId",
                table: "Groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ScraperId1",
                table: "Groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedDocumentId",
                table: "Groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedDocumentId1",
                table: "Groups",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedDocuments_ClassificationId",
                table: "SubmittedDocuments",
                column: "ClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Scrapers_ClassificationId",
                table: "Scrapers",
                column: "ClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingFeeds_ClassificationId",
                table: "IncomingFeeds",
                column: "ClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_ImporterId",
                table: "Groups",
                column: "ImporterId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_ImporterId1",
                table: "Groups",
                column: "ImporterId1");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_ScraperId",
                table: "Groups",
                column: "ScraperId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_ScraperId1",
                table: "Groups",
                column: "ScraperId1");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_SubmittedDocumentId",
                table: "Groups",
                column: "SubmittedDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_SubmittedDocumentId1",
                table: "Groups",
                column: "SubmittedDocumentId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_IncomingFeeds_ImporterId",
                table: "Groups",
                column: "ImporterId",
                principalTable: "IncomingFeeds",
                principalColumn: "ImporterId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_IncomingFeeds_ImporterId1",
                table: "Groups",
                column: "ImporterId1",
                principalTable: "IncomingFeeds",
                principalColumn: "ImporterId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Scrapers_ScraperId",
                table: "Groups",
                column: "ScraperId",
                principalTable: "Scrapers",
                principalColumn: "ScraperId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Scrapers_ScraperId1",
                table: "Groups",
                column: "ScraperId1",
                principalTable: "Scrapers",
                principalColumn: "ScraperId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_SubmittedDocuments_SubmittedDocumentId",
                table: "Groups",
                column: "SubmittedDocumentId",
                principalTable: "SubmittedDocuments",
                principalColumn: "SubmittedDocumentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_SubmittedDocuments_SubmittedDocumentId1",
                table: "Groups",
                column: "SubmittedDocumentId1",
                principalTable: "SubmittedDocuments",
                principalColumn: "SubmittedDocumentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IncomingFeeds_Classifications_ClassificationId",
                table: "IncomingFeeds",
                column: "ClassificationId",
                principalTable: "Classifications",
                principalColumn: "ClassificationId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Scrapers_Classifications_ClassificationId",
                table: "Scrapers",
                column: "ClassificationId",
                principalTable: "Classifications",
                principalColumn: "ClassificationId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubmittedDocuments_Classifications_ClassificationId",
                table: "SubmittedDocuments",
                column: "ClassificationId",
                principalTable: "Classifications",
                principalColumn: "ClassificationId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Groups_IncomingFeeds_ImporterId",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_Groups_IncomingFeeds_ImporterId1",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Scrapers_ScraperId",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Scrapers_ScraperId1",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_Groups_SubmittedDocuments_SubmittedDocumentId",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_Groups_SubmittedDocuments_SubmittedDocumentId1",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_IncomingFeeds_Classifications_ClassificationId",
                table: "IncomingFeeds");

            migrationBuilder.DropForeignKey(
                name: "FK_Scrapers_Classifications_ClassificationId",
                table: "Scrapers");

            migrationBuilder.DropForeignKey(
                name: "FK_SubmittedDocuments_Classifications_ClassificationId",
                table: "SubmittedDocuments");

            migrationBuilder.DropIndex(
                name: "IX_SubmittedDocuments_ClassificationId",
                table: "SubmittedDocuments");

            migrationBuilder.DropIndex(
                name: "IX_Scrapers_ClassificationId",
                table: "Scrapers");

            migrationBuilder.DropIndex(
                name: "IX_IncomingFeeds_ClassificationId",
                table: "IncomingFeeds");

            migrationBuilder.DropIndex(
                name: "IX_Groups_ImporterId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_ImporterId1",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_ScraperId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_ScraperId1",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_SubmittedDocumentId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_SubmittedDocumentId1",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "ClassificationId",
                table: "SubmittedDocuments");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "SubmittedDocuments");

            migrationBuilder.DropColumn(
                name: "ClassificationId",
                table: "Scrapers");

            migrationBuilder.DropColumn(
                name: "OverrideClassification",
                table: "Scrapers");

            migrationBuilder.DropColumn(
                name: "ClassificationId",
                table: "IncomingFeeds");

            migrationBuilder.DropColumn(
                name: "OverrideClassification",
                table: "IncomingFeeds");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "IncomingFeeds");

            migrationBuilder.DropColumn(
                name: "ImporterId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "ImporterId1",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "ScraperId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "ScraperId1",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "SubmittedDocumentId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "SubmittedDocumentId1",
                table: "Groups");
        }
    }
}
