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
using DocIntel.Core.Repositories.Query;

namespace DocIntel.Core.Repositories
{
    public interface ITagFacetRepository
    {
        // TODO Ensure that the naming are coherent with Add/Create and Update/Edit
        Task<TagFacet> AddAsync(AmbientContext ambientContext, TagFacet tagFacet);
        Task<TagFacet> RemoveAsync(AmbientContext ambientContext, Guid tagFacetId);
        Task<TagFacet> UpdateAsync(AmbientContext ambientContext, TagFacet tagFacet);

        Task<bool> ExistsAsync(AmbientContext ambientContext, Guid tagFacetId);

        IAsyncEnumerable<TagFacet> GetAllAsync(AmbientContext ambientContext, FacetQuery query = null,
            string[] includeRelatedData = null);

        Task<TagFacet> GetAsync(AmbientContext ambientContext, Guid id, string[] includeRelatedData = null);
        Task<TagFacet> GetAsync(AmbientContext ambientContext, string prefix, string[] includeRelatedData = null);

        Task SubscribeAsync(AmbientContext ambientContext, AppUser user, Guid facetId, bool notification);
        Task UnsubscribeAsync(AmbientContext ambientContext, AppUser user, Guid facetId);

        Task MergeAsync(AmbientContext ambientContext, Guid primary, Guid secondary);
    }
}