using System;

namespace DocIntel.Core.Modules;

public class ModuleExporterAttribute : Attribute
{
    public string Name { get; }

    public ModuleExporterAttribute(string name)
    {
        Name = name;
    }
}