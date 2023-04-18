using System;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocIntel.Core.Migrations
{
    /// <inheritdoc />
    public partial class IntroducesCollectors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Collectors",
                columns: table => new
                {
                    CollectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CronExpression = table.Column<string>(type: "text", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    SkipInbox = table.Column<bool>(type: "boolean", nullable: false),
                    ImportStructuredData = table.Column<bool>(type: "boolean", nullable: false),
                    Limit = table.Column<int>(type: "integer", nullable: false),
                    Settings = table.Column<JsonObject>(type: "jsonb", nullable: true),
                    Module = table.Column<string>(type: "text", nullable: true),
                    CollectorName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastCollection = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collectors", x => x.CollectorId);
                    table.ForeignKey(
                        name: "FK_Collectors_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Collectors_Classifications_ClassificationId",
                        column: x => x.ClassificationId,
                        principalTable: "Classifications",
                        principalColumn: "ClassificationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Collectors_Sources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Sources",
                        principalColumn: "SourceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollectorGroupEyesOnly",
                columns: table => new
                {
                    CollectorEyesOnlyCollectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    EyesOnlyGroupId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectorGroupEyesOnly", x => new { x.CollectorEyesOnlyCollectorId, x.EyesOnlyGroupId });
                    table.ForeignKey(
                        name: "FK_CollectorGroupEyesOnly_Collectors_CollectorEyesOnlyCollecto~",
                        column: x => x.CollectorEyesOnlyCollectorId,
                        principalTable: "Collectors",
                        principalColumn: "CollectorId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectorGroupEyesOnly_Groups_EyesOnlyGroupId",
                        column: x => x.EyesOnlyGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollectorRelToGroup",
                columns: table => new
                {
                    CollectorReleasableToCollectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleasableToGroupId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectorRelToGroup", x => new { x.CollectorReleasableToCollectorId, x.ReleasableToGroupId });
                    table.ForeignKey(
                        name: "FK_CollectorRelToGroup_Collectors_CollectorReleasableToCollect~",
                        column: x => x.CollectorReleasableToCollectorId,
                        principalTable: "Collectors",
                        principalColumn: "CollectorId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectorRelToGroup_Groups_ReleasableToGroupId",
                        column: x => x.ReleasableToGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollectorTags",
                columns: table => new
                {
                    CollectorsCollectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagsTagId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectorTags", x => new { x.CollectorsCollectorId, x.TagsTagId });
                    table.ForeignKey(
                        name: "FK_CollectorTags_Collectors_CollectorsCollectorId",
                        column: x => x.CollectorsCollectorId,
                        principalTable: "Collectors",
                        principalColumn: "CollectorId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CollectorTags_Tags_TagsTagId",
                        column: x => x.TagsTagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectorGroupEyesOnly_EyesOnlyGroupId",
                table: "CollectorGroupEyesOnly",
                column: "EyesOnlyGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectorRelToGroup_ReleasableToGroupId",
                table: "CollectorRelToGroup",
                column: "ReleasableToGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Collectors_ClassificationId",
                table: "Collectors",
                column: "ClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Collectors_SourceId",
                table: "Collectors",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Collectors_UserId",
                table: "Collectors",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectorTags_TagsTagId",
                table: "CollectorTags",
                column: "TagsTagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectorGroupEyesOnly");

            migrationBuilder.DropTable(
                name: "CollectorRelToGroup");

            migrationBuilder.DropTable(
                name: "CollectorTags");

            migrationBuilder.DropTable(
                name: "Collectors");
        }
    }
}
