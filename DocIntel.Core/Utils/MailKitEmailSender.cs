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
using System.IO;
using System.Threading.Tasks;

using DocIntel.Core.EmailViews.Actions;
using DocIntel.Core.Models;
using DocIntel.Core.Settings;

using MailKit.Security;

using Microsoft.Extensions.Logging;

using MimeKit;
using MimeKit.Text;

using RazorLight;
using RazorLight.Compilation;
using RazorLight.Generation;

namespace DocIntel.Core.Utils
{
    public class MailKitEmailSender 
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<MailKitEmailSender> _logger;

        public MailKitEmailSender(ApplicationSettings settings, ILogger<MailKitEmailSender> logger)
        {
            _logger = logger;
            _emailSettings = settings.Email;
        }

        public async Task SendPasswordReset(AppUser user, string callback)
        {
            await SendActionEmail(user, "Reset your password", "Actions.ResetPassword", new ResetPasswordEmailModel()
            {
                User = user,
                Callback = callback
            });
        }

        public async Task SendActionEmail(AppUser user, string subject, string emailTemplate, object model)
        {
            string assemblyFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var metadataReference = Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(Path.Combine(assemblyFolder, "DocIntel.Core.dll"));

            var engine = new RazorLightEngineBuilder()
                .UseEmbeddedResourcesProject(typeof(MailKitEmailSender).Assembly, "DocIntel.Core.EmailViews.")
                .UseMemoryCachingProvider()
                .AddMetadataReferences(metadataReference)
                .UseOptions(new RazorLightOptions()
                {
                    EnableDebugMode = true
                })
                .Build();

            try
            {
                string body = await engine.CompileRenderAsync(emailTemplate, model);
                SendEmail(user, subject, body);
            }
            catch (TemplateNotFoundException e)
            {
                Console.WriteLine("---");
                Console.WriteLine(string.Join("\n ", e.KnownDynamicTemplateKeys));
                Console.WriteLine("---");
                Console.WriteLine(string.Join("\n ", e.KnownProjectTemplateKeys));
                Console.WriteLine("---");
                throw e;
            }
            catch (TemplateGenerationException e)
            {
                Console.WriteLine("---");
                Console.WriteLine(string.Join("\n ", e.Diagnostics));
                Console.WriteLine("---");
                throw e;
            }
            catch (TemplateCompilationException e)
            {
                Console.WriteLine("---");
                Console.WriteLine(string.Join("\n ", e.CompilationErrors));
                Console.WriteLine("---");
                throw e;
            }
        }
        
        private void SendEmail(AppUser user, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.NoReplyName, _emailSettings.NoReplyEmail));
            message.To.Add(new MailboxAddress(user.FriendlyName, user.Email));
            message.Subject = subject;
            message.Body = new TextPart(TextFormat.Html)
            {
                Text = body
            };

            using var client = new MailKit.Net.Smtp.SmtpClient();
            client.CheckCertificateRevocation = _emailSettings.CheckCertificateRevocation;
            client.Connect(_emailSettings.SMTPServer, _emailSettings.SMTPPort, SecureSocketOptions.StartTls);
            client.Authenticate(_emailSettings.SMTPUser, _emailSettings.SMTPPassword);
            if (_emailSettings.EmailEnabled)
                client.Send(message);
            client.Disconnect(true);
        }

        /// <summary>
        /// Send an email with a confirmation link. 
        /// </summary>
        /// <param name="user">The user to confirm the email for</param>
        /// <param name="callback">The URL for the confirmation</param>
        /// <param name="reset">Whether the user requested a password reset with an unverified email.</param>
        public async Task SendEmailConfirmation(AppUser user, string callback, bool reset = false)
        {
            await SendActionEmail(user, "Confirm your email address", reset ? "Actions.ConfirmEmailReset" : "Actions.ConfirmEmail", new ConfirmEmailModel()
            {
                User = user,
                Callback = callback
            });
        }
    }
}