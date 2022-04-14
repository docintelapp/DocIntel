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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Search.Documents;
using DocIntel.Core.Utils.Search.Tags;
using DocIntel.WebApp.Helpers;
using DocIntel.WebApp.ViewModels.TagViewModel;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json.Linq;

namespace DocIntel.WebApp.Controllers
{
    public class TagController : BaseController
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IAppAuthorizationService _appAuthorizationService;
        private readonly IDocumentRepository _documentRepository;
        private readonly ITagFacetRepository _facetRepository;
        private readonly ILogger _logger;
        private readonly IDocumentSearchEngine _searchEngine;
        private readonly ITagRepository _tagRepository;
        private readonly ITagSearchService _tagSearchEngine;

        private readonly Regex regex = new(@"[^a-zA-Z0-9 ]");

        public TagController(DocIntelContext context,
            IAppAuthorizationService appAuthorizationService,
            ITagSearchService tagSearchEngine,
            ILogger<DocumentController> logger,
            IDocumentSearchEngine searchEngine,
            ApplicationSettings configuration,
            UserManager<AppUser> userManager,
            ITagRepository tagRepository,
            IDocumentRepository documentRepository,
            IAuthorizationService authorizationService,
            IHttpContextAccessor accessor, ITagFacetRepository facetRepository)
            : base(context,
                userManager,
                configuration,
                authorizationService)
        {
            _logger = logger;
            _searchEngine = searchEngine;
            _tagSearchEngine = tagSearchEngine;
            _appAuthorizationService = appAuthorizationService;
            _tagRepository = tagRepository;
            _documentRepository = documentRepository;
            _accessor = accessor;
            _facetRepository = facetRepository;
        }

        public async Task<IActionResult> Index(
            string searchTerm = "",
            int page = 1)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var query = new TagSearchQuery
                {
                    SearchTerms = searchTerm,
                    Page = page,
                    PageSize = 3000
                };
                var results = _tagSearchEngine.Search(query);

