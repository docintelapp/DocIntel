using System;

namespace DocIntel.Core.Modules;

public class ModuleAttribute : Attribute
{
    public string Name { get; }

    public ModuleAttribute(string name)
    {
        Name = name;
    }
}