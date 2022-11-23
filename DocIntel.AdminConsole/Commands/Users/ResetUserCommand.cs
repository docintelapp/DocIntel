using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;

using Microsoft.AspNetCore.Identity;

using Spectre.Console;
using Spectre.Console.Cli;

namespace DocIntel.AdminConsole.Commands.Users
{
    public class ResetUserCommand : UserCommand<UserCommandSettings>
    {
        private readonly AppUserManager _userManager;

        public ResetUserCommand(DocIntelContext context,
            AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
            AppUserManager userManager, ApplicationSettings applicationSettings) : base(context,
            userClaimsPrincipalFactory, applicationSettings)
        {
            _userManager = userManager;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, UserCommandSettings settings)
        {
            await base.ExecuteAsync(context, settings);
            
            if (!TryGetAmbientContext(out var ambientContext))
                return 1;

            var userName = GetUserName(settings);
            var user = await _userManager.FindByNameAsync(userName);

            if (user == null)
            {
                AnsiConsole.Render(new Markup($"[red]User [b]{userName}[/] could not be found[/]\n"));
                return 0;
            }
            
            var password = GetPassword(settings);

            if (await _userManager.ResetPassword(ambientContext.Claims, user, password))
            {
                await _context.SaveChangesAsync();
                AnsiConsole.Render(new Markup("[green]Password has been successfully changed.[/]"));
            }
            else
            {
                AnsiConsole.Render(new Markup("[red]Password was not changed.[/]"));
            }

            return 0;
        }
    }
}