using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class Addsskipinboxandsourceidtofeeds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                "SkipInbox",
                "IncomingFeeds",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                "SourceId",
                "IncomingFeeds",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                "SkipInbox",
                "IncomingFeeds");

            migrationBuilder.DropColumn(
                "SourceId",
                "IncomingFeeds");
        }
    }
}