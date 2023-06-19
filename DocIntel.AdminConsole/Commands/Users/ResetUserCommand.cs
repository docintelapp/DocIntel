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

        public ResetUserCommand(DocIntelContext context,
            AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
            UserManager<AppUser> userManager, ApplicationSettings applicationSettings, AppRoleManager roleManager) : base(context,
            userClaimsPrincipalFactory, applicationSettings, userManager, roleManager)
        {
        }

        public override async Task<int> ExecuteAsync(CommandContext context, UserCommandSettings settings)
        {
            await base.ExecuteAsync(context, settings);
            
            var ambientContext = await TryGetAmbientContext();
            if (ambientContext == null)
                return 1;

            var userName = GetUserName(settings);
            var user = await _userManager.FindByNameAsync(userName);

            if (user == null)
            {
                AnsiConsole.Render(new Markup($"[red]User [b]{userName}[/] could not be found[/]\n"));
                return 0;
            }
            
            var password = GetPassword(settings);
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            if ((await _userManager.ResetPasswordAsync(user, token, password)).Succeeded)
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