/* DocIntel
 * Copyright (C) 2018-2023 Belgian Defense, Antoine Cailliau
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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DocIntel.Core.Models;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Importers
{
    [Importer("fbb51f5c-7cce-409d-a39d-b0a9291e3b91", Name = "Mailbox Importer",
        Description = "Import documents from a mailbox")]
    // ReSharper disable once UnusedType.Global because it is used, but with reflexion
    public class MailboxImporter : DefaultImporter
    {
        private readonly Importer _importer;
        private readonly ILogger<MailboxImporter> _logger;

        public MailboxImporter(IServiceProvider serviceProvider, Importer importer) : base(serviceProvider)
        {
            _importer = importer;
            _logger = (ILogger<MailboxImporter>) serviceProvider.GetService(typeof(ILogger<MailboxImporter>));
        }

        public override async IAsyncEnumerable<SubmittedDocument> PullAsync(DateTime? lastPull, int limit)
        {
            _logger.LogDebug(
                $"Pulling {GetType().FullName} from {lastPull?.ToString() ?? "(not date)"} but max {limit} documents.");
            
            var importSettings = _importer.Settings.ToObject<MailboxSettings>();
            
            if (!string.IsNullOrEmpty(importSettings.Host) && !string.IsNullOrEmpty(importSettings.Username))
            {
                using var client = new ImapClient(new ProtocolLogger("imap.log"));
                if (importSettings.SSL)
                    await client.ConnectAsync(importSettings.Host, importSettings.Port, SecureSocketOptions.SslOnConnect);
                else
                    await client.ConnectAsync(importSettings.Host, importSettings.Port);

                await client.AuthenticateAsync(importSettings.Username, importSettings.Password);
                _logger.LogDebug("Authenticated");

                await client.Inbox.OpenAsync(FolderAccess.ReadOnly);
                _logger.LogDebug("INBOX opened");

                var items = await client.Inbox.FetchAsync(0,
                    -1,
                    MessageSummaryItems.BodyStructure | MessageSummaryItems.All | MessageSummaryItems.UniqueId);
                _logger.LogDebug("Fetched " + items.Count + " messages.");

                foreach (var item in items)
                {
                    var message = await client.Inbox.GetMessageAsync(item.UniqueId);
                    if (!message.BodyParts.Any(x => x.IsAttachment && x.ContentType.MimeType == "application/pdf"))
                        continue;

                    if (!Regex.Match(message.Subject, importSettings.SubjectRegex).Success)
                        continue;
                    
                    var uri = "imap://" + importSettings.Email + "/" + client.Inbox.FullName + "/;uid=" + item.UniqueId;
                    yield return new SubmittedDocument
                    {
                        Title = item.NormalizedSubject,
                        URL = uri
                    };
                }

                await client.DisconnectAsync(true);
            }
            else
                _logger.LogDebug("Invalid AccessKey or SecretKey");
        }

        public override Type GetSettingsType()
        {
            return (typeof(MailboxSettings));
        }

        class MailboxSettings
        {
            public string Host { get; set; }
            public int Port { get; set; }
            public bool SSL { get; set; }
            public string Email { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string SubjectRegex { get; set; }
        }
    }
}