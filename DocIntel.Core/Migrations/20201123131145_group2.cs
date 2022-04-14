using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class group2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_EyesOnly_Group_GroupId",
                "EyesOnly");

            migrationBuilder.DropForeignKey(
                "FK_Group_AspNetUsers_AppUserId",
                "Group");

            migrationBuilder.DropForeignKey(
                "FK_Group_Group_ParentGroupId",
                "Group");

            migrationBuilder.DropForeignKey(
                "FK_Member_Group_GroupId",
                "Member");

            migrationBuilder.DropForeignKey(
                "FK_Member_AspNetUsers_UserId",
                "Member");

            migrationBuilder.DropForeignKey(
                "FK_ReleasableTo_Group_GroupId",
                "ReleasableTo");

            migrationBuilder.DropPrimaryKey(
                "PK_Member",
                "Member");

            migrationBuilder.DropUniqueConstraint(
                "AK_Member_UserId_GroupId",
                "Member");

            migrationBuilder.DropPrimaryKey(
                "PK_Group",
                "Group");

            migrationBuilder.RenameTable(
                "Member",
                newName: "Members");

            migrationBuilder.RenameTable(
                "Group",
                newName: "Groups");

            migrationBuilder.RenameIndex(
                "IX_Member_GroupId",
                table: "Members",
                newName: "IX_Members_GroupId");

            migrationBuilder.RenameIndex(
                "IX_Group_ParentGroupId",
                table: "Groups",
                newName: "IX_Groups_ParentGroupId");

            migrationBuilder.RenameIndex(
                "IX_Group_AppUserId",
                table: "Groups",
                newName: "IX_Groups_AppUserId");

            migrationBuilder.AddPrimaryKey(
                "PK_Members",
                "Members",
                "MemberId");

            migrationBuilder.AddUniqueConstraint(
                "AK_Members_UserId_GroupId",
                "Members",
                new[] {"UserId", "GroupId"});

            migrationBuilder.AddPrimaryKey(
                "PK_Groups",
                "Groups",
                "GroupId");

            migrationBuilder.AddForeignKey(
                "FK_EyesOnly_Groups_GroupId",
                "EyesOnly",
                "GroupId",
                "Groups",
                principalColumn: "GroupId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Groups_AspNetUsers_AppUserId",
                "Groups",
                "AppUserId",
                "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_Groups_Groups_ParentGroupId",
                "Groups",
                "ParentGroupId",
                "Groups",
                principalColumn: "GroupId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_Members_Groups_GroupId",
                "Members",
                "GroupId",
                "Groups",
                principalColumn: "GroupId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Members_AspNetUsers_UserId",
                "Members",
                "UserId",
                "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_ReleasableTo_Groups_GroupId",
                "ReleasableTo",
                "GroupId",
                "Groups",
                principalColumn: "GroupId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_EyesOnly_Groups_GroupId",
                "EyesOnly");

            migrationBuilder.DropForeignKey(
                "FK_Groups_AspNetUsers_AppUserId",
                "Groups");

            migrationBuilder.DropForeignKey(
                "FK_Groups_Groups_ParentGroupId",
                "Groups");

            migrationBuilder.DropForeignKey(
                "FK_Members_Groups_GroupId",
                "Members");

            migrationBuilder.DropForeignKey(
                "FK_Members_AspNetUsers_UserId",
                "Members");

            migrationBuilder.DropForeignKey(
                "FK_ReleasableTo_Groups_GroupId",
                "ReleasableTo");

            migrationBuilder.DropPrimaryKey(
                "PK_Members",
                "Members");

            migrationBuilder.DropUniqueConstraint(
                "AK_Members_UserId_GroupId",
                "Members");

            migrationBuilder.DropPrimaryKey(
                "PK_Groups",
                "Groups");

            migrationBuilder.RenameTable(
                "Members",
                newName: "Member");

            migrationBuilder.RenameTable(
                "Groups",
                newName: "Group");

            migrationBuilder.RenameIndex(
                "IX_Members_GroupId",
                table: "Member",
                newName: "IX_Member_GroupId");

            migrationBuilder.RenameIndex(
                "IX_Groups_ParentGroupId",
                table: "Group",
                newName: "IX_Group_ParentGroupId");

            migrationBuilder.RenameIndex(
                "IX_Groups_AppUserId",
                table: "Group",
                newName: "IX_Group_AppUserId");

            migrationBuilder.AddPrimaryKey(
                "PK_Member",
                "Member",
                "MemberId");

            migrationBuilder.AddUniqueConstraint(
                "AK_Member_UserId_GroupId",
                "Member",
                new[] {"UserId", "GroupId"});

            migrationBuilder.AddPrimaryKey(
                "PK_Group",
                "Group",
                "GroupId");

            migrationBuilder.AddForeignKey(
                "FK_EyesOnly_Group_GroupId",
                "EyesOnly",
                "GroupId",
                "Group",
                principalColumn: "GroupId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Group_AspNetUsers_AppUserId",
                "Group",
                "AppUserId",
                "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_Group_Group_ParentGroupId",
                "Group",
                "ParentGroupId",
                "Group",
                principalColumn: "GroupId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_Member_Group_GroupId",
                "Member",
                "GroupId",
                "Group",
                principalColumn: "GroupId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Member_AspNetUsers_UserId",
                "Member",
                "UserId",
                "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_ReleasableTo_Group_GroupId",
                "ReleasableTo",
                "GroupId",
                "Group",
                principalColumn: "GroupId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}