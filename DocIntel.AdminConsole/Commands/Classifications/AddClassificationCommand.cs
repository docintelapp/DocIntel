using System;
using System.ComponentModel;
using System.Dynamic;
using System.Threading.Tasks;

using DocIntel.AdminConsole.Commands.Users;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;

using Microsoft.AspNetCore.Identity;

using Newtonsoft.Json;

using Spectre.Console;
using Spectre.Console.Cli;

namespace DocIntel.AdminConsole.Commands.Classifications
{
    public class AddClassificationCommand : DocIntelCommand<AddClassificationCommand.Settings>
    {
        private readonly IClassificationRepository _classificationRepository;

        public AddClassificationCommand(DocIntelContext context,
            IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory,
            IClassificationRepository classificationRepository, ApplicationSettings applicationSettings) : base(context,
            userClaimsPrincipalFactory, applicationSettings)
        {
            _classificationRepository = classificationRepository;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await base.ExecuteAsync(context, settings);
            
            if (!TryGetAmbientContext(out var ambientContext))
                return 1;

            var classification = await _classificationRepository.AddAsync(ambientContext, new Classification
            {
                Title = settings.Title,
                Abbreviation = settings.Abbreviation,
                Default = settings.Default
            });

            if (classification != null)
            {
                await _context.SaveChangesAsync();
                if (settings.JSON)
                {
                    AnsiConsole.Render(new Markup($"[green]Classification {settings.Title} successfully created.[/]\n"));
                }
                else
                {
                    Console.WriteLine(JsonConvert.SerializeObject(classification));
                }
            }
            else
            {
                if (settings.JSON)
                    AnsiConsole.Render(new Markup($"[red]Could not create classification '{settings.Title}'.[/]\n"));
            }

            return 0;
        }

        public class Settings : DocIntelCommandSettings
        {
            [CommandArgument(0, "<Title>")]
            [Description("Title for the classification")]
            public string Title { get; set; }

            [CommandOption("--abbreviation <Abbreviation>")]
            [Description("Last name of the user")]
            public string Abbreviation { get; set; }

            [CommandOption("--default")]
            [Description("Whether the classification is the default")]
            public bool Default { get; set; }
        }
    }
}