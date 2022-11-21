/* DocIntel
 * Copyright (C) 2018-2021 Belgian Defense, Antoine Cailliau, Kevin Menten
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
using DocIntel.Core.Utils.Search.Tags;
using DocIntel.WebApp.Areas.API.Models;
using DocIntel.WebApp.Helpers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace DocIntel.WebApp.Areas.API.Controllers;

/// <summary>
/// Facets in DocIntel are bags of tags. Facets are used to group coherent set of tags such as actors, geographical
/// regions, etc. Facets can be mandatory, forcing users to select at least one tag. Hidden facets are not displayed
/// in search results and are only displayed on detailed pages. Last, facets control the automated extraction of
/// tags during the pre-processing. Facets with a specified regular expression will create new tags with
/// matching expression, otherwise, when enabled, only tags with be matched when found.
///
/// ## Facets Attributes
///
/// | Attribute        | Description                                                                                                              |
/// |------------------|--------------------------------------------------------------------------------------------------------------------------|
/// | FacetId          | The identifier                                                                                                           |
/// | Title            | The title                                                                                                                |
/// | Prefix           | The facet prefix                                                                                                         |
/// | Description      | The description                                                                                                          |
/// | Mandatory        | Whether the facet is mandatory                                                                                           |
/// | Hidden           | Whether the facet is not shown in search results                                                                         |
/// | ExtractionRegex  | The regular expression used when extracting new tags                                                                     |
/// | AutoExtract      | Whether tags of the facet should be matched and extracted                                                                |
/// | TagNormalization | Normalization of the tags (Valid values are `""`, `"camelize"`, `"capitalize"`, `"downcase"`, `"handleize"`, `"upcase"`) |
/// | CreationDate     | The creation date                                                                                                        |
/// | ModificationDate | The last modification date                                                                                               |
///
/// ## Facets Relationships
/// 
/// | Relationship   | Description                             |
/// |----------------|-----------------------------------------|
/// | Tags           | The tags                                |
/// | RegisteredBy   | The user who registered the facet       |
/// | LastModifiedBy | The user who last modified the facet    |
/// 
/// </summary>
[Area("API")]
[Route("API/Facet")]
[ApiController]
public class FacetController : DocIntelAPIControllerBase
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ITagFacetRepository _facetRepository;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IFacetSearchService _facetSearchEngine;

    public FacetController(UserManager<AppUser> userManager,
        DocIntelContext context,
        ILogger<FacetController> logger,
        IHttpContextAccessor accessor,
        ITagFacetRepository facetRepository,
        IFacetSearchService facetSearchEngine,
        IMapper mapper)
        : base(userManager, context)
    {
        _logger = logger;
        _accessor = accessor;
        _facetRepository = facetRepository;
        _facetSearchEngine = facetSearchEngine;
        _mapper = mapper;
    }

    /// <summary>
    /// Get facets
    /// </summary>
    /// <remarks>
    /// Get the facets
    ///
    /// For example, with cURL
    ///
    ///     curl --request GET \
    ///       --url http://localhost:5001/API/Facet \
    ///       --header 'Authorization: Bearer $TOKEN'
    ///
    /// </remarks>
    /// <returns>The facets</returns>
    /// <response code="200">Returns the facets</response>
    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ApiFacetDetails>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Produces("application/json")]
    public async Task<IActionResult> Index()
    {
        return Ok(_mapper.Map<IEnumerable<ApiFacetDetails>>(await _facetRepository.GetAllAsync(AmbientContext).ToListAsync()));
    }
    
    /// <summary>
    /// Get a facet
    /// </summary>
    /// <remarks>
    /// Gets the details of the facet.
    ///
    /// For example, with cURL
    ///
    ///     curl --request GET \
    ///       --url http://localhost:5001/API/Facet/640afad4-0a3d-416a-b6f0-22cb85e0d638 \
    ///       --header 'Authorization: Bearer $TOKEN'
    /// 
    /// </remarks>
    /// <param name="facetId" example="1ee4eac9-6d56-4665-bb78-6986dd6bf7a2">The facet identifier</param>
    /// <returns>The facet</returns>
    /// <response code="200">Returns the facet</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">Facet does not exists</response>
    [HttpGet("{facetId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiFacetDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Details([FromRoute] Guid facetId)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            _logger.Log(LogLevel.Information,
                EventIDs.GetFacetSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully got facet '{facetId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);
            
            return Ok(_mapper.Map<ApiFacetDetails>(await _facetRepository.GetAsync(AmbientContext, facetId, 
                new [] { nameof(TagFacet.Tags),nameof(TagFacet.CreatedBy),nameof(TagFacet.LastModifiedBy) })));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.GetFacetFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to get facet '{facetId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.GetFacetFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to get a non-existing facet '{facetId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("facet.id", facetId),
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
    ///       --url http://localhost:5001/API/Facet/ \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///       --header 'Content-Type: application/json' \
    ///       --data '{
    ///       "title": "My facet",
    ///       "prefix": "myFacet",
    ///       "description": "<p>A simple facet</p>",
    ///       "mandatory": false,
    ///       "hidden": false,
    ///       "extractionRegex": "",
    ///       "autoExtract": true,
    ///       "tagNormalization": "upcase"
    ///     }'
    /// 
    /// </remarks>
    /// <param name="submittedFacet">The facet</param>
    /// <returns>The newly created facet, as recorded</returns>
    /// <response code="200">Returns the newly created facet</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="400">Submitted value is invalid</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(ApiFacetDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> Create([FromBody] ApiFacet submittedFacet)
    {
        var currentUser = await GetCurrentUser();
        try
        {
            if (ModelState.IsValid)
            {
                var facet = new TagFacet
                {
                    Title = submittedFacet.Title,
                    Prefix = submittedFacet.Prefix,
                    Description = submittedFacet.Description,
                    Mandatory = submittedFacet.Mandatory,
                    Hidden = submittedFacet.Hidden,
                    AutoExtract = submittedFacet.AutoExtract,
                    ExtractionRegex = submittedFacet.ExtractionRegex,
                    TagNormalization = submittedFacet.TagNormalization
                };
                facet = await _facetRepository.AddAsync(AmbientContext, facet);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.APICreateTagFacetSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully created a new facet.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddFacet(facet),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<ApiFacetDetails>(facet));
            }

            throw new InvalidArgumentException(ModelState);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APICreateTagFacetFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to create a facet without legitimate rights.")
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

            _logger.Log(LogLevel.Warning,
                EventIDs.APICreateTagFacetFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to create a facet with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return BadRequest(ModelState);
        }
    }

    /// <summary>
    /// Update a facet
    /// </summary>
    /// <remarks>
    /// Updates a facet
    ///
    /// For example, with cURL
    ///
    ///     curl --request PATCH \
    ///       --url http://localhost:5001/API/Facet/640afad4-0a3d-416a-b6f0-22cb85e0d638 \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///       --header 'Content-Type: application/json' \
    ///       --data '{
    ///       "title": "My (updated) facet",
    ///       "prefix": "myFacet",
    ///       "description": "<p>A simple facet</p>",
    ///       "mandatory": false,
    ///       "hidden": false,
    ///       "extractionRegex": "",
    ///       "autoExtract": true,
    ///       "tagNormalization": "upcase"
    ///     }'
    /// 
    /// </remarks>
    /// <param name="facetId" example="640afad4-0a3d-416a-b6f0-22cb85e0d638">The facet identifier</param>
    /// <param name="submittedFacet">The updated facet</param>
    /// <returns>The updated facet</returns>
    /// <response code="200">Returns the newly updated facet</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">The facet does not exist</response>
    [HttpPatch("{facetId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(ApiFacetDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> Update([FromRoute] Guid facetId, [FromBody] ApiFacet submittedFacet)
    {
        var currentUser = await GetCurrentUser();
        try
        {
            var facet = await _facetRepository.GetAsync(AmbientContext, facetId);

            if (ModelState.IsValid)
            {
                facet.Title = submittedFacet.Title;
                facet.Prefix = submittedFacet.Prefix;
                facet.Description = submittedFacet.Description;
                facet.Mandatory = submittedFacet.Mandatory;
                facet.Hidden = submittedFacet.Hidden;
                facet.TagNormalization = submittedFacet.TagNormalization;
                facet.AutoExtract = submittedFacet.AutoExtract;
                facet.ExtractionRegex = submittedFacet.ExtractionRegex;
                
                facet = await _facetRepository.UpdateAsync(AmbientContext, facet);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.APIEditTagFacetSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully edited facet '{facetId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddFacet(facet),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<ApiFacetDetails>(facet));
            }

            throw new InvalidArgumentException(ModelState);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIEditTagFacetFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit facet '{facetId}' without legitimate rights.")
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
                EventIDs.APIEditTagFacetFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit a non-existing facet '{facetId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("facet.id", facetId),
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

            _logger.Log(LogLevel.Warning,
                EventIDs.APIEditTagFacetFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit facet '{facetId}' with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("facet.id", facetId),
                null,
                LogEvent.Formatter);

            return BadRequest(ModelState);
        }
    }

    /// <summary>
    /// Deletes a facet
    /// </summary>
    /// <remarks>
    /// Deletes the specified facet and the enclosed tags,
    ///
    /// For example, with cURL
    ///
    ///     curl --request DELETE \
    ///       --url http://localhost:5001/API/Facet/6e7635a0-27bb-495d-a218-15b54cb938fd \
    ///       --header 'Authorization: Bearer $TOKEN'
    ///
    /// </remarks>
    /// <param name="facetId" example="6e7635a0-27bb-495d-a218-15b54cb938fd">The facet identifier</param>
    /// <response code="200">The facet is deleted</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">The facet does not exist</response>
    [HttpDelete("{facetId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Delete([FromRoute] Guid facetId)
    {
        var currentUser = await GetCurrentUser();
        try
        {
            var facet = await _facetRepository.RemoveAsync(AmbientContext, facetId);
            _logger.Log(LogLevel.Information,
                EventIDs.APIDeleteTagFacetSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully deleted '{facetId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddFacet(facet),
                null,
                LogEvent.Formatter);

            await _context.SaveChangesAsync();
            return Ok();
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIDeleteTagFacetFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to delete facet '{facetId}' without legitimate rights.")
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
                EventIDs.APIDeleteTagFacetFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to delete a non-existing facet '{facetId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("facet.id", facetId),
                null,
                LogEvent.Formatter);
            return NotFound();
        }
    }

    /// <summary>
    /// Merge two facets
    /// </summary>
    /// <remarks>
    /// Merges two facets.
    ///
    /// For example, with cURL
    ///
    ///     curl --request POST \
    ///       --url http://localhost:5001/API/Facet/a28a36ec-88a4-4dfc-a241-de602c530998/Merge/1902bfa6-2fd7-4a8a-bc22-33b3fbbdd660 \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///             
    /// </remarks>
    /// <param name="primaryId" example="a28a36ec-88a4-4dfc-a241-de602c530998">The identifier of the facet to keep</param>
    /// <param name="secondaryId" example="1902bfa6-2fd7-4a8a-bc22-33b3fbbdd660">The identifier of the facet to remove</param>
    /// <returns>The merged facet</returns>
    /// <response code="200">The facets are merged</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">A facet does not exist</response>
    [HttpPost("{primaryId}/Merge/{secondaryId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiFacetDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Merge(Guid primaryId, Guid secondaryId)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var primaryFacet = await _facetRepository.GetAsync(AmbientContext, primaryId);
            if (primaryFacet == null)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIMergeFacetFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to merge a non-existing facet '{primaryId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddFacet(primaryFacet),
                    null,
                    LogEvent.Formatter);
                return NotFound();
            }

            var secondaryFacet = await _facetRepository.GetAsync(AmbientContext, secondaryId);
            if (secondaryFacet == null)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIMergeFacetFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to merge a non-existing facet '{secondaryId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddFacet(secondaryFacet),
                    null,
                    LogEvent.Formatter);
                return NotFound();
            }

            if (primaryFacet.FacetId == secondaryFacet.FacetId)
            {
                _logger.Log(LogLevel.Information,
                    EventIDs.APIMergeFacetSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully merged facet '{primaryFacet.FacetId}' with '{secondaryFacet.FacetId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddFacet(primaryFacet, "facet_primary")
                        .AddFacet(secondaryFacet, "facet_secondary"),
                    null,
                    LogEvent.Formatter);
                return Ok(_mapper.Map<ApiFacetDetails>(primaryFacet));
            }

            if (!ModelState.IsValid) throw new InvalidArgumentException();
            await _facetRepository.MergeAsync(AmbientContext, primaryFacet.FacetId, secondaryFacet.FacetId);

            _logger.Log(LogLevel.Information,
                EventIDs.APIMergeFacetSuccessful,
                new LogEvent(
                        $"User '{currentUser.UserName}' successfully merged facet '{primaryFacet.FacetId}' with '{secondaryFacet.FacetId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddFacet(primaryFacet, "facet_primary")
                    .AddFacet(secondaryFacet, "facet_secondary"),
                null,
                LogEvent.Formatter);
            return Ok(_mapper.Map<ApiFacetDetails>(primaryFacet));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIMergeFacetFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to merge facet '{primaryId}' with '{secondaryId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("facet_primary.id", primaryId)
                    .AddProperty("facet_secondary.id", secondaryId),
                null,
                LogEvent.Formatter);
            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIMergeFacetFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to merge a non-existing facet.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("facet_primary.id", primaryId)
                    .AddProperty("facet_secondary.id", secondaryId),
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

            _logger.Log(LogLevel.Warning,
                EventIDs.APIMergeFacetFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to merge a facets with invalid arguments.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("facet_primary.id", primaryId)
                    .AddProperty("facet_secondary.id", secondaryId),
                null,
                LogEvent.Formatter);

            return BadRequest(ModelState);
        }
    }

    /// <summary>
    /// Get subscription status
    /// </summary>
    /// <remarks>
    /// Gets whether the user is subscribed to the facet.
    ///
    /// For example, with cURL
    ///
    ///     curl --request GET \
    ///       --url http://localhost:5001/API/Facet/4a9ef072-0cc5-41f8-b42a-a018ef4fb4b8/Subscription \
    ///       --header 'Authorization: Bearer $TOKEN'
    /// 
    /// </remarks>
    /// <param name="facetId" example="4a9ef072-0cc5-41f8-b42a-a018ef4fb4b8">The facet identifier</param>
    /// <returns>The subscription to the facet</returns>
    /// <response code="200">Return the user subscription to the facet</response>
    /// <response code="404">The facet does not exists.</response>
    /// <response code="401">Action is not authorized</response>
    [HttpGet("{facetId}/Subscription")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(ApiSubscriptionStatus))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Subscription([FromRoute] Guid facetId)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var result = await _facetRepository.IsSubscribedAsync(AmbientContext, AmbientContext.CurrentUser, facetId);

            _logger.Log(LogLevel.Information,
                EventIDs.APISubscribeSuccessful,
                new LogEvent($"User '{currentUser.UserName}' queried subscription to facet '{facetId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("facet.id", facetId),
                null,
                LogEvent.Formatter);

            return Ok(_mapper.Map<ApiSubscriptionStatus>(result));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APISubscribeFacetFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to query subscription to facet '{facetId}' without legitimate rights.")
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
                EventIDs.APISubscribeFacetFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to query subscription to a non-existing facet '{facetId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("facet.id", facetId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }

    /// <summary>
    /// Subscribe to a facet
    /// </summary>
    /// <remarks>
    /// Subscribe to the facet. The body indicates whether the user should receive a notification
    /// when the facet changes.
    ///
    /// For example, with cURL
    ///
    ///     curl --request PUT \
    ///       --url http://localhost:5001/API/Facet/a203c3dd-72e2-488f-a999-a4a8a7acd0a8/Subscribe \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///       --header 'Content-Type: application/json' \
    ///       --data false
    /// 
    /// </remarks>
    /// <param name="facetId" example="a203c3dd-72e2-488f-a999-a4a8a7acd0a8">The facet identifier</param>
    /// <param name="notification" example="false">Whether the user needs to be notified</param>
    /// <response code="200">User is subscribed to the facet</response>
    /// <response code="404">The facet does not exists.</response>
    /// <response code="401">Action is not authorized</response>
    [HttpPut("{facetId}/Subscribe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Subscribe([FromRoute] Guid facetId, [FromBody] bool notification = false)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            await _facetRepository.SubscribeAsync(AmbientContext, AmbientContext.CurrentUser, facetId,
                notification);
            await AmbientContext.DatabaseContext.SaveChangesAsync();
            
            _logger.Log(LogLevel.Information,
                EventIDs.APISubscribeSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully subscribed to tag '{facetId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("facet.id", facetId),
                null,
                LogEvent.Formatter);

            return Ok();
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APISubscribeFacetFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to subscribe to tag '{facetId}' without legitimate rights.")
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
                EventIDs.APISubscribeFacetFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to subscribe to a non-existing tag '{facetId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("facet.id", facetId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }

    
    /// <summary>
    /// Unsubscribe to a facet
    /// </summary>
    /// <remarks>
    /// Unsubscribe to the facet.
    ///
    /// For example, with cURL
    ///
    ///     curl --request PUT \
    ///       --url http://localhost:5001/API/Facet/a203c3dd-72e2-488f-a999-a4a8a7acd0a8/Unsubscribe \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///       --header 'Content-Type: application/json'
    /// 
    /// </remarks>
    /// <param name="facetId" example="a203c3dd-72e2-488f-a999-a4a8a7acd0a8">The facet identifier</param>
    /// <response code="200">User is not subscribed to the facet</response>
    /// <response code="404">The facet does not exists.</response>
    /// <response code="401">Action is not authorized</response>
    [HttpPut("{facetId}/Unsubscribe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Unsubscribe([FromRoute] Guid facetId)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            await _facetRepository.UnsubscribeAsync(AmbientContext, AmbientContext.CurrentUser, facetId);
            await AmbientContext.DatabaseContext.SaveChangesAsync();

            _logger.Log(LogLevel.Information,
                EventIDs.APISubscribeSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully unsubscribed to tag '{facetId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("facet.id", facetId),
                null,
                LogEvent.Formatter);

            return Ok();
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APISubscribeFacetFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to unsubscribe to tag '{facetId}' without legitimate rights.")
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
                EventIDs.APISubscribeFacetFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to unsubscribe to a non-existing tag '{facetId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("facet.id", facetId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }
}