using System;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace DocIntel.Core.Helpers;

public class DependencyInjectionResolver : ITypeResolver, IDisposable
{
    internal DependencyInjectionResolver(ServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    private ServiceProvider ServiceProvider { get; }

    public void Dispose()
    {
        ServiceProvider.Dispose();
    }

    public object Resolve(Type type)
    {
        return ServiceProvider.GetService(type) ?? Activator.CreateInstance(type);
    }
}