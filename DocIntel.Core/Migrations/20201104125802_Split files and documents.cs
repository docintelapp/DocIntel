using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class Splitfilesanddocuments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "Files",
                table => new
                {
                    FileId = table.Column<Guid>(nullable: false),
                    DocumentId = table.Column<Guid>(nullable: false),
                    Filename = table.Column<string>(nullable: true),
                    MimeType = table.Column<string>(nullable: true),
                    DocumentDate = table.Column<DateTime>(nullable: false),
                    RegistrationDate = table.Column<DateTime>(nullable: false),
                    ModificationDate = table.Column<DateTime>(nullable: false),
                    RegisteredById = table.Column<string>(nullable: true),
                    LastModifiedById = table.Column<string>(nullable: true),
                    SourceUrl = table.Column<string>(nullable: true),
                    Classification = table.Column<int>(nullable: false),
                    Filepath = table.Column<string>(nullable: true),
                    Sha256Hash = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.FileId);
                    table.ForeignKey(
                        "FK_Files_Documents_DocumentId",
                        x => x.DocumentId,
                        "Documents",
                        "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_Files_AspNetUsers_LastModifiedById",
                        x => x.LastModifiedById,
                        "AspNetUsers",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        "FK_Files_AspNetUsers_RegisteredById",
                        x => x.RegisteredById,
                        "AspNetUsers",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                "IX_Files_DocumentId",
                "Files",
                "DocumentId");

            migrationBuilder.CreateIndex(
                "IX_Files_LastModifiedById",
                "Files",
                "LastModifiedById");

            migrationBuilder.CreateIndex(
                "IX_Files_RegisteredById",
                "Files",
                "RegisteredById");

            migrationBuilder.Sql(
                @"INSERT INTO ""Files""(""FileId"", ""DocumentId"", ""Filename"", ""MimeType"", ""DocumentDate"", ""RegistrationDate"", ""ModificationDate"", ""RegisteredById"", ""LastModifiedById"", ""SourceUrl"", ""Classification"", ""Filepath"", ""Sha256Hash"")
                (SELECT uuid_generate_v4(), ""DocumentId"", CONCAT(""DocumentId"", '.pdf'), 'application/pdf', ""DocumentDate"", ""RegistrationDate"", ""ModificationDate"", ""RegisteredById"", ""LastModifiedById"", ""SourceUrl"", ""Classification"", ""Filepath"", ""Sha256Hash"" FROM ""Documents"");"
            );

            migrationBuilder.DropColumn(
                "Classification",
                "Documents");

            migrationBuilder.DropColumn(
                "DocumentDate",
                "Documents");

            migrationBuilder.DropColumn(
                "Filepath",
                "Documents");

            migrationBuilder.DropColumn(
                "Sha256Hash",
                "Documents");

            migrationBuilder.DropColumn(
                "SourceUrl",
                "Documents");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "Files");

            migrationBuilder.AddColumn<int>(
                "Classification",
                "Documents",
                "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                "DocumentDate",
                "Documents",
                "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                "Filepath",
                "Documents",
                "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "Sha256Hash",
                "Documents",
                "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "SourceUrl",
                "Documents",
                "text",
                nullable: true);
        }
    }
}