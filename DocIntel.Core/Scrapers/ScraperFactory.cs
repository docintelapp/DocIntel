/* DocIntel
 * Copyright (C) 2018-2023 Belgian Defense, Antoine Cailliau
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
            
            if (type == null)
            {
                throw new ArgumentNullException(
                    $"Could not find scraper for {referenceClass}");
            }

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

        public static Task<IScraper> CreateScraper(Scraper scraper, Type type, IServiceProvider serviceProvider,
            AmbientContext context)
        {
            var instance = (IScraper) Activator.CreateInstance(type, scraper, serviceProvider);
            return Task.FromResult(instance);
        }
    }
}