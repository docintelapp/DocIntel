using System;

namespace DocIntel.Core.Modules;

public class ModuleExporterAttribute : Attribute
{
    public ModuleExporterAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}