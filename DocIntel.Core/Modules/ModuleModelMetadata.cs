using System;
using Newtonsoft.Json;

namespace DocIntel.Core.Modules;

public class ModuleModelMetadata {
    public string Name { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string ClassName { get; set; }
    [JsonIgnore]
    public Type Type { get; set; }
}