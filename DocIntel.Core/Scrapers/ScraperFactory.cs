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

namespace DocIntel.Core.Scrapers
{
    public static class ScraperFactory
    {
        public static Type[] GetAllScrapers()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p =>
                {
                    var importerAttribute = p.GetCustomAttribute<ScraperAttribute>();
                    return p.IsClass & (importerAttribute != null);
                })
                .ToArray();
            return types;
        }

        public static Task<IScraper> CreateScraper(Scraper scraper, IServiceProvider serviceProvider, AmbientContext context)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .SingleOrDefault(p =>
                {
                    var importerAttribute = p.GetCustomAttribute<ScraperAttribute>();
                    return p.IsClass & (importerAttribute != null) && importerAttribute.Identifier == scraper.ReferenceClass;
                });

            return CreateScraper(scraper, type, serviceProvider, context);
        }

        public static Task<IScraper> CreateScraper(Guid referenceClass, IServiceProvider serviceProvider, AmbientContext context)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .SingleOrDefault(p =>
                {
                    var importerAttribute = p.GetCustomAttribute<ScraperAttribute>();
                    return p.IsClass & (importerAttribute != null) && importerAttribute.Identifier == referenceClass;
                });

            return CreateScraper(null, type, serviceProvider, context);
        }

        public static async Task<IScraper> CreateScraper(Type type, IServiceProvider serviceProvider, AmbientContext context)
        {
            var importerAttribute = type.GetCustomAttribute<ScraperAttribute>();
            
            var repository = (IScraperRepository) serviceProvider.GetService(typeof(IScraperRepository));
            Scraper scraper = null;
            try
            {
                scraper = await repository.GetAsync(context, importerAttribute.Identifier);
            }
            catch (NotFoundEntityException)
            {
            }
            
            return await CreateScraper(scraper, type, serviceProvider, context);
        }

        public static async Task<IScraper> CreateScraper(Scraper scraper, Type type, IServiceProvider serviceProvider,
            AmbientContext context)
        {
            var logger = serviceProvider.GetService(typeof(ILogger<>).MakeGenericType(type));

            var instance = (IScraper) Activator.CreateInstance(type, new object?[] {scraper, serviceProvider});

            FillProperties(_ => _?.ToString(), type, scraper, instance);
            FillProperties(bool.Parse, type, scraper, instance);
            FillProperties(int.Parse, type, scraper, instance);
            FillProperties(double.Parse, type, scraper, instance);

            return instance;
        }

        private static void FillProperties<T>(Func<string, T> convert, Type importerType, Scraper importer,
            IScraper instance)
        {
            var stringProperties = importerType.GetProperties()
                .Where(property =>
                {
                    var attribute = property.GetCustomAttribute<ScraperSettingAttribute>();
                    return (property.PropertyType == typeof(T)) & (attribute != null);
                });

            foreach (var property in stringProperties)
            {
                var attribute = property.GetCustomAttribute<ScraperSettingAttribute>();
                var val = attribute.DefaultValue;
                if (importer?.Settings != null && importer.Settings.ContainsKey(attribute.Name))
                    val = importer.Settings[attribute.Name]?.ToString();
                try
                {
                    property.SetValue(instance, !string.IsNullOrEmpty(val) ? convert(val) : default);
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