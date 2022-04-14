using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class ReferenceClassAsId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferenceClass",
                table: "Scrapers");
            
            migrationBuilder.DropColumn(
                name: "ReferenceClass",
                table: "IncomingFeeds");
            
            migrationBuilder.AddColumn<Guid>(
                name: "ReferenceClass",
                table: "Scrapers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ReferenceClass",
                table: "IncomingFeeds",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("UPDATE \"Scrapers\" SET \"ReferenceClass\" = \"ScraperId\";");
            migrationBuilder.Sql("UPDATE \"IncomingFeeds\" SET \"ReferenceClass\" = \"ImporterId\";");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ReferenceClass",
                table: "Scrapers",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceClass",
                table: "IncomingFeeds",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
