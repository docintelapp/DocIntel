using System.ComponentModel;

using Spectre.Console.Cli;

namespace DocIntel.AdminConsole.Commands.Roles
{
    public class RoleCommandSettings : DocIntelCommandSettings
    {
        [CommandOption("-r|--role <Role>")]
        [Description("Role name")]
        public string Role { get; set; }
    }
}