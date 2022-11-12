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
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Search.Documents;
using DocIntel.Core.Utils.Search.Sources;
using DocIntel.Core.Utils.Search.Tags;
using DocIntel.WebApp.Helpers;
using DocIntel.WebApp.ViewModels.SourceViewModel;

using Ganss.Xss;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json.Linq;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace DocIntel.WebApp.Controllers
{
    public class SourceController : BaseController
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IAppAuthorizationService _appAuthorizationService;
        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger _logger;
        private readonly IDocumentSearchEngine _searchEngine;
        private readonly ISourceRepository _sourceRepository;
        private readonly ISourceSearchService _sourceSearchEngine;
        private readonly ITagSearchService _tagSearchEngine;
        private readonly HtmlSanitizer _sanitizer;
        private readonly ApplicationSettings _appSettings;

        public SourceController(IAppAuthorizationService appAuthorizationService,
            DocIntelContext context,
            ITagSearchService tagSearchEngine,
            ILogger<SourceController> logger,
            IDocumentSearchEngine searchEngine,
            ApplicationSettings configuration,
            UserManager<AppUser> userManager,
            IAuthorizationService authorizationService,
            ISourceSearchService sourceSearchEngine,
            ISourceRepository sourceRepository,
            IDocumentRepository documentRepository,
            IHttpContextAccessor accessor, ApplicationSettings appSettings)
            : base(context,
                userManager,
                configuration,
                authorizationService)
        {
            _logger = logger;
            _searchEngine = searchEngine;
            _tagSearchEngine = tagSearchEngine;
            _sourceSearchEngine = sourceSearchEngine;
            _sourceRepository = sourceRepository;
            _documentRepository = documentRepository;
            _appAuthorizationService = appAuthorizationService;
            _accessor = accessor;
            _appSettings = appSettings;

            _sanitizer = new HtmlSanitizer();
            _sanitizer.AllowedSchemes.Add("data");
        }

        [HttpGet("Source")]
        [HttpGet("Source/Index")]
        public async Task<IActionResult> Index(
            string searchTerm = "",
            SourceSortCriteria sortCriteria = SourceSortCriteria.Title,
            int[] reliabilities = null,
            int page = 1)
        {
            var currentUser = await GetCurrentUser();

            var filteredReliabilities = reliabilities
                ?.Where(_ => Enum.IsDefined(typeof(SourceReliability), _))
                .Select(_ => (SourceReliability) _)
                .ToList();
            
            var query = new SourceSearchQuery
            {
                SearchTerms = searchTerm,
                SortCriteria = sortCriteria,
                Page = page,
                PageSize = 10,
                SelectedReliabilities = filteredReliabilities,
            };
            
            var searchResults = _sourceSearchEngine.Search(query);
            
            var vm = new IndexViewModel
            {
                SearchQuery = query,
                SearchResults = searchResults
            };
            
            vm.PageCount = searchResults.TotalHits == 0 ? 1 : (int) Math.Ceiling(searchResults.TotalHits / 10.0);
            
            _logger.Log(LogLevel.Information,
                EventIDs.ListSourceSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully listed sources.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return View(vm);
        }

        [HttpGet("Source/Details/{url}/{page?}")]
        public async Task<IActionResult> Details(string url, int page = 1)
        {
            if (Guid.TryParse(url, out var guid))
            {
                var redirectSource = await _sourceRepository.GetAsync(AmbientContext, guid);
                return RedirectToAction("Details", new {redirectSource.URL});
            }

            var currentUser = await GetCurrentUser();

            try
            {
                var source =
                    await _sourceRepository.GetAsync(AmbientContext, new SourceQuery {URL = url}, 
                        new[] {"Documents", "Documents.DocumentTags", "Documents.DocumentTags.Tag", "Documents.DocumentTags.Tag.Facet"});
                var vm = new DetailViewModel();
                vm.Source = source;
                vm.DocumentCount =
                    await _documentRepository.CountAsync(AmbientContext, new DocumentQuery {Source = source});
                vm.PageCount = vm.DocumentCount == 0 ? 1 : (int) Math.Ceiling(vm.DocumentCount / 10.0);
                vm.Page = page;
                vm.Documents = _documentRepository.GetAllAsync(AmbientContext, new DocumentQuery
                {
                    Source = source,
                    Page = page,
                    Limit = 10,
                    OrderBy = SortCriteria.DocumentDate
                }).ToEnumerable();
                
                vm.Subscribed =
                    await _sourceRepository.IsSubscribedAsync(AmbientContext, AmbientContext.CurrentUser,
                        source.SourceId);
                
                vm.Muted =
                    await _sourceRepository.IsMutedAsync(AmbientContext, AmbientContext.CurrentUser,
                        source.SourceId);

                vm.FirstDocument = source.Documents != null && source.Documents.Any()
                    ? source.Documents.Min(_ => _.DocumentDate)
                    : null;
                vm.LastDocument = source.Documents != null && source.Documents.Any()
                    ? source.Documents.Max(_ => _.DocumentDate)
                    : null;

                _logger.LogDebug("Country: " + source.Country);
                if (!string.IsNullOrEmpty(source.Country))
                    vm.Country = GeoHelpers.GetUNList().Where(_ => _.M49Code == source.Country);

                _logger.Log(LogLevel.Information,
                    EventIDs.DetailsSourceSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully viewed details of source '{source.Title}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddSource(source),
                    null,
                    LogEvent.Formatter);

                return View(vm);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DetailsSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view details of source '{url}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source.url", url),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DetailsSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view details of a non-existing source '{url}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source.url", url),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        public async Task<IActionResult> Create()
        {
            var currentUser = await GetCurrentUser();
            if (!await _appAuthorizationService.CanCreateSource(User, null))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.CreateSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new source without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            _logger.Log(LogLevel.Information,
                EventIDs.CreateSourceSuccessful,
                new LogEvent($"User '{currentUser.UserName}' requested page to create a new source.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return View(new Source());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Title", "Description", "HomePage", "RSSFeed", "Facebook", "Twitter", "LinkedIn", "Reddit", "Country", "Reliability")]
            Source submittedSource,
            [Bind(Prefix = "MetaData")] Dictionary<string, string> metadata,
            [Bind(Prefix = "logo")] IFormFile logo)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var source = new Source();

                source.Title = submittedSource.Title;
                source.Description = _sanitizer.Sanitize(submittedSource.Description);
                source.Reliability = submittedSource.Reliability;
                source.HomePage = submittedSource.HomePage;
                source.RSSFeed = submittedSource.RSSFeed;
                source.Facebook = submittedSource.Facebook;
                source.Twitter = submittedSource.Twitter;
                source.Reddit = submittedSource.Reddit;
                source.LinkedIn = submittedSource.LinkedIn;
                source.Country = submittedSource.Country;
                
                if (!string.IsNullOrEmpty(submittedSource.Keywords))
                    source.Keywords = string.Join(", ", submittedSource.Keywords.Split(",").Select(_ => _.Trim()));

                SaveLogo(logo, source);

                if (source.MetaData != null)
                    source.MetaData.Merge(JObject.FromObject(metadata));
                else
                    source.MetaData = JObject.FromObject(metadata);

                await _sourceRepository.CreateAsync(AmbientContext, source);
                await AmbientContext.DatabaseContext.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.CreateSourceSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully created a new source.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddSource(source),
                    null,
                    LogEvent.Formatter);

                return RedirectToAction(nameof(Details), new {id = source.SourceId});
            }
            catch (InvalidArgumentException e)
            {
                ModelState.Clear();
                foreach (var kv in e.Errors)
                foreach (var errorMessage in kv.Value)
                    ModelState.AddModelError(kv.Key, errorMessage);

                _logger.Log(LogLevel.Information,
                    EventIDs.CreateSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to creat a new source with an invalid model.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddSource(submittedSource),
                    null,
                    LogEvent.Formatter);

                return View(submittedSource);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.CreateSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new source without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var source = await _sourceRepository.GetAsync(AmbientContext, id);
                _logger.Log(LogLevel.Information,
                    EventIDs.EditSourceSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' requested to edit source '{source.Title}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddSource(source),
                    null,
                    LogEvent.Formatter);
                source.MetaData ??= new JObject();
                return View(source);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit source '{id}' without legitimate rights.")
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
                    EventIDs.EditSourceFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to edit a non-existing source '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            [Bind("SourceId",
                "Title",
                "Description",
                "HomePage",
                "RSSFeed",
                "Facebook",
                "Twitter",
                "LinkedIn",
                "Reddit",
                "Keywords",
                "Reliability",
                "Country")] Source submittedSource,
            [Bind(Prefix = "MetaData")] Dictionary<string, string> metadata,
            [Bind(Prefix = "logo")] IFormFile logo)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var source = await _sourceRepository.GetAsync(AmbientContext, submittedSource.SourceId);
                SaveLogo(logo, source);
                
                _logger.LogDebug("ModelState is " + ModelState.IsValid);

                if (ModelState.IsValid)
                {
                    source.Title = submittedSource.Title;
                    source.Description = _sanitizer.Sanitize(submittedSource.Description);

                    source.Reliability = submittedSource.Reliability;

                    source.HomePage = submittedSource.HomePage;
                    source.RSSFeed = submittedSource.RSSFeed;
                    source.Facebook = submittedSource.Facebook;
                    source.Twitter = submittedSource.Twitter;
                    source.Reddit = submittedSource.Reddit;
                    source.LinkedIn = submittedSource.LinkedIn;
                    source.Country = submittedSource.Country;

                    if (source.MetaData != null)
                        source.MetaData.Merge(JObject.FromObject(metadata));
                    else
                        source.MetaData = JObject.FromObject(metadata);

                    if (!string.IsNullOrEmpty(submittedSource.Keywords))
                        source.Keywords = string.Join(", ", submittedSource.Keywords.Split(",").Select(_ => _.Trim()));

                    await _sourceRepository.UpdateAsync(AmbientContext, source);
                    await AmbientContext.DatabaseContext.SaveChangesAsync();

                    _logger.Log(LogLevel.Information,
                        EventIDs.EditSourceSuccessful,
                        new LogEvent($"User '{currentUser.UserName}' successfully edited source '{source.Title}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddSource(source),
                        null,
                        LogEvent.Formatter);

                    return RedirectToAction(nameof(Details), new {id = source.SourceId});
                }

                throw new InvalidArgumentException(ModelState);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new source without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source.id", submittedSource.SourceId),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit a non-existing source '{submittedSource.SourceId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source.id", submittedSource.SourceId),
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
                    EventIDs.EditSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit source '{submittedSource.SourceId}' with an invalid model.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddSource(submittedSource),
                    null,
                    LogEvent.Formatter);

                return View(submittedSource);
            }
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var source = await _sourceRepository.GetAsync(AmbientContext, id);
                _logger.Log(LogLevel.Information,
                    EventIDs.DeleteSourceSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' requested to delete source '{source.Title}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddSource(source),
                    null,
                    LogEvent.Formatter);

                return View(source);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DeleteSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to delete source '{id}' without legitimate rights.")
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
                    EventIDs.DeleteSourceFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to edit a non-existing source '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source.id", id),
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
                await _sourceRepository.RemoveAsync(AmbientContext, id);
                await AmbientContext.DatabaseContext.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.DeleteSourceSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' deleted source '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source.id", id),
                    null,
                    LogEvent.Formatter);

                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DeleteSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to delete source '{id}' without legitimate rights.")
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
                    EventIDs.DeleteSourceFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to edit a non-existing source '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        public async Task<IActionResult> Merge(Guid? source1)
        {
            var currentUser = await GetCurrentUser();
            if (!await _appAuthorizationService.CanMergeSource(User, null))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DeleteSourceFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to merge source without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            _logger.Log(LogLevel.Information,
                EventIDs.DeleteSourceSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully requested to merge sources.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            if (source1 != default)
                ViewData["source1"] = await _sourceRepository.GetAsync(AmbientContext, (Guid) source1);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Merge(Guid source1, Guid source2)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                if (source1 == source2)
                    return RedirectToAction(nameof(Details), new {id = source1});

                await _sourceRepository.MergeAsync(AmbientContext, source1, source2);
                await AmbientContext.DatabaseContext.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.MergeSourceSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully merged source '{source1}' with '{source2}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source_primary.id", source1)
                        .AddProperty("source_secondary.id", source2),
                    null,
                    LogEvent.Formatter);

                return RedirectToAction("Details", new {id = source1});
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.MergeSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to merge source '{source1}' with '{source2}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source_primary.id", source1)
                        .AddProperty("source_secondary.id", source2),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.MergeSourceFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to merge a non-existing source '{source1}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        public async Task<IActionResult> LogoAsync(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var placeHolderImage = Path.Combine(_appSettings.StaticFiles, "images", "thumbnail-placeholder.png");
                var source = await _sourceRepository.GetAsync(AmbientContext, id);
                if (!string.IsNullOrEmpty(source.LogoFilename))
                {
                    var imageFolder = Path.Combine(_configuration.DocFolder, "images", "sources");
                    var file = Path.Combine(imageFolder, source.LogoFilename);
                    if (file.EndsWith(".jpg"))
                        return PhysicalFile(file, "image/jpg");
                    if (file.EndsWith(".png"))
                        return PhysicalFile(file, "image/png");
                }

                return PhysicalFile(placeHolderImage, "image/png");
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.LogoSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view logo of source '{id}' without legitimate rights.")
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
                    EventIDs.LogoSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view logo of a non-existing source '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source.id", id),
                    null,
                    LogEvent.Formatter);

                return PhysicalFile(Path.Combine(Directory.GetCurrentDirectory(),
                    "wwwroot/images/thumbnail-placeholder.png"), "image/png");
            }
        }

        [HttpGet("Source/Subscribe/{id}/{notification?}")]
        public async Task<IActionResult> Subscribe(Guid id, bool notification = false, string returnUrl = null)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                await _sourceRepository.SubscribeAsync(AmbientContext, AmbientContext.CurrentUser, id, notification);
                await AmbientContext.DatabaseContext.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.SubscribeSourceSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully subscribed to source '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source.id", id),
                    null,
                    LogEvent.Formatter);


                if (string.IsNullOrEmpty(returnUrl))
                    return RedirectToAction(nameof(Details), new {url = id});
                return Redirect(returnUrl);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.SubscribeSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to subscribe to source '{id}' without legitimate rights.")
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
                    EventIDs.SubscribeSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to subscribe to a non-existing source '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpGet("Source/Unsubscribe/{id}")]
        public async Task<IActionResult> Unsubscribe(Guid id, string returnUrl = null)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                await _sourceRepository.UnsubscribeAsync(AmbientContext, AmbientContext.CurrentUser, id);
                await AmbientContext.DatabaseContext.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.SubscribeSourceSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully unsubscribed to source '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source.id", id),
                    null,
                    LogEvent.Formatter);


                if (string.IsNullOrEmpty(returnUrl))
                    return RedirectToAction(nameof(Details), new {url = id});
                return Redirect(returnUrl);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.SubscribeSourceFailed,
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
                    EventIDs.SubscribeSourceFailed,
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
        
        [HttpGet("Source/Mute/{id}")]
        public async Task<IActionResult> Mute(Guid id, string returnUrl = null)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var source = await _sourceRepository.GetAsync(AmbientContext, id);
                await _sourceRepository.MuteAsync(AmbientContext, AmbientContext.CurrentUser, source.SourceId);
                await AmbientContext.DatabaseContext.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.SubscribeTagSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully muted source '{source.Title}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddSource(source),
                    null,
                    LogEvent.Formatter);


                if (string.IsNullOrEmpty(returnUrl))
                    return RedirectToAction(nameof(Details), new {url = source.SourceId});
                return Redirect(returnUrl);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.SubscribeTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to mute source '{id}' without legitimate rights.")
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
                    new LogEvent($"User '{currentUser.UserName}' attempted to mute a non-existing source '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("tag.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpGet("Source/Unmute/{id}")]
        public async Task<IActionResult> Unmute(Guid id, string returnUrl = null)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var source = await _sourceRepository.GetAsync(AmbientContext, id);
                await _sourceRepository.UnmuteAsync(AmbientContext, AmbientContext.CurrentUser, source.SourceId);
                await AmbientContext.DatabaseContext.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.SubscribeTagSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully muted tag '{source.Title}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddSource(source),
                    null,
                    LogEvent.Formatter);

                if (string.IsNullOrEmpty(returnUrl))
                    return RedirectToAction(nameof(Details), new {url = source.SourceId});
                return Redirect(returnUrl);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.SubscribeTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to unmute source '{id}' without legitimate rights.")
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
                    EventIDs.SubscribeTagFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to unmute a non-existing source '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        #region Helpers

        private void SaveLogo(IFormFile logo, Source source)
        {
            if (logo != null && logo.Length > 0)
            {
                _logger.LogDebug("Saving logo for " + source.SourceId);
                _logger.LogDebug("Content-type: " + logo.ContentType);
                var imageFolder = Path.Combine(_configuration.DocFolder, "images", "sources");
                if (!Directory.Exists(imageFolder)) Directory.CreateDirectory(imageFolder);
                source.LogoFilename = $"source-{source.SourceId}-logo";

                bool error = false;
                if (logo.ContentType == "image/png")
                {
                    source.LogoFilename += ".png";
                }
                else if (logo.ContentType == "image/jpg")
                {
                    source.LogoFilename += ".jpg";
                }
                else if (logo.ContentType == "image/jpeg")
                {
                    source.LogoFilename += ".jpg";
                }
                else
                {
                    error = true;
                    ModelState.AddModelError("logo", "Select a PNG or JPG file.");
                }
                _logger.LogDebug(source.LogoFilename);

                if (!error)
                {
                    _logger.LogDebug("Logo is valid " + source.SourceId);
                    var filePath = Path.Combine(imageFolder, source.LogoFilename);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        logo.CopyTo(stream);
                    }
                    Resize(filePath, 280);
                }
            }
            else
            {
                _logger.LogDebug("No logo for " + source.SourceId);
            }
        }

        private void Resize(string filePath, int size)
        {
            float ratio = .25f;
            using var image = Image.Load<Rgba32>(filePath);
            image.Mutate(x => x.EntropyCrop());

            Color topColor = Color.White;
            image.ProcessPixelRows(accessor =>
            {
                Span<Rgba32> pixelRow = accessor.GetRowSpan(0);
                topColor = pixelRow.ToArray().GroupBy(_ => _).OrderBy(_ => _.Count()).Select(_ => _.Key).First();
            });
            
            // var topColor = image.GetPixelRowSpan(0).ToArray().GroupBy(_ => _).Select(_ => _.Key).First();
            image.Mutate(x => x.Resize(new ResizeOptions()
            {
                Size = new Size((int) (size - size * ratio), (int) (size * 2 / 3f - size * 2 / 3f * ratio)),
                Mode = ResizeMode.Max
            }));
            image.Mutate(x => x.Resize(new ResizeOptions()
                            {
                                Size = new Size(size, (int) (size * 2 / 3f) ),
                                Mode = ResizeMode.BoxPad
                            })
                                .BackgroundColor(topColor)
                            );
            image.Save(filePath);
            _logger.LogDebug("Saved at " + filePath);
        }

        #endregion
    }
}