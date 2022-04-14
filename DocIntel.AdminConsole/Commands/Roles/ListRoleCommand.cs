using System;
using System.Linq;
using System.Threading.Tasks;

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
        private readonly IRoleRepository _roleRepository;

        public ListRoleCommand(DocIntelContext context,
            IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory,
            IRoleRepository roleRepository, ApplicationSettings applicationSettings) : base(context,
            userClaimsPrincipalFactory, applicationSettings)
        {
            _roleRepository = roleRepository;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, RoleCommandSettings settings)
        {
            await base.ExecuteAsync(context, settings);
            
            if (!TryGetAmbientContext(out var ambientContext))
                return 1;

            if (settings.JSON)
            {
                Console.Write(JsonConvert.SerializeObject(await _roleRepository.GetAllAsync(ambientContext).ToArrayAsync()));
            }
            else
            {
                var table = new Table();
                table.AddColumn("Id");
                table.AddColumn("Name");
                table.AddColumn("Description");

                await foreach (var role in _roleRepository.GetAllAsync(ambientContext))
                    table.AddRow(role.Id, role.Name?.EscapeMarkup() ?? "", role.Description?.EscapeMarkup() ?? "");

                AnsiConsole.Render(table);   
            }

            return 0;
        }
    }
}