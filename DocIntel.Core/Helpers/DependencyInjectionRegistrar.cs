using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace DocIntel.Core.Helpers;

public class DependencyInjectionRegistrar : ITypeRegistrar
{
    private IServiceCollection Services { get; }
    private IList<IDisposable> BuiltProviders { get; }

    public DependencyInjectionRegistrar(IServiceCollection services)
    {
        Services = services;
        BuiltProviders = new List<IDisposable>();
    }

    public ITypeResolver Build()
    {
        var buildServiceProvider = Services.BuildServiceProvider();
        BuiltProviders.Add(buildServiceProvider);
        return new DependencyInjectionResolver(buildServiceProvider);
    }

    public void Register(Type service, Type implementation)
    {
        Services.AddSingleton(service, implementation);
    }

    public void RegisterInstance(Type service, object implementation)
    {
        Services.AddSingleton(service, implementation);
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        Services.AddSingleton(service, _ => factory());
    }

    public void Dispose()
    {
        foreach (var provider in BuiltProviders)
        {
            provider.Dispose();
        }
    }
}