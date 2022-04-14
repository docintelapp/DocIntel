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

using AutoMapper;

using DocIntel.Core.Models;

using Microsoft.Extensions.Logging;

using SolrNet;

namespace DocIntel.Core.Utils.Indexation.SolR
{
    public class SolRSourceIndexingUtility : ISourceIndexingUtility
    {
        private readonly ILogger<SolRSourceIndexingUtility> _logger;
        private readonly IMapper _mapper;
        private readonly ISolrOperations<IndexedSource> _solr;

        public SolRSourceIndexingUtility(
            ISolrOperations<IndexedSource> solr,
            ILogger<SolRSourceIndexingUtility> logger,
            IMapper mapper)
        {
            _solr = solr;
            _logger = logger;
            _mapper = mapper;
        }

        public void Add(Source source)
        {
            _logger.LogDebug("Add " + source.SourceId);
            _solr.Add(_mapper.Map<IndexedSource>(source));
            _solr.Commit();
        }

        public void Remove(Guid sourceId)
        {
            _logger.LogDebug("Delete " + sourceId);
            _solr.Delete(sourceId.ToString());
            _solr.Commit();
        }

        public void RemoveAll()
        {
            _solr.Delete(SolrQuery.All);
            _solr.Commit();
        }

        public void Update(Source source)
        {
            _logger.LogDebug("Update " + source.SourceId);
            _solr.Add(_mapper.Map<IndexedSource>(source));
            _solr.Commit();
        }
    }
}