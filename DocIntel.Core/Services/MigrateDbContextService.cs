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
using System.Threading.Tasks;
using DocIntel.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using RunMethodsSequentially;

namespace DocIntel.Core.Services
{
    public class MigrateDbContextService : IStartupServiceToRunSequentially
    {
        public int OrderNum { get; }
 
        public async ValueTask ApplyYourChangeAsync(
            IServiceProvider scopedServices)
        {
            var logger = scopedServices
                .GetRequiredService<ILogger<MigrateDbContextService>>();
            if (logger != null) logger.LogDebug("Running EF core migrations.");
            
            var context = scopedServices.GetRequiredService<DocIntelContext>();
            await context.Database.MigrateAsync();
            
            if (logger != null) logger.LogDebug("EF Core Migrations done.");
        }
    }
}