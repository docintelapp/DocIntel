// 

using System.ComponentModel;

using Spectre.Console.Cli;

namespace DocIntel.AdminConsole.Commands
{
    public class DocIntelCommandSettings : CommandSettings
    {
        [CommandOption("-i|--interactive")]
        [Description("Interactive mode")]
        [DefaultValue(false)]
        public bool Interactive { get; set; }
        
        [CommandOption("-j|--json")]
        [Description("Output JSON content")]
        [DefaultValue(false)]
        public bool JSON { get; set; }
    }
}