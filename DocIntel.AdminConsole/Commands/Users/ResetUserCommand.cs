using System.Threading.Tasks;

using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;

using Microsoft.AspNetCore.Identity;

using Spectre.Console;
using Spectre.Console.Cli;

namespace DocIntel.AdminConsole.Commands.Users
{
    public class ResetUserCommand : UserCommand<UserCommandSettings>
    {
        private readonly IUserRepository _userRepository;

        public ResetUserCommand(DocIntelContext context,
            IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory,
            IUserRepository userRepository, ApplicationSettings applicationSettings) : base(context,
            userClaimsPrincipalFactory, applicationSettings)
        {
            _userRepository = userRepository;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, UserCommandSettings settings)
        {
            await base.ExecuteAsync(context, settings);
            
            if (!TryGetAmbientContext(out var ambientContext))
                return 1;

            var userName = GetUserName(settings);
            var user = await _userRepository.GetByUserName(ambientContext, userName);
            var password = GetPassword(settings);

            if (await _userRepository.ResetPassword(ambientContext, user, password))
            {
                await _context.SaveChangesAsync();
                AnsiConsole.Render(new Markup("[green]Password has been successfully changed.[/]"));
            }
            else
            {
                AnsiConsole.Render(new Markup("[red]Password was not changed.[/]"));
            }

            return 0;
        }
    }
}