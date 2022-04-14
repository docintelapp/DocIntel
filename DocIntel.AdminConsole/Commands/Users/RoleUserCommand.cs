using System;
using System.ComponentModel;
using System.Threading.Tasks;

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
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;

        public RoleUserCommand(DocIntelContext context,
            IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory,
            IUserRepository userRepository, ApplicationSettings applicationSettings, IRoleRepository roleRepository) : base(context,
            userClaimsPrincipalFactory, applicationSettings)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await base.ExecuteAsync(context, settings);
            
            if (!TryGetAmbientContext(out var ambientContext))
                return 1;

            var userName = GetUserName(settings);
            var user = await _userRepository.GetByUserName(ambientContext, userName);

            var roleName = settings.Role;
            var role = await _roleRepository.GetByNameAsync(ambientContext, roleName);

            if (user != null && role != null)
            {
                await _roleRepository.AddUserRoleAsync(ambientContext, user.Id, role.Id);
                await ambientContext.DatabaseContext.SaveChangesAsync();
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