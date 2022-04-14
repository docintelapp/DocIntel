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
    public interface ICommentRepository
    {
        Task AddAsync(AmbientContext ambientContext, Comment comment);
        Task UpdateAsync(AmbientContext ambientContext, Comment comment);
        Task RemoveAsync(AmbientContext ambientContext, Guid commentId);

        Task<bool> ExistsAsync(AmbientContext ambientContext, Guid id);

        Task<int> CountAsync(AmbientContext ambientContext, CommentQuery query = null);

        Task<Comment> GetAsync(AmbientContext ambientContext, Guid id, string[] includeRelatedData = null);

        IAsyncEnumerable<Comment> GetAllAsync(AmbientContext ambientContext, CommentQuery query = null,
            string[] includeRelatedData = null);
    }
}