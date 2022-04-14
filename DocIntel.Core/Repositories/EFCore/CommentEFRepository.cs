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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Messages;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories.Query;

using Ganss.XSS;

using MassTransit;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace DocIntel.Core.Repositories.EFCore
{
    public class CommentEFRepository : ICommentRepository
    {
        private readonly IAppAuthorizationService _appAuthorizationService;
        private readonly IPublishEndpoint _busClient;

        private readonly HtmlSanitizer _sanitizer;

        public CommentEFRepository(IPublishEndpoint busClient,
            IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory,
            IAppAuthorizationService appAuthorizationService)
        {
            _busClient = busClient;
            _appAuthorizationService = appAuthorizationService;

            _sanitizer = new HtmlSanitizer();
        }

        public async Task AddAsync(AmbientContext ambientContext,
            Comment comment)
        {
            var document = await ambientContext.DatabaseContext.Documents.FindAsync(comment.DocumentId);
            if (document == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanAddComment(ambientContext.Claims, document, comment))
                throw new UnauthorizedOperationException();

            if (IsValid(comment, out var modelErrors))
            {
                comment.CommentDate = DateTime.Now;
                comment.ModificationDate = comment.CommentDate;

                comment.AuthorId = ambientContext.CurrentUser.Id;
                comment.LastModifiedById = ambientContext.CurrentUser.Id;

                comment.Body = _sanitizer.Sanitize(comment.Body);

                var trackingEntity = await ambientContext.DatabaseContext.AddAsync(comment);
                ambientContext.DatabaseContext.OnSaveCompleteTasks.Add(
                    () => _busClient.Publish(new CommentCreatedMessage
                    {
                        CommentId = trackingEntity.Entity.CommentId,
                        UserId = ambientContext.CurrentUser.Id
                    })
                );
            }
            else
            {
                throw new InvalidArgumentException(modelErrors);
            }
        }

        public async Task UpdateAsync(AmbientContext ambientContext,
            Comment comment)
        {
            var retrievedComment = await ambientContext.DatabaseContext.Comments.FindAsync(comment.CommentId);
            if (retrievedComment == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanEditComment(ambientContext.Claims, retrievedComment))
                throw new UnauthorizedOperationException();

            if (IsValid(comment, out var modelErrors))
            {
                comment.ModificationDate = DateTime.UtcNow;
                comment.Body = _sanitizer.Sanitize(comment.Body);
                comment.LastModifiedById = ambientContext.CurrentUser.Id;

                var trackingEntity = ambientContext.DatabaseContext.Update(comment);
                ambientContext.DatabaseContext.OnSaveCompleteTasks.Add(
                    () => _busClient.Publish(new CommentUpdatedMessage
                    {
                        CommentId = trackingEntity.Entity.CommentId,
                        UserId = ambientContext.CurrentUser.Id
                    })
                );
            }
            else
            {
                throw new InvalidArgumentException(modelErrors);
            }
        }

        public async Task RemoveAsync(AmbientContext ambientContext,
            Guid commentId)
        {
            var comment = await ambientContext.DatabaseContext.Comments.FindAsync(commentId);
            if (comment == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanDeleteComment(ambientContext.Claims, comment))
                throw new UnauthorizedOperationException();

            var trackingEntity = ambientContext.DatabaseContext.Comments.Remove(comment);
            ambientContext.DatabaseContext.OnSaveCompleteTasks.Add(
                () => _busClient.Publish(new CommentRemovedMessage
                {
                    CommentId = trackingEntity.Entity.CommentId,
                    UserId = ambientContext.CurrentUser.Id
                })
            );
        }

        public Task<int> CountAsync(AmbientContext ambientContext,
            CommentQuery commentQuery = null)
        {
            var query = BuildQuery(ambientContext.DatabaseContext.Comments, commentQuery);
            return query.CountAsync();
        }

        public async Task<bool> ExistsAsync(AmbientContext ambientContext,
            Guid commentId)
        {
            var comment = await ambientContext.DatabaseContext.Comments.FindAsync(commentId);
            if (comment != null) return await _appAuthorizationService.CanViewComment(ambientContext.Claims, comment);

            return false;
        }

        public async IAsyncEnumerable<Comment> GetAllAsync(AmbientContext ambientContext,
            CommentQuery query = null,
            string[] includeRelatedData = null)
        {
            IQueryable<Comment> enumerable = ambientContext.DatabaseContext.Comments;

            if (includeRelatedData != default)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var filteredComments = BuildQuery(enumerable, query);

            foreach (var comment in filteredComments)
                if (await _appAuthorizationService.CanViewComment(ambientContext.Claims, comment))
                    yield return comment;
        }

        public async Task<Comment> GetAsync(AmbientContext ambientContext,
            Guid id,
            string[] includeRelatedData = null)
        {
            IQueryable<Comment> enumerable = ambientContext.DatabaseContext.Comments;

            if (includeRelatedData != default)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            var comment = enumerable.SingleOrDefault(_ => _.CommentId == id);

            if (comment == null)
                throw new NotFoundEntityException();

            if (await _appAuthorizationService.CanViewComment(ambientContext.Claims, comment))
                return comment;
            throw new UnauthorizedOperationException();
        }

        private static IQueryable<Comment> BuildQuery(IQueryable<Comment> comments,
            CommentQuery query)
        {
            if (query == null)
                return comments;

            if (query.DocumentId != null)
                comments = comments.Where(_ => _.DocumentId == query.DocumentId).AsQueryable();

            if ((query.Page > 0) & (query.Limit > 0))
                comments = comments.Skip((query.Page - 1) * query.Limit).Take(query.Limit);

            return comments;
        }

        private bool IsValid(Comment comment,
            out List<ValidationResult> modelErrors)
        {
            var validationContext = new ValidationContext(comment);
            modelErrors = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(comment,
                validationContext,
                modelErrors);
            return isValid;
        }
    }
}