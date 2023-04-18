using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace DocIntel.Core.Modules;

public class ModuleConfiguration
{
    public string Name { get; set; }
    public string Assembly { get; set; }
    public Dictionary<string,string> Exporters { get; set; }
    public Dictionary<string,ModuleCollector> Collectors { get; set; }
    public List<string> Profiles { get; set; }
    public Dictionary<string,ModuleModelMetadata> Metadata { get; set; }
    public Type Settings { get; set; }
}

public class ModuleCollector
{
    public string Class { get; set; }
    public string Settings { get; set; }
}