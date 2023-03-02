using System.Collections.Generic;
using Json.Schema;

namespace DocIntel.Core.Modules;

public class ModuleConfiguration
{
    public string Name { get; set; }
    public string Assembly { get; set; }
    public Dictionary<string,string> Exporters { get; set; }
    public List<string> Profiles { get; set; }
    public Dictionary<string,ModuleModelMetadata> Metadata { get; set; }
}