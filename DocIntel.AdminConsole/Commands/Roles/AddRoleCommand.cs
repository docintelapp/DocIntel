using System.ComponentModel;
using System.Threading.Tasks;

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
        private readonly IRoleRepository _roleRepository;

        public AddRoleCommand(DocIntelContext context,
            IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory,
            IRoleRepository roleRepository, ApplicationSettings applicationSettings) : base(context,
            userClaimsPrincipalFactory, applicationSettings)
        {
            _roleRepository = roleRepository;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await base.ExecuteAsync(context, settings);
            
            if (!TryGetAmbientContext(out var ambientContext))
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

            var result = await _roleRepository.AddAsync(ambientContext, role);
            if (result != null)
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