/*
 * DocIntel
 * Copyright (C) 2018-2021 Belgian Defense, Antoine Cailliau
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocIntel.Core.Migrations
{
    public partial class RefactorNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_Comments_Documents_DocumentId",
                "Comments");

            migrationBuilder.DropForeignKey(
                "FK_Notifications_Documents_DocumentId",
                "Notifications");

            migrationBuilder.DropForeignKey(
                "FK_Notifications_AspNetUsers_SenderId",
                "Notifications");

            migrationBuilder.DropForeignKey(
                "FK_Notifications_Comments_CommentId",
                "Notifications");

            migrationBuilder.DropForeignKey(
                "FK_Notifications_Documents_MentionedDocumentId",
                "Notifications");

            migrationBuilder.DropIndex(
                "IX_Notifications_DocumentId",
                "Notifications");

            migrationBuilder.DropIndex(
                "IX_Notifications_SenderId",
                "Notifications");

            migrationBuilder.DropIndex(
                "IX_Notifications_CommentId",
                "Notifications");

            migrationBuilder.DropIndex(
                "IX_Notifications_MentionedDocumentId",
                "Notifications");

            migrationBuilder.DropColumn(
                "DocumentId",
                "Notifications");

            migrationBuilder.DropColumn(
                "SenderId",
                "Notifications");

            migrationBuilder.DropColumn(
                "CommentId",
                "Notifications");

            migrationBuilder.DropColumn(
                "MentionedDocumentId",
                "Notifications");

            migrationBuilder.DropColumn(
                "Action",
                "Notifications");

            migrationBuilder.DropColumn(
                "Discriminator",
                "Notifications");

            migrationBuilder.DropColumn(
                "Message",
                "Notifications");

            migrationBuilder.DropColumn(
                "NotificationDate",
                "Notifications");

            migrationBuilder.AlterColumn<bool>(
                "Read",
                "Notifications",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AddColumn<string>(
                "ActivityId",
                "Notifications",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                "DocumentId",
                "Comments",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                "TargetId",
                "Activity",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                "ObjectId",
                "Activity",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                "IX_Notifications_ActivityId",
                "Notifications",
                "ActivityId");

            migrationBuilder.AddForeignKey(
                "FK_Comments_Documents_DocumentId",
                "Comments",
                "DocumentId",
                "Documents",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Notifications_Activity_ActivityId",
                "Notifications",
                "ActivityId",
                "Activity",
                principalColumn: "ActivityId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                "FK_Comments_Documents_DocumentId",
                "Comments");

            migrationBuilder.DropForeignKey(
                "FK_Notifications_Activity_ActivityId",
                "Notifications");

            migrationBuilder.DropIndex(
                "IX_Notifications_ActivityId",
                "Notifications");

            migrationBuilder.DropColumn(
                "ActivityId",
                "Notifications");

            migrationBuilder.AlterColumn<bool>(
                "Read",
                "Notifications",
                "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool));

            migrationBuilder.AddColumn<Guid>(
                "DocumentId",
                "Notifications",
                "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "SenderId",
                "Notifications",
                "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                "CommentId",
                "Notifications",
                "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                "MentionedDocumentId",
                "Notifications",
                "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "Action",
                "Notifications",
                "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                "Discriminator",
                "Notifications",
                "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                "Message",
                "Notifications",
                "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                "NotificationDate",
                "Notifications",
                "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<Guid>(
                "DocumentId",
                "Comments",
                "uuid",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AlterColumn<Guid>(
                "TargetId",
                "Activity",
                "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                "ObjectId",
                "Activity",
                "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.CreateIndex(
                "IX_Notifications_DocumentId",
                "Notifications",
                "DocumentId");

            migrationBuilder.CreateIndex(
                "IX_Notifications_SenderId",
                "Notifications",
                "SenderId");

            migrationBuilder.CreateIndex(
                "IX_Notifications_CommentId",
                "Notifications",
                "CommentId");

            migrationBuilder.CreateIndex(
                "IX_Notifications_MentionedDocumentId",
                "Notifications",
                "MentionedDocumentId");

            migrationBuilder.CreateIndex(
                "IX_Notifications_CommentId1",
                "Notifications",
                "CommentId");

            migrationBuilder.CreateIndex(
                "IX_Notifications_CommentId2",
                "Notifications",
                "CommentId");

            migrationBuilder.AddForeignKey(
                "FK_Comments_Documents_DocumentId",
                "Comments",
                "DocumentId",
                "Documents",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_Notifications_Documents_DocumentId",
                "Notifications",
                "DocumentId",
                "Documents",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Notifications_AspNetUsers_SenderId",
                "Notifications",
                "SenderId",
                "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_Notifications_Comments_CommentId",
                "Notifications",
                "CommentId",
                "Comments",
                principalColumn: "CommentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Notifications_Documents_MentionedDocumentId",
                "Notifications",
                "MentionedDocumentId",
                "Documents",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Notifications_Comments_CommentId1",
                "Notifications",
                "CommentId",
                "Comments",
                principalColumn: "CommentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Notifications_Comments_CommentId2",
                "Notifications",
                "CommentId",
                "Comments",
                principalColumn: "CommentId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}