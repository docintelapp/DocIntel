using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
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
            AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
            ApplicationSettings appSettings, AppRoleManager roleManager)
            : base(context, userClaimsPrincipalFactory, appSettings, userManager, roleManager)
        {
            _userManager = userManager;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, UserCommandSettings settings)
        {
            await base.ExecuteAsync(context, settings);
            
            var userName = GetUserName(settings);
            var password = GetPassword(settings);
            if (string.IsNullOrEmpty(userName))
            {
                userName = _applicationSettings.AutomationAccount;
            }
            
            if (await _userManager.FindByNameAsync(userName) != null)
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

            user = await _userManager.FindByNameAsync(userName);
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
                    Name = "Administrator"
                };
                var r = await _roleManager.CreateAsync(role);
                if (r.Succeeded)
                {
                    await _roleManager.SetPermissionAsync(role, permissions.ToArray());
                }
                
                AnsiConsole.Render(new Markup($"Creating role '{role.Name.EscapeMarkup()}' with all permissions.\n"));

            }

            await _userManager.AddToRoleAsync(user, role.Name);
            AnsiConsole.Render(
                new Markup($"Assigning role '{role.Name.EscapeMarkup()}' to user '{userName.EscapeMarkup()}'.\n"));
            await _context.SaveChangesAsync();

            return 0;
        }
    }
}