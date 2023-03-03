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
            return await CreateImporter(null, type, serviceProvider, context);
        }
        
        public static Task<IImporter> CreateImporter(Importer importer, Type type, IServiceProvider serviceProvider, AmbientContext context)
        {
            var instance = (IImporter) Activator.CreateInstance(type, serviceProvider, importer);
            return Task.FromResult(instance);
        }
    }
}