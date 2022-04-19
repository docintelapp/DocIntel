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
using DocIntel.Core.Utils.Search.Tags;
using DocIntel.WebApp.Areas.API.Models;
using DocIntel.WebApp.Helpers;
using DotLiquid;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Tag = DocIntel.Core.Models.Tag;

namespace DocIntel.WebApp.Areas.API.Controllers
{
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

        [HttpGet("")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<APITag>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Index(
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

                return Ok(_mapper.Map<IEnumerable<APITag>>(tags));
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
        
        
        [HttpGet("Suggest")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<APITag>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Suggest(
            string searchTerm = "")
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var results = _tagSearchEngine.Suggest(new TagSearchQuery
                {
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

                return Ok(_mapper.Map<IEnumerable<APITag>>(tags));
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

        [HttpGet("Details")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APITag))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Details(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var tag = await _tagRepository.GetAsync(AmbientContext, id, new[] {"Facet"});
                return Ok(_mapper.Map<APITag>(tag));
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDetailsTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view details of tag '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("tag.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDetailsTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view details of a non-existing tag '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("tag.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpPost("")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ModelStateDictionary))]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Create([FromBody] APITag submittedViewModel,
            [FromQuery] bool createFacetIfNotExist)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                TagFacet facet = null;
                if (submittedViewModel.Facet == null)
                {
                    ModelState.AddModelError("FacetId", "Select a valid facet");
                }
                else
                {
                    if (submittedViewModel.Facet.Id == Guid.Empty &&
                        !string.IsNullOrWhiteSpace(submittedViewModel.Facet.Prefix))
                        try
                        {
                            facet = await _facetRepository.GetAsync(AmbientContext,
                                submittedViewModel.Facet.Prefix);
                            submittedViewModel.Facet.Id = facet.FacetId;
                        }
                        catch (NotFoundEntityException)
                        {
                            if (createFacetIfNotExist)
                            {
                                _logger.LogError("create facet");
                                var tagFacet = new TagFacet
                                {
                                    Prefix = submittedViewModel.Facet.Prefix,
                                    CreationDate = DateTime.Now,
                                    ModificationDate = DateTime.Now,
                                    Title = submittedViewModel.Facet.Prefix
                                };
                                facet = await _facetRepository.AddAsync(AmbientContext,
                                    tagFacet);
                                submittedViewModel.Facet.Id = facet.FacetId;
                            }
                            else

                            {
                                ModelState.AddModelError("FacetPrefix", "Select a valid facet");
                            }
                        }

                    if (await _tagRepository.ExistsAsync(AmbientContext, submittedViewModel.Facet.Id,
                        submittedViewModel.Label))
                        ModelState.AddModelError(nameof(submittedViewModel.Label), "Label already exists.");
                }

                if (ModelState.IsValid)
                {
                    var tag = new Tag
                    {
                        Label = submittedViewModel.Label,
                        Description = submittedViewModel.Description,
                        Keywords = string.Join(",", submittedViewModel.Keywords.Select(_ => _.Trim())),
                        BackgroundColor = submittedViewModel.BackgroundColor,
                        Facet = facet,
                        FacetId = submittedViewModel.Facet.Id,
                        ExtractionKeywords = string.Join(",", submittedViewModel.ExtractionKeywords.Select(_ => _.Trim()))
                    };

                    await _tagRepository.CreateAsync(AmbientContext, tag);

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

                    return Ok();
                }

                return BadRequest(ModelState);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APICreateTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new tag '{submittedViewModel.Label}' on facet '{submittedViewModel.Facet.Id}' without legitimate rights.")
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

        [HttpPatch("")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ModelStateDictionary))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Edit([FromBody] APITag submittedTag)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var tag = await _tagRepository.GetAsync(AmbientContext, submittedTag.TagId);

                if (submittedTag.Facet?.Id == null ||
                    !await _facetRepository.ExistsAsync(AmbientContext, submittedTag.Facet.Id))
                    ModelState.AddModelError("FacetId", "Facet is not known");

                if (ModelState.IsValid)
                {
                    TagFacet facet = await _facetRepository.GetAsync(AmbientContext, submittedTag.Facet.Id);
                    
                    tag.Label = submittedTag.Label;
                    tag.Description = submittedTag.Description;
                    tag.Keywords = string.Join(",", submittedTag.Keywords.Select(_ => _.Trim()));
                    tag.BackgroundColor = submittedTag.BackgroundColor;
                    tag.Facet = facet;
                    tag.FacetId = submittedTag.Facet.Id;
                    tag.ExtractionKeywords = string.Join(",", submittedTag.ExtractionKeywords.Select(_ => _.Trim()));
                    
                    await _tagRepository.UpdateAsync(AmbientContext, tag);
                    await _context.SaveChangesAsync();

                    _logger.Log(LogLevel.Information,
                        EventIDs.APIEditTagSuccessful,
                        new LogEvent($"User '{currentUser.UserName}' successfully edit tag '{tag.FriendlyName}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddTag(tag),
                        null,
                        LogEvent.Formatter);

                    return Ok();
                }

                throw new InvalidArgumentException(ModelState);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIEditTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit tag '{submittedTag.TagId}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("tag.id", submittedTag.TagId),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIEditTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit a non-existing tag '{submittedTag.TagId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("tag.id", submittedTag.TagId),
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
                            $"User '{currentUser.UserName}' attempted to edit tag '{submittedTag.FriendlyName}' with an invalid model.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("tag.id", submittedTag.TagId),
                    null,
                    LogEvent.Formatter);

                return BadRequest(ModelState);
            }
        }

        [HttpPost("Merge")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ModelStateDictionary))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Merge(Guid primaryTagId, Guid secondaryTagId)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var primaryTag = await _tagRepository.GetAsync(AmbientContext, primaryTagId);
                if (primaryTag == null)
                {
                    _logger.Log(LogLevel.Warning,
                        EventIDs.APIMergeTagFailed,
                        new LogEvent(
                                $"User '{currentUser.UserName}' attempted to merge a non-existing tag '{primaryTagId}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddTag(primaryTag),
                        null,
                        LogEvent.Formatter);
                    return NotFound();
                }

                var secondaryTag = await _tagRepository.GetAsync(AmbientContext, secondaryTagId);
                if (secondaryTag == null)
                {
                    _logger.Log(LogLevel.Warning,
                        EventIDs.APIMergeTagFailed,
                        new LogEvent(
                                $"User '{currentUser.UserName}' attempted to merge a non-existing tag '{secondaryTagId}'.")
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
                    await _tagRepository.MergeAsync(AmbientContext, primaryTag.TagId, secondaryTag.TagId);

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

                    return Ok();
                }

                throw new InvalidArgumentException();
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIMergeTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to merge tag '{primaryTagId}' with '{secondaryTagId}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("tag_primary.id", primaryTagId)
                        .AddProperty("tag_secondary.id", secondaryTagId),
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
                        .AddProperty("tag_primary.id", primaryTagId)
                        .AddProperty("tag_secondary.id", secondaryTagId),
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

        [HttpDelete("")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
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

        [HttpPut("Subscribe")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Subscribe(Guid tagId, bool notification = false)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                await _tagRepository.SubscribeAsync(AmbientContext, AmbientContext.CurrentUser, tagId, notification);

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

        [HttpPut("Unsubscribe")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Unsubscribe(Guid tagId)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                await _tagRepository.UnsubscribeAsync(AmbientContext, AmbientContext.CurrentUser, tagId);

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
}