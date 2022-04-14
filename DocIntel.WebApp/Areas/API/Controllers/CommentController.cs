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
using System.Threading.Tasks;

using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.WebApp.Areas.API.Models;
using DocIntel.WebApp.Helpers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DocIntel.WebApp.Areas.API.Controllers
{
    [Area("API")]
    [Route("API/Comment")]
    [ApiController]
    public class CommentController : DocIntelAPIControllerBase
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly ICommentRepository _commentRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<DocumentController> _logger;

        public CommentController(UserManager<AppUser> userManager,
            DocIntelContext context,
            ILogger<DocumentController> logger,
            IHttpContextAccessor accessor,
            ICommentRepository commentRepository,
            IDocumentRepository documentRepository)
            : base(userManager, context)
        {
            _logger = logger;
            _accessor = accessor;
            _commentRepository = commentRepository;
            _documentRepository = documentRepository;
        }

        [HttpPost("")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Comment))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Create([FromBody] string body, [FromQuery] Guid documentId)
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
                await _documentRepository.SubscribeAsync(AmbientContext, document.DocumentId);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.APICreateCommentSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully commented document '{document.Reference}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddDocument(document)
                        .AddComment(comment),
                    null,
                    LogEvent.Formatter);

                return Ok(comment);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APICreateCommentFailed,
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
                    EventIDs.APICreateCommentFailed,
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

        [HttpPatch("")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Comment))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Update([FromBody] APIComment submittedComment)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var comment = await _commentRepository.GetAsync(AmbientContext, submittedComment.CommentId);

                comment.Body = submittedComment.Body;

                await _commentRepository.UpdateAsync(AmbientContext, comment);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.APIUpdateCommentSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully updated comment '{comment.CommentId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddComment(comment),
                    null,
                    LogEvent.Formatter);

                return Ok(comment);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIUpdateCommentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to update comment '{submittedComment.CommentId}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", submittedComment.CommentId),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIUpdateCommentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to update a non-existing comment '{submittedComment.CommentId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", submittedComment.CommentId),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpDelete("")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Delete([FromQuery] Guid commentId)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var comment = await _commentRepository.GetAsync(AmbientContext, commentId);

                await _commentRepository.RemoveAsync(AmbientContext, commentId);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.APIDeleteCommentSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully deleted a comment.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddComment(comment),
                    null,
                    LogEvent.Formatter);

                return Ok();
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDeleteCommentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to delete comment without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("comment.id", commentId),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDeleteCommentFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to delete a non-existing comment.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("comment.id", commentId),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }
    }
}