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
    public class SolRTagIndexingUtility : ITagIndexingUtility
    {
        private readonly ILogger<SolRTagIndexingUtility> _logger;
        private readonly IMapper _mapper;
        private readonly ISolrOperations<IndexedTag> _solr;

        public SolRTagIndexingUtility(
            ISolrOperations<IndexedTag> solr,
            ILogger<SolRTagIndexingUtility> logger,
            IMapper mapper)
        {
            _solr = solr;
            _logger = logger;
            _mapper = mapper;
        }

        public void Add(Tag tag)
        {
            _logger.LogDebug("Add " + tag.TagId);
            _solr.Add(_mapper.Map<IndexedTag>(tag));
            _solr.Commit();
        }

        public void Remove(Guid tagId)
        {
            _logger.LogDebug("Delete " + tagId);
            _solr.Delete(tagId.ToString());
            _solr.Commit();
        }

        public void RemoveAll()
        {
            _solr.Delete(SolrQuery.All);
            _solr.Commit();
        }

        public void Update(Tag tag)
        {
            _logger.LogDebug("Update " + tag.TagId);
            _solr.Add(_mapper.Map<IndexedTag>(tag));
            _solr.Commit();
        }
    }
}