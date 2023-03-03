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
using AutoMapper;
using Bogus;
using DocIntel.Core.Authentication;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.WebApp.Areas.API.Models;
using DocIntel.WebApp.Helpers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace DocIntel.WebApp.Areas.API.Controllers;

/// <summary>
/// A comment, in the context of DocIntel, is a piece of text expressing an opinion or reaction about a document.
/// Comments are commonly used by analysts to share their thoughts or actions about the shared reports and information.
/// Comments are always authored by a user.
///
/// ## Comment Attributes
///
/// * `CommentId`: The identifier of the comment
/// * `Body`: The body, in HTML, of the comment. HTML is sanitized at input and output. 
/// * `CommentDate`: The date of comment
/// * `ModificationDate`: The date of the last modification
/// 
/// ## Comment Relationships
/// * `Author`: The original author
/// * `LastModifiedBy`: The last modifier
/// 
/// </summary>
[Area("API")]
[Route("API/Comment")]
[ApiController]
public class CommentController : DocIntelAPIControllerBase
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ICommentRepository _commentRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<CommentController> _logger;
    private readonly IMapper _mapper;

    public CommentController(AppUserManager userManager,
        DocIntelContext context,
        ILogger<CommentController> logger,
        IHttpContextAccessor accessor,
        ICommentRepository commentRepository,
        IDocumentRepository documentRepository, 
        IMapper mapper)
        : base(userManager, context)
    {
        _logger = logger;
        _accessor = accessor;
        _commentRepository = commentRepository;
        _documentRepository = documentRepository;
        _mapper = mapper;
    }
    
    /// <summary>
    /// Get the comments
    /// </summary>
    /// <remarks>
    /// Gets all the comments about a document.
    ///
    /// For example, with cURL
    ///
    ///     curl --request GET \
    ///       --url http://localhost:5001/API/Comment/08a7474f-1912-4617-9ec4-b0bae39ed84a \
    ///       --header 'Authorization: Bearer $TOKEN' \
    /// 
    /// </remarks>
    /// <param name="documentId" example="5f9deb24-2b01-44c0-bb51-971b05a0667f">The document identifier</param>
    /// <returns>The comments</returns>
    /// <response code="200">Returns the comments about the document</response>
    /// <response code="404">The document does not exists.</response>
    /// <response code="401">Action is not authorized</response>
    [HttpGet("{documentId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ApiCommentDetails>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "GetAll"
    )]
    public async Task<IActionResult> Index([FromRoute] Guid documentId)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var comments = _commentRepository.GetAllAsync(AmbientContext, new CommentQuery()
            {
                DocumentId = documentId
            }, new[] { nameof(Comment.Author), nameof(Comment.LastModifiedBy) });

            _logger.Log(LogLevel.Information,
                EventIDs.APICreateCommentSuccessful,
                new LogEvent(
                        $"User '{currentUser.UserName}' successfully retrieved comments for document '{documentId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("document.id", documentId),
                null,
                LogEvent.Formatter);

            return Ok(_mapper.Map<IEnumerable<ApiCommentDetails>>(await comments.ToListAsync()));
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

    
    /// <summary>
    /// Post a comment
    /// </summary>
    /// <remarks>
    /// Post a new comment about a document. 
    ///
    /// For example, with cURL
    ///
    ///     curl --request POST \
    ///       --url http://localhost:5001/API/Comment/08a7474f-1912-4617-9ec4-b0bae39ed84a \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///       --header 'Content-Type: application/json' \
    ///       --data '{
    ///     	"body": "<p>This is my comment</p>"
    ///     }'
    /// 
    /// </remarks>
    /// <param name="documentId" example="5f9deb24-2b01-44c0-bb51-971b05a0667f">The document identifier</param>
    /// <param name="submittedComment">The comment to post</param>
    /// <returns>The posted comment, as recorded.</returns>
    /// <response code="200">Returns the newly posted comment</response>
    /// <response code="404">The document does not exists.</response>
    /// <response code="401">Action is not authorized</response>
    [HttpPost("{documentId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiCommentDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "Post"
    )]
    public async Task<IActionResult> Create([FromRoute] Guid documentId, [FromBody] ApiComment submittedComment)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var document = await _documentRepository.GetAsync(AmbientContext, documentId);
            var comment = new Comment
            {
                AuthorId = currentUser.Id,
                DocumentId = document.DocumentId,
                Body = submittedComment.Body
            };

            comment = await _commentRepository.AddAsync(AmbientContext, comment);
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

            return Ok(_mapper.Map<ApiCommentDetails>(comment));
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

    /// <summary>
    /// Update a comment
    /// </summary>
    /// <remarks>
    /// Update an existing comment, 
    ///
    /// For example, with cURL
    ///
    ///     curl --request PATCH \
    ///       --url http://localhost:5001/API/Comment/f3ff2759-9808-4218-8647-2cf48016f67c \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///       --header 'Content-Type: application/json' \
    ///       --data '{
    ///     	"body": "<p>This is my updated comment</p>"
    ///     }'
    /// 
    /// </remarks>
    /// <param name="commentId" example="932ba2c1-9d7d-4788-8f12-e55892828c2e">The comment identifier</param>
    /// <param name="submittedComment">The updated comment</param>
    /// <returns>The update comment, as recorded.</returns>
    /// <response code="200">Returns the newly updated comment</response>
    /// <response code="404">The comment does not exists.</response>
    /// <response code="401">Action is not authorized</response>
    [SwaggerOperation(
        OperationId = "Update"
    )]
    [HttpPatch("{commentId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiCommentDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Update([FromRoute] Guid commentId, [FromBody] ApiComment submittedComment)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var comment = await _commentRepository.GetAsync(AmbientContext, commentId);

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

            return Ok(_mapper.Map<ApiCommentDetails>(comment));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIUpdateCommentFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to update comment '{commentId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("document.id", commentId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIUpdateCommentFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to update a non-existing comment '{commentId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("document.id", commentId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }

    /// <summary>
    /// Delete a comment
    /// </summary>
    /// <remarks>
    /// Delete an existing comment, 
    ///
    /// For example, with cURL
    ///
    ///     curl --request DELETE \
    ///       --url http://localhost:5001/API/Comment/f3ff2759-9808-4218-8647-2cf48016f67c \
    ///       --header 'Authorization: Bearer $TOKEN'
    /// 
    /// </remarks>
    /// <param name="commentId" example="84777684-8b85-4e11-a92e-b553b84ae26c">The comment identifier</param>
    /// <response code="200">Returns the newly posted comment</response>
    /// <response code="404">The document does not exists.</response>
    /// <response code="401">Action is not authorized</response>
    [HttpDelete("{commentId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "Delete"
    )]
    public async Task<IActionResult> Delete([FromRoute] Guid commentId)
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
    
    public class ApiCommentExample : IExamplesProvider<ApiComment>
    {
        public ApiComment GetExamples()
        {
            var faker = new Faker();
            return new ApiComment()
            {
                Body = "<p>" + faker.Lorem.Paragraphs(separator:"</p><p>") + "</p>"
            };
        }
    }
    
    public class ApiCommentDetailsAbstractExample
    {
        protected Faker Faker;
        protected ApiCommentDetails[] Data;

        protected ApiCommentDetailsAbstractExample()
        {
            Faker = new Faker();
            var userExample = new ApiUserExample();
            var commentDate = Faker.Date.Recent(3);
            Data = new[]
            {
                new ApiCommentDetails()
                {
                    Body = "<p>" + Faker.Lorem.Paragraphs(separator:"</p><p>") + "</p>",
                    Author = userExample.GetExamples(),
                    CommentDate = commentDate,
                    CommentId = Faker.Random.Guid(),
                    ModificationDate = commentDate
                },
                new ApiCommentDetails()
                {
                    Body = "<p>" + Faker.Lorem.Paragraphs(separator:"</p><p>") + "</p>",
                    Author = userExample.GetExamples(),
                    CommentDate = commentDate,
                    CommentId = Faker.Random.Guid(),
                    ModificationDate = commentDate
                },
                new ApiCommentDetails()
                {
                    Body = "<p>" + Faker.Lorem.Paragraphs(separator:"</p><p>") + "</p>",
                    Author = userExample.GetExamples(),
                    CommentDate = commentDate,
                    CommentId = Faker.Random.Guid(),
                    ModificationDate = commentDate
                }
            };
        }
    }

    public class ApiCommentDetailsExample : ApiCommentDetailsAbstractExample, IExamplesProvider<ApiCommentDetails>
    {
        public ApiCommentDetails GetExamples()
        {
            return Faker.PickRandom(Data);
        }
    }
    
    public class ApiCommentDetailsExamples : ApiCommentDetailsAbstractExample, IExamplesProvider<IEnumerable<ApiCommentDetails>>
    {
        public IEnumerable<ApiCommentDetails> GetExamples()
        {
            return Faker.PickRandom(Data, 3);
        }
    }
    
    public class ApiUserExample : IExamplesProvider<APIAppUser>
    {
        public APIAppUser GetExamples()
        {
            var faker = new Faker();
            var person = faker.Person;
            return new APIAppUser()
            {
                UserId = faker.Random.Guid().ToString(),
                UserName = person.UserName,
                FirstName = person.FirstName,
                LastName = person.LastName,
                Email = person.Email,
                Enabled = true,
                Function = faker.Name.JobTitle(),
                LastActivity = faker.Date.Recent(),
                RegistrationDate = faker.Date.Recent(15)
            };
        }
    }
}