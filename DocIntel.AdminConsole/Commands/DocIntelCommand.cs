// 

using System.Linq;
using System.Threading.Tasks;
using DocIntel.Core.Authorization;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DocIntel.AdminConsole.Commands
{
    public abstract class DocIntelCommand<T> : AsyncCommand<T> where T : DocIntelCommandSettings
    {
        protected readonly ApplicationSettings _applicationSettings;
        protected readonly DocIntelContext _context;
        private readonly AppUserClaimsPrincipalFactory _userClaimsPrincipalFactory;

        protected DocIntelCommand(DocIntelContext context,
            AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
            ApplicationSettings applicationSettings)
        {
            _context = context;
            _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
            _applicationSettings = applicationSettings;
        }

        protected bool TryGetAmbientContext(out AmbientContext ambientContext)
        {
            var automationUser = GetAutomationUser();
            if (automationUser == null)
            {
                ambientContext = null;
                return false;
            }

            var claims = _userClaimsPrincipalFactory.CreateAsync(automationUser).Result;
            ambientContext = new AmbientContext
            {
                DatabaseContext = _context,
                Claims = claims,
                CurrentUser = automationUser
            };

            return true;
        }

        private AppUser GetAutomationUser()
        {
            var automationUser =
                _context.Users.AsNoTracking().FirstOrDefault(_ => _.UserName == _applicationSettings.AutomationAccount);
            if (automationUser == null)
                AnsiConsole.Render(
                    new Markup($"[red]The user '{_applicationSettings.AutomationAccount}' was not found.[/]"));

            return automationUser;
        }

        protected static string GetField(DocIntelCommandSettings settings, string prompt, string defaultValue)
        {
            if (!settings.Interactive) return defaultValue;
            return string.IsNullOrEmpty(defaultValue) ? AnsiConsole.Ask<string>($"{prompt}:") : defaultValue;
        }

        public override Task<int> ExecuteAsync(CommandContext context, T settings)
        {   
            if (!settings.JSON)
                AnsiConsole.Write(new Markup($"[bold yellow]DocIntel Administrative Console[/]\n" +
                                             "*** For more information on DocIntel see https://docintel.org/ ***\n" +
                                             "*** Please report bugs to https://github.com/docintelapp/DocIntel ***\n"));
            return Task.FromResult(0);
        }
    }
}