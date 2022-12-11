using System;
using System.ComponentModel;
using System.Linq;
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
    public class RoleUserCommand : UserCommand<RoleUserCommand.Settings>
    {
        public RoleUserCommand(DocIntelContext context,
            AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
            ApplicationSettings applicationSettings,
            UserManager<AppUser> userManager, 
            AppRoleManager roleManager) 
            : base(context,
                userClaimsPrincipalFactory, 
                applicationSettings, 
                userManager, 
                roleManager)
        {
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await base.ExecuteAsync(context, settings);
            
            var ambientContext = await TryGetAmbientContext();
            if (ambientContext == null)
                return 1;

            var userName = GetUserName(settings);
            var user = await _userManager.FindByNameAsync(userName);

            var roleName = settings.Role;
            var role = await _roleManager.FindByNameAsync(roleName);

            if (user != null && role != null)
            {
                    await _userManager.AddToRoleAsync(user, role.Name);
                    AnsiConsole.Render(new Markup($"[green]User '{userName}' has now role '{roleName}'.[/]\n"));
            }
            else
            {   
                AnsiConsole.Render(new Markup($"[red]Could not find user '{userName}' or role '{roleName}'.[/]\n"));
            }

            return 0;
        }

        public class Settings : UserCommandSettings
        {
            [CommandOption("--role <Role>")]
            [Description("Role for the user")]
            public string Role { get; set; }
        }
    }
}