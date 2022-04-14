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

using DocIntel.Core.Models;

using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Importers
{
    [Importer("2f6217ee-ec62-4c73-bbe9-38d2880827b6", Name = "PassiveTotal", Description = "Importer for PassiveTotal")]
    // ReSharper disable once UnusedType.Global because it is used, but with reflexion
    public class PassiveTotalImporter : DefaultImporter
    {
        private readonly Importer _importer;
        private readonly ILogger<PassiveTotalImporter> _logger;
        
        public PassiveTotalImporter(IServiceProvider serviceProvider, Importer importer) : base(serviceProvider)
        {
            _importer = importer;
            _logger = (ILogger<PassiveTotalImporter>) serviceProvider.GetService(typeof(ILogger<PassiveTotalImporter>));
        }
        public override IAsyncEnumerable<SubmittedDocument> PullAsync(DateTime? lastPull, int limit)
        {
            _logger.LogDebug(
                $"Pulling {this.GetType().FullName} from {(lastPull?.ToString() ?? "(not date)")} but max {limit} documents.");
            // TODO Implementation removed as it is now behind a protection mechanism that prevent the scraping to be reliable
            return Enumerable.Empty<SubmittedDocument>().ToAsyncEnumerable();
        }
    }
}