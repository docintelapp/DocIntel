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
    public partial class AddNotificationsAndDocumentSubscription : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "Notifications",
                table => new
                {
                    NotificationId = table.Column<int>(nullable: false),
                    // .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    RecipientId = table.Column<string>(nullable: false),
                    NotificationDate = table.Column<DateTime>(nullable: false),
                    Read = table.Column<bool>(nullable: false, defaultValue: false),
                    Action = table.Column<string>(nullable: true),
                    Message = table.Column<string>(nullable: false),
                    Discriminator = table.Column<string>(nullable: false),
                    SenderId = table.Column<string>(nullable: true),
                    DocumentId = table.Column<int>(nullable: true),
                    CommentId = table.Column<int>(nullable: true),
                    MentionedDocumentId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                    table.ForeignKey(
                        "FK_Notifications_Documents_DocumentId",
                        x => x.DocumentId,
                        "Documents",
                        "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_Notifications_AspNetUsers_SenderId",
                        x => x.SenderId,
                        "AspNetUsers",
                        "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        "FK_Notifications_Comments_CommentId",
                        x => x.CommentId,
                        "Comments",
                        "CommentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_Notifications_Documents_MentionedDocumentId",
                        x => x.MentionedDocumentId,
                        "Documents",
                        "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_Notifications_AspNetUsers_RecipientId",
                        x => x.RecipientId,
                        "AspNetUsers",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "UserDocumentSubscription",
                table => new
                {
                    DocumentId = table.Column<int>(nullable: false),
                    UserId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDocumentSubscription", x => new {x.UserId, x.DocumentId});
                    table.ForeignKey(
                        "FK_UserDocumentSubscription_Documents_DocumentId",
                        x => x.DocumentId,
                        "Documents",
                        "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_UserDocumentSubscription_AspNetUsers_UserId",
                        x => x.UserId,
                        "AspNetUsers",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                @"INSERT INTO ""UserDocumentSubscription"" (""DocumentId"", ""UserId"")
                  (SELECT ""DocumentId"", ""RegisteredById"" AS ""UserId"" FROM ""Documents"" WHERE ""RegisteredById"" IS NOT NULL)
                  UNION
                  (SELECT ""DocumentId"", ""LastModifiedById"" AS ""UserId"" FROM ""Documents"" WHERE ""LastModifiedById"" IS NOT NULL);");

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
                "IX_Notifications_RecipientId",
                "Notifications",
                "RecipientId");

            migrationBuilder.CreateIndex(
                "IX_UserDocumentSubscription_DocumentId",
                "UserDocumentSubscription",
                "DocumentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "Notifications");

            migrationBuilder.DropTable(
                "UserDocumentSubscription");
        }
    }
}