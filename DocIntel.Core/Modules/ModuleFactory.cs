using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using AutoMapper;
using DocIntel.Core.Collectors;
using DocIntel.Core.Settings;
using JetBrains.Annotations;
using Json.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DocIntel.Core.Modules;

public class ModuleFactory
{
    private static Dictionary<string, ModuleConfiguration> _modules;
    private static Dictionary<string, Type> _exporters;
    private static Dictionary<string, Type> _collectors;
    private static Dictionary<string, ModuleLoadContext> _assemblyLoadContexts;

    private static List<WeakReference> _wr = new List<WeakReference>();
    private static ApplicationSettings _applicationSettings;
    private static Dictionary<string,Assembly> _assemblies;
    private readonly ILogger<ModuleFactory> _logger;
    private readonly IServiceProvider _serviceProvider;

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
            RegisterCollectors(module);
            // Console.WriteLine($"Done for module '{module.Name}'");
        }

        // Console.WriteLine("Done registering modules");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RegisterExporters(ModuleConfiguration configuration)
    {
        if (!configuration.Exporters?.Any() ?? true)
        {
            // Console.WriteLine($"No exporters found in configuration of {configuration.Name}");
            return;
        }

        foreach (var exporter in configuration.Exporters)
        {   
            if (exporter.Key.Contains("."))
            {
                // Console.WriteLine("Exporter name contains an illegal character: . (dot)");
                continue;
            }
            
            var type = _assemblies[configuration.Name].GetType(exporter.Value);
            // Console.WriteLine($"Installing exporter '{exporter.Key}' ({exporter.Value}) from module '{configuration.Name}'");
            _exporters.Add(GetKey(configuration.Name, exporter.Key), type);
        }
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RegisterCollectors(ModuleConfiguration configuration)
    {
        if (!configuration.Collectors?.Any() ?? true)
        {
            // Console.WriteLine($"No collectors found in configuration of {configuration.Name}");
            return;
        }

        foreach (var collector in configuration.Collectors)
        {
            if (collector.Key.Contains("."))
            {
                // Console.WriteLine("Collector name contains an illegal character: . (dot)");
                continue;
            }
            
            var type = _assemblies[configuration.Name].GetType(collector.Value.Class);
            // Console.WriteLine($"Installing collector '{collector.Key}' ({collector.Value.Class}) from module '{configuration.Name}'");
            _collectors.Add(GetKey(configuration.Name, collector.Key), type);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static MapperConfigurationExpression GetProfiles(string moduleName, ApplicationSettings applicationSettings,
        IServiceProvider serviceProvider)
    {
        var mapperConfigurationExpression = new MapperConfigurationExpression();

        if (_modules.ContainsKey(moduleName))
        {
            var configuration = _modules[moduleName];
            if (!configuration.Profiles?.Any() ?? true)
            {
                // Console.WriteLine($"No profile found in configuration of {configuration.Name}");
            }
            else
            {
                var profiles = configuration.Profiles
                    .Select(_ => _assemblies[configuration.Name].GetType(_))
                    .Where(_ => _ != null);

                // Console.WriteLine($"Installing profiles for module '{configuration.Name}'");

                foreach (var profile in profiles)
                {
                    var instance = (Profile)ActivatorUtilities.CreateInstance(serviceProvider, profile);
                    // Console.WriteLine(instance.ProfileName);
                    mapperConfigurationExpression.AddProfile(instance);
                    // Console.WriteLine($"Added profile '{profile.FullName}' to the AutoMapper configuration");
                }
            }
        }

        return mapperConfigurationExpression;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Init(ApplicationSettings applicationSettings)
    {
        // Console.WriteLine("Clear loaded modules");
        _modules = null;
        _exporters = null;
        _assemblies = null;

        Unload();
        
        if (_wr != null)
        {
            foreach (var assemblyLoadContext in _wr)
            {
                // Console.WriteLine("Wait for plugin to be unloaded...");
                for (int i = 0; assemblyLoadContext.IsAlive && (i < 100); i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    // Console.WriteLine($"GC.Collect {i}");
                }
                // Console.WriteLine($"Done: {assemblyLoadContext.IsAlive}");
            }
            
            _wr = new List<WeakReference>();
        }

        _modules = new();
        _exporters = new();
        _collectors = new();
        _assemblyLoadContexts = new();
        _assemblies = new();
        
        // Hot-load modules from external binaries
        var moduleFolder = applicationSettings.ModuleFolder;
        
        if (!string.IsNullOrEmpty(moduleFolder) & Directory.Exists(moduleFolder))
        {
            // Console.WriteLine($"Loading external modules...");
            Matcher matcher = new();
            matcher.AddIncludePatterns(new[] { "/**/module.json" });

            var configFiles = matcher.Execute(
                new DirectoryInfoWrapper(
                    new DirectoryInfo(moduleFolder)));

            foreach (var configFile in configFiles.Files)
            {
                var configFilepath = Path.Combine(moduleFolder, configFile.Path);
                // Console.WriteLine($"Reading configuration file '{configFilepath}'");

                try
                {
                    var moduleConfiguration = JsonConvert.DeserializeObject<ModuleConfiguration>(File.ReadAllText(configFilepath));
                    if (moduleConfiguration == null) {
                        // Console.WriteLine("Could not load module configuration");
                        continue;
                    }

                    if (moduleConfiguration.Name.Contains("."))
                    {
                        // Console.WriteLine("Name contains an illegal character: . (dot)");
                        continue;
                    }
                
                    var baseDirectory = Path.GetDirectoryName(configFilepath);
                    // Console.WriteLine($"Base directory for the module is '{baseDirectory}'");
                    string pluginLocation = baseDirectory.Replace('/', Path.DirectorySeparatorChar);

                    var moduleLoadContext = new ModuleLoadContext(
                        Path.Combine(baseDirectory, moduleConfiguration.Assembly));
                
                    // Console.WriteLine($"Loading '{moduleConfiguration.Assembly}'");

                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(moduleConfiguration.Assembly);

                    var assembly = moduleLoadContext.LoadFromAssemblyName(
                        new AssemblyName(fileNameWithoutExtension));

                    // Console.WriteLine($"Assembly {assembly.FullName} loaded");
                    _assemblies.Add(moduleConfiguration.Name, assembly);

                    // foreach (var t in assembly.GetTypes())
                    // {
                        // Console.WriteLine($"- {t.Name}");
                    // }

                    var assemblyLoadContext = new WeakReference(moduleLoadContext, trackResurrection: true);
                    _assemblyLoadContexts.Add(moduleConfiguration.Name, moduleLoadContext);
                    _wr.Add(assemblyLoadContext);
                
                    AddModules(moduleConfiguration);
                }
                catch(Exception e)
                {
                    // Console.WriteLine(e.Message);
                    // Console.WriteLine(e.StackTrace);
                }
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
                    // Console.WriteLine("could not get target");
                }
            }
            _assemblyLoadContexts = null;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AddModules(ModuleConfiguration configuration)
    {
        // Console.WriteLine($"Saving configuration module '{configuration.Name}'");
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
            // Console.WriteLine($"Creating instance of {type}");
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

    public static IMapper GetMapper(string moduleName, IServiceProvider serviceProvider)
    {
        if (_modules?.ContainsKey(moduleName) ?? false)
        {
            var mapperConfigurationExpression = GetProfiles(moduleName, _applicationSettings, serviceProvider);
            var mapperConfig = new MapperConfiguration(mapperConfigurationExpression);    
            return new Mapper(mapperConfig);
        }
        else
        {
            // Console.WriteLine("No modules found.");
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public IEnumerable<ModuleModelMetadata> GetMetadata(Type t)
    {
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool HasCollectorSettings([NotNull] string module, [NotNull] string collector)
    {
        if (module == null) return false;
        if (collector == null) return false;
        
        if (_modules.ContainsKey(module))
        {
            var moduleCollectors = _modules[module].Collectors;
            if (moduleCollectors != null && moduleCollectors.ContainsKey(collector))
            {
                var moduleConfiguration = _modules[module];
                var settingsTypeName = moduleConfiguration.Collectors[collector].Settings;
                return settingsTypeName != null;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Type GetCollectorSettings(string module, string collector)
    {
        if (collector == null) return null;
        if (module == null) return null;
        
        if (_modules.ContainsKey(module) && _modules[module].Collectors.ContainsKey(collector))
        {
            var moduleConfiguration = _modules[module];
            var settingsTypeName = moduleConfiguration.Collectors[collector].Settings;
            if (settingsTypeName != null)
            {
                var assembly = _assemblies[moduleConfiguration.Name];
                return assembly.GetType(settingsTypeName);
            }
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public IDocumentCollector GetCollector(string module, string exporter)
    {
        var key = GetKey(module, exporter);
        // Console.WriteLine(key);
        if (_collectors.ContainsKey(key))
        {
            var type = _collectors[key];
            return CreateCollectorInstance(type);
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private IDocumentCollector CreateCollectorInstance(Type type)
    {
        // Console.WriteLine($"Creating instance of {type}");
        var scope = _serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        var instance = (IDocumentCollector)ActivatorUtilities.CreateInstance(sp, type);
        return instance;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public IEnumerable<ModuleConfiguration> GetAll()
    {
        return _modules.Values;
    }
}