using System;
using System.Linq;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;

using Microsoft.AspNetCore.Identity;

using Newtonsoft.Json;

using Spectre.Console;
using Spectre.Console.Cli;

namespace DocIntel.AdminConsole.Commands.Roles
{
    public class ListRoleCommand : RoleCommand<RoleCommandSettings>
    {
        public ListRoleCommand(DocIntelContext context,
            AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
            ApplicationSettings applicationSettings, UserManager<AppUser> userManager,
            AppRoleManager roleManager) : base(context,
            userClaimsPrincipalFactory, applicationSettings, userManager, roleManager)
        {
        }

        public override async Task<int> ExecuteAsync(CommandContext context, RoleCommandSettings settings)
        {
            await base.ExecuteAsync(context, settings);
            
            var ambientContext = await TryGetAmbientContext();
            if (ambientContext == null)
                return 1;

            if (settings.JSON)
            {
                Console.Write(JsonConvert.SerializeObject(_roleManager.Roles.ToArray()));
            }
            else
            {
                var table = new Table();
                table.AddColumn("Id");
                table.AddColumn("Name");
                table.AddColumn("Description");

                foreach (var role in _roleManager.Roles.ToArray())
                    table.AddRow(role.Id, role.Name?.EscapeMarkup() ?? "", role.Description?.EscapeMarkup() ?? "");

                AnsiConsole.Render(table);   
            }

            return 0;
        }
    }
}