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
using DocIntel.Core.Settings;
using SolrNet;

namespace DocIntel.Core.Utils.Indexation.SolR
{
    public class SolRTagFacetIndexingUtility : ITagFacetIndexingUtility
    {
        private readonly IMapper _mapper;
        private readonly ISolrOperations<IndexedTagFacet> _solr;
        private readonly ApplicationSettings _settings;

        public SolRTagFacetIndexingUtility(
            ISolrOperations<IndexedTagFacet> solr,
            IMapper mapper, ApplicationSettings settings)
        {
            _solr = solr;
            _mapper = mapper;
            _settings = settings;
        }

        public void Add(TagFacet tag)
        {
            _solr.Add(_mapper.Map<IndexedTagFacet>(tag), new AddParameters()
            {
                CommitWithin = _settings.Schedule.CommitWithin,
                Overwrite = true
            });
        }

        public void Remove(Guid tagId)
        {
            _solr.Delete(tagId.ToString(), new DeleteParameters()
            {
                CommitWithin = _settings.Schedule.CommitWithin
            });
        }

        public void RemoveAll()
        {
            _solr.Delete(SolrQuery.All, new DeleteParameters()
            {
                CommitWithin = _settings.Schedule.CommitWithin
            });
        }

        public void Update(TagFacet tag)
        {
            _solr.Add(_mapper.Map<IndexedTagFacet>(tag), new AddParameters()
            {
                CommitWithin = _settings.Schedule.CommitWithin,
                Overwrite = true
            });
        }

        public void Commit()
        {
            _solr.Commit();
        }

    }
}
