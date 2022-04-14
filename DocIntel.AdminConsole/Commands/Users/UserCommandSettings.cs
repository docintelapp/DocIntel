using System.ComponentModel;

using Spectre.Console.Cli;

namespace DocIntel.AdminConsole.Commands.Users
{
    public class UserCommandSettings : DocIntelCommandSettings
    {
        [CommandOption("-u|--username <UserName>")]
        [Description("User name of the user")]
        public string UserName { get; set; }

        [CommandOption("-p|--password <Password>")]
        [Description("Password of the user")]
        public string Password { get; set; }

        [CommandOption("-r|--random")]
        [Description("Generate a random password for the user")]
        [DefaultValue(false)]
        public bool RandomPassword { get; set; }
    }
}