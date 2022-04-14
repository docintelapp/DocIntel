using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using DocIntel.Core.Authorization.Operations;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;

using Microsoft.AspNetCore.Identity;

using Spectre.Console;
using Spectre.Console.Cli;

namespace DocIntel.AdminConsole.Commands.Roles
{
    public class PermissionRoleCommand : RoleCommand<PermissionRoleCommand.Settings>
    {
        private readonly IRoleRepository _roleRepository;

        public PermissionRoleCommand(DocIntelContext context,
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

            var roleName = GetField(settings, "Role", settings.Role);
            if (string.IsNullOrEmpty(roleName))
            {
                AnsiConsole.Render(new Markup("[red]Please provide a role name.[/]"));
                return 1;
            }

            if (!await _roleRepository.Exists(ambientContext, roleName))
            {
                AnsiConsole.Render(new Markup($"[red]The role '{roleName}' was not found.[/]"));
                return 1;
            }

            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IOperationConstants).IsAssignableFrom(p) & (typeof(IOperationConstants) != p))
                .ToArray();

            var permissions = new HashSet<string>();

            var possiblePermissions = types.SelectMany(type =>
            {
                var typeInstance = Activator.CreateInstance(type);
                return type.GetProperties().Select(x => x.GetValue(typeInstance)?.ToString());
            }).Where(str => !string.IsNullOrEmpty(str)).ToHashSet();

            if (settings.Permissions == "*")
            {
                permissions = new HashSet<string>(possiblePermissions);
            }
            else if (!settings.Interactive)
            {
                if (settings.Permissions != null)
                    permissions = new HashSet<string>(settings.Permissions.Split(",").Select(x => x.Trim()));
                else
                    AnsiConsole.Render(new Markup("[yellow]Provide a list of permissions or use interactive mode.[/]"));
            }
            else
            {
                var multi = new MultiSelectionPrompt<string>()
                    .Title($"Select the permissions to assign to role '[bold]{roleName}[/]'.")
                    .NotRequired()
                    .PageSize(20)
                    .MoreChoicesText("[grey](Move up and down to reveal more permissions)[/]")
                    .InstructionsText(
                        "[grey](Press [blue]<space>[/] to toggle a permission, " +
                        "[green]<enter>[/] to accept)[/]");

                foreach (var type in types)
                {
                    var typeInstance = Activator.CreateInstance(type);
                    var attribute = type.GetCustomAttribute<DisplayNameAttribute>();
                    multi.AddChoiceGroup(attribute?.DisplayName ?? type.Name,
                        type.GetProperties()
                            .Select(x =>
                            {
                                var propAttribute = x.GetCustomAttribute<DisplayNameAttribute>();
                                return x.GetValue(typeInstance).ToString();
                            }));
                }

                permissions = AnsiConsole.Prompt(multi).ToHashSet();
            }

            var role = await _roleRepository.GetByNameAsync(ambientContext, roleName);

            // Ensure that we only add viable permissions
            permissions.IntersectWith(possiblePermissions);
            role.Permissions = permissions.ToArray();

            _context.Roles.Update(role);
            await _context.SaveChangesAsync();

            return 0;
        }

        public class Settings : RoleCommandSettings
        {
            [CommandOption("-p|--permissions <Permissions>")]
            [Description("Permissions. Use '*' to select all permissions.")]
            public string Permissions { get; set; }
        }
    }
}