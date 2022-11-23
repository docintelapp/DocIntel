/* DocIntel
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DocIntel.Core.Authorization;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;
using DocIntel.Core.Razor;
using DocIntel.Core.Repositories.Query;
using DocIntel.Services.Newsletters.Views.Emails;

using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DocIntel.Services.Newsletters
{
    public class NewsletterSender
    {
        private readonly ApplicationSettings _applicationSettings;
        private readonly EmailSettings _emailSettings;
        private readonly ITagRepository _tagRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly ISourceRepository _sourceRepository;
        private readonly ILogger<NewsletterSender> _logger;
        private readonly AppUserClaimsPrincipalFactory _userClaimsPrincipalFactory;
        private readonly IServiceProvider _serviceProvider;

        public NewsletterSender(EmailSettings emailSettings,
                           IUserRepository userRepository,
                           ITagRepository tagRepository,
                           IDocumentRepository documentRepository,
                           ISourceRepository sourceRepository,
                           ILogger<NewsletterSender> logger,
                           AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
                           ApplicationSettings applicationSettings, IServiceProvider serviceProvider)
        {
            _emailSettings = emailSettings;
            _userRepository = userRepository;
            _tagRepository = tagRepository;
            _documentRepository = documentRepository;
            _sourceRepository = sourceRepository;
            _logger = logger;
            _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
            _applicationSettings = applicationSettings;
            _serviceProvider = serviceProvider;
        }

        // Courtesy of https://stackoverflow.com/questions/22368434/best-way-to-split-string-into-lines-with-maximum-length-without-breaking-words
        static IEnumerable<string> SplitToLines(string stringToSplit, int maximumLineLength)
        {
            var words = stringToSplit.Split(' ').Concat(new [] { "" }).ToArray();
            return
                words
                    .Skip(1)
                    .Aggregate(
                        words.Take(1).ToList(),
                        (a, w) =>
                        {
                            var last = a.Last();
                            while (last.Length > maximumLineLength)
                            {
                                a[^1] = last[..maximumLineLength];
                                last = last[maximumLineLength..];
                                a.Add(last);
                            }
                            var test = $"{last} {w}";
                            if (test.Length > maximumLineLength)
                            {
                                a.Add(w);
                            }
                            else
                            {
                                a[^1] = test;
                            }
                            return a;
                        });
        }

        public async Task RunAsync() {
            var engine = new CustomRazorLightEngineBuilder()
	            .UseFileSystemProject(Path.Combine(Directory.GetCurrentDirectory(), "Views"))
                .UseMemoryCachingProvider()
                .Build();

            AmbientContext ambientContext = GetAmbientContext();
            var users = await _userRepository.GetUsersForNewsletter(ambientContext).ToArrayAsync();
            
            int i = 0;
            foreach (var user in users) {
                _logger.LogInformation("Sending newsletter to " + user.UserName);

                // TODO Support weekly and hourly newsletters, for example for saved searches.
                DateTime dateTime = DateTime.Now.AddDays(-1);
                
                HashSet<Document> docs = await GetDocuments(ambientContext, user, dateTime);
                if (!docs.Any())
                    continue;

                var model = new WelcomeViewModel { User = user, Date = dateTime, Documents = docs };
                string body = await engine.CompileRenderAsync("Emails/Welcome.cshtml", model);
                SendEmail(user, "Daily summary - " + model.Date.ToString("MMM dd, yyyy"), body);
                i++;
            }
            
            if (i == 0)
                _logger.LogInformation("No newsletter sent.");
            else if (i == 1)
                _logger.LogInformation("1 newsletter sent.");
            else
                _logger.LogInformation($"{i} newsletters sent.");
        }

        private void SendEmail(AppUser user, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.NoReplyName, _emailSettings.NoReplyEmail));
            message.To.Add(new MailboxAddress(user.FriendlyName, user.Email));
            message.Subject = subject;
            message.Body = new TextPart("html")
            {
                Text = body
            };

            using var client = new MailKit.Net.Smtp.SmtpClient();
            client.Connect(_emailSettings.SMTPServer, _emailSettings.SMTPPort, SecureSocketOptions.StartTls);
            client.Authenticate(_emailSettings.SMTPUser, _emailSettings.SMTPPassword);
            if (_emailSettings.EmailEnabled)
                client.Send(message);
            client.Disconnect(true);
        }

        private async Task<HashSet<Document>> GetDocuments(AmbientContext ambientContext, AppUser user, DateTime date)
        {
            var docs = new HashSet<Document>();
            
            await foreach (var document in GetFromSubscribedTags(ambientContext, user, date)) 
                docs.Add(document);

            await foreach (var document in GetFromSubscribedSources(ambientContext, user, date)) 
                docs.Add(document);
            
            return await Task.FromResult(docs);
        }

        private async IAsyncEnumerable<Document> GetFromSubscribedSources(AmbientContext ambientContext,
            AppUser user,
            DateTime date)
        {
            var documentSubscriptions =
                _sourceRepository.GetSubscriptionsAsync(ambientContext, user, 0, -1).ToEnumerable();
            foreach (var s in documentSubscriptions)
            {
                DocumentQuery query = new DocumentQuery
                {
                    Source = s.source,
                    Page = 0,
                    Limit = -1,
                    ExcludeMuted = true,
                    RegisteredAfter = date
                };

                await foreach (var d in _documentRepository.GetAllAsync(ambientContext, query))
                {
                        yield return d;
                }
            }
        }

        private async IAsyncEnumerable<Document> GetFromSubscribedTags(AmbientContext ambientContext,
            AppUser user,
            DateTime date)
        {
            var tagSubscriptions = _tagRepository.GetSubscriptionsAsync(ambientContext, user, 0, -1).ToEnumerable();
            foreach (var s in tagSubscriptions)
            {
                DocumentQuery query = new DocumentQuery
                {
                    TagIds = new[] {s.tag.TagId},
                    Page = 0,
                    Limit = -1,
                    ExcludeMuted = true,
                    RegisteredAfter = date
                };

                await foreach (var d in _documentRepository.GetAllAsync(ambientContext, query))
                {
                        yield return d;
                }
            }
        }

        private string SummaryText(string text, IEnumerable<Document> docs)
        {
            foreach (var doc in docs.OrderByDescending(_ => _.DocumentDate))
            {
                var lines = SplitToLines("[" + doc.DocumentDate.ToString("dd/MM/yyyy") + "] " + doc.Title, 80);
                text += lines.First() + "\n";
                text += new String('-', lines.Max(_ => _.Length) - 1) + "\n";
                foreach (var t in lines.Skip(1))
                {
                    text += new String(' ', "* ".Length) + t + "\n";
                }
                if (!string.IsNullOrEmpty(doc.ShortDescription))
                {
                    string stringToSplit = Regex.Replace(doc.ShortDescription, @"[\r\n\t]", " ");
                    var wrappedText = SplitToLines(stringToSplit, 80);
                    foreach (var t in wrappedText)
                    {
                        text += t + "\n";
                    }
                }
                var splitTags = SplitToLines(string.Join(", ",
                        doc.DocumentTags.Select(_ => _.Tag.FriendlyName)),
                    80 - "> Tags: ".Length);
                text += "> Tags: " + splitTags.First() + "\n";
                foreach (var t in splitTags.Skip(1))
                {
                    text += new String(' ', "> Tags: ".Length) + t + "\n";
                }
                text +=
                    $"> Direct link: http://{_applicationSettings.ApplicationBaseURL}/Document/Details/{doc.Reference}\n\n";
            }

            return text;
        }

        private AmbientContext GetAmbientContext()
        {
            var dbContextOptions = _serviceProvider.GetRequiredService<DbContextOptions<DocIntelContext>>();
            var dbContextLogger = _serviceProvider.GetRequiredService<ILogger<DocIntelContext>>();
            var _dbContext = new DocIntelContext(dbContextOptions, dbContextLogger);
            var automationUser =
                _dbContext.Users.AsNoTracking().FirstOrDefault(_ => _.UserName == _applicationSettings.AutomationAccount);
            if (automationUser == null)
                throw new ArgumentNullException($"User '{_applicationSettings.AutomationAccount}' does not exists.");

            var claims = _userClaimsPrincipalFactory.CreateAsync(_dbContext, automationUser).Result;
            var ambientContext = new AmbientContext
            {
                DatabaseContext = _dbContext,
                Claims = claims,
                CurrentUser = automationUser
            };
            return ambientContext;
        }
    }
}