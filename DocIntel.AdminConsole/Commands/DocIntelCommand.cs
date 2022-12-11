// 

using System.Linq;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
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

        protected readonly UserManager<AppUser> _userManager;
        protected readonly AppRoleManager _roleManager;

        protected DocIntelCommand(DocIntelContext context,
            AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
            ApplicationSettings applicationSettings, UserManager<AppUser> userManager, AppRoleManager roleManager)
        {
            _context = context;
            _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
            _applicationSettings = applicationSettings;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        protected async Task<AmbientContext> TryGetAmbientContext()
        {
            var automationUser = await GetAutomationUserAsync();
            if (automationUser == null)
            {
                return null;
            }

            var claims = await _userClaimsPrincipalFactory.CreateAsync(automationUser);
            return new AmbientContext
            {
                DatabaseContext = _context,
                Claims = claims,
                CurrentUser = automationUser
            };
        }

        protected async Task<AppUser> GetAutomationUserAsync()
        {
            var automationUser = await _userManager.FindByNameAsync(_applicationSettings.AutomationAccount);
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