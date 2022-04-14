using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class UpdateSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                "ModificationDate",
                "Tags",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                "SequenceId",
                "Documents",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                "LastModifiedById",
                "Comments",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                "ModificationDate",
                "Comments",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                "CreatedById",
                "AspNetRoles",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                "CreationDate",
                "AspNetRoles",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                "LastModifiedById",
                "AspNetRoles",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                "ModificationDate",
                "AspNetRoles",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                "IX_Comments_LastModifiedById",
                "Comments",
                "LastModifiedById");

            migrationBuilder.CreateIndex(
                "IX_AspNetRoles_CreatedById",
                "AspNetRoles",
                "CreatedById");

            migrationBuilder.CreateIndex(
                "IX_AspNetRoles_LastModifiedById",
                "AspNetRoles",
                "LastModifiedById");

            migrationBuilder.AddForeignKey(
                "FK_AspNetRoles_AspNetUsers_CreatedById",
                "AspNetRoles",
                "CreatedById",
                "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_AspNetRoles_AspNetUsers_LastModifiedById",
                "AspNetRoles",
                "LastModifiedById",
                "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_Comments_AspNetUsers_LastModifiedById",
                "Comments",
                "LastModifiedById",
                "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_AspNetRoles_AspNetUsers_CreatedById",
                "AspNetRoles");

            migrationBuilder.DropForeignKey(
                "FK_AspNetRoles_AspNetUsers_LastModifiedById",
                "AspNetRoles");

            migrationBuilder.DropForeignKey(
                "FK_Comments_AspNetUsers_LastModifiedById",
                "Comments");

            migrationBuilder.DropIndex(
                "IX_Comments_LastModifiedById",
                "Comments");

            migrationBuilder.DropIndex(
                "IX_AspNetRoles_CreatedById",
                "AspNetRoles");

            migrationBuilder.DropIndex(
                "IX_AspNetRoles_LastModifiedById",
                "AspNetRoles");

            migrationBuilder.DropColumn(
                "ModificationDate",
                "Tags");

            migrationBuilder.DropColumn(
                "SequenceId",
                "Documents");

            migrationBuilder.DropColumn(
                "LastModifiedById",
                "Comments");

            migrationBuilder.DropColumn(
                "ModificationDate",
                "Comments");

            migrationBuilder.DropColumn(
                "CreatedById",
                "AspNetRoles");

            migrationBuilder.DropColumn(
                "CreationDate",
                "AspNetRoles");

            migrationBuilder.DropColumn(
                "LastModifiedById",
                "AspNetRoles");

            migrationBuilder.DropColumn(
                "ModificationDate",
                "AspNetRoles");
        }
    }
}