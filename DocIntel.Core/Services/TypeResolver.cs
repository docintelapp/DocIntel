using System;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace DocIntel.Core.Services
{
    public sealed class TypeResolver : ITypeResolver, IDisposable
    {
        private readonly IServiceProvider _provider;

        public TypeResolver(IServiceProvider provider)
        {
            Console.WriteLine("provider " + (provider == null ? "null" : "not null") );
            Console.Out.Flush();
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public void Dispose()
        {
            if (_provider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        public object Resolve(Type type)
        {
            Console.WriteLine("Resolving " + type.FullName);
            Console.Out.Flush();
            return _provider.GetRequiredService(type);
        }
    }
}