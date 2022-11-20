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
using DocIntel.Core.Utils.Search.Sources;
using DocIntel.WebApp.Areas.API.Models;
using DocIntel.WebApp.Helpers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DocIntel.WebApp.Areas.API.Controllers;

[Area("API")]
[Route("API/Source")]
[ApiController]
public class SourceController : DocIntelAPIControllerBase
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;

    private readonly ISourceRepository _sourceRepository;

    private readonly ISourceSearchService _sourceSearchEngine;

    public SourceController(DocIntelContext context,
        ILogger<SourceController> logger,
        UserManager<AppUser> userManager,
        ISourceSearchService sourceSearchEngine,
        ISourceRepository sourceRepository,
        IHttpContextAccessor accessor,
        IMapper mapper)
        : base(userManager, context)
    {
        _logger = logger;
        _sourceSearchEngine = sourceSearchEngine;
        _sourceRepository = sourceRepository;
        _accessor = accessor;
        _mapper = mapper;
    }
    
    /// <summary>
    /// Get sources
    /// </summary>
    /// <remarks>
    /// Get the sources
    ///
    /// For example, with cURL
    ///
    ///     curl --request GET \
    ///       --url http://localhost:5001/API/Source \
    ///       --header 'Authorization: Bearer $TOKEN'
    ///
    /// </remarks>
    /// <returns>The sources</returns>
    /// <response code="200">Returns the sources</response>
    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ApiSourceDetails>))]
    [Produces("application/json")]
    public async Task<IActionResult> Index(
        int page = 1, int pageSize = 10)
    {
        var currentUser = await GetCurrentUser();

        var query = new SourceQuery
        {
            Page = page,
            Limit = pageSize
        };

        var searchResults = await _sourceRepository.GetAllAsync(AmbientContext, query).ToListAsync();

        _logger.Log(LogLevel.Information,
            EventIDs.APIListSourceSuccessful,
            new LogEvent($"User '{currentUser.UserName}' successfully listed sources.")
                .AddUser(currentUser)
                .AddHttpContext(_accessor.HttpContext),
            null,
            LogEvent.Formatter);

        return Ok(_mapper.Map<IEnumerable<ApiSourceDetails>>(searchResults));
    }

    [HttpGet("Search")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ApiSourceDetails>))]
    [Produces("application/json")]
    public async Task<IActionResult> Search(
        string searchTerm = "",
        SourceSortCriteria sortCriteria = SourceSortCriteria.Title,
        int page = 1, int pageSize = 10)
    {
        var currentUser = await GetCurrentUser();

        var query = new SourceSearchQuery
        {
            SearchTerms = searchTerm,
            SortCriteria = sortCriteria,
            Page = page,
            PageSize = pageSize
        };

        var searchResults = _sourceSearchEngine.Search(query);

        var resultsSource = new List<Source>();
        foreach (var s in searchResults.Hits.OrderBy(_ => _.Position))
            try
            {
                resultsSource.Add(await _sourceRepository.GetAsync(AmbientContext, s.Source.SourceId));
            }
            catch (NotFoundEntityException e)
            {
                _logger.Log(LogLevel.Error,
                    EventIDs.APIListSourceFailed,
                    new LogEvent($"Source '{s.Source.SourceId}' was found in the index but not in database.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddException(e),
                    null,
                    LogEvent.Formatter);
            }

        _logger.Log(LogLevel.Information,
            EventIDs.APIListSourceSuccessful,
            new LogEvent($"User '{currentUser.UserName}' successfully listed sources.")
                .AddUser(currentUser)
                .AddHttpContext(_accessor.HttpContext),
            null,
            LogEvent.Formatter);

        return Ok(_mapper.Map<IEnumerable<ApiSourceDetails>>(resultsSource));
    }
        
    [HttpGet("Suggest")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ApiSourceDetails>))]
    [Produces("application/json")]
    public async Task<IActionResult> Suggest(string searchTerm = "")
    {
        var currentUser = await GetCurrentUser();

        var query = new SourceSearchQuery
        {
            SearchTerms = searchTerm
        };

        var searchResults = _sourceSearchEngine.Suggest(query);

        var resultsSource = new List<Source>();
        foreach (var s in searchResults.Hits.OrderBy(_ => _.Position))
            try
            {
                resultsSource.Add(await _sourceRepository.GetAsync(AmbientContext, s.Source.SourceId));
            }
            catch (NotFoundEntityException e)
            {
                _logger.Log(LogLevel.Error,
                    EventIDs.APIListSourceFailed,
                    new LogEvent($"Source '{s.Source.SourceId}' was found in the index but not in database.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddException(e),
                    null,
                    LogEvent.Formatter);
            }

        _logger.Log(LogLevel.Information,
            EventIDs.APIListSourceSuccessful,
            new LogEvent($"User '{currentUser.UserName}' successfully listed sources.")
                .AddUser(currentUser)
                .AddHttpContext(_accessor.HttpContext),
            null,
            LogEvent.Formatter);

        return Ok(_mapper.Map<IEnumerable<ApiSourceDetails>>(resultsSource));
    }

    /// <summary>
    /// Get a source
    /// </summary>
    /// <remarks>
    /// Gets the details of the source.
    ///
    /// For example, with cURL
    ///
    ///     curl --request GET \
    ///       --url http://localhost:5001/API/Source/640afad4-0a3d-416a-b6f0-22cb85e0d638 \
    ///       --header 'Authorization: Bearer $TOKEN'
    /// 
    /// </remarks>
    /// <param name="sourceId" example="1ee4eac9-6d56-4665-bb78-6986dd6bf7a2">The source identifier</param>
    /// <returns>The source</returns>
    /// <response code="200">Returns the source</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">Source does not exists</response>
    [HttpGet("{sourceId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiSourceDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Details([FromRoute] Guid sourceId)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var source = await _sourceRepository.GetAsync(AmbientContext, sourceId);
            return Ok(_mapper.Map<ApiSourceDetails>(source));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIDetailsSourceFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to view details of source '{sourceId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("source.id", sourceId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIDetailsSourceFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to view details of a non-existing source '{sourceId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("source.id", sourceId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }

    /// <summary>
    /// Create a source
    /// </summary>
    /// <remarks>
    /// Creates a new source.
    ///
    /// For example, with cURL
    ///
    ///     curl --request POST \
    ///       --url http://localhost:5001/API/Source/ \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///       --header 'Content-Type: application/json' \
    ///       --data '{
    ///       "title": "My source",
    ///       "prefix": "mySource",
    ///       "description": "<p>A simple source</p>",
    ///       "mandatory": false,
    ///       "hidden": false,
    ///       "extractionRegex": "",
    ///       "autoExtract": true,
    ///       "tagNormalization": "upcase"
    ///     }'
    /// 
    /// </remarks>
    /// <param name="submittedSource">The source</param>
    /// <returns>The newly created source, as recorded</returns>
    /// <response code="200">Returns the newly created source</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="400">Submitted value is invalid</response>
    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiSource))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> Create([FromBody] ApiSource submittedSource)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var source = new Source
            {
                Title = submittedSource.Title,
                Description = submittedSource.Description,
                HomePage = submittedSource.HomePage,
                RSSFeed = submittedSource.RSSFeed,
                Facebook = submittedSource.Facebook,
                Twitter = submittedSource.Twitter,
                Reddit = submittedSource.Reddit,
                LinkedIn = submittedSource.LinkedIn,
                Reliability = submittedSource.Reliability,
                Country = submittedSource.Country
            };

            if (submittedSource.Keywords?.Any() ?? false)
                source.Keywords = string.Join(", ", submittedSource.Keywords.Select(_ => _.Trim()));

            source = await _sourceRepository.CreateAsync(AmbientContext, source);
            await AmbientContext.DatabaseContext.SaveChangesAsync();

            _logger.Log(LogLevel.Information,
                EventIDs.APICreateSourceSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully created a new source.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddSource(source),
                null,
                LogEvent.Formatter);

            return Ok(_mapper.Map<ApiSourceDetails>(source));
        }
        catch (InvalidArgumentException e)
        {
            _logger.Log(LogLevel.Information,
                EventIDs.APICreateSourceFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to create a new source with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return BadRequest(e.Errors);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APICreateSourceFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to create a new source without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
    }

    /// <summary>
    /// Update a source
    /// </summary>
    /// <remarks>
    /// Updates a source
    ///
    /// For example, with cURL
    ///
    ///     curl --request PATCH \
    ///       --url http://localhost:5001/API/Source/640afad4-0a3d-416a-b6f0-22cb85e0d638 \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///       --header 'Content-Type: application/json' \
    ///       --data '{
    ///       "title": "My (updated) source",
    ///       "prefix": "mySource",
    ///       "description": "<p>A simple source</p>",
    ///       "mandatory": false,
    ///       "hidden": false,
    ///       "extractionRegex": "",
    ///       "autoExtract": true,
    ///       "tagNormalization": "upcase"
    ///     }'
    /// 
    /// </remarks>
    /// <param name="sourceId" example="640afad4-0a3d-416a-b6f0-22cb85e0d638">The source identifier</param>
    /// <param name="submittedSource">The updated source</param>
    /// <returns>The updated source</returns>
    /// <response code="200">Returns the newly updated source</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">The source does not exist</response>
    [HttpPatch("{sourceId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiSource))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> Edit([FromRoute] Guid sourceId, [FromBody] ApiSource submittedSource)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var source = await _sourceRepository.GetAsync(AmbientContext, sourceId);

            source.Title = submittedSource.Title;
            source.Description = submittedSource.Description;

            source.Reliability = submittedSource.Reliability;

            source.HomePage = submittedSource.HomePage;
            source.RSSFeed = submittedSource.RSSFeed;
            source.Facebook = submittedSource.Facebook;
            source.Twitter = submittedSource.Twitter;
            source.Reddit = submittedSource.Reddit;
            source.LinkedIn = submittedSource.LinkedIn;
            source.Country = submittedSource.Country;

            if (submittedSource.Keywords != null && submittedSource.Keywords.Any())
                source.Keywords = string.Join(", ", submittedSource.Keywords.Select(_ => _.Trim()));

            source = await _sourceRepository.UpdateAsync(AmbientContext, source);
            await AmbientContext.DatabaseContext.SaveChangesAsync();

            _logger.Log(LogLevel.Information,
                EventIDs.APIEditSourceSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully edited source '{source.Title}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddSource(source),
                null,
                LogEvent.Formatter);

            return Ok(_mapper.Map<ApiSourceDetails>(source));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIEditSourceFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to create a new source without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("source.id", sourceId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIEditSourceFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit a non-existing source '{sourceId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("source.id", sourceId),
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
                EventIDs.APIEditSourceFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit source '{sourceId}' with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("source.id", sourceId),
                null,
                LogEvent.Formatter);

            return BadRequest(submittedSource);
        }
    }

    /// <summary>
    /// Deletes a source
    /// </summary>
    /// <remarks>
    /// Deletes the specified source and the enclosed tags,
    ///
    /// For example, with cURL
    ///
    ///     curl --request DELETE \
    ///       --url http://localhost:5001/API/Source/6e7635a0-27bb-495d-a218-15b54cb938fd \
    ///       --header 'Authorization: Bearer $TOKEN'
    ///
    /// </remarks>
    /// <param name="sourceId" example="6e7635a0-27bb-495d-a218-15b54cb938fd">The source identifier</param>
    /// <response code="200">The source is deleted</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">The source does not exist</response>
    [HttpDelete("{sourceId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Delete(Guid sourceId)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            await _sourceRepository.RemoveAsync(AmbientContext, sourceId);

            _logger.Log(LogLevel.Information,
                EventIDs.APIDeleteSourceSuccessful,
                new LogEvent($"User '{currentUser.UserName}' deleted source '{sourceId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("source.id", sourceId),
                null,
                LogEvent.Formatter);

            return Ok();
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIDeleteSourceFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to delete source '{sourceId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("source.id", sourceId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIDeleteSourceFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to edit a non-existing source '{sourceId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("source.id", sourceId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }

    /// <summary>
    /// Merge two sources
    /// </summary>
    /// <remarks>
    /// Merges two sources.
    ///
    /// For example, with cURL
    ///
    ///     curl --request POST \
    ///       --url http://localhost:5001/API/Source/a28a36ec-88a4-4dfc-a241-de602c530998/Merge/1902bfa6-2fd7-4a8a-bc22-33b3fbbdd660 \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///             
    /// </remarks>
    /// <param name="primaryId" example="a28a36ec-88a4-4dfc-a241-de602c530998">The identifier of the source to keep</param>
    /// <param name="secondaryId" example="1902bfa6-2fd7-4a8a-bc22-33b3fbbdd660">The identifier of the source to remove</param>
    /// <returns>The merged source</returns>
    /// <response code="200">The sources are merged</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">A source does not exist</response>
    [HttpPost("{primaryId}/Merge/{secondaryId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Merge([FromRoute] Guid primaryId, [FromRoute] Guid secondaryId)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            if (primaryId == secondaryId)
                return Ok();

            await _sourceRepository.MergeAsync(AmbientContext, primaryId, secondaryId);
            await AmbientContext.DatabaseContext.SaveChangesAsync();

            _logger.Log(LogLevel.Information,
                EventIDs.APIMergeSourceSuccessful,
                new LogEvent(
                        $"User '{currentUser.UserName}' successfully merged source '{primaryId}' with '{secondaryId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("source_primary.id", primaryId)
                    .AddProperty("source_secondary.id", secondaryId),
                null,
                LogEvent.Formatter);

            return Ok();
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIMergeSourceFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to merge source '{primaryId}' with '{secondaryId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("source_primary.id", primaryId)
                    .AddProperty("source_secondary.id", secondaryId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APIMergeSourceFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to merge a non-existing source '{primaryId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }

    /// <summary>
    /// Get subscription status
    /// </summary>
    /// <remarks>
    /// Gets whether the user is subscribed to the source.
    ///
    /// For example, with cURL
    ///
    ///     curl --request GET \
    ///       --url http://localhost:5001/API/Source/4a9ef072-0cc5-41f8-b42a-a018ef4fb4b8/Subscription \
    ///       --header 'Authorization: Bearer $TOKEN'
    /// 
    /// </remarks>
    /// <param name="sourceId" example="4a9ef072-0cc5-41f8-b42a-a018ef4fb4b8">The source identifier</param>
    /// <returns>The subscription to the source</returns>
    /// <response code="200">Return the user subscription to the source</response>
    /// <response code="404">The source does not exists.</response>
    /// <response code="401">Action is not authorized</response>
    [HttpGet("{sourceId}/Subscription")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(ApiSubscriptionStatus))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Subscription([FromRoute] Guid sourceId)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var result = await _sourceRepository.IsSubscribedAsync(AmbientContext, AmbientContext.CurrentUser, sourceId);

            _logger.Log(LogLevel.Information,
                EventIDs.APISubscribeSuccessful,
                new LogEvent($"User '{currentUser.UserName}' queried subscription to source '{sourceId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("source.id", sourceId),
                null,
                LogEvent.Formatter);

            return Ok(_mapper.Map<ApiSubscriptionStatus>(result));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APISubscribeTagFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to query subscription to source '{sourceId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("source.id", sourceId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APISubscribeTagFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to query subscription to a non-existing source '{sourceId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("source.id", sourceId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }

    /// <summary>
    /// Subscribe to a source
    /// </summary>
    /// <remarks>
    /// Subscribe to the source. The body indicates whether the user should receive a notification
    /// when the source changes.
    ///
    /// For example, with cURL
    ///
    ///     curl --request PUT \
    ///       --url http://localhost:5001/API/Source/a203c3dd-72e2-488f-a999-a4a8a7acd0a8/Subscribe \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///       --header 'Content-Type: application/json' \
    ///       --data false
    /// 
    /// </remarks>
    /// <param name="sourceId" example="a203c3dd-72e2-488f-a999-a4a8a7acd0a8">The source identifier</param>
    /// <param name="notification" example="false">Whether the user needs to be notified</param>
    /// <response code="200">User is subscribed to the source</response>
    /// <response code="404">The source does not exists.</response>
    /// <response code="401">Action is not authorized</response>
    [HttpPut("{sourceId}/Subscribe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Subscribe([FromRoute] Guid sourceId, [FromBody] bool notification = false)
    {
        var currentUser = await GetCurrentUser();
        try
        {
            await _sourceRepository.SubscribeAsync(AmbientContext, AmbientContext.CurrentUser, sourceId, notification);
            await AmbientContext.DatabaseContext.SaveChangesAsync();
            
            _logger.Log(LogLevel.Information,
                EventIDs.APISubscribeSourceSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully subscribed to source '{sourceId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("source.id", sourceId),
                null,
                LogEvent.Formatter);

            return Ok();
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APISubscribeSourceFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to subscribe to source '{sourceId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("source.id", sourceId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APISubscribeSourceFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to subscribe to a non-existing source '{sourceId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("source.id", sourceId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }

    /// <summary>
    /// Unsubscribe to a source
    /// </summary>
    /// <remarks>
    /// Unsubscribe to the source.
    ///
    /// For example, with cURL
    ///
    ///     curl --request PUT \
    ///       --url http://localhost:5001/API/Source/a203c3dd-72e2-488f-a999-a4a8a7acd0a8/Unsubscribe \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///       --header 'Content-Type: application/json'
    /// 
    /// </remarks>
    /// <param name="sourceId" example="a203c3dd-72e2-488f-a999-a4a8a7acd0a8">The source identifier</param>
    /// <response code="200">User is not subscribed to the source</response>
    /// <response code="404">The source does not exists.</response>
    /// <response code="401">Action is not authorized</response>
    [HttpPut("{sourceId}/Unsubscribe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Unsubscribe(Guid id)
    {
        var currentUser = await GetCurrentUser();
        try
        {
            await _sourceRepository.UnsubscribeAsync(AmbientContext, AmbientContext.CurrentUser, id);
            await AmbientContext.DatabaseContext.SaveChangesAsync();
            
            _logger.Log(LogLevel.Information,
                EventIDs.APISubscribeSourceSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully unsubscribed to source '{id}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("source.id", id),
                null,
                LogEvent.Formatter);

            return Ok();
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APISubscribeSourceFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to unsubscribe to source '{id}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("source.id", id),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APISubscribeSourceFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to unsubscribe to a non-existing source '{id}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("source.id", id),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }
}