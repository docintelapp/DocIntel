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
using System.Threading.Tasks;

using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;

namespace DocIntel.Core.Utils
{
    public class TagUtility
    {
        private readonly ITagRepository _tagRepository;
        private readonly ITagFacetRepository _facetRepository;

        public TagUtility(ITagRepository tagRepository, ITagFacetRepository facetRepository)
        {
            _tagRepository = tagRepository;
            _facetRepository = facetRepository;
        }

        public async Task<Tag> GetOrCreateTag(AmbientContext ambientContext, TagFacet facet, string label, HashSet<Tag> cache)
        {
            Tag cached;
            if ((cached = cache.FirstOrDefault(_ => _.FacetId == facet.FacetId & _.Label.ToUpper() == label.ToUpper())) != null)
            {
                return cached;
            }

            var retrievedTag = _tagRepository.GetAllAsync(ambientContext, new TagQuery()
            {
                FacetId = facet.FacetId,
                Label = label
            }).ToEnumerable().SingleOrDefault();
            
            if (retrievedTag == null)
            {
                retrievedTag = await _tagRepository.CreateAsync(ambientContext, new Tag
                {
                    Label = label,
                    Facet = facet,
                    FacetId = facet.FacetId
                });
            }
            else
            {
                await _tagRepository.UpdateAsync(ambientContext, retrievedTag);
            }

            cache.Add(retrievedTag);
            return retrievedTag;
        }

        public async Task<TagFacet> GetOrCreateFacet(AmbientContext ambientContext, string prefix, string name = "")
        {
            var facet = await _facetRepository.GetAllAsync(ambientContext, new FacetQuery
            {
                Prefix = prefix
            }).SingleOrDefaultAsync();
            if (facet != null)
            {
                await _facetRepository.UpdateAsync(ambientContext, facet);
                return facet;
            }

            return await _facetRepository.AddAsync(ambientContext, new TagFacet
            {
                Title = string.IsNullOrEmpty(name) ? prefix : name,
                Prefix = prefix
            });
        }
    }
}