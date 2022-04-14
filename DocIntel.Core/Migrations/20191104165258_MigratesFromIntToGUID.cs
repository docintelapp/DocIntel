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
    public partial class MigratesFromIntToGUID : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex("IX_DocumentRead_DocumentId");
            migrationBuilder.DropIndex("IX_UserTagSubscriptions_TagId");
            migrationBuilder.DropIndex("IX_DocumentTag_TagId");
            migrationBuilder.DropIndex("IX_Observables_DocumentId");
            migrationBuilder.DropIndex("IX_Notifications_CommentId");
            migrationBuilder.DropIndex("IX_Notifications_DocumentId");
            migrationBuilder.DropIndex("IX_Notifications_MentionedDocumentId");
            migrationBuilder.DropIndex("IX_Comments_DocumentId");

            migrationBuilder.DropForeignKey("FK_Comments_Documents_DocumentId", "Comments");
            migrationBuilder.DropForeignKey("FK_DocumentRead_Documents_DocumentId", "DocumentRead");
            migrationBuilder.DropForeignKey("FK_DocumentTag_Documents_DocumentId", "DocumentTag");
            migrationBuilder.DropForeignKey("FK_DocumentTag_Tags_TagId", "DocumentTag");
            migrationBuilder.DropForeignKey("FK_UserTagSubscriptions_Tags_TagId", "UserTagSubscriptions");
            migrationBuilder.DropForeignKey("FK_UserDocumentSubscription_Documents_DocumentId",
                "UserDocumentSubscription");
            migrationBuilder.DropForeignKey("FK_Observables_Documents_DocumentId", "Observables");
            migrationBuilder.DropForeignKey("FK_Notifications_Comments_CommentId", "Notifications");
            migrationBuilder.DropForeignKey("FK_Notifications_Documents_DocumentId", "Notifications");
            migrationBuilder.DropForeignKey("FK_Notifications_Documents_MentionedDocumentId", "Notifications");
            migrationBuilder.DropForeignKey("FK_Tags_Facets_FacetId", "Tags");

            migrationBuilder.DropPrimaryKey("PK_DocumentRead", "DocumentRead");
            migrationBuilder.DropPrimaryKey("PK_Tags", "Tags");
            migrationBuilder.DropPrimaryKey("PK_Comments", "Comments");
            migrationBuilder.DropPrimaryKey("PK_DocumentTag", "DocumentTag");
            migrationBuilder.DropPrimaryKey("PK_UserTagSubscriptions", "UserTagSubscriptions");
            migrationBuilder.DropPrimaryKey("PK_Observables", "Observables");
            migrationBuilder.DropPrimaryKey("PK_UserDocumentSubscription", "UserDocumentSubscription");
            migrationBuilder.DropPrimaryKey("PK_Documents", "Documents");
            migrationBuilder.DropPrimaryKey("PK_Facets", "Facets");

            migrationBuilder.AddColumn<Guid>(
                "TagIdTemporary",
                "Tags",
                defaultValueSql: "uuid_generate_v4()");
            // // .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AddColumn<Guid>(
                "ObservableIdTemporary",
                "Observables",
                defaultValueSql: "uuid_generate_v4()");
            // // .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AddColumn<Guid>(
                "DocumentIdTemporary",
                "Documents",
                defaultValueSql: "uuid_generate_v4()");
            // // .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AddColumn<Guid>(
                "CommentIdTemporary",
                "Comments",
                defaultValueSql: "uuid_generate_v4()");
            // // .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AddColumn<Guid>(
                "FacetIdTemporary",
                "Facets",
                defaultValueSql: "uuid_generate_v4()");
            // // .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            // Migrate Tags / FacetId

            migrationBuilder.AddColumn<Guid>(
                "FacetIdTemporary",
                "Tags",
                nullable: true);

            migrationBuilder.Sql(
                @"UPDATE ""Tags""
                  SET ""FacetIdTemporary"" = (
                      SELECT ""FacetIdTemporary"" from ""Facets""
                      WHERE ""Tags"".""FacetId"" = ""Facets"".""Id""
                  );"
            );

            migrationBuilder.DropColumn("FacetId", "Tags");
            migrationBuilder.AlterColumn<Guid>("FacetIdTemporary", "Tags", nullable: false, oldNullable: true);
            migrationBuilder.RenameColumn("FacetIdTemporary", "Tags", "FacetId");

            // end of Tags / FacetId

            // Migrate UserTagSubscriptions

            migrationBuilder.AddColumn<Guid>(
                "TagIdTemporary",
                "UserTagSubscriptions",
                nullable: true);

            migrationBuilder.Sql(
                @"UPDATE ""UserTagSubscriptions""
                  SET ""TagIdTemporary"" = (
                      SELECT ""TagIdTemporary"" from ""Tags""
                      WHERE ""UserTagSubscriptions"".""TagId"" = ""Tags"".""TagId""
                  );"
            );

            migrationBuilder.DropColumn("TagId", "UserTagSubscriptions");
            migrationBuilder.RenameColumn("TagIdTemporary", "UserTagSubscriptions", "TagId");
            migrationBuilder.AlterColumn<Guid>("TagId", "UserTagSubscriptions", nullable: false, oldNullable: true);

            // end of UserTagSubscriptions

            // Migrate UserDocumentSubscription

            migrationBuilder.AddColumn<Guid>(
                "DocumentIdTemporary",
                "UserDocumentSubscription",
                nullable: true);

            migrationBuilder.Sql(
                @"UPDATE ""UserDocumentSubscription""
                  SET ""DocumentIdTemporary"" = (
                      SELECT ""DocumentIdTemporary"" from ""Documents""
                      WHERE ""UserDocumentSubscription"".""DocumentId"" = ""Documents"".""DocumentId""
                  );"
            );

            migrationBuilder.DropColumn("DocumentId", "UserDocumentSubscription");
            migrationBuilder.RenameColumn("DocumentIdTemporary", "UserDocumentSubscription", "DocumentId");
            migrationBuilder.AlterColumn<Guid>("DocumentId", "UserDocumentSubscription", nullable: false,
                oldNullable: true);

            // end of UserDocumentSubscription

            // Migrate Observables

            migrationBuilder.AddColumn<Guid>(
                "DocumentIdTemporary",
                "Observables",
                nullable: true);

            migrationBuilder.Sql(
                @"UPDATE ""Observables""
                  SET ""DocumentIdTemporary"" = (
                      SELECT ""DocumentIdTemporary"" from ""Documents""
                      WHERE ""Observables"".""DocumentId"" = ""Documents"".""DocumentId""
                  );"
            );

            migrationBuilder.DropColumn("DocumentId", "Observables");
            migrationBuilder.RenameColumn("DocumentIdTemporary", "Observables", "DocumentId");
            migrationBuilder.AlterColumn<Guid>("DocumentId", "Observables", nullable: false, oldNullable: true);

            // end of Observables

            // Migrate Notifications

            migrationBuilder.AddColumn<Guid>(
                "MentionedDocumentIdTemporary",
                "Notifications",
                nullable: true);

            migrationBuilder.Sql(
                @"UPDATE ""Notifications""
                  SET ""MentionedDocumentIdTemporary"" = (
                      SELECT ""DocumentIdTemporary"" from ""Documents""
                      WHERE ""Notifications"".""MentionedDocumentId"" = ""Documents"".""DocumentId""
                  );"
            );

            migrationBuilder.DropColumn("MentionedDocumentId", "Notifications");
            migrationBuilder.RenameColumn("MentionedDocumentIdTemporary", "Notifications", "MentionedDocumentId");

            // end of Notifications

            // Migrate Notifications

            migrationBuilder.AddColumn<Guid>(
                "CommentIdTemporary",
                "Notifications",
                nullable: true);

            migrationBuilder.Sql(
                @"UPDATE ""Notifications""
                  SET ""CommentIdTemporary"" = (
                      SELECT ""CommentIdTemporary"" from ""Comments""
                      WHERE ""Notifications"".""CommentId"" = ""Comments"".""CommentId""
                  );"
            );

            migrationBuilder.DropColumn("CommentId", "Notifications");
            migrationBuilder.RenameColumn("CommentIdTemporary", "Notifications", "CommentId");

            // end of Notifications

            // Migrate Notifications

            migrationBuilder.AddColumn<Guid>(
                "DocumentIdTemporary",
                "Notifications",
                nullable: true);

            migrationBuilder.Sql(
                @"UPDATE ""Notifications""
                  SET ""DocumentIdTemporary"" = (
                      SELECT ""DocumentIdTemporary"" from ""Documents""
                      WHERE ""Notifications"".""DocumentId"" = ""Documents"".""DocumentId""
                  );"
            );

            migrationBuilder.DropColumn("DocumentId", "Notifications");
            migrationBuilder.RenameColumn("DocumentIdTemporary", "Notifications", "DocumentId");

            // end of Notifications

            // Migrate DocumentTag

            migrationBuilder.AddColumn<Guid>(
                "TagIdTemporary",
                "DocumentTag",
                nullable: true);

            migrationBuilder.Sql(
                @"UPDATE ""DocumentTag""
                  SET ""TagIdTemporary"" = (
                      SELECT ""TagIdTemporary"" from ""Tags""
                      WHERE ""DocumentTag"".""TagId"" = ""Tags"".""TagId""
                  );"
            );

            migrationBuilder.DropColumn("TagId", "DocumentTag");
            migrationBuilder.RenameColumn("TagIdTemporary", "DocumentTag", "TagId");
            migrationBuilder.AlterColumn<Guid>("TagId", "DocumentTag", nullable: false, oldNullable: true);

            // end of DocumentTag

            // Migrate DocumentTag

            migrationBuilder.AddColumn<Guid>(
                "DocumentIdTemporary",
                "DocumentTag",
                nullable: true);

            migrationBuilder.Sql(
                @"UPDATE ""DocumentTag""
                  SET ""DocumentIdTemporary"" = (
                      SELECT ""DocumentIdTemporary"" from ""Documents""
                      WHERE ""DocumentTag"".""DocumentId"" = ""Documents"".""DocumentId""
                  );"
            );

            migrationBuilder.DropColumn("DocumentId", "DocumentTag");
            migrationBuilder.RenameColumn("DocumentIdTemporary", "DocumentTag", "DocumentId");
            migrationBuilder.AlterColumn<Guid>("DocumentId", "DocumentTag", nullable: false, oldNullable: true);

            // end of DocumentTag

            // Migrate DocumentRead

            migrationBuilder.AddColumn<Guid>(
                "DocumentIdTemporary",
                "DocumentRead",
                nullable: true);

            migrationBuilder.Sql(
                @"UPDATE ""DocumentRead""
                  SET ""DocumentIdTemporary"" = (
                      SELECT ""DocumentIdTemporary"" from ""Documents""
                      WHERE ""DocumentRead"".""DocumentId"" = ""Documents"".""DocumentId""
                  );"
            );

            migrationBuilder.DropColumn("DocumentId", "DocumentRead");
            migrationBuilder.RenameColumn("DocumentIdTemporary", "DocumentRead", "DocumentId");
            migrationBuilder.AlterColumn<Guid>("DocumentId", "DocumentRead", nullable: false, oldNullable: true);

            // end of DocumentRead

            // Migrate Comments

            migrationBuilder.AddColumn<Guid>(
                "DocumentIdTemporary",
                "Comments",
                nullable: true);

            migrationBuilder.Sql(
                @"UPDATE ""Comments""
                  SET ""DocumentIdTemporary"" = (
                      SELECT ""DocumentIdTemporary"" from ""Documents""
                      WHERE ""Comments"".""DocumentId"" = ""Documents"".""DocumentId""
                  );"
            );

            migrationBuilder.DropColumn("DocumentId", "Comments");
            migrationBuilder.RenameColumn("DocumentIdTemporary", "Comments", "DocumentId");

            // end of Comments

            migrationBuilder.DropColumn("TagId", "Tags");
            migrationBuilder.DropColumn("ObservableId", "Observables");
            migrationBuilder.DropColumn("DocumentId", "Documents");
            migrationBuilder.DropColumn("CommentId", "Comments");
            migrationBuilder.DropColumn("Id", "Facets");

            migrationBuilder.RenameColumn("TagIdTemporary", "Tags", "TagId");
            migrationBuilder.RenameColumn("ObservableIdTemporary", "Observables", "ObservableId");
            migrationBuilder.RenameColumn("DocumentIdTemporary", "Documents", "DocumentId");
            migrationBuilder.RenameColumn("CommentIdTemporary", "Comments", "CommentId");
            migrationBuilder.RenameColumn("FacetIdTemporary", "Facets", "Id");

            // Rebuild indexes

            migrationBuilder.CreateIndex(
                "IX_DocumentRead_DocumentId",
                "DocumentRead",
                "DocumentId");

            migrationBuilder.CreateIndex(
                "IX_UserTagSubscriptions_TagId",
                "UserTagSubscriptions",
                "TagId");

            migrationBuilder.CreateIndex(
                "IX_DocumentTag_TagId",
                "DocumentTag",
                "TagId");

            migrationBuilder.CreateIndex(
                "IX_Observables_DocumentId",
                "Observables",
                "DocumentId");

            migrationBuilder.CreateIndex(
                "IX_Notifications_CommentId",
                "Notifications",
                "CommentId");

            migrationBuilder.CreateIndex(
                "IX_Notifications_DocumentId",
                "Notifications",
                "DocumentId");

            migrationBuilder.CreateIndex(
                "IX_Notifications_MentionedDocumentId",
                "Notifications",
                "MentionedDocumentId");

            migrationBuilder.CreateIndex(
                "IX_Comments_DocumentId",
                "Comments",
                "DocumentId");

            // Rebuild the primary key

            migrationBuilder.AddPrimaryKey("PK_DocumentRead", "DocumentRead", new[] {"UserId", "DocumentId"});
            migrationBuilder.AddPrimaryKey("PK_Tags", "Tags", "TagId");
            migrationBuilder.AddPrimaryKey("PK_Comments", "Comments", "CommentId");
            migrationBuilder.AddPrimaryKey("PK_DocumentTag", "DocumentTag", new[] {"DocumentId", "TagId"});
            migrationBuilder.AddPrimaryKey("PK_UserTagSubscriptions", "UserTagSubscriptions",
                new[] {"UserId", "TagId"});
            migrationBuilder.AddPrimaryKey("PK_Observables", "Observables", "ObservableId");
            migrationBuilder.AddPrimaryKey("PK_UserDocumentSubscription", "UserDocumentSubscription",
                new[] {"UserId", "DocumentId"});
            migrationBuilder.AddPrimaryKey("PK_Documents", "Documents", "DocumentId");
            migrationBuilder.AddPrimaryKey("PK_Facets", "Facets", "Id");

            // Rebuild Foreign Keys

            migrationBuilder.AddForeignKey(
                "FK_Tags_Facets_FacetId", "Tags",
                "FacetId",
                "Facets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_Comments_Documents_DocumentId", "Comments",
                "DocumentId",
                "Documents",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                "FK_DocumentRead_Documents_DocumentId", "DocumentRead",
                "DocumentId",
                "Documents",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_DocumentTag_Documents_DocumentId", "DocumentTag",
                "DocumentId",
                "Documents",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_DocumentTag_Tags_TagId", "DocumentTag",
                "TagId",
                "Tags",
                principalColumn: "TagId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_UserTagSubscriptions_Tags_TagId", "UserTagSubscriptions",
                "TagId",
                "Tags",
                principalColumn: "TagId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_UserDocumentSubscription_Documents_DocumentId", "UserDocumentSubscription",
                "DocumentId",
                "Documents",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Observables_Documents_DocumentId", "Observables",
                "DocumentId",
                "Documents",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Notifications_Comments_CommentId", "Notifications",
                "CommentId",
                "Comments",
                principalColumn: "CommentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Notifications_Documents_DocumentId", "Notifications",
                "DocumentId",
                "Documents",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                "FK_Notifications_Documents_MentionedDocumentId", "Notifications",
                "MentionedDocumentId",
                "Documents",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // TODO
        }
    }
}