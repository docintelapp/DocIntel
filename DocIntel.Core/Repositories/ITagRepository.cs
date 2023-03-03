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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories.Query;

namespace DocIntel.Core.Repositories
{
    public interface ITagRepository
    {
        Task<Tag> CreateAsync(AmbientContext ambientContext, Tag tag);
        Task<Tag> RemoveAsync(AmbientContext ambientContext, Guid tagId);
        Task<Tag> UpdateAsync(AmbientContext ambientContext, Tag tag);

        Task<Tag> MergeAsync(AmbientContext ambientContext, Guid tag1, Guid tag2);

        Task<bool> ExistsAsync(AmbientContext ambientContext, Guid tagId);
        Task<bool> ExistsAsync(AmbientContext ambientContext, string facetPrefix, string label);
        Task<bool> ExistsAsync(AmbientContext ambientContext, Guid facetId, string label, Guid? tagId = null);

        IAsyncEnumerable<Tag> GetAllAsync(AmbientContext ambientContext, TagQuery query,
            string[] includeRelatedData = null);

        IAsyncEnumerable<Tag> GetAllAsync(AmbientContext context, Func<IQueryable<Tag>, IQueryable<Tag>> query);
        
        Task<Tag> GetAsync(AmbientContext ambientContext, Guid id, string[] includeRelatedData = null);
        Task<Tag> GetAsync(AmbientContext ambientContext, string label, string[] includeRelatedData = null);

        Task<Tag> GetAsync(AmbientContext ambientContext, Guid facetId, string label,
            string[] includeRelatedData = null);

        Task<Tag> GetAsync(AmbientContext ambientContext, TagQuery query, string[] includeRelatedData = null);


        Task<int> CountAsync(AmbientContext ambientContext, TagQuery tagQuery = null);

        Task<SubscriptionStatus> IsSubscribedAsync(AmbientContext ambientContext, AppUser user, Guid tagId);
        Task SubscribeAsync(AmbientContext ambientContext, AppUser user, Guid id, bool notification = false);
        Task UnsubscribeAsync(AmbientContext ambientContext, AppUser user, Guid id);

        Task<MuteStatus> IsMutedAsync(AmbientContext ambientContext, AppUser user, Guid tagId);
        Task MuteAsync(AmbientContext ambientContext, AppUser user, Guid id);
        Task UnmuteAsync(AmbientContext ambientContext, AppUser user, Guid id);

        IAsyncEnumerable<(Tag tag, SubscriptionStatus status)> GetSubscriptionsAsync(AmbientContext ambientContext,
            AppUser user, int page = 0, int limit = 10);

        IAsyncEnumerable<Tag> GetMutedTagsAsync(AmbientContext ambientContext, AppUser user);
    }
}