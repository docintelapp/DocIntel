using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DocIntel.Core.Authorization.Operations;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Spectre.Console;
using Spectre.Console.Cli;

namespace DocIntel.AdminConsole.Commands.Users
{
    public class InitUserCommand : UserCommand<UserCommandSettings>
    {
        private readonly UserManager<AppUser> _userManager;
        
        public InitUserCommand(DocIntelContext context,
            UserManager<AppUser> userManager,
            IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory,
            ApplicationSettings appSettings)
            : base(context, userClaimsPrincipalFactory, appSettings)
        {
            _userManager = userManager;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, UserCommandSettings settings)
        {
            await base.ExecuteAsync(context, settings);
            
            var userName = GetUserName(settings);
            var password = GetPassword(settings);

            if (_context.Users.Any(_ => _.NormalizedUserName == userName.ToUpper()))
            {
                AnsiConsole.Render(new Markup($"[red]User '{userName}' already exists.[/]\n"));
                return 0;
            }
            
            var user = new AppUser
            {
                UserName = userName,
                RegistrationDate = DateTime.UtcNow,
                Bot = true
            };

            var result = _userManager.CreateAsync(user, password);
            result.Wait();

            if (result.Result.Succeeded)
            {
                AnsiConsole.Render(new Markup($"[green]User '{userName}' successfully created.[/]\n"));
            }
            else
            {
                AnsiConsole.Render(new Markup($"[red]User '{userName}' could not be created:[/]\n"));
                foreach (var error in result.Result.Errors)
                    AnsiConsole.Render(new Markup("- " + error.Description.EscapeMarkup()));
            }

            user = _context.Users.Single(_ => _.UserName == userName);
            var type = typeof(IOperationConstants);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p)).ToArray();
            var permissions = new HashSet<string>();
            foreach (var i in types.SelectMany(t =>
                t.GetFields().Where(f => f.IsPublic).Select(x => (string) x.GetValue(null)))) permissions.Add(i);
            foreach (var i in types.SelectMany(t => t.GetProperties().Select(x => (string) x.GetValue(null))))
                permissions.Add(i);

            var role = await _context.Roles.AsQueryable().SingleOrDefaultAsync();
            if (role == null)
            {
                role = new AppRole
                {
                    Name = "Administrator", NormalizedName = "ADMINISTRATOR",
                    PermissionList = string.Join(",", permissions)
                };
                AnsiConsole.Render(new Markup($"Creating role '{role.Name.EscapeMarkup()}' with all permissions.\n"));

                _context.Add((object) role);   
            }
            _context.Add(new AppUserRole {User = user, Role = role});
            AnsiConsole.Render(
                new Markup($"Assigning role '{role.Name.EscapeMarkup()}' to user '{userName.EscapeMarkup()}'.\n"));
            await _context.SaveChangesAsync();

            return 0;
        }
    }
}