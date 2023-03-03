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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories.Query;

namespace DocIntel.Core.Repositories
{
    /// <summary>
    ///     Represents a document repository
    /// </summary>
    public interface IDocumentRepository
    {
        /// <summary>
        ///     Add the document to the repository
        /// </summary>
        /// <param name="document">The document</param>
        /// <param name="file">The file</param>
        /// <returns>
        ///     <c>True</c> if the document was added, <c>False</c> otherwise.
        /// </returns>
        Task<Document> AddAsync(AmbientContext context, Document document, Tag[] tags = null,
            ISet<Group> releasableTo = null,
            ISet<Group> eyesOnly = null);

        /// <summary>
        ///     Removes the document from the repository
        /// </summary>
        /// <param name="document">The document</param>
        /// <returns>
        ///     <c>True</c> if the document was added, <c>False</c> otherwise.
        /// </returns>
        Task<Document> RemoveAsync(AmbientContext context, Guid document);

        /// <summary>
        ///     Updates the document in the repository. The file is updated if
        ///     not null.
        /// </summary>
        /// <param name="document">The document</param>
        /// <param name="file">The file</param>
        /// <returns>
        ///     <c>True</c> if the document was updated, <c>False</c> otherwise.
        /// </returns>
        Task<Document> UpdateAsync(AmbientContext context, Document document, ISet<Tag> tags = null,
            ISet<Group> releasableTo = null, ISet<Group> eyesOnly = null);

        /// <summary>
        ///     Returns whether the document is in the repository
        /// </summary>
        /// <param name="id">The identifier</param>
        /// <returns>
        ///     <c>True</c> if the document exists, <c>False</c> otherwise.
        /// </returns>
        Task<bool> ExistsAsync(AmbientContext context, Guid id);

        Task<bool> ExistsAsync(AmbientContext context, DocumentQuery query);

        /// <summary>
        ///     Returns all documents matching the query parameters.
        /// </summary>
        /// <param name="query">Parameters for the query</param>
        /// <returns>The documents</returns>
        IAsyncEnumerable<Document> GetAllAsync(AmbientContext context, DocumentQuery query = null,
            string[] includeRelatedData = null);

        /// <summary>
        ///     Returns the role matching the specified identifier.
        /// </summary>
        /// <param name="id">The identifier</param>
        /// <returns>The role</returns>
        Task<Document> GetAsync(AmbientContext context, Guid id, string[] includeRelatedData = null);

        Task<Document> GetAsync(AmbientContext context, DocumentQuery query, string[] includeRelatedData = null);

        Task<int> CountAsync(AmbientContext context, DocumentQuery query = null);

        Task<SubscriptionStatus> IsSubscribedAsync(AmbientContext context, Guid documentId);

        Task SubscribeAsync(AmbientContext context,
            Guid documentId,
            bool notification = false);

        Task UnsubscribeAsync(AmbientContext context,
            Guid documentId);

        IAsyncEnumerable<(Document document, SubscriptionStatus status)> GetSubscriptionsAsync(AmbientContext context,
            int page = 0,
            int limit = 10);

        Task<DocumentFile> GetFileAsync(AmbientContext ambientContext, Guid id, string[] includeRelatedData = null);

        Task<DocumentFile> AddFile(AmbientContext ambientContext, DocumentFile file, Stream stream, ISet<Group> releasableTo = null, ISet<Group> eyesOnly = null);
        Task<DocumentFile> UpdateFile(AmbientContext ambientContext, DocumentFile file, Stream stream = null, ISet<Group> releasableTo = null, ISet<Group> eyesOnly = null);
        Task<DocumentFile> DeleteFile(AmbientContext ambientContext, Guid id);

        Task<SubmittedDocument> SubmitDocument(AmbientContext ambientContext, SubmittedDocument doc);
        IAsyncEnumerable<SubmittedDocument> GetSubmittedDocuments(AmbientContext ambientContext, Func<IQueryable<SubmittedDocument>, IQueryable<SubmittedDocument>> query = null, int page = 0, int limit = 100);
        void  DeleteSubmittedDocument(AmbientContext ambientContext, Guid id, SubmissionStatus status = SubmissionStatus.Processed, bool hard = false);
        Task<SubmittedDocument> GetSubmittedDocument(AmbientContext ambientContext, Guid submissionId, Func<IQueryable<SubmittedDocument>, IQueryable<SubmittedDocument>> query = null);
        Task<bool> ExistsAsync(AmbientContext context, Func<IQueryable<Document>, IQueryable<Document>> query);
        IAsyncEnumerable<Document> GetAllAsync(AmbientContext context, Func<IQueryable<Document>, IQueryable<Document>> query);
        Task<Document> UpdateStatusAsync(AmbientContext context, Guid documentId, DocumentStatus status);
    }
}