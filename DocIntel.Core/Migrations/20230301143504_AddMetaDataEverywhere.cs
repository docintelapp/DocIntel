using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json.Linq;

#nullable disable

namespace DocIntel.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddMetaDataEverywhere : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Dictionary<string, JObject>>(
                name: "MetaData",
                table: "SubmittedDocuments",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Dictionary<string, JObject>>(
                name: "MetaData",
                table: "SavedSearches",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Dictionary<string, JObject>>(
                name: "MetaData",
                table: "Groups",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Dictionary<string, JObject>>(
                name: "MetaData",
                table: "Comments",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Dictionary<string, JObject>>(
                name: "MetaData",
                table: "AspNetUsers",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Dictionary<string, JObject>>(
                name: "MetaData",
                table: "AspNetRoles",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Dictionary<string, JObject>>(
                name: "MetaData",
                table: "APIKeys",
                type: "jsonb",
                nullable: true);

            // Sources already use metadata to store information about RSS
            migrationBuilder.Sql(@"UPDATE ""Sources"" 
SET ""MetaData"" = 
    CASE 
        WHEN ((""MetaData""->'rss_enabled' IS NOT NULL) OR (""MetaData""->'rss_last_pull' IS NOT NULL) OR (""MetaData""->'rss_keywords' IS NOT NULL))
        THEN jsonb_build_object('rss', 
            CASE 
                WHEN (""MetaData""->'rss_enabled' IS NOT NULL) 
                THEN jsonb_build_object('enabled', ""MetaData""->>'rss_enabled' = 'true') 
                ELSE '{}'::jsonb 
            END  
            || CASE 
                WHEN (""MetaData""->'rss_last_pull' IS NOT NULL) 
                THEN jsonb_build_object('last_pull', ""MetaData""->'rss_last_pull') 
                ELSE '{}'::jsonb 
            END  
            || CASE 
                WHEN (""MetaData""->'rss_keywords' IS NOT NULL) 
                THEN jsonb_build_object('keywords', ""MetaData""->'rss_keywords') 
                ELSE '{}'::jsonb 
            END 
            ) 
        ELSE '{}'::jsonb 
    END 
    || 
    CASE 
        WHEN (""MetaData""->'extract_structured_data' IS NOT NULL) 
        THEN jsonb_build_object('extraction', jsonb_build_object('structured_data', ""MetaData""->>'extract_structured_data' = 'true')) 
        ELSE '{}'::jsonb 
    END 
    || 
    CASE 
        WHEN (""MetaData""->'auto_register' IS NOT NULL) 
        THEN jsonb_build_object('registration', jsonb_build_object('auto', ""MetaData""->>'auto_register' = 'true')) 
        ELSE '{}'::jsonb 
    END 
WHERE ""MetaData"" IS NOT NULL;");
            migrationBuilder.Sql(@"UPDATE ""Documents"" 
SET ""MetaData"" = 
    CASE 
        WHEN (""MetaData""->'ScrapePriority' IS NOT NULL) 
        THEN jsonb_build_object('scraper', jsonb_build_object('priority', ""MetaData""->'ScrapePriority')) 
        ELSE '{}'::jsonb 
    END 
    || 
    CASE 
        WHEN (""MetaData""->'ExtractObservables' IS NOT NULL) 
        THEN jsonb_build_object('extraction', jsonb_build_object('structured_data', ""MetaData""->>'ExtractObservables' = 'true')) 
        ELSE '{}'::jsonb 
    END
WHERE ""MetaData"" IS NOT NULL;");
            
            migrationBuilder.Sql(@"UPDATE ""Tags"" 
SET ""MetaData"" = jsonb_build_object('misp', ""MetaData"") 
WHERE ""MetaData"" IS NOT NULL;");
            
            migrationBuilder.Sql(@"UPDATE ""Facets"" 
SET ""MetaData"" = jsonb_build_object('misp', ""MetaData"") 
WHERE ""MetaData"" IS NOT NULL;");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MetaData",
                table: "SubmittedDocuments");

            migrationBuilder.DropColumn(
                name: "MetaData",
                table: "SavedSearches");

            migrationBuilder.DropColumn(
                name: "MetaData",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "MetaData",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "MetaData",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MetaData",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "MetaData",
                table: "APIKeys");
        }
    }
}
