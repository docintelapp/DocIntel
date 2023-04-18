/* DocIntel
 * Copyright (C) 2018-2023 Belgian Defense, Antoine Cailliau, Kevin Menten
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
using System.Linq;

using DocIntel.Core.Logging;
using DocIntel.Core.Models;

using Microsoft.AspNetCore.Http;
using Synsharp;
using Synsharp.Telepath.Messages;

namespace DocIntel.WebApp.Helpers
{
    public static class LoggingHelpers
    {
        public static LogEvent AddHttpContext(this LogEvent logEvent, HttpContext connection)
        {
            logEvent.AddProperty("http.request.method", connection.Request.Method);
            logEvent.AddProperty("http.request.referrer", connection.Request.Headers["referer"].ToString());

            logEvent.AddProperty("http.version", connection.Request.Headers["referer"].ToString());
            logEvent.AddProperty("http.response.status_code", connection.Response.StatusCode);

            logEvent.AddProperty("url.path", connection.Request.Path);
            logEvent.AddProperty("url.query", connection.Request.QueryString);
            logEvent.AddProperty("url.scheme", connection.Request.Scheme);

            logEvent.AddClient(connection.Connection);
            return logEvent;
        }

        private static LogEvent AddClient(this LogEvent logEvent, ConnectionInfo connection)
        {
            logEvent.AddProperty("client.ip", connection.RemoteIpAddress);
            logEvent.AddProperty("client.port", connection.RemotePort);
            logEvent.AddProperty("server.ip", connection.LocalIpAddress);
            logEvent.AddProperty("server.port", connection.LocalPort);
            return logEvent;
        }

        public static LogEvent AddDocument(this LogEvent logEvent, Document document)
        {
            logEvent.AddProperty("document.id", document.DocumentId);
            logEvent.AddProperty("document.reference", document.Reference);
            logEvent.AddProperty("document.title", document.Title);
            logEvent.AddProperty("document.classification", document.Classification);
            logEvent.AddProperty("document.last_modified_by.id", document.LastModifiedById);
            logEvent.AddProperty("document.registered_by.id", document.RegisteredById);
            logEvent.AddProperty("document.modification_date", document.ModificationDate);
            logEvent.AddProperty("document.registration_date", document.RegistrationDate);
            logEvent.AddProperty("document.source_id", document.SourceId);
            logEvent.AddProperty("document.tags", string.Join(",", document.DocumentTags
                .Select(_ => _.Tag)
                .Select(_ => _.FriendlyName)));
            logEvent.AddProperty("document.status", document.Status);
            // TODO log information about the files    
            return logEvent;
        }

        public static LogEvent AddCollector(this LogEvent logEvent, Collector collector)
        {
            logEvent.AddProperty("collector.id", collector.CollectorId);
            return logEvent;
        }
        
        public static LogEvent AddComment(this LogEvent logEvent, Comment comment)
        {
            logEvent.AddProperty("comment.id", comment.CommentId);
            logEvent.AddProperty("comment.document.id", comment.DocumentId);
            logEvent.AddProperty("comment.author.id", comment.AuthorId);
            logEvent.AddProperty("comment.date", comment.CommentDate);
            logEvent.AddProperty("comment.body", comment.Body);
            return logEvent;
        }

        public static LogEvent AddFile(this LogEvent logEvent, DocumentFile file)
        {
            logEvent.AddProperty("file.id", file.FileId);
            logEvent.AddProperty("file.title", file.Title);
            logEvent.AddProperty("file.name", file.Filename);
            logEvent.AddProperty("file.sha256", file.Sha256Hash);
            logEvent.AddProperty("file.visible", file.Visible);
            logEvent.AddProperty("file.preview", file.Preview);
            logEvent.AddProperty("file.classification.id", file.Classification?.ClassificationId);
            logEvent.AddProperty("file.eyes_only.id", file.EyesOnly?.Select(_ => _.GroupId));
            logEvent.AddProperty("file.releasable_to.id", file.ReleasableTo?.Select(_ => _.GroupId));
            return logEvent;
        }

        public static LogEvent AddImportRule(this LogEvent logEvent, ImportRule rule)
        {
            logEvent.AddProperty("import_rule.id", rule.ImportRuleId);
            logEvent.AddProperty("import_rule.set_id", rule.ImportRuleSetId);
            logEvent.AddProperty("import_rule.name", rule.Name);
            logEvent.AddProperty("import_rule.description", rule.Description);
            logEvent.AddProperty("import_rule.replacement", rule.Replacement);
            logEvent.AddProperty("import_rule.search_pattern", rule.SearchPattern);
            logEvent.AddProperty("import_rule.position", rule.Position);
            return logEvent;
        }

        public static LogEvent AddImportRuleSet(this LogEvent logEvent, ImportRuleSet set)
        {
            logEvent.AddProperty("import_ruleset.id", set.ImportRuleSetId);
            logEvent.AddProperty("import_ruleset.name", set.Name);
            logEvent.AddProperty("import_ruleset.description", set.Description);
            return logEvent;
        }

        public static LogEvent AddIncomingFeed(this LogEvent logEvent, Importer feed)
        {
            logEvent.AddProperty("incoming_feed.id", feed.ImporterId);
            logEvent.AddProperty("incoming_feed.name", feed.Name);
            logEvent.AddProperty("incoming_feed.description", feed.Description);
            return logEvent;
        }

        public static LogEvent AddScraper(this LogEvent logEvent, Scraper scraper)
        {
            logEvent.AddProperty("scraper.id", scraper.ScraperId);
            logEvent.AddProperty("scraper.name", scraper.Name);
            logEvent.AddProperty("scraper.description", scraper.Description);
            return logEvent;
        }

        public static LogEvent AddRole(this LogEvent logEvent, AppRole role)
        {
            logEvent.AddProperty("role.id", role.Id);
            logEvent.AddProperty("role.name", role.Name);
            logEvent.AddProperty("role.description", role.Description);
            return logEvent;
        }

        public static LogEvent AddGroup(this LogEvent logEvent, Group role)
        {
            logEvent.AddProperty("group.id", role.GroupId);
            logEvent.AddProperty("group.name", role.Name);
            return logEvent;
        }

        public static LogEvent AddClassification(this LogEvent logEvent, Classification classification)
        {
            logEvent.AddProperty("classification.id", classification.ClassificationId);
            logEvent.AddProperty("classification.name", classification.Title);
            return logEvent;
        }

        public static LogEvent AddObservable(this LogEvent logEvent, SynapseNode observable)
        {
            logEvent.AddProperty("observable.id", observable.Iden);
            logEvent.AddProperty("observable.form", observable.Form);
            logEvent.AddProperty("observable.value", observable.Valu);
            return logEvent;
        }


        public static LogEvent AddSource(this LogEvent logEvent, Source source, string prefix = "source")
        {
            if (string.IsNullOrEmpty(prefix))
                throw new ArgumentException("Prefix cannot be null or empty.", nameof(prefix));

            logEvent.AddProperty(prefix + ".id", source.SourceId);
            logEvent.AddProperty(prefix + ".name", source.Title);
            logEvent.AddProperty(prefix + ".description", source.Description);
            logEvent.AddProperty(prefix + ".creation_date", source.CreationDate);
            logEvent.AddProperty(prefix + ".modification_date", source.ModificationDate);
            logEvent.AddProperty(prefix + ".registered_by.id", source.RegisteredById);
            logEvent.AddProperty(prefix + ".last_modified_by.id", source.LastModifiedById);
            return logEvent;
        }

        public static LogEvent AddTag(this LogEvent logEvent, Tag tag, string prefix = "tag")
        {
            if (string.IsNullOrEmpty(prefix))
                throw new ArgumentException("Prefix cannot be null or empty.", nameof(prefix));

            logEvent.AddProperty(prefix + ".id", tag.TagId);
            logEvent.AddProperty(prefix + ".name", tag.Label);
            logEvent.AddProperty(prefix + ".facet.id", tag.FacetId);
            logEvent.AddProperty(prefix + ".description", tag.Description);
            logEvent.AddProperty(prefix + ".creation_date", tag.CreationDate);
            logEvent.AddProperty(prefix + ".created_by.id", tag.CreatedById);
            logEvent.AddProperty(prefix + ".last_modified_by.id", tag.LastModifiedById);
            return logEvent;
        }

        public static LogEvent AddFacet(this LogEvent logEvent, TagFacet facet, string prefix = "facet")
        {
            if (string.IsNullOrEmpty(prefix))
                throw new ArgumentException("Prefix cannot be null or empty.", nameof(prefix));

            logEvent.AddProperty(prefix + ".id", facet.FacetId);
            logEvent.AddProperty(prefix + ".name", facet.Title);
            logEvent.AddProperty(prefix + ".description", facet.Description);
            logEvent.AddProperty(prefix + ".prefix", facet.Prefix);
            logEvent.AddProperty(prefix + ".hidden", facet.Hidden);
            logEvent.AddProperty(prefix + ".mandatory", facet.Mandatory);
            return logEvent;
        }
    }
}