using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class addgroups : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "Group",
                table => new
                {
                    GroupId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    CreationDate = table.Column<DateTime>(nullable: false),
                    ModificationDate = table.Column<DateTime>(nullable: false),
                    ParentGroupId = table.Column<Guid>(nullable: true),
                    AppUserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Group", x => x.GroupId);
                    table.ForeignKey(
                        "FK_Group_AspNetUsers_AppUserId",
                        x => x.AppUserId,
                        "AspNetUsers",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        "FK_Group_Group_ParentGroupId",
                        x => x.ParentGroupId,
                        "Group",
                        "GroupId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                "EyesOnly",
                table => new
                {
                    EyesOnlyId = table.Column<Guid>(nullable: false),
                    DocumentId = table.Column<Guid>(nullable: false),
                    GroupId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EyesOnly", x => x.EyesOnlyId);
                    table.UniqueConstraint("AK_EyesOnly_DocumentId_GroupId", x => new {x.DocumentId, x.GroupId});
                    table.ForeignKey(
                        "FK_EyesOnly_Documents_DocumentId",
                        x => x.DocumentId,
                        "Documents",
                        "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_EyesOnly_Group_GroupId",
                        x => x.GroupId,
                        "Group",
                        "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "Member",
                table => new
                {
                    MemberId = table.Column<Guid>(nullable: false),
                    GroupId = table.Column<Guid>(nullable: false),
                    UserId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Member", x => x.MemberId);
                    table.UniqueConstraint("AK_Member_UserId_GroupId", x => new {x.UserId, x.GroupId});
                    table.ForeignKey(
                        "FK_Member_Group_GroupId",
                        x => x.GroupId,
                        "Group",
                        "GroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_Member_AspNetUsers_UserId",
                        x => x.UserId,
                        "AspNetUsers",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "ReleasableTo",
                table => new
                {
                    ReleasableToId = table.Column<Guid>(nullable: false),
                    DocumentId = table.Column<Guid>(nullable: false),
                    GroupId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleasableTo", x => x.ReleasableToId);
                    table.UniqueConstraint("AK_ReleasableTo_DocumentId_GroupId", x => new {x.DocumentId, x.GroupId});
                    table.ForeignKey(
                        "FK_ReleasableTo_Documents_DocumentId",
                        x => x.DocumentId,
                        "Documents",
                        "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_ReleasableTo_Group_GroupId",
                        x => x.GroupId,
                        "Group",
                        "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_EyesOnly_GroupId",
                "EyesOnly",
                "GroupId");

            migrationBuilder.CreateIndex(
                "IX_Group_AppUserId",
                "Group",
                "AppUserId");

            migrationBuilder.CreateIndex(
                "IX_Group_ParentGroupId",
                "Group",
                "ParentGroupId");

            migrationBuilder.CreateIndex(
                "IX_Member_GroupId",
                "Member",
                "GroupId");

            migrationBuilder.CreateIndex(
                "IX_ReleasableTo_GroupId",
                "ReleasableTo",
                "GroupId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "EyesOnly");

            migrationBuilder.DropTable(
                "Member");

            migrationBuilder.DropTable(
                "ReleasableTo");

            migrationBuilder.DropTable(
                "Group");
        }
    }
}