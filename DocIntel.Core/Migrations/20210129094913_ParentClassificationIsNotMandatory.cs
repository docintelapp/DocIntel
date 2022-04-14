using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class ParentClassificationIsNotMandatory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classifications_Classifications_ParentClassificationId",
                table: "Classifications");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentClassificationId",
                table: "Classifications",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Classifications_Classifications_ParentClassificationId",
                table: "Classifications",
                column: "ParentClassificationId",
                principalTable: "Classifications",
                principalColumn: "ClassificationId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classifications_Classifications_ParentClassificationId",
                table: "Classifications");

            migrationBuilder.AlterColumn<Guid>(
                name: "ParentClassificationId",
                table: "Classifications",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Classifications_Classifications_ParentClassificationId",
                table: "Classifications",
                column: "ParentClassificationId",
                principalTable: "Classifications",
                principalColumn: "ClassificationId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
