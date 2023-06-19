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
[Route("API/Search")]
[ApiController]
public class SearchController : DocIntelAPIControllerBase
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

    public SearchController(UserManager<AppUser> userManager,
        DocIntelContext context,
        ITagSearchService tagSearchEngine,
        ILogger<SearchController> logger,
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
    /// Search for tags
    /// </summary>
    /// <param name="query">The query</param>
    /// <returns>The tags matching the search</returns>
    /// <response code="200">Returns the tags</response>
    /// <response code="401">Action is not authorized</response>
    [HttpPost("Tag")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<APITagDetails>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Produces("application/json")]
    public async Task<IActionResult> SearchTags(ApiTagSearchQuery query)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var results = _tagSearchEngine.Search(_mapper.Map<TagSearchQuery>(query));

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
    /// Search for facets
    /// </summary>
    /// <param name="query">The search query</param>
    /// <returns>The facets matching the search</returns>
    /// <response code="200">Returns the facets</response>
    /// <response code="401">Action is not authorized</response>
    [HttpPost("Facet")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ApiFacetDetails>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Produces("application/json")]
    public async Task<IActionResult> SearchFacets([FromBody] ApiFacetSearchQuery query)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var results = _facetSearchEngine.Search(_mapper.Map<TagFacetSearchQuery>(query));

            var tags = new List<object>();
            foreach (var h in results.Hits) tags.Add(await _facetRepository.GetAsync(AmbientContext, h.Facet.FacetId));

            return Ok(_mapper.Map<IEnumerable<ApiFacetDetails>>(tags));
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
    /// Search for sources
    /// </summary>
    /// <param name="query">The search query</param>
    /// <returns>The sources</returns>
    /// <response code="200">Returns the facets</response>
    /// <response code="401">Action is not authorized</response>
    [HttpPost("Source")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ApiSourceDetails>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Produces("application/json")]
    public async Task<IActionResult> SearchSource([FromBody] ApiSourceSearchQuery query)
    {
        var currentUser = await GetCurrentUser();
        var searchResults = _sourceSearchEngine.Search(_mapper.Map<SourceSearchQuery>(query));

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