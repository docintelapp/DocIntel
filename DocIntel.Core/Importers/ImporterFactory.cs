/* DocIntel
 * Copyright (C) 2018-2021 Belgian Defense, Antoine Cailliau
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using DocIntel.Core.Exceptions;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;

using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Importers
{
    public static class ImporterFactory
    {
        public static Type[] GetAllImporters()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p =>
                {
                    var importerAttribute = p.GetCustomAttribute<ImporterAttribute>();
                    return p.IsClass & importerAttribute != null;
                })
                .ToArray();
            return types;
        }
        
        public static Task<IImporter> CreateImporter(Importer importer, IServiceProvider serviceProvider, AmbientContext context)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .SingleOrDefault(p =>
                {
                    var importerAttribute = p.GetCustomAttribute<ImporterAttribute>();
                    return p.IsClass & importerAttribute != null && importerAttribute.Identifier == importer.ReferenceClass;
                });

            return CreateImporter(importer, type, serviceProvider, context);
        }
        
        public static Task<IImporter> CreateImporter(Guid referenceClass, IServiceProvider serviceProvider, AmbientContext context)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .SingleOrDefault(p =>
                {
                    var importerAttribute = p.GetCustomAttribute<ImporterAttribute>();
                    return p.IsClass & importerAttribute != null && importerAttribute.Identifier == referenceClass;
                });
            return CreateImporter(null, type, serviceProvider, context);
        }

        public static async Task<IImporter> CreateImporter(Type type, IServiceProvider serviceProvider, AmbientContext context)
        {
            var logger = (ILogger) serviceProvider.GetService(typeof(ILogger<>).MakeGenericType(type));
            var importerAttribute = type.GetCustomAttribute<ImporterAttribute>();
            var repository = (IIncomingFeedRepository) serviceProvider.GetService(typeof(IIncomingFeedRepository));
            Importer importer = null;
            try {
                importer = await repository.GetAsync(context, importerAttribute.Identifier);
            }
            catch (NotFoundEntityException)
            {
                logger.LogError("Could not find entity " + importerAttribute.Identifier.ToString());
            }
            return await CreateImporter(importer, type, serviceProvider, context);
        }
        
        public static Task<IImporter> CreateImporter(Importer importer, Type type, IServiceProvider serviceProvider, AmbientContext context)
        {
            var logger = (ILogger) serviceProvider.GetService(typeof(ILogger<>).MakeGenericType(type));
            
            var instance = (IImporter) Activator.CreateInstance(type, serviceProvider, importer);

            FillProperties<string>(_ => _?.ToString(), type, importer, instance, logger);
            FillProperties<bool>(bool.Parse, type, importer, instance, logger);
            FillProperties<int>(int.Parse, type, importer, instance, logger);
            FillProperties<double>(double.Parse, type, importer, instance, logger);

            return Task.FromResult(instance);
        }

        private static void FillProperties<T>(Func<string, T> convert, Type importerType, Importer importer,
            IImporter instance, ILogger logger)
        {
            var stringProperties = importerType.GetProperties()
                .Where(property =>
                {
                    var attribute = property.GetCustomAttribute<ImporterSettingAttribute>();
                    return property.PropertyType == typeof(T) & attribute != null;
                });

            logger.LogDebug("Detected properties for " + typeof(T) + ": " + string.Join(",", stringProperties.Select(_ => _.Name)) );
            
            foreach (var property in stringProperties)
            {
                var attribute = property.GetCustomAttribute<ImporterSettingAttribute>();
                var val = attribute.DefaultValue;
                if (importer?.Settings != null && importer.Settings.ContainsKey(attribute.Name))
                    val = importer.Settings[attribute.Name]?.ToString();
                try
                {
                    property.SetValue(instance, !string.IsNullOrEmpty(val) ? convert(val) : default(T));
                }
                catch (FormatException e)
                {
                    Console.WriteLine(property.Name);
                    Console.WriteLine("could not format " + e.StackTrace);
                }
            }
        }
    }
}