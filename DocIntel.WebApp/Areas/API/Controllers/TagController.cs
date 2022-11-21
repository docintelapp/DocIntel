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

using AutoMapper;

using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Utils.Search.Tags;
using DocIntel.WebApp.Areas.API.Models;
using DocIntel.WebApp.Helpers;
using DotLiquid;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using Tag = DocIntel.Core.Models.Tag;

namespace DocIntel.WebApp.Areas.API.Controllers;

[Area("API")]
[Route("API/Tag")]
[ApiController]
public class TagController : DocIntelAPIControllerBase
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ITagFacetRepository _facetRepository;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly ITagRepository _tagRepository;
    private readonly ITagSearchService _tagSearchEngine;

    public TagController(UserManager<AppUser> userManager,
        DocIntelContext context,
        ITagSearchService tagSearchEngine,
        ILogger<TagController> logger,
        ITagRepository tagRepository,
        IHttpContextAccessor accessor,
        ITagFacetRepository facetRepository,
        IMapper mapper)
        : base(userManager, context)
    {
        _logger = logger;
        _tagSearchEngine = tagSearchEngine;
        _tagRepository = tagRepository;
        _accessor = accessor;
        _facetRepository = facetRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Get tags
    /// </summary>
    /// <remarks>
    /// Get the tags
    ///
    /// For example, with cURL
    ///
    ///     curl --request GET \
    ///       --url http://localhost:5001/API/Tag \
    ///       --header 'Authorization: Bearer $TOKEN'
    ///
    /// </remarks>
    /// <returns>The facets</returns>
    /// <response code="200">Returns the facets</response>
    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<APITagDetails>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Produces("application/json")]
    public async Task<IActionResult> Index(
        [FromQuery] int page = 1)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            TagQuery query = new TagQuery()
            {
                Page = page
            };
            
            _logger.Log(LogLevel.Information,
                EventIDs.APIListTagSuccessful,
                new LogEvent($"User '{currentUser.UserName}' listed tags.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);
            
            return Ok(_mapper.Map<IEnumerable<APITagDetails>>(
                await _tagRepository.GetAllAsync(AmbientContext, query, new [] {"Facet"}).ToListAsync()
                ));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIListTagFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to list tag without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
    }
    
    /// <summary>
    /// Get the facet tags
    /// </summary>
    /// <remarks>
    /// Get the tags for a facet
    ///
    /// For example, with cURL
    ///
    ///     curl --request GET \
    ///       --url http://localhost:5001/API/Facet/1ee4eac9-6d56-4665-bb78-6986dd6bf7a2/Tags \
    ///       --header 'Authorization: Bearer $TOKEN'
    ///
    /// </remarks>
    /// <param name="facetId" example="1ee4eac9-6d56-4665-bb78-6986dd6bf7a2">The facet identifier</param>
    /// <returns>The tags</returns>
    /// <response code="200">Returns the tags</response>
    /// <response code="404">Facet does not exists</response>
    [HttpGet("/API/Facet/{facetId}/Tags")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<APITagDetails>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    [SwaggerOperation(Tags=new [] { "Tag", "Facet" })]
    public async Task<IActionResult> GetTags(
        [FromRoute] Guid facetId)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            TagQuery query = new TagQuery()
            {
                FacetId = facetId
            };
            
            _logger.Log(LogLevel.Information,
                EventIDs.APIListTagSuccessful,
                new LogEvent($"User '{currentUser.UserName}' listed tag on facet '{facetId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("facet.id", facetId),
                null,
                LogEvent.Formatter);
            
            return Ok(_mapper.Map<IEnumerable<APITagDetails>>(
                await _tagRepository.GetAllAsync(AmbientContext, query).ToListAsync()
            ));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIListTagFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to list tag on facet '{facetId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("facet.id", facetId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIListTagFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to list tag of a non-existing facet.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("facet.id", facetId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
    }

    [HttpGet("Search")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<APITagDetails>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Produces("application/json")]
    public async Task<IActionResult> Search(
        string searchTerm = "",
        int page = 1)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var results = _tagSearchEngine.Search(new TagSearchQuery
            {
                SearchTerms = searchTerm,
                Page = page
            });

            var tags = new List<object>();
            foreach (var h in results.Hits)
                try
                {
                    var tag = await _tagRepository.GetAsync(AmbientContext, h.Tag.TagId, new[] {"Facet"});
                    tags.Add(tag);
                }
                catch (NotFoundEntityException)
                {
                    // TODO Fail silently and log the error.
                }

            return Ok(_mapper.Map<IEnumerable<APITagDetails>>(tags));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIListTagFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to list tag without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
    }

    /// <summary>
    /// Get a tag
    /// </summary>
    /// <remarks>
    /// Gets the details of the tag.
    ///
    /// For example, with cURL
    ///
    ///     curl --request GET \
    ///       --url http://localhost:5001/API/Tag/640afad4-0a3d-416a-b6f0-22cb85e0d638 \
    ///       --header 'Authorization: Bearer $TOKEN'
    /// 
    /// </remarks>
    /// <param name="tagId" example="1ee4eac9-6d56-4665-bb78-6986dd6bf7a2">The tag identifier</param>
    /// <returns>The tag</returns>
    /// <response code="200">Returns the tag</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">Tag does not exists</response>
    [HttpGet("{tagId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APITagDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Details(Guid tagId)
    {
        var currentUser = await GetCurrentUser();
        try
        {
            var tag = await _tagRepository.GetAsync(AmbientContext, tagId, new[] {"Facet"});
            return Ok(_mapper.Map<APITagDetails>(tag));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIDetailsTagFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to view details of tag '{tagId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("tag.id", tagId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIDetailsTagFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to view details of a non-existing tag '{tagId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("tag.id", tagId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }

    /// <summary>
    /// Create a facet
    /// </summary>
    /// <remarks>
    /// Creates a new facet.
    ///
    /// For example, with cURL
    ///
    ///     curl --request POST \
    ///       --url http://localhost:5001/API/Facet/1ee4eac9-6d56-4665-bb78-6986dd6bf7a2/Tags \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///       --header 'Content-Type: application/json' \
    ///       --data '{
    ///       "label": "My tag",
    ///       "description": "<p>My tag description</p>",
    ///       "keywords": [],
    ///       "extractionKeywords": [],
    ///       "backgroundColor": "bg-primary-500"
    ///     }'
    /// 
    /// </remarks>
    /// <param name="facetId" example="1ee4eac9-6d56-4665-bb78-6986dd6bf7a2">The facet identifier</param>
    /// <param name="submittedTag">The tag</param>
    /// <returns>The newly created tag, as recorded</returns>
    /// <response code="200">Returns the newly created tag</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="400">Submitted value is invalid</response>
    [HttpPost("/API/Facet/{facetId}/Tags")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(APITagDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> Create([FromRoute] Guid facetId, [FromBody] APITag submittedTag)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            if (!await _facetRepository.ExistsAsync(AmbientContext, facetId))
                ModelState.AddModelError("FacetId", "Facet is not known");

            if (await _tagRepository.ExistsAsync(AmbientContext, facetId,
                    submittedTag.Label))
                ModelState.AddModelError(nameof(submittedTag.Label), "Label already exists.");

            if (ModelState.IsValid)
            {
                var tag = new Tag
                {
                    Label = submittedTag.Label,
                    Description = submittedTag.Description,
                    Keywords = string.Join(",", submittedTag.Keywords.Select(_ => _.Trim())),
                    BackgroundColor = submittedTag.BackgroundColor,
                    FacetId = facetId,
                    ExtractionKeywords = string.Join(",", submittedTag.ExtractionKeywords.Select(_ => _.Trim()))
                };

                tag = await _tagRepository.CreateAsync(AmbientContext, tag);

                await AmbientContext.DatabaseContext.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.APICreateTagSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully created a new tag '{tag.FriendlyName}' with id '{tag.TagId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddTag(tag),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<APITagDetails>(tag));
            }

            return BadRequest(ModelState);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APICreateTagFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to create a new tag '{submittedTag.Label}' on facet '{facetId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (InvalidArgumentException e)
        {
            ModelState.Clear();
            foreach (var kv in e.Errors)
            foreach (var errorMessage in kv.Value)
                ModelState.AddModelError(kv.Key, errorMessage);

            _logger.Log(LogLevel.Information,
                EventIDs.APICreateTagSuccessful,
                new LogEvent($"User '{currentUser.UserName}' attempted to create a new tag with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return BadRequest(ModelState);
        }
    }

    /// <summary>
    /// Update a tag
    /// </summary>
    /// <remarks>
    /// Updates a tag
    ///
    /// For example, with cURL
    ///
    ///     curl --request PATCH \
    ///       --url http://localhost:5001/API/Tag/640afad4-0a3d-416a-b6f0-22cb85e0d638 \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///       --header 'Content-Type: application/json' \
    ///       --data '{
    ///       "label": "My (updated) tag",
    ///       "description": "<p>My tag description</p>",
    ///       "keywords": [],
    ///       "extractionKeywords": [],
    ///       "backgroundColor": "bg-primary-500"
    ///     }'
    /// 
    /// </remarks>
    /// <param name="tagId" example="640afad4-0a3d-416a-b6f0-22cb85e0d638">The tag identifier</param>
    /// <param name="submittedTag">The updated tag</param>
    /// <returns>The tag facet</returns>
    /// <response code="200">Returns the newly updated tag</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">The tag does not exist</response>
    [HttpPatch("{tagId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(APITagDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Edit([FromRoute] Guid tagId, [FromBody] APITag submittedTag)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var tag = await _tagRepository.GetAsync(AmbientContext, tagId);

            if (submittedTag.FacetId != null)
            {
                if (!await _facetRepository.ExistsAsync(AmbientContext, (Guid)submittedTag.FacetId))
                    ModelState.AddModelError("FacetId", "Facet is not known");
            
                if (await _tagRepository.ExistsAsync(AmbientContext, (Guid)submittedTag.FacetId,
                        submittedTag.Label, tagId))
                    ModelState.AddModelError(nameof(submittedTag.Label), "Label already exists.");
            }

            if (ModelState.IsValid)
            {
                tag.Label = submittedTag.Label;
                tag.Description = submittedTag.Description;
                tag.Keywords = string.Join(",", submittedTag.Keywords.Select(_ => _.Trim()));
                tag.BackgroundColor = submittedTag.BackgroundColor;
                if (submittedTag.FacetId != null)
                    tag.FacetId = (Guid)submittedTag.FacetId;
                tag.ExtractionKeywords = string.Join(",", submittedTag.ExtractionKeywords.Select(_ => _.Trim()));
                    
                tag = await _tagRepository.UpdateAsync(AmbientContext, tag);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.APIEditTagSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully edit tag '{tag.FriendlyName}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddTag(tag),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<APITag>(tag));
            }

            throw new InvalidArgumentException(ModelState);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIEditTagFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit tag '{tagId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("tag.id", tagId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIEditTagFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit a non-existing tag '{tagId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("tag.id", tagId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
        catch (InvalidArgumentException e)
        {
            ModelState.Clear();
            foreach (var kv in e.Errors)
            foreach (var errorMessage in kv.Value)
                ModelState.AddModelError(kv.Key, errorMessage);

            _logger.Log(LogLevel.Information,
                EventIDs.APIEditTagFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit tag '{tagId}' with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("tag.id", tagId),
                null,
                LogEvent.Formatter);

            return BadRequest(ModelState);
        }
    }

    /// <summary>
    /// Merge two tags
    /// </summary>
    /// <remarks>
    /// Merges two tags.
    ///
    /// For example, with cURL
    ///
    ///     curl --request POST \
    ///       --url http://localhost:5001/API/Tag/a28a36ec-88a4-4dfc-a241-de602c530998/Merge/1902bfa6-2fd7-4a8a-bc22-33b3fbbdd660 \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///             
    /// </remarks>
    /// <param name="primaryId" example="a28a36ec-88a4-4dfc-a241-de602c530998">The identifier of the tag to keep</param>
    /// <param name="secondaryId" example="1902bfa6-2fd7-4a8a-bc22-33b3fbbdd660">The identifier of the tag to remove</param>
    /// <returns>The merged tag</returns>
    /// <response code="200">The tags are merged</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">A tag does not exist</response>
    [HttpPost("{primaryId}/Merge/{secondaryId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(APITagDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Merge(Guid primaryId, Guid secondaryId)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var primaryTag = await _tagRepository.GetAsync(AmbientContext, primaryId);
            if (primaryTag == null)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIMergeTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to merge a non-existing tag '{primaryId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddTag(primaryTag),
                    null,
                    LogEvent.Formatter);
                return NotFound();
            }

            var secondaryTag = await _tagRepository.GetAsync(AmbientContext, secondaryId);
            if (secondaryTag == null)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIMergeTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to merge a non-existing tag '{secondaryId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddTag(secondaryTag),
                    null,
                    LogEvent.Formatter);
                return NotFound();
            }

            if (primaryTag.TagId == secondaryTag.TagId) return Ok();

            if (ModelState.IsValid)
            {
                primaryTag = await _tagRepository.MergeAsync(AmbientContext, primaryTag.TagId, secondaryTag.TagId);
                await AmbientContext.DatabaseContext.SaveChangesAsync();
                
                _logger.Log(LogLevel.Information,
                    EventIDs.APIMergeTagSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully merged tag '{primaryTag.FriendlyName}' with '{secondaryTag.FriendlyName}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddTag(primaryTag, "tag_primary")
                        .AddTag(secondaryTag, "tag_secondary"),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<APITagDetails>(primaryTag));
            }

            throw new InvalidArgumentException();
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIMergeTagFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to merge tag '{primaryId}' with '{secondaryId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("tag_primary.id", primaryId)
                    .AddProperty("tag_secondary.id", secondaryId),
                null,
                LogEvent.Formatter);
            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIMergeTagFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to merge a non-existing tag.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("tag_primary.id", primaryId)
                    .AddProperty("tag_secondary.id", secondaryId),
                null,
                LogEvent.Formatter);
            return NotFound();
        }
        catch (InvalidArgumentException e)
        {
            ModelState.Clear();
            foreach (var kv in e.Errors)
            foreach (var errorMessage in kv.Value)
                ModelState.AddModelError(kv.Key, errorMessage);

            return BadRequest(ModelState);
        }
    }

    /// <summary>
    /// Deletes a tag
    /// </summary>
    /// <remarks>
    /// Deletes the specified tag,
    ///
    /// For example, with cURL
    ///
    ///     curl --request DELETE \
    ///       --url http://localhost:5001/API/Tag/6e7635a0-27bb-495d-a218-15b54cb938fd \
    ///       --header 'Authorization: Bearer $TOKEN'
    ///
    /// </remarks>
    /// <param name="tagId" example="6e7635a0-27bb-495d-a218-15b54cb938fd">The tag identifier</param>
    /// <response code="200">The tag is deleted</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">The tag does not exist</response>
    [HttpDelete("{tagId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Delete(Guid tagId)
    {
        var currentUser = await GetCurrentUser();
        try
        {
            await _tagRepository.RemoveAsync(AmbientContext, tagId);
            await _context.SaveChangesAsync();

            _logger.Log(LogLevel.Information,
                EventIDs.APIDeleteTagSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully deleted tag '{tagId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("tag.id", tagId),
                null,
                LogEvent.Formatter);

            return Ok();
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIDeleteTagFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to delete a new tag '{tagId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("tag.id", tagId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIDeleteTagFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to delete a non-existing tag '{tagId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("tag.id", tagId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }
    
    /// <summary>
    /// Get subscription status
    /// </summary>
    /// <remarks>
    /// Gets whether the user is subscribed to the tag.
    ///
    /// For example, with cURL
    ///
    ///     curl --request GET \
    ///       --url http://localhost:5001/API/Tag/4a9ef072-0cc5-41f8-b42a-a018ef4fb4b8/Subscription \
    ///       --header 'Authorization: Bearer $TOKEN'
    /// 
    /// </remarks>
    /// <param name="tagId" example="4a9ef072-0cc5-41f8-b42a-a018ef4fb4b8">The tag identifier</param>
    /// <returns>The subscription to the tag</returns>
    /// <response code="200">Return the user subscription to the tag</response>
    /// <response code="404">The facet does not exists.</response>
    /// <response code="401">Action is not authorized</response>
    [HttpGet("{tagId}/Subscription")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(ApiSubscriptionStatus))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Subscription([FromRoute] Guid tagId)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var result = await _tagRepository.IsSubscribedAsync(AmbientContext, AmbientContext.CurrentUser, tagId);

            _logger.Log(LogLevel.Information,
                EventIDs.APISubscribeSuccessful,
                new LogEvent($"User '{currentUser.UserName}' queried subscription to tag '{tagId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("facet.id", tagId),
                null,
                LogEvent.Formatter);

            return Ok(_mapper.Map<ApiSubscriptionStatus>(result));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APISubscribeTagFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to query subscription to tag '{tagId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("tag.id", tagId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APISubscribeTagFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to query subscription to a non-existing tag '{tagId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("tag.id", tagId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }

    /// <summary>
    /// Subscribe to a tag
    /// </summary>
    /// <remarks>
    /// Subscribe to the tag. The body indicates whether the user should receive a notification
    /// when the tag changes.
    ///
    /// For example, with cURL
    ///
    ///     curl --request PUT \
    ///       --url http://localhost:5001/API/Tag/a203c3dd-72e2-488f-a999-a4a8a7acd0a8/Subscribe \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///       --header 'Content-Type: application/json' \
    ///       --data false
    /// 
    /// </remarks>
    /// <param name="tagId" example="a203c3dd-72e2-488f-a999-a4a8a7acd0a8">The tag identifier</param>
    /// <param name="notification" example="false">Whether the user needs to be notified</param>
    /// <response code="200">User is subscribed to the tag</response>
    /// <response code="404">The tag does not exists.</response>
    /// <response code="401">Action is not authorized</response>
    [HttpPut("{tagId}/Subscribe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Subscribe(Guid tagId, bool notification = false)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            await _tagRepository.SubscribeAsync(AmbientContext, AmbientContext.CurrentUser, tagId, notification);
            await AmbientContext.DatabaseContext.SaveChangesAsync();
            
            _logger.Log(LogLevel.Information,
                EventIDs.APISubscribeTagSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully subscribed to tag '{tagId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("tag.id", tagId),
                null,
                LogEvent.Formatter);

            return Ok();
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APISubscribeTagFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to subscribe to tag '{tagId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("tag.id", tagId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APISubscribeTagFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to subscribe to a non-existing tag '{tagId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("tag.id", tagId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }

    /// <summary>
    /// Unsubscribe to a tag
    /// </summary>
    /// <remarks>
    /// Unsubscribe to the tag.
    ///
    /// For example, with cURL
    ///
    ///     curl --request PUT \
    ///       --url http://localhost:5001/API/Tag/a203c3dd-72e2-488f-a999-a4a8a7acd0a8/Unsubscribe \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///       --header 'Content-Type: application/json'
    /// 
    /// </remarks>
    /// <param name="tagId" example="a203c3dd-72e2-488f-a999-a4a8a7acd0a8">The tag identifier</param>
    /// <response code="200">User is not subscribed to the tag</response>
    /// <response code="404">The tag does not exists.</response>
    /// <response code="401">Action is not authorized</response>
    [HttpPut("{tagId}/Unsubscribe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Unsubscribe(Guid tagId)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            await _tagRepository.UnsubscribeAsync(AmbientContext, AmbientContext.CurrentUser, tagId);
            await AmbientContext.DatabaseContext.SaveChangesAsync();
            
            _logger.Log(LogLevel.Information,
                EventIDs.APIUnsubscribeTagSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully unsubscribed to tag '{tagId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("tag.id", tagId),
                null,
                LogEvent.Formatter);

            return Ok();
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIUnsubscribeTagFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to unsubscribe to tag '{tagId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("tag.id", tagId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIUnsubscribeTagFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to unsubscribe to a non-existing tag '{tagId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("tag.id", tagId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }
}