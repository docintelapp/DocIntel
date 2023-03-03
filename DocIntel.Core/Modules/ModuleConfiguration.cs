using System.Collections.Generic;

namespace DocIntel.Core.Modules;

public class ModuleConfiguration
{
    public string Name { get; set; }
    public string Assembly { get; set; }
    public Dictionary<string,string> Exporters { get; set; }
    public List<string> Profiles { get; set; }
    public Dictionary<string,ModuleModelMetadata> Metadata { get; set; }
}