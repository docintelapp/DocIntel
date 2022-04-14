using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class AddFacetSubscriptions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "UserFacetSubscriptions",
                table => new
                {
                    FacetId = table.Column<Guid>(nullable: false),
                    UserId = table.Column<string>(nullable: false),
                    Notify = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFacetSubscriptions", x => new {x.UserId, x.FacetId});
                    table.ForeignKey(
                        "FK_UserFacetSubscriptions_Facets_FacetId",
                        x => x.FacetId,
                        "Facets",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_UserFacetSubscriptions_AspNetUsers_UserId",
                        x => x.UserId,
                        "AspNetUsers",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_UserFacetSubscriptions_FacetId",
                "UserFacetSubscriptions",
                "FacetId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "UserFacetSubscriptions");
        }
    }
}