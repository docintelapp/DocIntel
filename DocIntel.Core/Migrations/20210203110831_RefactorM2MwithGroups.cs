using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class RefactorM2MwithGroups : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EyesOnly");

            migrationBuilder.DropTable(
                name: "ReleasableTo");

            migrationBuilder.CreateTable(
                name: "DocumentGroupEyesOnly",
                columns: table => new
                {
                    DocumentsEyesOnlyDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EyesOnlyGroupId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentGroupEyesOnly", x => new { x.DocumentsEyesOnlyDocumentId, x.EyesOnlyGroupId });
                    table.ForeignKey(
                        name: "FK_DocumentGroupEyesOnly_Documents_DocumentsEyesOnlyDocumentId",
                        column: x => x.DocumentsEyesOnlyDocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentGroupEyesOnly_Groups_EyesOnlyGroupId",
                        column: x => x.EyesOnlyGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentRelToGroup",
                columns: table => new
                {
                    DocumentsReleasableToDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleasableToGroupId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentRelToGroup", x => new { x.DocumentsReleasableToDocumentId, x.ReleasableToGroupId });
                    table.ForeignKey(
                        name: "FK_DocumentRelToGroup_Documents_DocumentsReleasableToDocumentId",
                        column: x => x.DocumentsReleasableToDocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentRelToGroup_Groups_ReleasableToGroupId",
                        column: x => x.ReleasableToGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileGroupEyesOnly",
                columns: table => new
                {
                    EyesOnlyGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    FilesEyesOnlyFileId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileGroupEyesOnly", x => new { x.EyesOnlyGroupId, x.FilesEyesOnlyFileId });
                    table.ForeignKey(
                        name: "FK_FileGroupEyesOnly_Files_FilesEyesOnlyFileId",
                        column: x => x.FilesEyesOnlyFileId,
                        principalTable: "Files",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileGroupEyesOnly_Groups_EyesOnlyGroupId",
                        column: x => x.EyesOnlyGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileRelToGroup",
                columns: table => new
                {
                    FilesReleasableToFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleasableToGroupId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileRelToGroup", x => new { x.FilesReleasableToFileId, x.ReleasableToGroupId });
                    table.ForeignKey(
                        name: "FK_FileRelToGroup_Files_FilesReleasableToFileId",
                        column: x => x.FilesReleasableToFileId,
                        principalTable: "Files",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileRelToGroup_Groups_ReleasableToGroupId",
                        column: x => x.ReleasableToGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentGroupEyesOnly_EyesOnlyGroupId",
                table: "DocumentGroupEyesOnly",
                column: "EyesOnlyGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRelToGroup_ReleasableToGroupId",
                table: "DocumentRelToGroup",
                column: "ReleasableToGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_FileGroupEyesOnly_FilesEyesOnlyFileId",
                table: "FileGroupEyesOnly",
                column: "FilesEyesOnlyFileId");

            migrationBuilder.CreateIndex(
                name: "IX_FileRelToGroup_ReleasableToGroupId",
                table: "FileRelToGroup",
                column: "ReleasableToGroupId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentGroupEyesOnly");

            migrationBuilder.DropTable(
                name: "DocumentRelToGroup");

            migrationBuilder.DropTable(
                name: "FileGroupEyesOnly");

            migrationBuilder.DropTable(
                name: "FileRelToGroup");

            migrationBuilder.CreateTable(
                name: "EyesOnly",
                columns: table => new
                {
                    EyesOnlyId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentFileFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EyesOnly", x => x.EyesOnlyId);
                    table.UniqueConstraint("AK_EyesOnly_DocumentId_GroupId", x => new { x.DocumentId, x.GroupId });
                    table.ForeignKey(
                        name: "FK_EyesOnly_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EyesOnly_Files_DocumentFileFileId",
                        column: x => x.DocumentFileFileId,
                        principalTable: "Files",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EyesOnly_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReleasableTo",
                columns: table => new
                {
                    ReleasableToId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentFileFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleasableTo", x => x.ReleasableToId);
                    table.UniqueConstraint("AK_ReleasableTo_DocumentId_GroupId", x => new { x.DocumentId, x.GroupId });
                    table.ForeignKey(
                        name: "FK_ReleasableTo_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReleasableTo_Files_DocumentFileFileId",
                        column: x => x.DocumentFileFileId,
                        principalTable: "Files",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReleasableTo_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EyesOnly_DocumentFileFileId",
                table: "EyesOnly",
                column: "DocumentFileFileId");

            migrationBuilder.CreateIndex(
                name: "IX_EyesOnly_GroupId",
                table: "EyesOnly",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleasableTo_DocumentFileFileId",
                table: "ReleasableTo",
                column: "DocumentFileFileId");

            migrationBuilder.CreateIndex(
                name: "IX_ReleasableTo_GroupId",
                table: "ReleasableTo",
                column: "GroupId");
        }
    }
}
