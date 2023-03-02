using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using AutoMapper;
using DocIntel.Core.Settings;
using Json.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DocIntel.Core.Modules;

public class ModuleFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ModuleFactory> _logger;
    
    private static Dictionary<string, ModuleConfiguration> _modules;
    private static Dictionary<string, Type> _exporters;
    private static Dictionary<string, ModuleLoadContext> _assemblyLoadContexts;
    
    private static List<WeakReference> _wr = new List<WeakReference>();
    private static ApplicationSettings _applicationSettings;
    private static Dictionary<string,Assembly> _assemblies;

    public ModuleFactory(IServiceProvider serviceProvider, ILogger<ModuleFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Register(ApplicationSettings applicationSettings)
    {
        _applicationSettings = applicationSettings;
        Init(applicationSettings);
        
        foreach (var module in _modules.Values)
        {
            RegisterExporters(module);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RegisterExporters(ModuleConfiguration configuration)
    {
        if (!configuration.Exporters?.Any() ?? true)
        {
            Console.WriteLine($"No exporters found in configuration of {configuration.Name}");
            return;
        }

        foreach (var exporter in configuration.Exporters)
        {
            var type = _assemblies[configuration.Name].GetType(exporter.Value);
            Console.WriteLine($"Installing exporter '{exporter.Key}' ({exporter.Value}) from module '{configuration.Name}'");
            _exporters.Add(GetKey(configuration.Name, exporter.Key), type);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static MapperConfigurationExpression RegisterProfiles(ApplicationSettings applicationSettings,
        IServiceProvider serviceProvider)
    {

        var mapperConfigurationExpression = new MapperConfigurationExpression();
            
        foreach (var configuration in _modules.Values)
        {
            if (!configuration.Profiles?.Any() ?? true)
            {
                Console.WriteLine($"No profile found in configuration of {configuration.Name}");
                continue;
            }

            var profiles = configuration.Profiles
                .Select(_ => _assemblies[configuration.Name].GetType(_))
                .Where(_ => _ != null);
            
            Console.WriteLine($"Installing profiles for module '{configuration.Name}'");
            
            foreach (var profile in profiles)
            {
                var instance = (Profile) ActivatorUtilities.CreateInstance(serviceProvider, profile);
                mapperConfigurationExpression.AddProfile(instance);
            }
        }

        return mapperConfigurationExpression;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Init(ApplicationSettings applicationSettings)
    {
        Console.WriteLine("Clear loaded modules");

        _modules = null;
        _exporters = null;
        _assemblies = null;

        Unload();
        
        if (_wr != null)
        {
            foreach (var assemblyLoadContext in _wr)
            {
                Console.WriteLine("Wait for plugin to be unloaded...");
                for (int i = 0; assemblyLoadContext.IsAlive && (i < 100); i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Console.WriteLine($"GC.Collect {i}");
                }
                Console.WriteLine($"Done: {assemblyLoadContext.IsAlive}");
            }
            
            _wr = new List<WeakReference>();
        }

        _modules = new();
        _exporters = new();
        _assemblyLoadContexts = new();
        _assemblies = new();
        
        // Hot-load modules from external binaries
        var moduleFolder = applicationSettings.ModuleFolder;
        
        if (!string.IsNullOrEmpty(moduleFolder) & Directory.Exists(moduleFolder))
        {
            Console.WriteLine($"Loading external modules...");
            Matcher matcher = new();
            matcher.AddIncludePatterns(new[] { "/**/module.json" });

            var configFiles = matcher.Execute(
                new DirectoryInfoWrapper(
                    new DirectoryInfo(moduleFolder)));

            foreach (var configFile in configFiles.Files)
            {
                var configFilepath = Path.Combine(moduleFolder, configFile.Path);
                Console.WriteLine($"Reading configuration file '{configFilepath}'");
                
                var moduleConfiguration = JsonConvert.DeserializeObject<ModuleConfiguration>(File.ReadAllText(configFilepath));
                if (moduleConfiguration == null) {
                    Console.WriteLine("Could not load module configuration");
                    continue;
                }
                
                var baseDirectory = Path.GetDirectoryName(configFilepath);
                Console.WriteLine($"Base directory for the module is '{baseDirectory}'");
                string pluginLocation = baseDirectory.Replace('/', Path.DirectorySeparatorChar);

                var moduleLoadContext = new ModuleLoadContext(Path.Combine(baseDirectory, moduleConfiguration.Assembly));
                
                Console.WriteLine($"Loading '{moduleConfiguration.Assembly}'");

                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(moduleConfiguration.Assembly);

                var assembly = moduleLoadContext.LoadFromAssemblyName(
                    new AssemblyName(fileNameWithoutExtension));

                Console.WriteLine($"Assembly {assembly.FullName} loaded");
                _assemblies.Add(moduleConfiguration.Name, assembly);

                foreach (var t in assembly.GetTypes())
                {
                    Console.WriteLine($"- {t.Name}");
                }

                Console.WriteLine("ThreatQuotientExporter: " + assembly.GetType($"{fileNameWithoutExtension}.ThreatQuotientExporter"));
                
                var assemblyLoadContext = new WeakReference(moduleLoadContext, trackResurrection: true);
                _assemblyLoadContexts.Add(moduleConfiguration.Name, moduleLoadContext);
                _wr.Add(assemblyLoadContext);
                
                AddModules(moduleConfiguration);
            }
        }
    }

    private static void Unload()
    {
        if (_assemblyLoadContexts != null)
        {
            foreach (var assemblyLoadContext in _assemblyLoadContexts)
            {
                var target = assemblyLoadContext.Value;
                if (target != null)
                {
                    target.Unload();
                }
                else
                {
                    Console.WriteLine("could not get target");
                }
            }
            _assemblyLoadContexts = null;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AddModules(ModuleConfiguration configuration)
    {
        Console.WriteLine($"Saving configuration module '{configuration.Name}'");
        if (_modules.ContainsKey(configuration.Name))
            _modules[configuration.Name] = configuration;
        else    
            _modules.Add(configuration.Name, configuration);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public IModuleExporter GetExporter(string module, string exporter)
    {
        var key = GetKey(module, exporter);
        if (_exporters.ContainsKey(key))
        {
            var type = _exporters[key];
            Console.WriteLine($"Creating instance of {type}");
            var scope = _serviceProvider.CreateScope();
            var sp = scope.ServiceProvider;

            var instance = (IModuleExporter) ActivatorUtilities.CreateInstance(sp, type);
            return instance;
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string GetKey(string module, string exporter)
    {
        return module + "." + exporter;
    }

    public static IMapper GetMapper(IServiceProvider serviceProvider)
    {
        if (_modules?.Any() ?? false)
        {
            var mapperConfigurationExpression = RegisterProfiles(_applicationSettings, serviceProvider);
            var mapperConfig = new MapperConfiguration(mapperConfigurationExpression);    
            return new Mapper(mapperConfig);   
        }

        return null;
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public IEnumerable<ModuleModelMetadata> GetMetadata(Type t)
    {
        var generator = new JsonSchemaBuilder();
        
        foreach (var configuration in _modules.Values) {
            foreach (var moduleMetadata in configuration.Metadata)
            {
                if (moduleMetadata.Key == t.FullName)
                {
                    var assembly = _assemblies[configuration.Name];
                    var type = assembly.GetType(moduleMetadata.Value.ClassName);
                    moduleMetadata.Value.Type = type;
                    yield return moduleMetadata.Value;
                }    
            }
        }
    }
}