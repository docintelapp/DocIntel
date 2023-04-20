using System;
using System.Reflection;
using System.Runtime.Loader;

namespace DocIntel.Core.Modules;

class ModuleLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public ModuleLoadContext(string modulePath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(modulePath );
        
        this.Resolving += OnResolving;
    }

    private Assembly OnResolving(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        // Console.WriteLine($"Could not resolve {assemblyName.Name}");
        return null; // Fail to read.
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }
        else
        {
            // Console.WriteLine($"Could not load assembly {assemblyName}");
            return null;
        }
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }
        else
        {
            // Console.WriteLine($"Could not load unmanaged dll {unmanagedDllName}");
            return IntPtr.Zero;
        }
    }
}