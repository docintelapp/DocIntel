using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class updateimporters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderedImportRuleSet_IncomingFeeds_IncomingFeedId",
                table: "OrderedImportRuleSet");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IncomingFeeds",
                table: "IncomingFeeds");

            migrationBuilder.DropColumn(
                name: "IncomingFeedId",
                table: "IncomingFeeds");

            migrationBuilder.DropColumn(
                name: "Enabled",
                table: "IncomingFeeds");

            migrationBuilder.DropColumn(
                name: "SkipInbox",
                table: "IncomingFeeds");

            migrationBuilder.RenameColumn(
                name: "SourceId",
                table: "IncomingFeeds",
                newName: "ImporterId");

            migrationBuilder.AddColumn<Guid>(
                name: "ImporterId",
                table: "OrderedImportRuleSet",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "CollectionDelay",
                table: "IncomingFeeds",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(1, 0, 0, 0, 0));

            migrationBuilder.AddColumn<string>(
                name: "FetchingUserId",
                table: "IncomingFeeds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCollection",
                table: "IncomingFeeds",
                type: "timestamp without time zone",
                nullable: true,
                defaultValue: null);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "IncomingFeeds",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_IncomingFeeds",
                table: "IncomingFeeds",
                column: "ImporterId");

            migrationBuilder.CreateTable(
                name: "ImporterError",
                columns: table => new
                {
                    ImporterErrorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImporterError", x => x.ImporterErrorId);
                    table.ForeignKey(
                        name: "FK_ImporterError_IncomingFeeds_ImporterId",
                        column: x => x.ImporterId,
                        principalTable: "IncomingFeeds",
                        principalColumn: "ImporterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderedImportRuleSet_ImporterId",
                table: "OrderedImportRuleSet",
                column: "ImporterId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingFeeds_FetchingUserId",
                table: "IncomingFeeds",
                column: "FetchingUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImporterError_ImporterId",
                table: "ImporterError",
                column: "ImporterId");

            migrationBuilder.AddForeignKey(
                name: "FK_IncomingFeeds_AspNetUsers_FetchingUserId",
                table: "IncomingFeeds",
                column: "FetchingUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderedImportRuleSet_IncomingFeeds_ImporterId",
                table: "OrderedImportRuleSet",
                column: "ImporterId",
                principalTable: "IncomingFeeds",
                principalColumn: "ImporterId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IncomingFeeds_AspNetUsers_FetchingUserId",
                table: "IncomingFeeds");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderedImportRuleSet_IncomingFeeds_ImporterId",
                table: "OrderedImportRuleSet");

            migrationBuilder.DropTable(
                name: "ImporterError");

            migrationBuilder.DropIndex(
                name: "IX_OrderedImportRuleSet_ImporterId",
                table: "OrderedImportRuleSet");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IncomingFeeds",
                table: "IncomingFeeds");

            migrationBuilder.DropIndex(
                name: "IX_IncomingFeeds_FetchingUserId",
                table: "IncomingFeeds");

            migrationBuilder.DropColumn(
                name: "ImporterId",
                table: "OrderedImportRuleSet");

            migrationBuilder.DropColumn(
                name: "CollectionDelay",
                table: "IncomingFeeds");

            migrationBuilder.DropColumn(
                name: "FetchingUserId",
                table: "IncomingFeeds");

            migrationBuilder.DropColumn(
                name: "LastCollection",
                table: "IncomingFeeds");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "IncomingFeeds");

            migrationBuilder.RenameColumn(
                name: "ImporterId",
                table: "IncomingFeeds",
                newName: "SourceId");

            migrationBuilder.AddColumn<Guid>(
                name: "IncomingFeedId",
                table: "IncomingFeeds",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "Enabled",
                table: "IncomingFeeds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SkipInbox",
                table: "IncomingFeeds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_IncomingFeeds",
                table: "IncomingFeeds",
                column: "IncomingFeedId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderedImportRuleSet_IncomingFeeds_IncomingFeedId",
                table: "OrderedImportRuleSet",
                column: "IncomingFeedId",
                principalTable: "IncomingFeeds",
                principalColumn: "IncomingFeedId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
