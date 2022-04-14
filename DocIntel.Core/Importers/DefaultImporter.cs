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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using DocIntel.Core.Models;
using DocIntel.Core.Repositories;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Importers
{
    public abstract class DefaultImporter: IImporter
    {
        private readonly IServiceProvider _serviceProvider;

        protected DefaultImporter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        protected AmbientContext GetContext()
        {
            var userClaimsPrincipalFactory =
                (IUserClaimsPrincipalFactory<AppUser>) _serviceProvider.GetService(
                    typeof(IUserClaimsPrincipalFactory<AppUser>));
            if (userClaimsPrincipalFactory == null) throw new ArgumentNullException(nameof(userClaimsPrincipalFactory));

            var options =
                (DbContextOptions<DocIntelContext>) _serviceProvider.GetService(
                    typeof(DbContextOptions<DocIntelContext>));
            var context = new DocIntelContext(options,
                (ILogger<DocIntelContext>) _serviceProvider.GetService(typeof(ILogger<DocIntelContext>)));

            // TODO Moves the name of the default automation user to the application configuration.
            var automationUser = context.Users.FirstOrDefault(_ => _.UserName == "automation");
            if (automationUser == null)
                return null;

            var claims = userClaimsPrincipalFactory.CreateAsync(automationUser).Result;
            return new AmbientContext {
                DatabaseContext = context,
                Claims = claims,
                CurrentUser = automationUser
            };
        }
        
        public ImporterInformation Get()
        {
            var importerAttribute = GetType().GetCustomAttribute<ImporterAttribute>();
            if (importerAttribute == null)
                throw new Exception("Classes extending DefaultImporter must have the attribute Importer.");

            return new()
            {
                Id = importerAttribute.Identifier,
                Name = importerAttribute.Name,
                Description = importerAttribute.Description
            };
        }

        public Importer Install()
        {
            var test = GetType().GetCustomAttribute<ImporterAttribute>();
            if (test == null)
                throw new Exception("Classes extending DefaultImporter must have the attribute Importer.");
            
            return new ()
            {
                ReferenceClass = test.Identifier,
                Name = test.Name,
                Description = test.Description
            };
        }

        public abstract IAsyncEnumerable<SubmittedDocument> PullAsync(DateTime? lastPull, int limit);
    }
}