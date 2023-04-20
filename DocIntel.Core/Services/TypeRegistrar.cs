using System;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace DocIntel.Core.Services
{
    public sealed class TypeRegistrar : ITypeRegistrar
    {
        private readonly IServiceCollection _serviceCollection;
        private IServiceProvider _serviceProvider;

        public TypeRegistrar(IServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
        }
        
        public TypeRegistrar(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ITypeResolver Build()
        {
            if (_serviceProvider == null)
            {
                Console.WriteLine("Build new service provider");
                _serviceProvider = _serviceCollection.BuildServiceProvider();
            }

            return new TypeResolver(_serviceProvider);
        }

        public void Register(Type type, Type implementation)
        {
            _serviceCollection.AddSingleton(type, implementation);
        }

        public void RegisterInstance(Type type, object implementation)
        {
            _serviceCollection.AddSingleton(type, implementation);
        }

        public void RegisterLazy(Type type, Func<object> func)
        {            
            if (func is null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            _serviceCollection.AddSingleton(type, (provider) => func());
        }

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
    }
}