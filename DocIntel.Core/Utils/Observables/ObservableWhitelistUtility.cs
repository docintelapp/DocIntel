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
using System.Threading.Tasks;

using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;

using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Utils.Observables
{
    // TODO Is this still used?
    public class ObservableWhitelistUtility : IObservableWhitelistUtility
    {
        private const string regex_domain =
            @"(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z0-9][a-z0-9-]{0,61}[a-z0-9]";

        private const string regex_url =
            @"(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)";

        private const string MD5_RE = @"[^a-fA-F\d\/=\\]([a-fA-F\d]{32})[^a-fA-F\d\/=\\]";
        private const string SHA1_RE = @"[^a-fA-F\d\/=\\]([a-fA-F\d]{40})[^a-fA-F\d\/=\\]";
        private const string SHA256_RE = @"[^a-fA-F\d\/=\\]([a-fA-F\d]{64})[^a-fA-F\d\/=\\]";
        private const string SHA512_RE = @"[^a-fA-F\d\/=\\]([a-fA-F\d]{128})[^a-fA-F\d\/=\\]";
        private readonly ApplicationSettings _configuration;
        private readonly DocIntelContext _context;
        private readonly ILogger<ObservableWhitelistUtility> _logger;
        private readonly IObservableRepository _observableRepository;

        public ObservableWhitelistUtility(ApplicationSettings configuration,
            DocIntelContext context,
            ILogger<ObservableWhitelistUtility> logger,
            IObservableRepository observableRepository)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
            _observableRepository = observableRepository;
        }

        public async Task<IEnumerable<Observable>> GetWhitelistedObservables()
        {
            return await _observableRepository.GetWhitelistedObservables();
        }

        public async Task<bool> AddWhitelistedObservable(Observable o)
        {
            return await _observableRepository.IngestWhitelistedObservables(o);
        }
        
        public async Task<bool> DeleteWhitelistedObservable(Guid observableId)
        {
            return await _observableRepository.DeleteWhitelistedObservable(observableId);
        }
    }
}