                var model = new IndexViewModel
                {
                    Results = results,
                    Query = query
                };
                return View(model);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.ListTagFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to list tag without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
        }

        [HttpGet("Tag/Details/{label}/{label2?}/{page?}")]
        public async Task<IActionResult> Details(string label, string label2 = null, int page = 1)
        {
            if (Guid.TryParse(label, out var guid))
            {
                var redirectTag = await _tagRepository.GetAsync(AmbientContext, guid, new[] {"Facet"});
                if (!string.IsNullOrEmpty(redirectTag.Facet.Prefix))
                    return RedirectToAction("Details",
                        new {label = redirectTag.Facet.Prefix, label2 = redirectTag.URL});
                return RedirectToAction("Details", new {label = redirectTag.URL});
            }

            var currentUser = await GetCurrentUser();
            try
            {
                TagFacet facet;
                Tag tag = null;
                if (!string.IsNullOrEmpty(label2))
                {
                    _logger.LogDebug($"Multiple labels are provided, get the facet '{label}' and the tag '{label2}'.");
                    facet = await _facetRepository.GetAsync(AmbientContext, label);
                    _logger.LogTrace($"Tag '{label}' is retrieved and is '{facet?.Id.ToString() ?? "(null)"}'.");
                    if (facet != null)
                    {
                        tag = await _tagRepository.GetAsync(AmbientContext, new TagQuery
                        {
                            FacetId = facet.Id,
                            URL = label2
                        }, new[] {"Facet"});
                        _logger.LogTrace($"Tag '{label}' is retrieved and is '{tag?.TagId.ToString() ?? "(null)"}'.");
                    }
                }
                else
                {
                    _logger.LogDebug(
                        $"Single label is provided, get the tag '{label}' from the facet with an empty prefix.");
                    facet = await _facetRepository.GetAsync(AmbientContext, "");
                    _logger.LogTrace(
                        $"Facet with empty prefix is retrieved and is '{facet?.Id.ToString() ?? "(null)"}'.");
                    if (facet != null)
                    {
                        tag = await _tagRepository.GetAsync(AmbientContext, new TagQuery
                        {
                            FacetId = facet.Id,
                            URL = label
                        }, new[] {"Facet"});
                        _logger.LogTrace($"Tag '{label}' is retrieved and is '{tag?.TagId.ToString() ?? "(null)"}'.");
                    }
                }

                if (tag != null)
                {
                    var query = new DocumentQuery
                    {
                        TagIds = new[] {tag.TagId},
                        Page = page,
                        Limit = 10,
                        OrderBy = SortCriteria.DocumentDate
                    };

                    var documentCount = await _documentRepository.CountAsync(AmbientContext, query);

                    var documents = _documentRepository.GetAllAsync(AmbientContext, query,
                        new[]
                        {
                            nameof(Document.DocumentTags),
                            nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag),
                            nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag) + "." + nameof(Tag.Facet),
                            nameof(Document.Source),
                            nameof(Document.Comments)
                        }
                    ).ToEnumerable();

                    return base.View(new TagDetailViewModel
                    {
                        Tag = tag,
                        Subscribed =
                            await _tagRepository.IsSubscribedAsync(AmbientContext, AmbientContext.CurrentUser,
                                tag.TagId),
                        Muted = 
                            await _tagRepository.IsMutedAsync(AmbientContext, AmbientContext.CurrentUser,
                                tag.TagId),
                        Documents = documents,
                        DocumentCount = documentCount,
                        Page = page,
                        PageCount = (int) Math.Ceiling(documentCount / 10.0)
                    });
                }

                throw new NotFoundEntityException();
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DetailsTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view details of tag '{label}:{label2}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("tag.label", label),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DetailsTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view details of a non-existing tag '{label}:{label2}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("tag.label", label),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        // GET: Tag/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await GetCurrentUser();
            if (!await _appAuthorizationService.CanCreateTag(User, null))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.CreateTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new tag without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            ViewBag.Facets = await GetAvailableFacetsAsync();

            return View(new Tag());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Label", "FacetId", "Description", "Keywords", "BackgroundColor")]
            Tag submittedViewModel)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                if (!await _facetRepository.ExistsAsync(AmbientContext, submittedViewModel.FacetId))
                    ModelState.AddModelError("FacetId", "Select a valid facet");

                if (await _tagRepository.ExistsAsync(AmbientContext, submittedViewModel.FacetId,
                    submittedViewModel.Label))
                    ModelState.AddModelError(nameof(submittedViewModel.Label), "Label already exists.");

                if (ModelState.IsValid)
                {
                    var tag = new Tag
                    {
                        Label = submittedViewModel.Label,
                        Description = submittedViewModel.Description,
                        Keywords = submittedViewModel.Keywords,
                        BackgroundColor = submittedViewModel.BackgroundColor,
                        FacetId = submittedViewModel.FacetId
                    };

                    if (!string.IsNullOrEmpty(submittedViewModel.Keywords))
                        tag.Keywords = string.Join(", ", submittedViewModel.Keywords.Split(',').Select(x => x.Trim()));

                    await _tagRepository.CreateAsync(AmbientContext, tag);
                    await AmbientContext.DatabaseContext.SaveChangesAsync();

                    _logger.Log(LogLevel.Information,
                        EventIDs.CreateTagSuccessful,
                        new LogEvent(
                                $"User '{currentUser.UserName}' successfully created a new tag '{tag.FriendlyName}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddTag(tag),
                        null,
                        LogEvent.Formatter);

                    return RedirectToAction(nameof(Index));
                }

                throw new InvalidArgumentException(ModelState);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.CreateTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new tag '{submittedViewModel.Label}' on facet '{submittedViewModel.FacetId}' without legitimate rights.")
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

                try
                {
                    submittedViewModel.Facet =
                        await _facetRepository.GetAsync(AmbientContext, submittedViewModel.FacetId);
                }
                catch (NotFoundEntityException)
                {
                    // TODO Log. Nice try though :-)
                }

                ViewBag.Facets = await GetAvailableFacetsAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.CreateTagSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' attempted to create a new tag with an invalid model.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return View(submittedViewModel);
            }
        }

        [HttpGet("/Tag/Edit/{id}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var tag = await _tagRepository.GetAsync(AmbientContext, id, new[] {"Facet"});
                ViewBag.Facets = await GetAvailableFacetsAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.EditTagSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully requested to edit tag '{tag.FriendlyName}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddTag(tag),
                    null,
                    LogEvent.Formatter);

                return View(tag);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new tag '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("tag.label", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditTagFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to edit a non-existing tag '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("tag.label", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpPost("/Tag/Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Guid id,
            [Bind("TagId", "Label", "Description", "Keywords", "BackgroundColor", "FacetId")]
            Tag submittedTag)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var tag = await _tagRepository.GetAsync(AmbientContext, id);

                if (submittedTag.FacetId == null ||
                    !await _facetRepository.ExistsAsync(AmbientContext, submittedTag.FacetId))
                    ModelState.AddModelError("FacetId", "Facet is not known");

                if (ModelState.IsValid)
                {
                    tag.Label = submittedTag.Label;
                    tag.Description = submittedTag.Description;
                    tag.Keywords = submittedTag.Keywords;
                    tag.BackgroundColor = submittedTag.BackgroundColor;
                    tag.FacetId = submittedTag.FacetId;
                    if (!string.IsNullOrEmpty(submittedTag.Keywords))
                        tag.Keywords = string.Join(", ", submittedTag.Keywords.Split(',').Select(x => x.Trim()));

                    if (tag.MetaData == null)
                        tag.MetaData = new JObject();

                    if (tag.MetaData.ContainsKey("mitre-id"))
                        tag.MetaData["mitre-id"] = Guid.NewGuid();
                    else
                        tag.MetaData.Add("mitre-id", Guid.NewGuid());

                    await _tagRepository.UpdateAsync(AmbientContext, tag);
                    await _context.SaveChangesAsync();

                    _logger.Log(LogLevel.Information,
                        EventIDs.EditTagSuccessful,
                        new LogEvent($"User '{currentUser.UserName}' successfully edit tag '{tag.FriendlyName}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddTag(tag),
                        null,
                        LogEvent.Formatter);

                    return RedirectToAction(nameof(Details), new {label = tag.TagId});
                }

                throw new InvalidArgumentException(ModelState);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditTagFailed,
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
                    EventIDs.EditTagFailed,
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

                submittedTag.Facet = await _facetRepository.GetAsync(AmbientContext, submittedTag.FacetId);
                ViewBag.Facets = await GetAvailableFacetsAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.EditTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit tag '{submittedTag.FriendlyName}' with an invalid model.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddTag(submittedTag),
                    null,
                    LogEvent.Formatter);

                return View(submittedTag);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Merge(Guid? primaryTagName = null)
        {
            var currentUser = await GetCurrentUser();

            if (!await _appAuthorizationService.CanMergeTags(User, null))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.MergeTagFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to merge tag without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            _logger.Log(LogLevel.Information,
                EventIDs.MergeTagSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully requested to merge tag.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            if (primaryTagName != default)
                ViewData["primaryTagName"] =
                    await _tagRepository.GetAsync(AmbientContext, (Guid) primaryTagName, new[] {"Facet"});

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Merge(Guid primaryTagName, Guid secondaryTagName)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var tag_src = await _tagRepository.GetAsync(AmbientContext, primaryTagName);
                if (tag_src == null)
                {
                    _logger.Log(LogLevel.Warning,
                        EventIDs.MergeTagFailed,
                        new LogEvent(
                                $"User '{currentUser.UserName}' attempted to merge a non-existing tag '{primaryTagName}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddTag(tag_src),
                        null,
                        LogEvent.Formatter);

                    ModelState.AddModelError("primaryTagName", "Please specify a valid tag");
                }

                var tag_target = await _tagRepository.GetAsync(AmbientContext, secondaryTagName);
                if (tag_target == null)
                {
                    _logger.Log(LogLevel.Warning,
                        EventIDs.MergeTagFailed,
                        new LogEvent(
                                $"User '{currentUser.UserName}' attempted to merge a non-existing tag '{secondaryTagName}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddTag(tag_target),
                        null,
                        LogEvent.Formatter);

                    ModelState.AddModelError("secondaryTagName", "Please specify a valid tag");
                }

                if (tag_src.TagId == tag_target.TagId)
                    return RedirectToAction(nameof(Details), new {label = tag_src.TagId});

                if (ModelState.IsValid)
                {
                    await _tagRepository.MergeAsync(AmbientContext, tag_src.TagId, tag_target.TagId);
                    await AmbientContext.DatabaseContext.SaveChangesAsync();

                    _logger.Log(LogLevel.Information,
                        EventIDs.MergeTagSuccessful,
                        new LogEvent(
                                $"User '{currentUser.UserName}' successfully merged tag '{tag_src.FriendlyName}' with '{tag_target.FriendlyName}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddTag(tag_src, "tag_primary")
                            .AddTag(tag_target, "tag_secondary"),
                        null,
                        LogEvent.Formatter);

                    return RedirectToAction("Details", new {label = tag_src.TagId});
                }

                throw new InvalidArgumentException();
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.MergeTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to merge tag '{primaryTagName}' with '{secondaryTagName}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("tag_primary.label", primaryTagName)
                        .AddProperty("tag_secondary.label", secondaryTagName),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.MergeTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to merge tag '{primaryTagName}' with '{secondaryTagName}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("tag_primary.label", primaryTagName)
                        .AddProperty("tag_secondary.label", secondaryTagName),
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

        [HttpGet("Tag/Delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var tag = await _tagRepository.GetAsync(AmbientContext, id);

                _logger.Log(LogLevel.Information,
                    EventIDs.DeleteTagSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully requested to delete tag '{tag.FriendlyName}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddTag(tag),
                    null,
                    LogEvent.Formatter);

                return View(tag);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DeleteTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to delete a tag '{id}' without legitimate rights.")
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
                    EventIDs.DeleteTagFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to delete a non-existing tag '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("tag.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpPost("Tag/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id, [Bind("TagId")] Tag submittedTag)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                await _tagRepository.RemoveAsync(AmbientContext, submittedTag.TagId);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.DeleteTagSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully deleted tag '{submittedTag.TagId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("tag.id", submittedTag.TagId),
                    null,
                    LogEvent.Formatter);

                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DeleteTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to delete a new tag '{id}' without legitimate rights.")
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
                    EventIDs.DeleteTagFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to delete a non-existing tag '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("tag.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpGet("Tag/Subscribe/{id}/{notification?}")]
        public async Task<IActionResult> Subscribe(Guid id, bool notification = false, string returnUrl = null)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var tag = await _tagRepository.GetAsync(AmbientContext, id);
                await _tagRepository.SubscribeAsync(AmbientContext, AmbientContext.CurrentUser, tag.TagId,
                    notification);
                await AmbientContext.DatabaseContext.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.SubscribeTagSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully subscribed to tag '{tag.FriendlyName}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddTag(tag),
                    null,
                    LogEvent.Formatter);

                if (string.IsNullOrEmpty(returnUrl))
                    return RedirectToAction(nameof(Details), new {label = tag.TagId});
                return Redirect(returnUrl);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.SubscribeTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to subscribe to tag '{id}' without legitimate rights.")
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
                    EventIDs.SubscribeTagFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to subscribe to a non-existing tag '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("tag.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }
        
        [HttpGet("Tag/Unsubscribe/{id}")]
        public async Task<IActionResult> Unsubscribe(Guid id, string returnUrl = null)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var tag = await _tagRepository.GetAsync(AmbientContext, id);
                await _tagRepository.UnsubscribeAsync(AmbientContext, AmbientContext.CurrentUser, tag.TagId);
                await AmbientContext.DatabaseContext.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.SubscribeTagSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully unsubscribed to tag '{tag.FriendlyName}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddTag(tag),
                    null,
                    LogEvent.Formatter);

                if (string.IsNullOrEmpty(returnUrl))
                    return RedirectToAction(nameof(Details), new {label = tag.TagId});
                return Redirect(returnUrl);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.SubscribeTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to unsubscribe to tag '{id}' without legitimate rights.")
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
                    EventIDs.SubscribeTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to unsubscribe to a non-existing tag '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("tag.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }
        
        [HttpGet("Tag/Mute/{id}")]
        public async Task<IActionResult> Mute(Guid id, string returnUrl = null)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var tag = await _tagRepository.GetAsync(AmbientContext, id);
                await _tagRepository.MuteAsync(AmbientContext, AmbientContext.CurrentUser, tag.TagId);
                await AmbientContext.DatabaseContext.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.SubscribeTagSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully muted tag '{tag.FriendlyName}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddTag(tag),
                    null,
                    LogEvent.Formatter);

                if (string.IsNullOrEmpty(returnUrl))
                    return RedirectToAction(nameof(Details), new {label = tag.TagId});
                return Redirect(returnUrl);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.SubscribeTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to mute tag '{id}' without legitimate rights.")
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
                    EventIDs.SubscribeTagFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to mute a non-existing tag '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("tag.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpGet("Tag/Unmute/{id}")]
        public async Task<IActionResult> Unmute(Guid id, string returnUrl = null)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var tag = await _tagRepository.GetAsync(AmbientContext, id);
                await _tagRepository.UnmuteAsync(AmbientContext, AmbientContext.CurrentUser, tag.TagId);
                await AmbientContext.DatabaseContext.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.SubscribeTagSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully muted tag '{tag.FriendlyName}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddTag(tag),
                    null,
                    LogEvent.Formatter);

                if (string.IsNullOrEmpty(returnUrl))
                    return RedirectToAction(nameof(Details), new {label = tag.TagId});
                return Redirect(returnUrl);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.SubscribeTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to unmute tag '{id}' without legitimate rights.")
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
                    EventIDs.SubscribeTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to unmute a non-existing tag '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("tag.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        public async Task<JsonResult> GetTagsAsync(string term)
        {
            // TODO Use the proper API, delete this ugly code.
            var currentUser = await GetCurrentUser();

            // term = regex.Replace(term, " ").Trim() + "*";
            var searchResults = _tagSearchEngine.Search(new TagSearchQuery
            {
                SearchTerms = term
            });
            var orderedTags = searchResults.Hits.Select(x => x.Tag.TagId).ToList();

            var tags = new List<Tag>();
            foreach (var tagId in orderedTags)
                try
                {
                    var tag = await _tagRepository.GetAsync(AmbientContext, tagId, new[] {nameof(Tag.Facet)});
                    tags.Add(tag);
                }
                catch (NotFoundEntityException)
                {
                    // FIXME log
                }
                catch (UnauthorizedOperationException)
                {
                    // FIXME log
                }

            var group_by_categories = tags
                .GroupBy(_ => _.FacetId, x => x)
                .ToList()
                .Select(x =>
                {
                    return new
                    {
                        Title = x.Select(_ => _.Facet.Title).First(),
                        TotalScore = x.Sum(_ => orderedTags.IndexOf(_.TagId)),
                        Children = x.Select(_ => new
                        {
                            _.TagId,
                            _.Label,
                            _.Facet.Prefix,
                            _.BackgroundColor
                        }).OrderBy(_ => orderedTags.IndexOf(_.TagId))
                    };
                }).OrderBy(_ => _.TotalScore);

            return Json(group_by_categories);
        }

        [HttpGet("Tag/GetStatistics/{tagId}")]
        public async Task<JsonResult> GetStatisticsAsync(Guid tagId)
        {
            // TODO Use SolR to provide statistics, it will be much more efficient.
            var currentUser = await GetCurrentUser();
            var period = (int) (DateTime.UtcNow - DateTime.UtcNow.AddMonths(-6)).TotalDays;
            try
            {
                // var tag = _tagRepository.GetAsync(AmbientContext, tagId);

                var query = new DocumentQuery
                {
                    TagIds = new[] {tagId},
                    Limit = -1,
                    OrderBy = SortCriteria.DocumentDate
                };

                var docs = _documentRepository.GetAllAsync(AmbientContext, query);

                var docStats = docs
                    .Where(_ => (DateTime.UtcNow - _.Files.Min(_ => _.DocumentDate)).TotalDays <= period)
                    .Select(_ =>
                        new
                        {
                            Date = new DateTime(_.Files.Min(_ => _.DocumentDate).Year,
                                _.Files.Min(_ => _.DocumentDate).Month, _.Files.Min(_ => _.DocumentDate).Day)
                        }
                    ).GroupBy(_ => _.Date)
                    .Select(_ => new
                    {
                        Date = _.Key,
                        Value = _.CountAsync().Result
                    }).ToEnumerable();

                var today = DateTime.UtcNow;
                var start = new DateTime(today.Year, today.Month, today.Day);
                var temp = from d in Enumerable.Range(0, period).Select(_ => start.AddDays(-_))
                    join s in docStats on d.Date equals s.Date into ds
                    from sds in ds.DefaultIfEmpty()
                    select new {Date = d.ToString("yyyy-MM-dd"), Value = sds?.Value ?? 0};

                return base.Json(new
                {
                    DataByTopic = new[] {new {Topic = -1, TopicName = "Documents", Dates = temp.OrderBy(_ => _.Date)}}
                });
                // return base.Json(temp.OrderBy(_ => _.Name));
            }
            catch (Exception)
            {
                return base.Json(Enumerable.Empty<Tag>());
            }
        }

        private async Task<IEnumerable<SelectListItem>> GetAvailableFacetsAsync()
        {
            var currentUser = await GetCurrentUser();
            return _facetRepository.GetAllAsync(AmbientContext)
                .Select(c => new SelectListItem {Text = c.Title, Value = c.Id.ToString()})
                .ToEnumerable();
        }
    }
}