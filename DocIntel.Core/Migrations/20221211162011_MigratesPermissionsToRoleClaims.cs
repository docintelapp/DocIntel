using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocIntel.Core.Migrations
{
    /// <inheritdoc />
    public partial class MigratesPermissionsToRoleClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
            INSERT INTO "AspNetRoleClaims" ("RoleId", "ClaimType", "ClaimValue") 
            (SELECT "Id" AS "RoleId", 'docintel.permission' AS "ClaimType", "ClaimValue" 
             FROM "AspNetRoles", unnest(string_to_array("PermissionList", ',')) "ClaimValue" 
             EXCEPT 
             SELECT "RoleId", "ClaimType", "ClaimValue" FROM "AspNetRoleClaims"); 
            """);
        
            migrationBuilder.DropColumn(
                name: "PermissionList",
                table: "AspNetRoles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PermissionList",
                table: "AspNetRoles",
                type: "text",
                nullable: true);
        }
    }
}
