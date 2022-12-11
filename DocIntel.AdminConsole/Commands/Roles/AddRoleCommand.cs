using System.ComponentModel;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;

using Microsoft.AspNetCore.Identity;

using Spectre.Console;
using Spectre.Console.Cli;

namespace DocIntel.AdminConsole.Commands.Roles
{
    public class AddRoleCommand : RoleCommand<AddRoleCommand.Settings>
    {
        public AddRoleCommand(DocIntelContext context,
            AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
            ApplicationSettings applicationSettings, UserManager<AppUser> userManager,
            AppRoleManager roleManager) : base(context,
            userClaimsPrincipalFactory, applicationSettings, userManager, roleManager)
        {
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await base.ExecuteAsync(context, settings);
            
            var ambientContext = await TryGetAmbientContext();
            if (ambientContext == null)
                return 1;

            var roleName = GetField(settings, "Role name", default);
            if (string.IsNullOrEmpty(roleName))
            {
                AnsiConsole.Render(new Markup(
                    "[yellow]Empty role name provided. Try with --name or with interactive mode --interactive.[/]"));
                return 1;
            }

            var description = GetField(settings, "Description", settings.Description);

            var role = new AppRole
            {
                Name = roleName,
                Description = description
            };

            if (await _roleManager.FindByNameAsync(roleName) != null)
            {
                AnsiConsole.Render(new Markup($"[darkorange]Role '{roleName}' already exists.[/]\n"));
                return 0;
            }
            
            var result = await _roleManager.CreateAsync(role);
            if (result.Succeeded)
            {
                AnsiConsole.Render(new Markup($"[green]Role {roleName} successfully created.[/]\n"));
                await _context.SaveChangesAsync();
            }
            else
            {
                AnsiConsole.Render(new Markup($"[red]Could not create role '{roleName}'.[/]\n"));
            }

            return 0;
        }

        public class Settings : RoleCommandSettings
        {
            [CommandOption("--description <Description>")]
            [Description("Description for the role")]
            public string Description { get; set; }
        }
    }
}