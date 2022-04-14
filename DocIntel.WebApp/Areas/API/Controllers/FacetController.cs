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

namespace DocIntel.WebApp.Areas.API.Controllers
{
    [Area("API")]
    [Route("API/Facet")]
    [ApiController]
    public class FacetController : DocIntelAPIControllerBase
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly ITagFacetRepository _facetRepository;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly ITagSearchService _tagSearchEngine;

        public FacetController(UserManager<AppUser> userManager,
            DocIntelContext context,
            ILogger<FacetController> logger,
            IHttpContextAccessor accessor,
            ITagFacetRepository facetRepository,
            ITagSearchService tagSearchEngine,
            IMapper mapper)
            : base(userManager, context)
        {
            _logger = logger;
            _accessor = accessor;
            _facetRepository = facetRepository;
            _tagSearchEngine = tagSearchEngine;
            _mapper = mapper;
        }

        [HttpGet("")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<APITagFacet>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Index(
            string searchTerm = "",
            int page = 1)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var results = _tagSearchEngine.SearchFacet(new TagFacetSearchQuery
                {
                    SearchTerms = searchTerm,
                    Page = page
                });

                var tags = new List<object>();
                foreach (var h in results.Hits) tags.Add(await _facetRepository.GetAsync(AmbientContext, h.FacetId));

                return Ok(_mapper.Map<IEnumerable<APITagFacet>>(tags));
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

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ModelStateDictionary))]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Create(APITagFacet tagFacet)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                if (ModelState.IsValid)
                {
                    var facet = new TagFacet
                    {
                        Title = tagFacet.Title,
                        Prefix = tagFacet.Prefix,
                        Description = tagFacet.Description,
                        Mandatory = tagFacet.Mandatory,
                        Hidden = tagFacet.Hidden
                    };
                    await _facetRepository.AddAsync(AmbientContext, facet);
                    await _context.SaveChangesAsync();

                    _logger.Log(LogLevel.Information,
                        EventIDs.APICreateTagFacetSuccessful,
                        new LogEvent($"User '{currentUser.UserName}' successfully created a new facet.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddFacet(facet),
                        null,
                        LogEvent.Formatter);

                    return Ok();
                }

                throw new InvalidArgumentException(ModelState);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APICreateTagFacetFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create facet '{tagFacet.Id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("facet.id", tagFacet.Id),
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
                            $"User '{currentUser.UserName}' attempted to create facet '{tagFacet.Id}' with an invalid model.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("facet.id", tagFacet.Id),
                    null,
                    LogEvent.Formatter);

                return BadRequest(ModelState);
            }
        }

        [HttpPatch("")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ModelStateDictionary))]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Update(APITagFacet tagFacet)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var facet = await _facetRepository.GetAsync(AmbientContext, tagFacet.Id);

                if (ModelState.IsValid)
                {
                    facet.Title = tagFacet.Title;
                    facet.Prefix = tagFacet.Prefix;
                    facet.Description = tagFacet.Description;
                    facet.Mandatory = tagFacet.Mandatory;
                    facet.Hidden = tagFacet.Hidden;

                    var updatedFacet = await _facetRepository.UpdateAsync(AmbientContext, facet);
                    await _context.SaveChangesAsync();

                    _logger.Log(LogLevel.Information,
                        EventIDs.APIEditTagFacetSuccessful,
                        new LogEvent($"User '{currentUser.UserName}' successfully edited facet '{tagFacet.Id}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddFacet(updatedFacet),
                        null,
                        LogEvent.Formatter);

                    return Ok();
                }

                throw new InvalidArgumentException(ModelState);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIEditTagFacetFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit facet '{tagFacet.Id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("facet.id", tagFacet.Id),
                    null,
                    LogEvent.Formatter);
                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIEditTagFacetFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit a non-existing facet '{tagFacet.Id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("facet.id", tagFacet.Id),
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
                            $"User '{currentUser.UserName}' attempted to edit facet '{tagFacet.Id}' with an invalid model.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("facet.id", tagFacet.Id),
                    null,
                    LogEvent.Formatter);

                return BadRequest(ModelState);
            }
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var facet = await _facetRepository.RemoveAsync(AmbientContext, id);
                _logger.Log(LogLevel.Information,
                    EventIDs.APIDeleteTagFacetSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully deleted '{facet.Title}'.")
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
                            $"User '{currentUser.UserName}' attempted to delete facet '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("facet.id", id),
                    null,
                    LogEvent.Formatter);
                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDeleteTagFacetFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to delete a non-existing facet '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("facet.id", id),
                    null,
                    LogEvent.Formatter);
                return NotFound();
            }
        }

        [HttpPost("Merge")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ModelStateDictionary))]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Merge(Guid primaryFacetId, Guid secondaryFacetId)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var primaryFacet = await _facetRepository.GetAsync(AmbientContext, primaryFacetId);
                if (primaryFacet == null)
                {
                    _logger.Log(LogLevel.Warning,
                        EventIDs.APIMergeFacetFailed,
                        new LogEvent(
                                $"User '{currentUser.UserName}' attempted to merge a non-existing facet '{primaryFacetId}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddFacet(primaryFacet),
                        null,
                        LogEvent.Formatter);
                    return NotFound();
                }

                var secondaryFacet = await _facetRepository.GetAsync(AmbientContext, secondaryFacetId);
                if (secondaryFacet == null)
                {
                    _logger.Log(LogLevel.Warning,
                        EventIDs.APIMergeFacetFailed,
                        new LogEvent(
                                $"User '{currentUser.UserName}' attempted to merge a non-existing facet '{secondaryFacetId}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddFacet(secondaryFacet),
                        null,
                        LogEvent.Formatter);
                    return NotFound();
                }

                if (primaryFacet.Id == secondaryFacet.Id)
                {
                    _logger.Log(LogLevel.Information,
                        EventIDs.APIMergeFacetSuccessful,
                        new LogEvent(
                                $"User '{currentUser.UserName}' successfully merged facet '{primaryFacet.Id}' with '{secondaryFacet.Id}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddFacet(primaryFacet, "facet_primary")
                            .AddFacet(secondaryFacet, "facet_secondary"),
                        null,
                        LogEvent.Formatter);
                    return Ok();
                }

                if (!ModelState.IsValid) throw new InvalidArgumentException();
                await _facetRepository.MergeAsync(AmbientContext, primaryFacet.Id, secondaryFacet.Id);

                _logger.Log(LogLevel.Information,
                    EventIDs.APIMergeFacetSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully merged facet '{primaryFacet.Id}' with '{secondaryFacet.Id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddFacet(primaryFacet, "facet_primary")
                        .AddFacet(secondaryFacet, "facet_secondary"),
                    null,
                    LogEvent.Formatter);
                return Ok();
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIMergeFacetFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to merge facet '{primaryFacetId}' with '{secondaryFacetId}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("facet_primary.id", primaryFacetId)
                        .AddProperty("facet_secondary.id", secondaryFacetId),
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
                        .AddProperty("facet_primary.id", primaryFacetId)
                        .AddProperty("facet_secondary.id", secondaryFacetId),
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
                        .AddProperty("facet_primary.id", primaryFacetId)
                        .AddProperty("facet_secondary.id", secondaryFacetId),
                    null,
                    LogEvent.Formatter);

                return BadRequest(ModelState);
            }
        }

        [HttpPut("Subscribe")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Subscribe(Guid facetId, bool notification = false)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                await _facetRepository.SubscribeAsync(AmbientContext, AmbientContext.CurrentUser, facetId,
                    notification);

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

        [HttpPut("Unsubscribe")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Unsubscribe(Guid facetId)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                await _facetRepository.UnsubscribeAsync(AmbientContext, AmbientContext.CurrentUser, facetId);

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
}