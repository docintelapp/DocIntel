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
using DocIntel.Core.Authentication;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Utils.Search.Sources;
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
[Route("API/Suggest")]
[ApiController]
public class SuggestController : DocIntelAPIControllerBase
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    
    private readonly ITagRepository _tagRepository;
    private readonly ISourceRepository _sourceRepository;
    private readonly ITagFacetRepository _facetRepository;
    
    private readonly ITagSearchService _tagSearchEngine;
    private readonly IFacetSearchService _facetSearchEngine;
    private readonly ISourceSearchService _sourceSearchEngine;

    public SuggestController(UserManager<AppUser> userManager,
        DocIntelContext context,
        ITagSearchService tagSearchEngine,
        ILogger<SuggestController> logger,
        ITagRepository tagRepository,
        IHttpContextAccessor accessor,
        ITagFacetRepository facetRepository,
        IMapper mapper, IFacetSearchService facetSearchEngine, ISourceRepository sourceRepository, ISourceSearchService sourceSearchEngine)
        : base(userManager, context)
    {
        _logger = logger;
        _tagSearchEngine = tagSearchEngine;
        _tagRepository = tagRepository;
        _accessor = accessor;
        _facetRepository = facetRepository;
        _mapper = mapper;
        _facetSearchEngine = facetSearchEngine;
        _sourceRepository = sourceRepository;
        _sourceSearchEngine = sourceSearchEngine;
    }

    /// <summary>
    /// Suggest tags
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <param name="facetPrefix">The prefix of the facet to search in</param>
    /// <returns>The tags matching the search and the facet if specified</returns>
    /// <response code="200">Returns the tags</response>
    /// <response code="401">Action is not authorized</response>
    [HttpGet("Tag")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ApiSourceDetails>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Produces("application/json")]
    public async Task<IActionResult> SuggestTag([FromQuery] string searchTerm = "", [FromQuery] string facetPrefix = "")
    {
        var currentUser = await GetCurrentUser();
        _logger.LogDebug("SuggestTag:" + facetPrefix);
        try
        {
            var results = _tagSearchEngine.Suggest(new TagSearchQuery
            {
                FacetPrefix = facetPrefix,
                SearchTerms = searchTerm
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

            _logger.Log(LogLevel.Information,
                EventIDs.APISearchTagSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully searched for tags.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return Ok(_mapper.Map<IEnumerable<APITagDetails>>(tags));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.APISearchTagFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to list tag without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
    }

    /// <summary>
    /// Suggest facets
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <returns>The facets matching the search</returns>
    /// <response code="200">Returns the facets</response>
    /// <response code="401">Action is not authorized</response>
    [HttpGet("Facet")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ApiFacetDetails>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Produces("application/json")]
    public async Task<IActionResult> SuggestFacet(
        string searchTerm = "")
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var results = _facetSearchEngine.Suggest(new TagFacetSearchQuery
            {
                SearchTerms = searchTerm
            });

            var facets = new List<TagFacet>();
            foreach (var h in results.Hits)
                try
                {
                    var facet = await _facetRepository.GetAsync(AmbientContext, h.Facet.FacetId);
                    facets.Add(facet);
                }
                catch (NotFoundEntityException)
                {
                    // TODO Fail silently and log the error.
                }

            return Ok(_mapper.Map<IEnumerable<ApiFacetDetails>>(facets));
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
    /// Suggest sources
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <returns>The sources</returns>
    /// <response code="200">Returns the facets</response>
    /// <response code="401">Action is not authorized</response>
    [HttpGet("Source")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ApiSourceDetails>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Produces("application/json")]
    public async Task<IActionResult> SearchSource(
        string searchTerm = "")
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
}