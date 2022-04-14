using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;

using Spectre.Console;
using Spectre.Console.Cli;

namespace DocIntel.AdminConsole.Commands.Users
{
    public class SendResetEmailCommand : UserCommand<SendResetEmailCommand.Settings>
    {
        public class Settings : UserCommandSettings
        {
            [CommandOption("-a|--all")]
            [Description("Verify for all user")]
            [DefaultValue(false)]
            public bool All { get; set; }
        }
        
        private readonly IUserRepository _userRepository;
        private readonly UserManager<AppUser> _userManager;
        private readonly MailKitEmailSender _emailSender;
        private readonly ApplicationSettings _settings;

        public SendResetEmailCommand(DocIntelContext context,
            IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory,
            IUserRepository userRepository, ApplicationSettings applicationSettings, UserManager<AppUser> userManager, ApplicationSettings settings, MailKitEmailSender emailSender) : base(context,
            userClaimsPrincipalFactory, applicationSettings)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _settings = settings;
            _emailSender = emailSender;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await base.ExecuteAsync(context, settings);
            
            if (!TryGetAmbientContext(out var ambientContext))
                return 1;

            if (settings.All)
            {
                foreach (var user in await _userRepository.GetAllAsync(ambientContext).ToArrayAsync())
                {
                    if (!user.EmailConfirmed && !string.IsNullOrEmpty(user.Email) && new EmailAddressAttribute().IsValid(user.Email))
                    {
                        try
                        {
                            await SendResetEmailToUser(ambientContext, user.UserName);
                            AnsiConsole.Render(new Markup($"[green]Reset email was sent to {user.Email}.[/]"));
                        }
                        catch (Exception e)
                        {
                            AnsiConsole.Render(new Markup($"[red]Reset email could not be sent to {user.Email}.[/]"));
                            AnsiConsole.WriteException(e);
                        }
                    }
                }
            }
            else
            {
                var userName = GetUserName(settings);
                if (string.IsNullOrEmpty(userName))
                {
                    AnsiConsole.Render(new Markup("[red]Provide a username or the flag --all.[/]"));
                }
                
                try
                {
                    var user = await SendResetEmailToUser(ambientContext, userName);
                    AnsiConsole.Render(new Markup($"[green]Reset email was sent to {user.Email}.[/]"));
                }
                catch (Exception e)
                {
                    AnsiConsole.Render(new Markup($"[red]Reset email could not be sent to {userName}.[/]"));
                    AnsiConsole.WriteException(e);
                }
            }

            await ambientContext.DatabaseContext.SaveChangesAsync();
            
            return 0;
        }

        private async Task<AppUser> SendResetEmailToUser(AmbientContext ambientContext, string userName)
        {
            var user = await _userRepository.GetByUserName(ambientContext, userName);
            if (user == null) throw new ArgumentNullException(nameof(user));

            // BUG Does not work, see https://stackoverflow.com/questions/48720170/invalid-token-using-generateemailconfirmationtokenasync-outside-controller-in-mv
            // but no solution is suggested
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);

            var baseUri = new Uri(_settings.ApplicationBaseURL);
            var action = new Uri(baseUri, "/Account/ResetPassword");

            var uriBuilder = new UriBuilder(action);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["userId"] = user.Id;
            query["code"] = code;
            uriBuilder.Query = query.ToString() ?? string.Empty;

            await _emailSender.SendPasswordReset(user, uriBuilder.ToString());
            return user;
        }
    }
}