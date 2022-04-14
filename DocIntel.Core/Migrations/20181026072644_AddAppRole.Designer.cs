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
// <auto-generated />
using System;
using DocIntel.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace DocIntel.Core.Migrations
{
    [DbContext(typeof(DocIntelContext))]
    [Migration("20181026072644_AddAppRole")]
    partial class AddAppRole
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "2.1.4-rtm-31024");

            modelBuilder.Entity("DocIntel.Core.Models.AppRole", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<string>("Name")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256);

                    b.Property<string>("PermissionList");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasName("RoleNameIndex");

                    b.ToTable("AspNetRoles");
                });

            modelBuilder.Entity("DocIntel.Core.Models.Comment", b =>
                {
                    b.Property<int>("CommentId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AuthorId");

                    b.Property<string>("Body")
                        .IsRequired();

                    b.Property<DateTime>("DateTime");

                    b.Property<int?>("DocumentId");

                    b.HasKey("CommentId");

                    b.HasIndex("AuthorId");

                    b.HasIndex("DocumentId");

                    b.ToTable("Comments");
                });

            modelBuilder.Entity("DocIntel.Core.Models.Document", b =>
                {
                    b.Property<int>("DocumentId")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Classification");

                    b.Property<DateTime>("DocumentDate");

                    b.Property<string>("Filepath");

                    b.Property<string>("LastModifiedById");

                    b.Property<string>("MispEvents");

                    b.Property<DateTime>("ModificationDate");

                    b.Property<string>("Note");

                    b.Property<string>("RTIRTickets");

                    b.Property<string>("Reference")
                        .IsRequired();

                    b.Property<string>("RegisteredById");

                    b.Property<DateTime>("RegistrationDate");

                    b.Property<string>("Sha256Hash")
                        .IsRequired();

                    b.Property<string>("ShortDescription");

                    b.Property<int>("SourceId");

                    b.Property<string>("SourceUrl");

                    b.Property<bool>("Starred");

                    b.Property<string>("Title")
                        .IsRequired();

                    b.HasKey("DocumentId");

                    b.HasIndex("LastModifiedById");

                    b.HasIndex("RegisteredById");

                    b.HasIndex("SourceId");

                    b.ToTable("Documents");
                });

            modelBuilder.Entity("DocIntel.Core.Models.DocumentRead", b =>
                {
                    b.Property<string>("UserId");

                    b.Property<int>("DocumentId");

                    b.HasKey("UserId", "DocumentId");

                    b.HasIndex("DocumentId");

                    b.ToTable("DocumentRead");
                });

            modelBuilder.Entity("DocIntel.Core.Models.DocumentTag", b =>
                {
                    b.Property<int>("DocumentId");

                    b.Property<int>("TagId");

                    b.HasKey("DocumentId", "TagId");

                    b.HasIndex("TagId");

                    b.ToTable("DocumentTag");
                });

            modelBuilder.Entity("DocIntel.Core.Models.InboxItem", b =>
                {
                    b.Property<int>("InboxItemId")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Classification");

                    b.Property<string>("ContentType");

                    b.Property<DateTime>("CreationDate");

                    b.Property<DateTime>("DocumentDate");

                    b.Property<string>("MispEvents");

                    b.Property<DateTime>("ModificationDate");

                    b.Property<string>("Note");

                    b.Property<string>("OriginalFilename");

                    b.Property<string>("OwnerId");

                    b.Property<string>("RTIRTickets");

                    b.Property<string>("Sha256Hash");

                    b.Property<string>("ShortDescription");

                    b.Property<string>("Source");

                    b.Property<string>("SourceUrl");

                    b.Property<string>("Tags");

                    b.Property<string>("TempFileToken");

                    b.Property<string>("TempFilepath");

                    b.Property<string>("Title");

                    b.HasKey("InboxItemId");

                    b.HasIndex("OwnerId");

                    b.ToTable("Inbox");
                });

            modelBuilder.Entity("DocIntel.Core.Models.Notification", b =>
                {
                    b.Property<int>("NotificationId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Action");

                    b.Property<string>("Discriminator")
                        .IsRequired();

                    b.Property<string>("Message")
                        .IsRequired();

                    b.Property<DateTime>("NotificationDate");

                    b.Property<bool>("Read")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValue(false);

                    b.Property<string>("RecipientId")
                        .IsRequired();

                    b.HasKey("NotificationId");

                    b.HasIndex("RecipientId");

                    b.ToTable("Notifications");

                    b.HasDiscriminator<string>("Discriminator").HasValue("Notification");
                });

            modelBuilder.Entity("DocIntel.Core.Models.Source", b =>
                {
                    b.Property<int>("SourceId")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreationDate");

                    b.Property<string>("Description");

                    b.Property<string>("HomePage");

                    b.Property<string>("LastModifiedById");

                    b.Property<DateTime>("ModificationDate");

                    b.Property<string>("RSSFeed");

                    b.Property<string>("RegisteredById");

                    b.Property<string>("Title")
                        .IsRequired();

                    b.HasKey("SourceId");

                    b.HasIndex("LastModifiedById");

                    b.HasIndex("RegisteredById");

                    b.ToTable("Sources");
                });

            modelBuilder.Entity("DocIntel.Core.Models.Tag", b =>
                {
                    b.Property<int>("TagId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("BackgroundColor");

                    b.Property<string>("CreatedById");

                    b.Property<string>("Description");

                    b.Property<string>("Keywords");

                    b.Property<string>("Label");

                    b.Property<string>("LastModifiedById");

                    b.Property<string>("TextColor");

                    b.HasKey("TagId");

                    b.HasIndex("CreatedById");

                    b.HasIndex("LastModifiedById");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("DocIntel.Core.Models.UserDocumentSubscription", b =>
                {
                    b.Property<string>("UserId");

                    b.Property<int>("DocumentId");

                    b.HasKey("UserId", "DocumentId");

                    b.HasIndex("DocumentId");

                    b.ToTable("UserDocumentSubscription");
                });

            modelBuilder.Entity("DocIntel.Core.Models.UserTagSubscription", b =>
                {
                    b.Property<string>("UserId");

                    b.Property<int>("TagId");

                    b.HasKey("UserId", "TagId");

                    b.HasIndex("TagId");

                    b.ToTable("UserTagSubscriptions");
                });

            modelBuilder.Entity("DocIntel.Ldap.AppUser", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("AccessFailedCount");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<string>("DefaultTagColor");

                    b.Property<string>("Email")
                        .HasMaxLength(256);

                    b.Property<bool>("EmailConfirmed");

                    b.Property<string>("FirstName");

                    b.Property<string>("Function");

                    b.Property<DateTime>("LastLogin");

                    b.Property<string>("LastName");

                    b.Property<bool>("LockoutEnabled");

                    b.Property<DateTimeOffset?>("LockoutEnd");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256);

                    b.Property<string>("PasswordHash");

                    b.Property<string>("PhoneNumber");

                    b.Property<bool>("PhoneNumberConfirmed");

                    b.Property<string>("SecurityStamp");

                    b.Property<bool>("TwoFactorEnabled");

                    b.Property<string>("UserName")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasName("UserNameIndex");

                    b.ToTable("AspNetUsers");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<string>("RoleId")
                        .IsRequired();

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<string>("UserId")
                        .IsRequired();

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.Property<string>("LoginProvider");

                    b.Property<string>("ProviderKey");

                    b.Property<string>("ProviderDisplayName");

                    b.Property<string>("UserId")
                        .IsRequired();

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.Property<string>("UserId");

                    b.Property<string>("RoleId");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.Property<string>("UserId");

                    b.Property<string>("LoginProvider");

                    b.Property<string>("Name");

                    b.Property<string>("Value");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens");
                });

            modelBuilder.Entity("DocIntel.Core.Models.DocumentEditNotification", b =>
                {
                    b.HasBaseType("DocIntel.Core.Models.Notification");

                    b.Property<int>("DocumentId")
                        .HasColumnName("DocumentId");

                    b.Property<string>("SenderId")
                        .HasColumnName("SenderId");

                    b.HasIndex("DocumentId");

                    b.HasIndex("SenderId");

                    b.ToTable("DocumentEditNotification");

                    b.HasDiscriminator().HasValue("DocumentEditNotification");
                });

            modelBuilder.Entity("DocIntel.Core.Models.DocumentMentionNotification", b =>
                {
                    b.HasBaseType("DocIntel.Core.Models.Notification");

                    b.Property<int>("CommentId")
                        .HasColumnName("CommentId");

                    b.Property<int>("DocumentId")
                        .HasColumnName("DocumentId");

                    b.Property<int>("MentionedDocumentId")
                        .HasColumnName("MentionedDocumentId");

                    b.Property<string>("SenderId")
                        .HasColumnName("SenderId");

                    b.HasIndex("CommentId");

                    b.HasIndex("DocumentId");

                    b.HasIndex("MentionedDocumentId");

                    b.HasIndex("SenderId");

                    b.ToTable("DocumentMentionNotification");

                    b.HasDiscriminator().HasValue("DocumentMentionNotification");
                });

            modelBuilder.Entity("DocIntel.Core.Models.MentionNotification", b =>
                {
                    b.HasBaseType("DocIntel.Core.Models.Notification");

                    b.Property<int>("CommentId")
                        .HasColumnName("CommentId");

                    b.Property<int>("DocumentId")
                        .HasColumnName("DocumentId");

                    b.Property<string>("SenderId")
                        .HasColumnName("SenderId");

                    b.HasIndex("CommentId")
                        .HasName("IX_Notifications_CommentId1");

                    b.HasIndex("DocumentId");

                    b.HasIndex("SenderId");

                    b.ToTable("MentionNotification");

                    b.HasDiscriminator().HasValue("MentionNotification");
                });

            modelBuilder.Entity("DocIntel.Core.Models.NewCommentNotification", b =>
                {
                    b.HasBaseType("DocIntel.Core.Models.Notification");

                    b.Property<int>("CommentId")
                        .HasColumnName("CommentId");

                    b.Property<int>("DocumentId")
                        .HasColumnName("DocumentId");

                    b.Property<string>("SenderId")
                        .HasColumnName("SenderId");

                    b.HasIndex("CommentId")
                        .HasName("IX_Notifications_CommentId2");

                    b.HasIndex("DocumentId");

                    b.HasIndex("SenderId");

                    b.ToTable("NewCommentNotification");

                    b.HasDiscriminator().HasValue("NewCommentNotification");
                });

            modelBuilder.Entity("DocIntel.Core.Models.Comment", b =>
                {
                    b.HasOne("DocIntel.Ldap.AppUser", "Author")
                        .WithMany()
                        .HasForeignKey("AuthorId");

                    b.HasOne("DocIntel.Core.Models.Document", "Document")
                        .WithMany("Comments")
                        .HasForeignKey("DocumentId");
                });

            modelBuilder.Entity("DocIntel.Core.Models.Document", b =>
                {
                    b.HasOne("DocIntel.Ldap.AppUser", "LastModifiedBy")
                        .WithMany()
                        .HasForeignKey("LastModifiedById");

                    b.HasOne("DocIntel.Ldap.AppUser", "RegisteredBy")
                        .WithMany()
                        .HasForeignKey("RegisteredById");

                    b.HasOne("DocIntel.Core.Models.Source", "Source")
                        .WithMany("Documents")
                        .HasForeignKey("SourceId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("DocIntel.Core.Models.DocumentRead", b =>
                {
                    b.HasOne("DocIntel.Core.Models.Document", "Document")
                        .WithMany("DocumentReadUsers")
                        .HasForeignKey("DocumentId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("DocIntel.Ldap.AppUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("DocIntel.Core.Models.DocumentTag", b =>
                {
                    b.HasOne("DocIntel.Core.Models.Document", "Document")
                        .WithMany("DocumentTags")
                        .HasForeignKey("DocumentId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("DocIntel.Core.Models.Tag", "Tag")
                        .WithMany("Documents")
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("DocIntel.Core.Models.InboxItem", b =>
                {
                    b.HasOne("DocIntel.Ldap.AppUser", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerId");
                });

            modelBuilder.Entity("DocIntel.Core.Models.Notification", b =>
                {
                    b.HasOne("DocIntel.Ldap.AppUser", "Recipient")
                        .WithMany("Notifications")
                        .HasForeignKey("RecipientId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("DocIntel.Core.Models.Source", b =>
                {
                    b.HasOne("DocIntel.Ldap.AppUser", "LastModifiedBy")
                        .WithMany()
                        .HasForeignKey("LastModifiedById");

                    b.HasOne("DocIntel.Ldap.AppUser", "RegisteredBy")
                        .WithMany()
                        .HasForeignKey("RegisteredById");
                });

            modelBuilder.Entity("DocIntel.Core.Models.Tag", b =>
                {
                    b.HasOne("DocIntel.Ldap.AppUser", "CreatedBy")
                        .WithMany()
                        .HasForeignKey("CreatedById");

                    b.HasOne("DocIntel.Ldap.AppUser", "LastModifiedBy")
                        .WithMany()
                        .HasForeignKey("LastModifiedById");
                });

            modelBuilder.Entity("DocIntel.Core.Models.UserDocumentSubscription", b =>
                {
                    b.HasOne("DocIntel.Core.Models.Document", "Document")
                        .WithMany("SubscribedUsers")
                        .HasForeignKey("DocumentId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("DocIntel.Ldap.AppUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("DocIntel.Core.Models.UserTagSubscription", b =>
                {
                    b.HasOne("DocIntel.Core.Models.Tag", "Tag")
                        .WithMany("SubscribedUser")
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("DocIntel.Ldap.AppUser", "User")
                        .WithMany("SubscribedTags")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.HasOne("DocIntel.Core.Models.AppRole")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.HasOne("DocIntel.Ldap.AppUser")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.HasOne("DocIntel.Ldap.AppUser")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.HasOne("DocIntel.Core.Models.AppRole")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("DocIntel.Ldap.AppUser")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.HasOne("DocIntel.Ldap.AppUser")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("DocIntel.Core.Models.DocumentEditNotification", b =>
                {
                    b.HasOne("DocIntel.Core.Models.Document", "Document")
                        .WithMany()
                        .HasForeignKey("DocumentId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("DocIntel.Ldap.AppUser", "Sender")
                        .WithMany()
                        .HasForeignKey("SenderId");
                });

            modelBuilder.Entity("DocIntel.Core.Models.DocumentMentionNotification", b =>
                {
                    b.HasOne("DocIntel.Core.Models.Comment", "Comment")
                        .WithMany()
                        .HasForeignKey("CommentId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("DocIntel.Core.Models.Document", "Document")
                        .WithMany()
                        .HasForeignKey("DocumentId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("DocIntel.Core.Models.Document", "MentionedDocument")
                        .WithMany()
                        .HasForeignKey("MentionedDocumentId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("DocIntel.Ldap.AppUser", "Sender")
                        .WithMany()
                        .HasForeignKey("SenderId");
                });

            modelBuilder.Entity("DocIntel.Core.Models.MentionNotification", b =>
                {
                    b.HasOne("DocIntel.Core.Models.Comment", "Comment")
                        .WithMany()
                        .HasForeignKey("CommentId")
                        .HasConstraintName("FK_Notifications_Comments_CommentId1")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("DocIntel.Core.Models.Document", "Document")
                        .WithMany()
                        .HasForeignKey("DocumentId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("DocIntel.Ldap.AppUser", "Sender")
                        .WithMany()
                        .HasForeignKey("SenderId");
                });

            modelBuilder.Entity("DocIntel.Core.Models.NewCommentNotification", b =>
                {
                    b.HasOne("DocIntel.Core.Models.Comment", "Comment")
                        .WithMany()
                        .HasForeignKey("CommentId")
                        .HasConstraintName("FK_Notifications_Comments_CommentId2")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("DocIntel.Core.Models.Document", "Document")
                        .WithMany()
                        .HasForeignKey("DocumentId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("DocIntel.Ldap.AppUser", "Sender")
                        .WithMany()
                        .HasForeignKey("SenderId");
                });
#pragma warning restore 612, 618
        }
    }
}
