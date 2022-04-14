﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class AddMuteStatusToSources : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Muted",
                table: "UserSourceSubscription",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Muted",
                table: "UserSourceSubscription");
        }
    }
}
