using System;
using DocIntel.Core.Models;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json.Linq;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DocIntel.Core.Migrations
{
    public partial class InitialImport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    Function = table.Column<string>(type: "text", nullable: true),
                    ProfilePicture = table.Column<string>(type: "text", nullable: true),
                    LastActivity = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RegistrationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Bot = table.Column<bool>(type: "boolean", nullable: false),
                    Preferences = table.Column<UserPreferences>(type: "jsonb", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Classifications",
                columns: table => new
                {
                    ClassificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Subtitle = table.Column<string>(type: "text", nullable: true),
                    Abbreviation = table.Column<string>(type: "text", nullable: true),
                    Color = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ParentClassificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Default = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classifications", x => x.ClassificationId);
                    table.ForeignKey(
                        name: "FK_Classifications_Classifications_ParentClassificationId",
                        column: x => x.ParentClassificationId,
                        principalTable: "Classifications",
                        principalColumn: "ClassificationId");
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Default = table.Column<bool>(type: "boolean", nullable: false),
                    Hidden = table.Column<bool>(type: "boolean", nullable: false),
                    ParentGroupId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.GroupId);
                    table.ForeignKey(
                        name: "FK_Groups_Groups_ParentGroupId",
                        column: x => x.ParentGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId");
                });

            migrationBuilder.CreateTable(
                name: "ImportRuleSets",
                columns: table => new
                {
                    ImportRuleSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportRuleSets", x => x.ImportRuleSetId);
                });

            migrationBuilder.CreateTable(
                name: "APIKeys",
                columns: table => new
                {
                    APIKeyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Key = table.Column<string>(type: "text", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUsage = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastIP = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APIKeys", x => x.APIKeyId);
                    table.ForeignKey(
                        name: "FK_APIKeys_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    PermissionList = table.Column<string>(type: "text", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "text", nullable: true),
                    LastModifiedById = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoles_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AspNetRoles_AspNetUsers_LastModifiedById",
                        column: x => x.LastModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Facets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Prefix = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    Hidden = table.Column<bool>(type: "boolean", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatedById = table.Column<string>(type: "text", nullable: true),
                    LastModifiedById = table.Column<string>(type: "text", nullable: true),
                    MetaData = table.Column<JObject>(type: "jsonb", nullable: true),
                    AutoExtract = table.Column<bool>(type: "boolean", nullable: false),
                    ExtractionRegex = table.Column<string>(type: "text", nullable: true),
                    TagNormalization = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Facets_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Facets_AspNetUsers_LastModifiedById",
                        column: x => x.LastModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Sources",
                columns: table => new
                {
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    HomePage = table.Column<string>(type: "text", nullable: true),
                    RSSFeed = table.Column<string>(type: "text", nullable: true),
                    Facebook = table.Column<string>(type: "text", nullable: true),
                    Twitter = table.Column<string>(type: "text", nullable: true),
                    Reddit = table.Column<string>(type: "text", nullable: true),
                    LinkedIn = table.Column<string>(type: "text", nullable: true),
                    Keywords = table.Column<string>(type: "text", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RegisteredById = table.Column<string>(type: "text", nullable: true),
                    LastModifiedById = table.Column<string>(type: "text", nullable: true),
                    LogoFilename = table.Column<string>(type: "text", nullable: true),
                    Reliability = table.Column<int>(type: "integer", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: true),
                    MetaData = table.Column<JObject>(type: "jsonb", nullable: true),
                    URL = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sources", x => x.SourceId);
                    table.ForeignKey(
                        name: "FK_Sources_AspNetUsers_LastModifiedById",
                        column: x => x.LastModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Sources_AspNetUsers_RegisteredById",
                        column: x => x.RegisteredById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IncomingFeeds",
                columns: table => new
                {
                    ImporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CollectionDelay = table.Column<TimeSpan>(type: "interval", nullable: false),
                    LastCollection = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FetchingUserId = table.Column<string>(type: "text", nullable: true),
                    Settings = table.Column<JObject>(type: "jsonb", nullable: true),
                    ReferenceClass = table.Column<Guid>(type: "uuid", nullable: false),
                    Limit = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    OverrideClassification = table.Column<bool>(type: "boolean", nullable: false),
                    ClassificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    OverrideReleasableTo = table.Column<bool>(type: "boolean", nullable: false),
                    OverrideEyesOnly = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncomingFeeds", x => x.ImporterId);
                    table.ForeignKey(
                        name: "FK_IncomingFeeds_AspNetUsers_FetchingUserId",
                        column: x => x.FetchingUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IncomingFeeds_Classifications_ClassificationId",
                        column: x => x.ClassificationId,
                        principalTable: "Classifications",
                        principalColumn: "ClassificationId");
                });

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.MemberId);
                    table.UniqueConstraint("AK_Members_UserId_GroupId", x => new { x.UserId, x.GroupId });
                    table.ForeignKey(
                        name: "FK_Members_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Members_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportRules",
                columns: table => new
                {
                    ImportRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SearchPattern = table.Column<string>(type: "text", nullable: true),
                    Replacement = table.Column<string>(type: "text", nullable: true),
                    ImportRuleSetId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportRules", x => x.ImportRuleId);
                    table.ForeignKey(
                        name: "FK_ImportRules_ImportRuleSets_ImportRuleSetId",
                        column: x => x.ImportRuleSetId,
                        principalTable: "ImportRuleSets",
                        principalColumn: "ImportRuleSetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Keywords = table.Column<string>(type: "text", nullable: true),
                    ExtractionKeywords = table.Column<string>(type: "text", nullable: true),
                    BackgroundColor = table.Column<string>(type: "text", nullable: true),
                    CreatedById = table.Column<string>(type: "text", nullable: true),
                    LastModifiedById = table.Column<string>(type: "text", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FacetId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetaData = table.Column<JObject>(type: "jsonb", nullable: true),
                    URL = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.TagId);
                    table.ForeignKey(
                        name: "FK_Tags_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tags_AspNetUsers_LastModifiedById",
                        column: x => x.LastModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tags_Facets_FacetId",
                        column: x => x.FacetId,
                        principalTable: "Facets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserFacetSubscriptions",
                columns: table => new
                {
                    FacetId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Notify = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFacetSubscriptions", x => new { x.UserId, x.FacetId });
                    table.ForeignKey(
                        name: "FK_UserFacetSubscriptions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserFacetSubscriptions_Facets_FacetId",
                        column: x => x.FacetId,
                        principalTable: "Facets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Scrapers",
                columns: table => new
                {
                    ScraperId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Settings = table.Column<JObject>(type: "jsonb", nullable: true),
                    ReferenceClass = table.Column<Guid>(type: "uuid", nullable: false),
                    OverrideSource = table.Column<bool>(type: "boolean", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    SkipInbox = table.Column<bool>(type: "boolean", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    OverrideClassification = table.Column<bool>(type: "boolean", nullable: false),
                    ClassificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    OverrideReleasableTo = table.Column<bool>(type: "boolean", nullable: false),
                    OverrideEyesOnly = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scrapers", x => x.ScraperId);
                    table.ForeignKey(
                        name: "FK_Scrapers_Classifications_ClassificationId",
                        column: x => x.ClassificationId,
                        principalTable: "Classifications",
                        principalColumn: "ClassificationId");
                    table.ForeignKey(
                        name: "FK_Scrapers_Sources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Sources",
                        principalColumn: "SourceId");
                });

            migrationBuilder.CreateTable(
                name: "UserSourceSubscription",
                columns: table => new
                {
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Notify = table.Column<bool>(type: "boolean", nullable: false),
                    Muted = table.Column<bool>(type: "boolean", nullable: false),
                    Subscribed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSourceSubscription", x => new { x.UserId, x.SourceId });
                    table.ForeignKey(
                        name: "FK_UserSourceSubscription_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSourceSubscription_Sources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Sources",
                        principalColumn: "SourceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImporterGroupEyesOnly",
                columns: table => new
                {
                    EyesOnlyGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImporterEyesOnlyImporterId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImporterGroupEyesOnly", x => new { x.EyesOnlyGroupId, x.ImporterEyesOnlyImporterId });
                    table.ForeignKey(
                        name: "FK_ImporterGroupEyesOnly_Groups_EyesOnlyGroupId",
                        column: x => x.EyesOnlyGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImporterGroupEyesOnly_IncomingFeeds_ImporterEyesOnlyImporte~",
                        column: x => x.ImporterEyesOnlyImporterId,
                        principalTable: "IncomingFeeds",
                        principalColumn: "ImporterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImporterRelToGroup",
                columns: table => new
                {
                    ImporterReleasableToImporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleasableToGroupId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImporterRelToGroup", x => new { x.ImporterReleasableToImporterId, x.ReleasableToGroupId });
                    table.ForeignKey(
                        name: "FK_ImporterRelToGroup_Groups_ReleasableToGroupId",
                        column: x => x.ReleasableToGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImporterRelToGroup_IncomingFeeds_ImporterReleasableToImport~",
                        column: x => x.ImporterReleasableToImporterId,
                        principalTable: "IncomingFeeds",
                        principalColumn: "ImporterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTagSubscriptions",
                columns: table => new
                {
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Notify = table.Column<bool>(type: "boolean", nullable: false),
                    Subscribed = table.Column<bool>(type: "boolean", nullable: false),
                    Muted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTagSubscriptions", x => new { x.UserId, x.TagId });
                    table.ForeignKey(
                        name: "FK_UserTagSubscriptions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserTagSubscriptions_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderedImportRuleSet",
                columns: table => new
                {
                    ScraperId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportRuleSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderedImportRuleSet", x => new { x.ScraperId, x.ImportRuleSetId });
                    table.ForeignKey(
                        name: "FK_OrderedImportRuleSet_ImportRuleSets_ImportRuleSetId",
                        column: x => x.ImportRuleSetId,
                        principalTable: "ImportRuleSets",
                        principalColumn: "ImportRuleSetId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderedImportRuleSet_Scrapers_ScraperId",
                        column: x => x.ScraperId,
                        principalTable: "Scrapers",
                        principalColumn: "ScraperId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScraperGroupEyesOnly",
                columns: table => new
                {
                    EyesOnlyGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScraperEyesOnlyScraperId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScraperGroupEyesOnly", x => new { x.EyesOnlyGroupId, x.ScraperEyesOnlyScraperId });
                    table.ForeignKey(
                        name: "FK_ScraperGroupEyesOnly_Groups_EyesOnlyGroupId",
                        column: x => x.EyesOnlyGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScraperGroupEyesOnly_Scrapers_ScraperEyesOnlyScraperId",
                        column: x => x.ScraperEyesOnlyScraperId,
                        principalTable: "Scrapers",
                        principalColumn: "ScraperId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScraperRelToGroup",
                columns: table => new
                {
                    ReleasableToGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScraperReleasableToScraperId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScraperRelToGroup", x => new { x.ReleasableToGroupId, x.ScraperReleasableToScraperId });
                    table.ForeignKey(
                        name: "FK_ScraperRelToGroup_Groups_ReleasableToGroupId",
                        column: x => x.ReleasableToGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScraperRelToGroup_Scrapers_ScraperReleasableToScraperId",
                        column: x => x.ScraperReleasableToScraperId,
                        principalTable: "Scrapers",
                        principalColumn: "ScraperId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    CommentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<string>(type: "text", nullable: true),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    LastModifiedById = table.Column<string>(type: "text", nullable: true),
                    ModificationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.CommentId);
                    table.ForeignKey(
                        name: "FK_Comments_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Comments_AspNetUsers_LastModifiedById",
                        column: x => x.LastModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DocumentGroupEyesOnly",
                columns: table => new
                {
                    DocumentsEyesOnlyDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EyesOnlyGroupId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentGroupEyesOnly", x => new { x.DocumentsEyesOnlyDocumentId, x.EyesOnlyGroupId });
                    table.ForeignKey(
                        name: "FK_DocumentGroupEyesOnly_Groups_EyesOnlyGroupId",
                        column: x => x.EyesOnlyGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentRelToGroup",
                columns: table => new
                {
                    DocumentsReleasableToDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleasableToGroupId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentRelToGroup", x => new { x.DocumentsReleasableToDocumentId, x.ReleasableToGroupId });
                    table.ForeignKey(
                        name: "FK_DocumentRelToGroup_Groups_ReleasableToGroupId",
                        column: x => x.ReleasableToGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceId = table.Column<int>(type: "integer", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: false),
                    ExternalReference = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    ShortDescription = table.Column<string>(type: "text", nullable: true),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    DocumentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RegistrationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RegisteredById = table.Column<string>(type: "text", nullable: true),
                    LastModifiedById = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    URL = table.Column<string>(type: "text", nullable: false),
                    ClassificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetaData = table.Column<JObject>(type: "jsonb", nullable: true),
                    ThumbnailId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_Documents_AspNetUsers_LastModifiedById",
                        column: x => x.LastModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Documents_AspNetUsers_RegisteredById",
                        column: x => x.RegisteredById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Documents_Classifications_ClassificationId",
                        column: x => x.ClassificationId,
                        principalTable: "Classifications",
                        principalColumn: "ClassificationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Documents_Sources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Sources",
                        principalColumn: "SourceId");
                });

            migrationBuilder.CreateTable(
                name: "DocumentTag",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTag", x => new { x.DocumentId, x.TagId });
                    table.ForeignKey(
                        name: "FK_DocumentTag_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentTag_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Filename = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    MimeType = table.Column<string>(type: "text", nullable: true),
                    DocumentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RegistrationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModificationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RegisteredById = table.Column<string>(type: "text", nullable: true),
                    LastModifiedById = table.Column<string>(type: "text", nullable: true),
                    SourceUrl = table.Column<string>(type: "text", nullable: true),
                    OverrideClassification = table.Column<bool>(type: "boolean", nullable: false),
                    ClassificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    OverrideReleasableTo = table.Column<bool>(type: "boolean", nullable: false),
                    OverrideEyesOnly = table.Column<bool>(type: "boolean", nullable: false),
                    Filepath = table.Column<string>(type: "text", nullable: true),
                    Sha256Hash = table.Column<string>(type: "text", nullable: true),
                    MetaData = table.Column<JObject>(type: "jsonb", nullable: true),
                    Visible = table.Column<bool>(type: "boolean", nullable: false),
                    Preview = table.Column<bool>(type: "boolean", nullable: false),
                    AutoGenerated = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.FileId);
                    table.ForeignKey(
                        name: "FK_Files_AspNetUsers_LastModifiedById",
                        column: x => x.LastModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Files_AspNetUsers_RegisteredById",
                        column: x => x.RegisteredById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Files_Classifications_ClassificationId",
                        column: x => x.ClassificationId,
                        principalTable: "Classifications",
                        principalColumn: "ClassificationId");
                    table.ForeignKey(
                        name: "FK_Files_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubmittedDocuments",
                columns: table => new
                {
                    SubmittedDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Tags = table.Column<string[]>(type: "text[]", nullable: true),
                    SubmissionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IngestionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ImporterId = table.Column<Guid>(type: "uuid", nullable: true),
                    URL = table.Column<string>(type: "text", nullable: true),
                    SubmitterId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    ClassificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    OverrideClassification = table.Column<bool>(type: "boolean", nullable: false),
                    OverrideReleasableTo = table.Column<bool>(type: "boolean", nullable: false),
                    OverrideEyesOnly = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmittedDocuments", x => x.SubmittedDocumentId);
                    table.ForeignKey(
                        name: "FK_SubmittedDocuments_AspNetUsers_SubmitterId",
                        column: x => x.SubmitterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SubmittedDocuments_Classifications_ClassificationId",
                        column: x => x.ClassificationId,
                        principalTable: "Classifications",
                        principalColumn: "ClassificationId");
                    table.ForeignKey(
                        name: "FK_SubmittedDocuments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId");
                    table.ForeignKey(
                        name: "FK_SubmittedDocuments_IncomingFeeds_ImporterId",
                        column: x => x.ImporterId,
                        principalTable: "IncomingFeeds",
                        principalColumn: "ImporterId");
                });

            migrationBuilder.CreateTable(
                name: "UserDocumentSubscription",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Notify = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDocumentSubscription", x => new { x.UserId, x.DocumentId });
                    table.ForeignKey(
                        name: "FK_UserDocumentSubscription_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserDocumentSubscription_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileGroupEyesOnly",
                columns: table => new
                {
                    EyesOnlyGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    FilesEyesOnlyFileId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileGroupEyesOnly", x => new { x.EyesOnlyGroupId, x.FilesEyesOnlyFileId });
                    table.ForeignKey(
                        name: "FK_FileGroupEyesOnly_Files_FilesEyesOnlyFileId",
                        column: x => x.FilesEyesOnlyFileId,
                        principalTable: "Files",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileGroupEyesOnly_Groups_EyesOnlyGroupId",
                        column: x => x.EyesOnlyGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileRelToGroup",
                columns: table => new
                {
                    FilesReleasableToFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReleasableToGroupId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileRelToGroup", x => new { x.FilesReleasableToFileId, x.ReleasableToGroupId });
                    table.ForeignKey(
                        name: "FK_FileRelToGroup_Files_FilesReleasableToFileId",
                        column: x => x.FilesReleasableToFileId,
                        principalTable: "Files",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileRelToGroup_Groups_ReleasableToGroupId",
                        column: x => x.ReleasableToGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionGroupEyesOnly",
                columns: table => new
                {
                    EyesOnlyGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedDocumentEyesOnlySubmittedDocumentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionGroupEyesOnly", x => new { x.EyesOnlyGroupId, x.SubmittedDocumentEyesOnlySubmittedDocumentId });
                    table.ForeignKey(
                        name: "FK_SubmissionGroupEyesOnly_Groups_EyesOnlyGroupId",
                        column: x => x.EyesOnlyGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubmissionGroupEyesOnly_SubmittedDocuments_SubmittedDocumen~",
                        column: x => x.SubmittedDocumentEyesOnlySubmittedDocumentId,
                        principalTable: "SubmittedDocuments",
                        principalColumn: "SubmittedDocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionRelToGroup",
                columns: table => new
                {
                    ReleasableToGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedDocumentReleasableToSubmittedDocumentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionRelToGroup", x => new { x.ReleasableToGroupId, x.SubmittedDocumentReleasableToSubmittedDocumentId });
                    table.ForeignKey(
                        name: "FK_SubmissionRelToGroup_Groups_ReleasableToGroupId",
                        column: x => x.ReleasableToGroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubmissionRelToGroup_SubmittedDocuments_SubmittedDocumentRe~",
                        column: x => x.SubmittedDocumentReleasableToSubmittedDocumentId,
                        principalTable: "SubmittedDocuments",
                        principalColumn: "SubmittedDocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_APIKeys_UserId",
                table: "APIKeys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoles_CreatedById",
                table: "AspNetRoles",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoles_LastModifiedById",
                table: "AspNetRoles",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classifications_ParentClassificationId",
                table: "Classifications",
                column: "ParentClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_AuthorId",
                table: "Comments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_DocumentId",
                table: "Comments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_LastModifiedById",
                table: "Comments",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentGroupEyesOnly_EyesOnlyGroupId",
                table: "DocumentGroupEyesOnly",
                column: "EyesOnlyGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRelToGroup_ReleasableToGroupId",
                table: "DocumentRelToGroup",
                column: "ReleasableToGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ClassificationId",
                table: "Documents",
                column: "ClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_LastModifiedById",
                table: "Documents",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_RegisteredById",
                table: "Documents",
                column: "RegisteredById");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_SourceId",
                table: "Documents",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ThumbnailId",
                table: "Documents",
                column: "ThumbnailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_URL",
                table: "Documents",
                column: "URL",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTag_TagId",
                table: "DocumentTag",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Facets_CreatedById",
                table: "Facets",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Facets_LastModifiedById",
                table: "Facets",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_Facets_Prefix",
                table: "Facets",
                column: "Prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileGroupEyesOnly_FilesEyesOnlyFileId",
                table: "FileGroupEyesOnly",
                column: "FilesEyesOnlyFileId");

            migrationBuilder.CreateIndex(
                name: "IX_FileRelToGroup_ReleasableToGroupId",
                table: "FileRelToGroup",
                column: "ReleasableToGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_ClassificationId",
                table: "Files",
                column: "ClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_DocumentId",
                table: "Files",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_LastModifiedById",
                table: "Files",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_Files_RegisteredById",
                table: "Files",
                column: "RegisteredById");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_ParentGroupId",
                table: "Groups",
                column: "ParentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ImporterGroupEyesOnly_ImporterEyesOnlyImporterId",
                table: "ImporterGroupEyesOnly",
                column: "ImporterEyesOnlyImporterId");

            migrationBuilder.CreateIndex(
                name: "IX_ImporterRelToGroup_ReleasableToGroupId",
                table: "ImporterRelToGroup",
                column: "ReleasableToGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportRules_ImportRuleSetId",
                table: "ImportRules",
                column: "ImportRuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingFeeds_ClassificationId",
                table: "IncomingFeeds",
                column: "ClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingFeeds_FetchingUserId",
                table: "IncomingFeeds",
                column: "FetchingUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Members_GroupId",
                table: "Members",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderedImportRuleSet_ImportRuleSetId",
                table: "OrderedImportRuleSet",
                column: "ImportRuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_ScraperGroupEyesOnly_ScraperEyesOnlyScraperId",
                table: "ScraperGroupEyesOnly",
                column: "ScraperEyesOnlyScraperId");

            migrationBuilder.CreateIndex(
                name: "IX_ScraperRelToGroup_ScraperReleasableToScraperId",
                table: "ScraperRelToGroup",
                column: "ScraperReleasableToScraperId");

            migrationBuilder.CreateIndex(
                name: "IX_Scrapers_ClassificationId",
                table: "Scrapers",
                column: "ClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Scrapers_SourceId",
                table: "Scrapers",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Sources_LastModifiedById",
                table: "Sources",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_Sources_RegisteredById",
                table: "Sources",
                column: "RegisteredById");

            migrationBuilder.CreateIndex(
                name: "IX_Sources_URL",
                table: "Sources",
                column: "URL",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionGroupEyesOnly_SubmittedDocumentEyesOnlySubmittedD~",
                table: "SubmissionGroupEyesOnly",
                column: "SubmittedDocumentEyesOnlySubmittedDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionRelToGroup_SubmittedDocumentReleasableToSubmitted~",
                table: "SubmissionRelToGroup",
                column: "SubmittedDocumentReleasableToSubmittedDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedDocuments_ClassificationId",
                table: "SubmittedDocuments",
                column: "ClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedDocuments_DocumentId",
                table: "SubmittedDocuments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedDocuments_ImporterId",
                table: "SubmittedDocuments",
                column: "ImporterId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedDocuments_SubmitterId",
                table: "SubmittedDocuments",
                column: "SubmitterId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_CreatedById",
                table: "Tags",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_FacetId",
                table: "Tags",
                column: "FacetId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_FacetId_Label",
                table: "Tags",
                columns: new[] { "FacetId", "Label" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_LastModifiedById",
                table: "Tags",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_URL",
                table: "Tags",
                column: "URL");

            migrationBuilder.CreateIndex(
                name: "IX_UserDocumentSubscription_DocumentId",
                table: "UserDocumentSubscription",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFacetSubscriptions_FacetId",
                table: "UserFacetSubscriptions",
                column: "FacetId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSourceSubscription_SourceId",
                table: "UserSourceSubscription",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTagSubscriptions_TagId",
                table: "UserTagSubscriptions",
                column: "TagId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Documents_DocumentId",
                table: "Comments",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentGroupEyesOnly_Documents_DocumentsEyesOnlyDocumentId",
                table: "DocumentGroupEyesOnly",
                column: "DocumentsEyesOnlyDocumentId",
                principalTable: "Documents",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentRelToGroup_Documents_DocumentsReleasableToDocumentId",
                table: "DocumentRelToGroup",
                column: "DocumentsReleasableToDocumentId",
                principalTable: "Documents",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Files_ThumbnailId",
                table: "Documents",
                column: "ThumbnailId",
                principalTable: "Files",
                principalColumn: "FileId",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_AspNetUsers_LastModifiedById",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_AspNetUsers_RegisteredById",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Files_AspNetUsers_LastModifiedById",
                table: "Files");

            migrationBuilder.DropForeignKey(
                name: "FK_Files_AspNetUsers_RegisteredById",
                table: "Files");

            migrationBuilder.DropForeignKey(
                name: "FK_Sources_AspNetUsers_LastModifiedById",
                table: "Sources");

            migrationBuilder.DropForeignKey(
                name: "FK_Sources_AspNetUsers_RegisteredById",
                table: "Sources");

            migrationBuilder.DropForeignKey(
                name: "FK_Files_Documents_DocumentId",
                table: "Files");

            migrationBuilder.DropTable(
                name: "APIKeys");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "DocumentGroupEyesOnly");

            migrationBuilder.DropTable(
                name: "DocumentRelToGroup");

            migrationBuilder.DropTable(
                name: "DocumentTag");

            migrationBuilder.DropTable(
                name: "FileGroupEyesOnly");

            migrationBuilder.DropTable(
                name: "FileRelToGroup");

            migrationBuilder.DropTable(
                name: "ImporterGroupEyesOnly");

            migrationBuilder.DropTable(
                name: "ImporterRelToGroup");

            migrationBuilder.DropTable(
                name: "ImportRules");

            migrationBuilder.DropTable(
                name: "Members");

            migrationBuilder.DropTable(
                name: "OrderedImportRuleSet");

            migrationBuilder.DropTable(
                name: "ScraperGroupEyesOnly");

            migrationBuilder.DropTable(
                name: "ScraperRelToGroup");

            migrationBuilder.DropTable(
                name: "SubmissionGroupEyesOnly");

            migrationBuilder.DropTable(
                name: "SubmissionRelToGroup");

            migrationBuilder.DropTable(
                name: "UserDocumentSubscription");

            migrationBuilder.DropTable(
                name: "UserFacetSubscriptions");

            migrationBuilder.DropTable(
                name: "UserSourceSubscription");

            migrationBuilder.DropTable(
                name: "UserTagSubscriptions");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "ImportRuleSets");

            migrationBuilder.DropTable(
                name: "Scrapers");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "SubmittedDocuments");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "IncomingFeeds");

            migrationBuilder.DropTable(
                name: "Facets");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropTable(
                name: "Sources");

            migrationBuilder.DropTable(
                name: "Classifications");
        }
    }
}
