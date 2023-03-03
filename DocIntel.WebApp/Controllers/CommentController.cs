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
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;
using DocIntel.WebApp.Helpers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DocIntel.WebApp.Controllers
{
    /// <summary>
    ///     Provides functionalities for commenting on documents.
    /// </summary>
    public class CommentController : BaseController
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly ICommentRepository _commentRepository;

        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger _logger;

        public CommentController(DocIntelContext context,
            ILogger<CommentController> logger,
            ApplicationSettings configuration,
            AppUserManager userManager,
            IAuthorizationService authorizationService,
            ICommentRepository commentRepository,
            IDocumentRepository documentRepository,
            IHttpContextAccessor accessor)
            : base(context,
                userManager,
                configuration,
                authorizationService)
        {
            _logger = logger;
            _documentRepository = documentRepository;
            _commentRepository = commentRepository;
            _accessor = accessor;
        }

        /// <summary>
        ///     Creates a comment with the specified body, for the specified
        ///     document and owned by the current user.
        /// </summary>
        /// <param name="documentId">
        ///     The identifier of the document to comment.
        /// </param>
        /// <param name="body">
        ///     The body of the comment.
        /// </param>
        /// <returns>
        ///     A redirection to the commented document if successful. A "Not Found"
        ///     response if the document does not exists. A "Unauthorized"
        ///     response if the current does not have the legitimate rights.
        /// </returns>
        [HttpPost("Comment/Create/{documentId}")]
        public async Task<IActionResult> Create(Guid documentId, string body)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var document = await _documentRepository.GetAsync(AmbientContext, documentId);
                var comment = new Comment
                {
                    AuthorId = currentUser.Id,
                    DocumentId = document.DocumentId,
                    Body = body
                };

                await _commentRepository.AddAsync(AmbientContext, comment);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.CreateCommentSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully commented document '{document.Reference}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddDocument(document)
                        .AddComment(comment),
                    null,
                    LogEvent.Formatter);

                return RedirectToAction(nameof(DocumentController.Details),
                    "Document",
                    new {url = document.URL});
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.CreateCommentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to comment document '{documentId}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", documentId),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.CreateCommentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to comment a non-existing document '{documentId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", documentId),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        /// <summary>
        ///     Deletes the specified comment.c
        /// </summary>
        /// <param name="id">
        ///     The identifier of the comment to delete.
        /// </param>
        /// <returns>
        ///     A redirection to the document that was containing the comment. A
        ///     "Not Found" response if the comment does not exists. A
        ///     "Unauthorized" if the user does not have the right to delete the
        ///     specified comment.
        /// </returns>
        [HttpGet("Comment/Delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var comment = await _commentRepository.GetAsync(AmbientContext, id, new[] {"Document"});

                var documentReference = comment.Document.Reference;
                await _commentRepository.RemoveAsync(AmbientContext, id);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.DeleteCommentSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully deleted a comment.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddComment(comment),
                    null,
                    LogEvent.Formatter);

                return RedirectToAction(nameof(DocumentController.Details),
                    "Document",
                    new {url = comment.Document.URL});
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DeleteCommentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to delete comment without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("comment.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DeleteCommentFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to delete a non-existing comment.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("comment.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpGet("Comment/Update/{id}")]
        public async Task<IActionResult> Update(Guid id)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var comment = await _commentRepository.GetAsync(AmbientContext, id,
                    new[] {"Document", "Document.Classification", "Document.EyesOnly", "Document.ReleasableTo"});

                _logger.Log(LogLevel.Information,
                    EventIDs.DeleteCommentSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully requested to edit a comment.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddComment(comment),
                    null,
                    LogEvent.Formatter);

                return View(comment);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DeleteCommentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to request to edit comment without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("comment.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DeleteCommentFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to request to edit a non-existing comment.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("comment.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpPost("Comment/Update/{id}")]
        public async Task<IActionResult> Update(Guid id, Comment submittedComment)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var comment = await _commentRepository.GetAsync(AmbientContext, id);
                comment.Body = submittedComment.Body;

                await _commentRepository.UpdateAsync(AmbientContext, comment);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.CreateCommentSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully updated comment '{comment.CommentId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddComment(comment),
                    null,
                    LogEvent.Formatter);

                return RedirectToAction(nameof(DocumentController.Details),
                    "Document",
                    new {url = comment.DocumentId});
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.CreateCommentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit comment '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("comment.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.CreateCommentFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to edit a non-existing comment '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("comment.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }
    }
}