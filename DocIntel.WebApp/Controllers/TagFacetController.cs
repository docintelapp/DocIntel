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
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Modules;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;
using DocIntel.WebApp.Helpers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DocIntel.WebApp.Controllers
{
    public class TagFacetController : BaseController
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IAppAuthorizationService _appAuthorizationService;
        private readonly ITagFacetRepository _facetRepository;
        private readonly ILogger _logger;
        private readonly ModuleFactory _moduleFactory;

        public TagFacetController(DocIntelContext context,
            ILogger<TagFacetController> logger,
            AppUserManager userManager,
            ApplicationSettings configuration,
            IAuthorizationService authorizationService,
            IAppAuthorizationService appAuthorizationService,
            IHttpContextAccessor accessor,
            ITagFacetRepository facetRepository, ModuleFactory moduleFactory)
            : base(context,
                userManager,
                configuration,
                authorizationService)
        {
            _logger = logger;
            _appAuthorizationService = appAuthorizationService;
            _accessor = accessor;
            _facetRepository = facetRepository;
            _moduleFactory = moduleFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var currentUser = await GetCurrentUser();
            if (!await _appAuthorizationService.CanCreateFacetTag(User, null))
            {
                _logger.Log(LogLevel.Warning, EventIDs.CreateTagFacetFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new facet without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            ViewBag.ModuleMetadata = _moduleFactory.GetMetadata(typeof(TagFacet));
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,Prefix,Mandatory,Hidden,AutoExtract,ExtractionRegex,TagNormalization")]
            TagFacet submittedViewModel,
            [Bind(Prefix = "Metadata")] Dictionary<string,string> metadata)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var tagFacet = await _facetRepository.AddAsync(AmbientContext, submittedViewModel);
                tagFacet.MetaData = this.ParseMetaData(metadata, currentUser);
                
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information, EventIDs.CreateTagFacetSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully created a new facet.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddFacet(tagFacet),
                    null,
                    LogEvent.Formatter);

                return RedirectToAction("Index", "Tag");
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.CreateTagFacetFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new facet without legitimate rights.")
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
                    EventIDs.CreateTagSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new facet without a valid model.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddFacet(submittedViewModel),
                    null,
                    LogEvent.Formatter);

                return View(submittedViewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var facet = await _facetRepository.GetAsync(AmbientContext, id, new[] {nameof(TagFacet.Tags)});
                if (!await _appAuthorizationService.CanEditFacetTag(User, facet))
                {
                    _logger.Log(LogLevel.Warning, EventIDs.EditTagFacetFailed,
                        new LogEvent(
                                $"User '{currentUser.UserName}' attempted to get details about facet '{facet.Title}' without legitimate rights.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddFacet(facet),
                        null,
                        LogEvent.Formatter);

                    return Unauthorized();
                }

                return View(facet);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditTagFacetFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to get details about facet '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("facet.id", id),
                    null,
                    LogEvent.Formatter);
                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditTagFacetFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to get details about a non-existing facet '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("facet.id", id),
                    null,
                    LogEvent.Formatter);
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var facet = await _facetRepository.GetAsync(AmbientContext, id, new[] {nameof(TagFacet.Tags)});
                if (!await _appAuthorizationService.CanEditFacetTag(User, facet))
                {
                    _logger.Log(LogLevel.Warning, EventIDs.EditTagFacetFailed,
                        new LogEvent(
                                $"User '{currentUser.UserName}' attempted to edit facet '{facet.Title}' without legitimate rights.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddFacet(facet),
                        null,
                        LogEvent.Formatter);

                    return Unauthorized();
                }
                ViewBag.ModuleMetadata = _moduleFactory.GetMetadata(typeof(TagFacet));
                return View(facet);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditTagFacetFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit facet '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("facet.id", id),
                    null,
                    LogEvent.Formatter);
                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditTagFacetFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to edit a non-existing facet '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("facet.id", id),
                    null,
                    LogEvent.Formatter);
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Title,Description,Prefix,Mandatory,Hidden,AutoExtract,ExtractionRegex,TagNormalization")]
            TagFacet submittedViewModel,
            [Bind(Prefix = "Metadata")] Dictionary<string,string> metadata)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var facet = await _facetRepository.GetAsync(AmbientContext, id);

                if (ModelState.IsValid)
                {
                    facet.Title = submittedViewModel.Title;
                    facet.Prefix = submittedViewModel.Prefix;
                    facet.Description = submittedViewModel.Description;
                    facet.Mandatory = submittedViewModel.Mandatory;
                    facet.Hidden = submittedViewModel.Hidden;
                    facet.AutoExtract = submittedViewModel.AutoExtract;
                    facet.ExtractionRegex = submittedViewModel.ExtractionRegex;
                    facet.TagNormalization = submittedViewModel.TagNormalization;
                    
                    facet.MetaData = this.ParseMetaData(metadata, currentUser, facet.MetaData);

                    var tagFacet = await _facetRepository.UpdateAsync(AmbientContext, facet);
                    await _context.SaveChangesAsync();

                    _logger.Log(LogLevel.Information, EventIDs.EditTagFacetSuccessful,
                        new LogEvent($"User '{currentUser.UserName}' successfully edited facet '{facet.Title}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddFacet(tagFacet),
                        null,
                        LogEvent.Formatter);

                    return RedirectToAction(nameof(TagController.Index), "Tag");
                }

                throw new InvalidArgumentException(ModelState);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditTagFacetFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit facet '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("facet.id", id),
                    null,
                    LogEvent.Formatter);
                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditTagFacetFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to edit a non-existing facet '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("facet.id", id),
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

                _logger.Log(LogLevel.Warning, EventIDs.EditTagFacetFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to edit facet '{id}' with an invalid model.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("facet.id", id),
                    null,
                    LogEvent.Formatter);
                ViewBag.ModuleMetadata = _moduleFactory.GetMetadata(typeof(TagFacet));
                return View(submittedViewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var facet = await _facetRepository.GetAsync(AmbientContext, id, new[] {"Tags"});
                if (!await _appAuthorizationService.CanDeleteFacetTag(User, facet))
                {
                    _logger.Log(LogLevel.Warning, EventIDs.DeleteTagFacetFailed,
                        new LogEvent(
                                $"User '{currentUser.UserName}' attempted to delete facet '{facet.Title}' without legitimate rights.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddFacet(facet),
                        null,
                        LogEvent.Formatter);

                    return Unauthorized();
                }

                return View(facet);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.DeleteTagFacetFailed,
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
                _logger.Log(LogLevel.Warning, EventIDs.DeleteTagFacetFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to delete a non-existing facet '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("facet.id", id),
                    null,
                    LogEvent.Formatter);
                return NotFound();
            }
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var facet = await _facetRepository.RemoveAsync(AmbientContext, id);
                _logger.Log(LogLevel.Information, EventIDs.DeleteTagFacetSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully deleted '{facet.Title}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddFacet(facet),
                    null,
                    LogEvent.Formatter);

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(TagController.Index), "Tag");
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.DeleteTagFacetFailed,
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
                _logger.Log(LogLevel.Warning, EventIDs.DeleteTagFacetFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to delete a non-existing facet '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("facet.id", id),
                    null,
                    LogEvent.Formatter);
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Merge(Guid? primaryFacetName = null)
        {
            var currentUser = await GetCurrentUser();

            if (!await _appAuthorizationService.CanMergeFacets(User, null))
            {
                _logger.Log(LogLevel.Warning, EventIDs.MergeFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to merge facets without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            _logger.Log(LogLevel.Information, EventIDs.MergeSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully requested to merge facets.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            if (primaryFacetName != default)
                ViewData["primaryFacetName"] =
                    await _facetRepository.GetAsync(AmbientContext, (Guid) primaryFacetName, new[] {"Facet"});

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Merge(Guid primaryFacetName, Guid secondaryFacetName)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var primaryFacet = await _facetRepository.GetAsync(AmbientContext, primaryFacetName);
                if (primaryFacet == null)
                {
                    _logger.Log(LogLevel.Warning, EventIDs.MergeFailed,
                        new LogEvent(
                                $"User '{currentUser.UserName}' attempted to merge a non-existing facet '{primaryFacetName}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddFacet(primaryFacet),
                        null,
                        LogEvent.Formatter);

                    ModelState.AddModelError("primaryFacetName", "Please specify a valid facet");
                }

                var secondaryFacet = await _facetRepository.GetAsync(AmbientContext, secondaryFacetName);
                if (secondaryFacet == null)
                {
                    _logger.Log(LogLevel.Warning, EventIDs.MergeFailed,
                        new LogEvent(
                                $"User '{currentUser.UserName}' attempted to merge a non-existing facet '{secondaryFacetName}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddFacet(secondaryFacet),
                        null,
                        LogEvent.Formatter);

                    ModelState.AddModelError("secondaryFacetName", "Please specify a valid facet");
                }

                if (primaryFacet.FacetId == secondaryFacet.FacetId) return RedirectToAction("Index", "Tag");

                if (ModelState.IsValid)
                {
                    await _facetRepository.MergeAsync(AmbientContext, primaryFacet.FacetId, secondaryFacet.FacetId);
                    await AmbientContext.DatabaseContext.SaveChangesAsync();

                    _logger.Log(LogLevel.Information, EventIDs.MergeSuccessful,
                        new LogEvent(
                                $"User '{currentUser.UserName}' successfully merged tag '{primaryFacet.Title}' with '{secondaryFacet.Title}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddFacet(primaryFacet, "tag_primary")
                            .AddFacet(secondaryFacet, "tag_secondary"),
                        null,
                        LogEvent.Formatter);

                    return RedirectToAction("Index", "Tag");
                }

                throw new InvalidArgumentException();
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.MergeFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to merge tag '{primaryFacetName}' with '{secondaryFacetName}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("tag_primary.label", primaryFacetName)
                        .AddProperty("tag_secondary.label", secondaryFacetName),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.MergeFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to merge tag '{primaryFacetName}' with '{secondaryFacetName}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("tag_primary.label", primaryFacetName)
                        .AddProperty("tag_secondary.label", secondaryFacetName),
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

                return View();
            }
        }
    }
}