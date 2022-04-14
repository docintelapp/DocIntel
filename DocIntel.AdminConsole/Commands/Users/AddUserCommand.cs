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
    public class AddUserCommand : UserCommand<AddUserCommand.Settings>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;

        public AddUserCommand(DocIntelContext context,
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
            var password = GetPassword(settings);

            var firstName = GetField(settings, "First name", settings.FirstName);
            var lastName = GetField(settings, "Last name", settings.LastName);
            var email = GetField(settings, "Email", settings.Email);

            var user = new AppUser
            {
                UserName = userName,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                RegistrationDate = DateTime.UtcNow
            };

            user = await _userRepository.CreateAsync(ambientContext, user, password);
            if (user != null)
            {
                AnsiConsole.Render(new Markup($"[green]User {userName} successfully created.[/]\n"));
                await _context.SaveChangesAsync();
            }
            else
            {
                AnsiConsole.Render(new Markup($"[red]Could not create user '{userName}'.[/]\n"));
            }

            return 0;
        }

        public class Settings : UserCommandSettings
        {
            [CommandOption("--firstName <FirstName>")]
            [Description("First name of the user")]
            public string FirstName { get; set; }

            [CommandOption("--lastName <LastName>")]
            [Description("Last name of the user")]
            public string LastName { get; set; }

            [CommandOption("--email <Email>")]
            [Description("Email of the user")]
            public string Email { get; set; }

            [CommandOption("--role <Role>")]
            [Description("Role for the user")]
            public string Role { get; set; }
        }
    }
}