using System;
using System.Collections.Generic;
using DocIntel.Core.Models;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocIntel.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedSearches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedSearches",
                columns: table => new
                {
                    SavedSearchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Public = table.Column<bool>(type: "boolean", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "text", nullable: true),
                    LastModifiedById = table.Column<string>(type: "text", nullable: true),
                    SearchTerm = table.Column<string>(type: "text", nullable: true),
                    Filters = table.Column<IList<SearchFilter>>(type: "jsonb", nullable: true),
                    SortCriteria = table.Column<int>(type: "integer", nullable: false),
                    PageSize = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedSearches", x => x.SavedSearchId);
                    table.ForeignKey(
                        name: "FK_SavedSearches_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SavedSearches_AspNetUsers_LastModifiedById",
                        column: x => x.LastModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserSavedSearches",
                columns: table => new
                {
                    SavedSearchId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Default = table.Column<bool>(type: "boolean", nullable: false),
                    Notify = table.Column<bool>(type: "boolean", nullable: false),
                    LastNotification = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    NotificationSpan = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSavedSearches", x => new { x.UserId, x.SavedSearchId });
                    table.ForeignKey(
                        name: "FK_UserSavedSearches_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSavedSearches_SavedSearches_SavedSearchId",
                        column: x => x.SavedSearchId,
                        principalTable: "SavedSearches",
                        principalColumn: "SavedSearchId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedSearches_CreatedById",
                table: "SavedSearches",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SavedSearches_LastModifiedById",
                table: "SavedSearches",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_UserSavedSearches_SavedSearchId",
                table: "UserSavedSearches",
                column: "SavedSearchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSavedSearches");

            migrationBuilder.DropTable(
                name: "SavedSearches");
        }
    }
}
