using System;

namespace DocIntel.Core.Modules;

public class ModuleAttribute : Attribute
{
    public ModuleAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}