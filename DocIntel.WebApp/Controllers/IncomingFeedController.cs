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
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Helpers;
using DocIntel.Core.Importers;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;
using DocIntel.WebApp.Helpers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocIntel.WebApp.Controllers
{
    /// <summary>
    ///     Provides the functionalities for managing incoming feeds.
    /// </summary>
    public class IncomingFeedController : BaseController
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IAppAuthorizationService _appAuthorizationService;
        private readonly IClassificationRepository _classificationRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly IIncomingFeedRepository _incomingFeedRepository;
        private readonly ITagRepository _tagRepository;
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ApplicationSettings _settings;

        public IncomingFeedController(IAppAuthorizationService appAuthorizationService,
            DocIntelContext context,
            ILogger<DocumentController> logger,
            ApplicationSettings configuration,
            UserManager<AppUser> userManager,
            IAuthorizationService authorizationService,
            IIncomingFeedRepository incomingFeedRepository,
            IHttpContextAccessor accessor, IServiceProvider serviceProvider,
            IClassificationRepository classificationRepository, ApplicationSettings settings,
            IGroupRepository groupRepository, ITagRepository tagRepository)
            : base(context,
                userManager,
                configuration,
                authorizationService)
        {
            _logger = logger;
            _appAuthorizationService = appAuthorizationService;
            _incomingFeedRepository = incomingFeedRepository;
            _accessor = accessor;
            _serviceProvider = serviceProvider;
            _classificationRepository = classificationRepository;
            _settings = settings;
            _groupRepository = groupRepository;
            _tagRepository = tagRepository;
        }

        /// <summary>
        ///     Provides the listing for the incoming feeds.
        /// </summary>
        /// <returns>
        ///     A view listing the incoming feeds. An "Unauthorized" response if
        ///     the user does not have the appropriate rights.
        /// </returns>
        [HttpGet("IncomingFeed")]
        [HttpGet("IncomingFeed/Index")]
        public async Task<IActionResult> Index()
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var enumerable = _incomingFeedRepository.GetAllAsync(
                    AmbientContext);

                _logger.Log(LogLevel.Information, EventIDs.ListIncomingFeedSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully list incoming feeds.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return View(await enumerable.ToListAsync());
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.ListIncomingFeedFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to list incoming feeds without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
        }

        /// <summary>
        ///     Provides a view to display the detail of an incoming feed.
        /// </summary>
        /// <param name="id">
        ///     The identifier of the incoming feed to display details of.
        /// </param>
        /// <returns>
        ///     A view for displaying details of the incoming feed. A "Not Found"
        ///     response if the feed does not exists. A "Unauthorized" response
        ///     if the user does not have the appropriate rights.
        /// </returns>
        [HttpGet("IncomingFeed/Details/{id}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var incomingFeed = await _incomingFeedRepository.GetAsync(
                    AmbientContext,
                    id,
                    _ => _.AsNoTracking()
                        .Include(__ => __.Classification)
                        .Include(__ => __.Source)
                        .Include(__ => __.Tags).ThenInclude(__ => __.Facet)
                        .Include(__ => __.ReleasableTo)
                        .Include(__ => __.EyesOnly));

                await InitializeViewBag(id, incomingFeed, currentUser);

                _logger.Log(LogLevel.Information, EventIDs.DetailsIncomingFeedSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully requested the view for the incoming feed '{incomingFeed.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddIncomingFeed(incomingFeed),
                    null,
                    LogEvent.Formatter);

                return View(incomingFeed);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.DetailsIncomingFeedFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view details of incoming feed '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.DetailsIncomingFeedFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view details of a non-existing incoming feed '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        /// <summary>
        ///     Provides a view to create a new incoming feed.
        /// </summary>
        /// <returns>
        ///     A view for creating details of the incoming feed. A "Not Found"
        ///     response if the feed does not exists. A "Unauthorized" response
        ///     if the user does not have the appropriate rights.
        /// </returns>
        [HttpGet("IncomingFeed/Create")]
        public async Task<IActionResult> Create()
        {
            var currentUser = await GetCurrentUser();

            if (!await _appAuthorizationService.CanCreateIncomingFeed(User, null))
            {
                _logger.Log(LogLevel.Warning, EventIDs.CreateIncomingFeedFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create an incoming feed without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            await InitializeViewBag(currentUser);

            _logger.Log(LogLevel.Information, EventIDs.CreateIncomingFeedSuccess,
                new LogEvent(
                        $"User '{currentUser.UserName}' successfully requested the view to create an incoming feed.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return View(new Importer());
        }

        private async Task InitializeViewBag(AppUser currentUser)
        {
            var installedImporters = await _incomingFeedRepository.GetAllAsync(AmbientContext).Select(_ => _.ImporterId)
                .ToArrayAsync();
            // Search for classes with Scrapers
            var types = ImporterFactory.GetAllImporters();

            ViewBag.Importers = types.Select(type =>
                    ImporterFactory.CreateImporter(type, _serviceProvider, AmbientContext).Result)
                .Where(instance => instance != null)
                .Select(instance => instance.Get())
                .ToList();

            ViewBag.BotUsers = _userManager.Users.AsNoTracking().Where(_ => _.Bot).ToList();

            ViewBag.AllClassifications = await _classificationRepository.GetAllAsync(AmbientContext).ToListAsync();
            var allGroups = await _groupRepository.GetAllAsync(AmbientContext).ToListAsync();
            ViewBag.AllGroups = allGroups;
            
            // TODO Use user claims instead
            ViewBag.OwnGroups = allGroups.Where(_ =>
                currentUser.Memberships.Any(__ => __.GroupId == _.GroupId));
        }

        /// <summary>
        ///     Creates the specified incoming feed.
        /// </summary>
        /// <param name="submittedImporter">
        ///     The feed identifier and <i>enabled</i> flag for the feed to update.
        /// </param>
        /// <returns>
        ///     A redirection to the incoming feed detail page if the edit was
        ///     successful. A "Not Found" response if the feed does not exists. A
        ///     "Unauthorized" response if the user does not have the appropriate
        ///     rights.
        /// </returns>
        [HttpPost("IncomingFeed/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind(
                "Status,Name,Description,CollectionDelay,FetchingUserId,Limit,OverrideClassification,OverrideReleasableTo,OverrideEyesOnly,ClassificationId,SourceId,OverrideSource")]
            Importer submittedImporter,
            [Bind(Prefix = "releasableTo")] Guid[] releasableTo,
            [Bind(Prefix = "eyesOnly")] Guid[] eyesOnly,
            [Bind(Prefix = "tags")] Guid[] tags,
            [Bind(Prefix = "Importer")] string importer)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                if (ModelState.IsValid)
                {
                    var instance =
                        await ImporterFactory.CreateImporter(Guid.Parse(importer), _serviceProvider, AmbientContext);

                    var incomingFeed = instance.Install();
                    if (!string.IsNullOrEmpty(submittedImporter.Name))
                        incomingFeed.Name = submittedImporter.Name;
                    if (!string.IsNullOrEmpty(submittedImporter.Description))
                        incomingFeed.Description = submittedImporter.Description;
                    incomingFeed.Status = submittedImporter.Status;
                    incomingFeed.CollectionDelay = submittedImporter.CollectionDelay;
                    incomingFeed.FetchingUserId = submittedImporter.FetchingUserId;
                    incomingFeed.Limit = submittedImporter.Limit;
                    incomingFeed.SkipInbox = submittedImporter.SkipInbox;
                    incomingFeed.OverrideClassification = submittedImporter.OverrideClassification;
                    incomingFeed.OverrideReleasableTo = submittedImporter.OverrideReleasableTo;
                    incomingFeed.OverrideEyesOnly = submittedImporter.OverrideEyesOnly;
                    incomingFeed.ClassificationId = submittedImporter.ClassificationId;
                    incomingFeed.OverrideSource = submittedImporter.OverrideSource;
                    incomingFeed.SourceId = submittedImporter.SourceId;
                    await UpdateTags(tags, incomingFeed);
                    
                    var filteredRelTo = await _groupRepository
                        .GetAllAsync(AmbientContext, new GroupQuery {Id = releasableTo}).ToListAsync();
                    var filteredEyes = await _groupRepository
                        .GetAllAsync(AmbientContext, new GroupQuery {Id = eyesOnly}).ToListAsync();

                    incomingFeed.ReleasableTo = filteredRelTo;
                    incomingFeed.EyesOnly = filteredEyes;

                    incomingFeed = await _incomingFeedRepository.CreateAsync(
                        AmbientContext,
                        incomingFeed);

                    await _context.SaveChangesAsync();

                    _logger.Log(LogLevel.Information, EventIDs.CreateIncomingFeedSuccess,
                        new LogEvent(
                                $"User '{currentUser.UserName}' successfully created the incoming feed '{incomingFeed.Name}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddIncomingFeed(incomingFeed),
                        null,
                        LogEvent.Formatter);

                    if (instance.HasSettings)
                    {
                        return RedirectToAction(
                            nameof(Configure),
                            new { id = incomingFeed.ImporterId });
                    }

                    return RedirectToAction(
                        nameof(Details),
                        new { id = incomingFeed.ImporterId });
                }

                return View(submittedImporter);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.CreateIncomingFeedSuccess,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create incoming feed without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddIncomingFeed(submittedImporter),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (InvalidArgumentException e)
            {
                _logger.Log(LogLevel.Information, EventIDs.EditIncomingFeedSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create an incoming feed with an invalid model or empty JSON.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddIncomingFeed(submittedImporter),
                    null,
                    LogEvent.Formatter);

                ModelState.Clear();
                foreach (var kv in e.Errors)
                foreach (var errorMessage in kv.Value)
                    ModelState.AddModelError(kv.Key, errorMessage);
                await InitializeViewBag(currentUser);

                return View(submittedImporter);
            }
        }

        /// <summary>
        ///     Provides a view to edit the incoming feed.
        /// </summary>
        /// <param name="id">
        ///     The identifier of the feed to edit.
        /// </param>
        /// <returns>
        ///     A view for editing details of the incoming feed. A "Not Found"
        ///     response if the feed does not exists. A "Unauthorized" response
        ///     if the user does not have the appropriate rights.
        /// </returns>
        [HttpGet("IncomingFeed/Edit/{id}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var incomingFeed = await _incomingFeedRepository.GetAsync(
                    AmbientContext,
                    id,
                    __ => __.AsNoTracking()
                        .Include(_ => _.Classification)
                        .Include(_ => _.Source)
                        .Include(_ => _.ReleasableTo)
                        .Include(_ => _.EyesOnly)
                        .Include(_ => _.Tags).ThenInclude(_ => _.Facet));

                if (incomingFeed.Settings != null) ViewData["settings"] = incomingFeed.Settings.ToString();

                ViewData["DefaultClassification"] = _classificationRepository.GetDefault(AmbientContext);

                _logger.Log(LogLevel.Information, EventIDs.EditIncomingFeedSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully requested the view to edit the incoming feed '{incomingFeed.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddIncomingFeed(incomingFeed),
                    null,
                    LogEvent.Formatter);

                await InitializeViewBag(id, incomingFeed, currentUser);

                return View(incomingFeed);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditIncomingFeedFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit details of incoming feed '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditIncomingFeedFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit details of a non-existing incoming feed '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        private async Task InitializeViewBag(Guid id, Importer incomingFeed, AppUser currentUser)
        {
            var instance = await ImporterFactory.CreateImporter(incomingFeed, _serviceProvider, AmbientContext);

            ViewBag.HasSettings = instance.HasSettings;
            ViewBag.View = instance.GetSettingsView();
            ViewBag.Schema = instance.GetSettingsSchema();
            ViewBag.Settings = incomingFeed.Settings?.Deserialize(instance.GetSettingsType());

            await InitializeViewBag(currentUser);
        }

        /// <summary>
        ///     Updates the details for the specified incoming feed.
        /// </summary>
        /// <param name="id">
        ///     The identifier of the feed to edit.
        /// </param>
        /// <param name="submittedImporter">
        ///     The feed identifier and <i>enabled</i> flag for the feed to update.
        /// </param>
        /// <returns>
        ///     A redirection to the incoming feed detail page if the edit was
        ///     successful. A "Not Found" response if the feed does not exists. A
        ///     "Unauthorized" response if the user does not have the appropriate
        ///     rights.
        /// </returns>
        [HttpPost("IncomingFeed/Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Guid id,
            [FromForm] Importer submittedImporter,
            [Bind(Prefix = "releasableTo")] Guid[] releasableTo,
            [Bind(Prefix = "eyesOnly")] Guid[] eyesOnly,
            [Bind(Prefix = "tags")] Guid[] tags
        )
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var incomingFeed = await _incomingFeedRepository.GetAsync(
                    AmbientContext,
                    submittedImporter.ImporterId,
                    _ => _.Include(__ => __.Tags).ThenInclude(__ => __.Facet)
                        .Include(__ => __.Classification)
                        .Include(__ => __.ReleasableTo)
                        .Include(__ => __.EyesOnly));

                try
                {
                    if (ModelState.IsValid)
                    {
                        incomingFeed.Name = submittedImporter.Name;
                        incomingFeed.Description = submittedImporter.Description;
                        incomingFeed.Status = submittedImporter.Status;
                        incomingFeed.CollectionDelay = submittedImporter.CollectionDelay;
                        incomingFeed.FetchingUserId = submittedImporter.FetchingUserId;
                        incomingFeed.Limit = submittedImporter.Limit;
                        incomingFeed.SkipInbox = submittedImporter.SkipInbox;
                        incomingFeed.OverrideClassification = submittedImporter.OverrideClassification;
                        incomingFeed.OverrideReleasableTo = submittedImporter.OverrideReleasableTo;
                        incomingFeed.OverrideEyesOnly = submittedImporter.OverrideEyesOnly;
                        incomingFeed.ClassificationId = submittedImporter.ClassificationId;
                        incomingFeed.OverrideSource = submittedImporter.OverrideSource;
                        incomingFeed.SourceId = submittedImporter.SourceId;
                        await UpdateTags(tags, incomingFeed);

                        var filteredRelTo = await _groupRepository
                            .GetAllAsync(AmbientContext, new GroupQuery {Id = releasableTo}).ToListAsync();
                        var filteredEyes = await _groupRepository
                            .GetAllAsync(AmbientContext, new GroupQuery {Id = eyesOnly}).ToListAsync();

                        incomingFeed.ReleasableTo = filteredRelTo;
                        incomingFeed.EyesOnly = filteredEyes;

                        await _incomingFeedRepository.UpdateAsync(
                            AmbientContext,
                            incomingFeed);

                        await _context.SaveChangesAsync();

                        _logger.Log(LogLevel.Information, EventIDs.EditIncomingFeedSuccessful,
                            new LogEvent(
                                    $"User '{currentUser.UserName}' successfully edited the incoming feed '{incomingFeed.Name}'.")
                                .AddUser(currentUser)
                                .AddHttpContext(_accessor.HttpContext)
                                .AddIncomingFeed(incomingFeed),
                            null,
                            LogEvent.Formatter);

                        return RedirectToAction(
                            nameof(Details),
                            new {id = incomingFeed.ImporterId});
                    }

                    await InitializeViewBag(id, incomingFeed, currentUser);

                    return View(submittedImporter);
                }
                catch (InvalidArgumentException e)
                {
                    _logger.Log(LogLevel.Information, EventIDs.EditIncomingFeedSuccessful,
                        new LogEvent(
                                $"User '{currentUser.UserName}' attempted to edit the incoming feed '{id}' with an invalid model or empty JSON.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddProperty("incoming_feed.id", id),
                        null,
                        LogEvent.Formatter);

                    ModelState.Clear();
                    foreach (var kv in e.Errors)
                    foreach (var errorMessage in kv.Value)
                        ModelState.AddModelError(kv.Key, errorMessage);

                    await InitializeViewBag(id, incomingFeed, currentUser);

                    return View(submittedImporter);
                }
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditIncomingFeedFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit details of incoming feed '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditIncomingFeedFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit details of a non-existing incoming feed '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        private async Task UpdateTags(Guid[] tags, Importer incomingFeed)
        {
            if (tags == null || !tags.Any())
            {
                _logger.LogDebug("Clear all tags");
                incomingFeed.Tags?.Clear();
                return;
            }

            var listAsync = await _tagRepository.GetAllAsync(AmbientContext, new TagQuery()
            {
                Ids = tags, Limit = -1
            }).ToListAsync();
            
            var removedTags = incomingFeed.Tags?.Except(listAsync, _ => _.TagId).ToArray() ?? Enumerable.Empty<Tag>();
            var newTags = listAsync?.Except(incomingFeed.Tags, _ => _.TagId).ToArray() ?? Enumerable.Empty<Tag>();
            
            if (incomingFeed.Tags == null)
                incomingFeed.Tags = new List<Tag>();
            
            foreach (var newTag in newTags)
            {
                incomingFeed.Tags.Add(newTag);
                _logger.LogTrace("Add '" + newTag.FriendlyName + "' tag to importer");
            }

            foreach (var removeTag in removedTags)
            {
                incomingFeed.Tags.Remove(removeTag);
                _logger.LogTrace("Remove '" + removeTag.FriendlyName+ "' tag from importer");
            }
        }

        [HttpGet("IncomingFeed/Configure/{id}")]
        public async Task<IActionResult> Configure(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var incomingFeed = await _incomingFeedRepository.GetAsync(
                    AmbientContext,
                    id,
                    __ => __.Include(_ => _.Classification).Include(_ => _.ReleasableTo).Include(_ => _.EyesOnly));

                var instance = await ImporterFactory.CreateImporter(incomingFeed, _serviceProvider, AmbientContext);
                if (!instance.HasSettings)
                    return RedirectToAction((nameof(Details)), new { id });
                
                _logger.Log(LogLevel.Information, EventIDs.EditIncomingFeedSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully requested the view to edit the incoming feed '{incomingFeed.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddIncomingFeed(incomingFeed),
                    null,
                    LogEvent.Formatter);

                await InitializeViewBag(id, incomingFeed, currentUser);

                return View(incomingFeed);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditIncomingFeedFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit details of incoming feed '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditIncomingFeedFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit details of a non-existing incoming feed '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpPost("IncomingFeed/Configure/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Configure(
            Guid id,
            [Bind("ImporterId")] Importer submittedImporter,
            [FromForm(Name="Settings")] string settings
        )
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var incomingFeed = await _incomingFeedRepository.GetAsync(
                    AmbientContext,
                    submittedImporter.ImporterId,
                    _ => _.Include(__ => __.ReleasableTo).Include(__ => __.EyesOnly));

                var instance = await ImporterFactory.CreateImporter(incomingFeed, _serviceProvider, AmbientContext);
                if (!instance.HasSettings)
                    return RedirectToAction((nameof(Details)), new { id });
                
                var json = JsonObject.Parse("{}");
                if (settings != null)
                    try
                    {
                        json = JsonObject.Parse(settings);
                    }
                    catch (Exception e)
                    {
                        ModelState.AddModelError("Settings", "The provided JSON is invalid.");
                        _logger.Log(LogLevel.Error, EventIDs.EditIncomingFeedError,
                            new LogEvent($"User '{currentUser.UserName}' provided an invalid JSON.")
                                .AddUser(currentUser)
                                .AddHttpContext(_accessor.HttpContext)
                                .AddIncomingFeed(incomingFeed)
                                .AddException(e),
                            e,
                            LogEvent.Formatter);
                    }

                try
                {
                    if (ModelState.IsValid)
                    {
                        incomingFeed.Settings = json.AsObject();

                        await _incomingFeedRepository.UpdateAsync(
                            AmbientContext,
                            incomingFeed);

                        await _context.SaveChangesAsync();

                        _logger.Log(LogLevel.Information, EventIDs.EditIncomingFeedSuccessful,
                            new LogEvent(
                                    $"User '{currentUser.UserName}' successfully edited the incoming feed '{incomingFeed.Name}'.")
                                .AddUser(currentUser)
                                .AddHttpContext(_accessor.HttpContext)
                                .AddIncomingFeed(incomingFeed),
                            null,
                            LogEvent.Formatter);

                        return RedirectToAction(
                            nameof(Details),
                            new {id = incomingFeed.ImporterId});
                    }

                    await InitializeViewBag(id, incomingFeed, currentUser);

                    return View(submittedImporter);
                }
                catch (InvalidArgumentException e)
                {
                    _logger.Log(LogLevel.Information, EventIDs.EditIncomingFeedSuccessful,
                        new LogEvent(
                                $"User '{currentUser.UserName}' attempted to edit the incoming feed '{id}' with an invalid model or empty JSON.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddProperty("incoming_feed.id", id),
                        null,
                        LogEvent.Formatter);

                    ModelState.Clear();
                    foreach (var kv in e.Errors)
                    foreach (var errorMessage in kv.Value)
                        ModelState.AddModelError(kv.Key, errorMessage);

                    await InitializeViewBag(id, incomingFeed, currentUser);

                    return View(submittedImporter);
                }
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditIncomingFeedFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit details of incoming feed '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditIncomingFeedFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit details of a non-existing incoming feed '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpGet("IncomingFeed/Uninstall/{id}")]
        public async Task<IActionResult> Uninstall(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var incomingFeed = await _incomingFeedRepository.GetAsync(
                    AmbientContext,
                    id);

                _logger.Log(LogLevel.Information, EventIDs.EditIncomingFeedSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully requested the view to uninstall the incoming feed '{incomingFeed.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddIncomingFeed(incomingFeed),
                    null,
                    LogEvent.Formatter);

                return View(incomingFeed);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditIncomingFeedFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to uninstall incoming feed '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditIncomingFeedFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to uninstall a non-existing incoming feed '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpPost("IncomingFeed/Uninstall/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, [Bind("ImporterId")] Importer viewModel)
        {
            var currentUser = await GetCurrentUser();
            Importer importer;
            if ((importer = await _incomingFeedRepository.GetAsync(AmbientContext, id)) == null)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditRoleFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to uninstall a non-existing feed '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("feed.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }

            if (!await _appAuthorizationService.CanDeleteIncomingFeed(User, importer))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditRoleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to uninstall feed '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("feed.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            _logger.Log(LogLevel.Information,
                EventIDs.DeleteRoleSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully uninstalled feed '{id}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddIncomingFeed(importer),
                null,
                LogEvent.Formatter);

            await _incomingFeedRepository.RemoveAsync(AmbientContext, importer.ImporterId);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}