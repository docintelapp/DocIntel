using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocIntel.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddsImporterSourceField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceId",
                table: "IncomingFeeds",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncomingFeeds_SourceId",
                table: "IncomingFeeds",
                column: "SourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_IncomingFeeds_Sources_SourceId",
                table: "IncomingFeeds",
                column: "SourceId",
                principalTable: "Sources",
                principalColumn: "SourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IncomingFeeds_Sources_SourceId",
                table: "IncomingFeeds");

            migrationBuilder.DropIndex(
                name: "IX_IncomingFeeds_SourceId",
                table: "IncomingFeeds");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "IncomingFeeds");
        }
    }
}
