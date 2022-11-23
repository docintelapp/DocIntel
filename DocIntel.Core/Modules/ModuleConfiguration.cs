using System;
using System.Collections.Generic;
using Json.Schema;
using Newtonsoft.Json;

namespace DocIntel.Core.Modules;

public class ModuleConfiguration
{
    public string Name { get; set; }
    public string Assembly { get; set; }
    
    public Dictionary<string,string> Exporters { get; set; }
    
    public List<string> Profiles { get; set; }
    
    public Dictionary<string,ModuleModelMetadata> Metadata { get; set; }
}

public class ModuleModelMetadata {
    public string Name { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string ClassName { get; set; }
    [JsonIgnore]
    public Type? Type { get; set; }
}