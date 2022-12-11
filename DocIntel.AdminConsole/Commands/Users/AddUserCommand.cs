using System;
using System.ComponentModel;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DocIntel.AdminConsole.Commands.Users
{
    public class AddUserCommand : UserCommand<AddUserCommand.Settings>
    {
        private readonly IOptions<IdentityOptions> _identityOptions;
        
        public AddUserCommand(DocIntelContext context,
            AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
            AppUserManager userManager,
            ApplicationSettings applicationSettings,
            IOptions<IdentityOptions> identityOptions, AppRoleManager roleManager) : base(context,
            userClaimsPrincipalFactory, applicationSettings, userManager, roleManager)
        {
            _identityOptions = identityOptions;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await base.ExecuteAsync(context, settings);
            
            var ambientContext = await TryGetAmbientContext();
            if (ambientContext == null)
                return 1;

            var userName = GetUserName(settings);
            var password = GetPassword(settings);

            var firstName = GetField(settings, "First name", settings.FirstName);
            var lastName = GetField(settings, "Last name", settings.LastName);
            var email = GetField(settings, "Email", settings.Email);

            if (string.IsNullOrEmpty(userName))
            {
                AnsiConsole.WriteLine("Please specify a non-empty username.");
                return 1;
            }
            
            var user = new AppUser
            {
                UserName = userName,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                RegistrationDate = DateTime.UtcNow
            };

            if (await _userManager.FindByNameAsync(user.UserName) != null)
            {
                AnsiConsole.Render(new Markup($"[darkorange]User '{userName}' already exists.[/]\n"));
                return 0;
            }

            var passwordValidator = new PasswordValidator<AppUser>();
            var isValidPassword = await passwordValidator.ValidateAsync(_userManager, null, password);

            if (!isValidPassword.Succeeded)
            {
                var options = _identityOptions.Value.Password;
                AnsiConsole.Render(new Markup($"[red]The password does not match the policy.[/]\n"));
                AnsiConsole.Render(
                    new Markup($"[red]- Length must be greater than {options.RequiredLength} characters.[/]\n"));
                if (options.RequireDigit)
                    AnsiConsole.Render(new Markup($"[red]- Password must contain numbers.[/]\n"));
                if (options.RequireLowercase)
                    AnsiConsole.Render(new Markup($"[red]- Password must contain lowercase letters.[/]\n"));
                if (options.RequireUppercase)
                    AnsiConsole.Render(new Markup($"[red]- Password must contain uppercase letters.[/]\n"));
                if (options.RequiredUniqueChars > 0)
                    AnsiConsole.Render(new Markup(
                        $"[red]- Password must contain at last {options.RequiredUniqueChars} unique characters.[/]\n"));
                if (options.RequireNonAlphanumeric)
                    AnsiConsole.Render(
                        new Markup($"[red]- Password must have at least one non-alphanumeric (e.g. @#$) symbol.[/]\n"));
                return 1;
            }

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                AnsiConsole.Render(new Markup($"[red]Could not create user '{userName}'.[/]\n"));
                return 1;
            }

            AnsiConsole.Render(new Markup($"[green]User {userName} successfully created.[/]\n"));
            await _context.SaveChangesAsync();
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