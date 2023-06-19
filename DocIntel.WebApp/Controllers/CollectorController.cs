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
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AutoMapper;
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Helpers;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Modules;
using DocIntel.Core.Settings;
using DocIntel.WebApp.Helpers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DocIntel.WebApp.Controllers
{
    /// <summary>
    ///     Provides the functionalities for managing collectors.
    /// </summary>
    public class CollectorController : BaseController
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IAppAuthorizationService _appAuthorizationService;
        private readonly IClassificationRepository _classificationRepository;

        private readonly IGroupRepository _groupRepository;
        private readonly ILogger _logger;
        private readonly ICollectorRepository _collectorRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly ModuleFactory _moduleFactory;

        public CollectorController(IAppAuthorizationService appAuthorizationService,
            DocIntelContext context,
            ILogger<CollectorController> logger,
            ApplicationSettings configuration,
            UserManager<AppUser> userManager,
            IAuthorizationService authorizationService,
            ICollectorRepository collectorRepository,
            IHttpContextAccessor accessor, IServiceProvider serviceProvider,
            IImportRuleRepository importRulesRepository, IGroupRepository groupRepository, ApplicationSettings setting,
            IClassificationRepository classificationRepository, ITagRepository tagRepository, ModuleFactory moduleFactory)
            : base(context,
                userManager,
                configuration,
                authorizationService)
        {
            _logger = logger;
            _appAuthorizationService = appAuthorizationService;
            _collectorRepository = collectorRepository;
            _accessor = accessor;
            _serviceProvider = serviceProvider;
            _groupRepository = groupRepository;
            _classificationRepository = classificationRepository;
            _tagRepository = tagRepository;
            _moduleFactory = moduleFactory;
        }

        /// <summary>
        ///     Provides the listing for the collectors.
        /// </summary>
        /// <returns>
        ///     A view listing the collectors. An "Unauthorized" response if
        ///     the user does not have the appropriate rights.
        /// </returns>
        [HttpGet("Collector")]
        [HttpGet("Collector/Index")]
        public async Task<IActionResult> Index()
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var enumerable = _collectorRepository.GetAllAsync(
                    AmbientContext);

                _logger.Log(LogLevel.Information,
                    EventIDs.ListCollectorSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully list collectors.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return View(await enumerable.ToListAsync());
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.ListCollectorFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to list collectors without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
        }

        /// <summary>
        ///     Provides a view to display the detail of an collector.
        /// </summary>
        /// <param name="id">
        ///     The identifier of the collector to display details of.
        /// </param>
        /// <returns>
        ///     A view for displaying details of the collector. A "Not Found"
        ///     response if the feed does not exists. A "Unauthorized" response
        ///     if the user does not have the appropriate rights.
        /// </returns>
        [HttpGet("Collector/Details/{id}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var collector = await _collectorRepository.GetAsync(
                    AmbientContext,
                    id,
                    _ => _.AsNoTracking()
                        .Include(__ => __.Classification)
                        .Include(__ => __.Tags).ThenInclude(__ => __.Facet)
                        .Include(__ => __.ReleasableTo)
                        .Include(__ => __.EyesOnly)
                        .Include(__ => __.Source));

                await InitializeViewBag(id, collector, currentUser);

                _logger.Log(LogLevel.Information,
                    EventIDs.DetailsCollectorSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully requested the view for the collector '{collector.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddCollector(collector),
                    null,
                    LogEvent.Formatter);

                return View(collector);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DetailsCollectorFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view details of collector '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("collector.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DetailsCollectorFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view details of a non-existing collector '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("collector.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        /// <summary>
        ///     Provides a view to create a new collector.
        /// </summary>
        /// <returns>
        ///     A view for creating details of the collector. A "Not Found"
        ///     response if the feed does not exists. A "Unauthorized" response
        ///     if the user does not have the appropriate rights.
        /// </returns>
        [HttpGet("Collector/Create/")]
        public async Task<IActionResult> Create()
        {
            var currentUser = await GetCurrentUser();

            if (!await _appAuthorizationService.CanCreateCollector(User, null))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.CreateCollectorFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create an collector without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            await InitializeViewBag(currentUser);

            _logger.Log(LogLevel.Information,
                EventIDs.CreateCollectorSuccess,
                new LogEvent($"User '{currentUser.UserName}' successfully requested the view to create an collector.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            var defaultClassification = _classificationRepository.GetDefault(AmbientContext);
            var c = new Collector()
            {
                Classification = defaultClassification,
                ClassificationId = defaultClassification.ClassificationId,
            };
            return View(c);
        }

        private async Task InitializeViewBag(AppUser currentUser)
        {
            ViewBag.DefaultClassification = _classificationRepository.GetDefault(AmbientContext);
            ViewBag.AllClassifications = AmbientContext.DatabaseContext.Classifications.ToList();
            var allGroups = await _groupRepository.GetAllAsync(AmbientContext).ToListAsync();
            ViewBag.AllGroups = allGroups;
            ViewBag.OwnGroups = allGroups.Where(_ =>
                currentUser.Memberships.Any(__ => __.GroupId == _.GroupId));

            // Search for classes with Collectors
            var types = _moduleFactory.GetAll().Where(_ => (_.Collectors?.Count ?? -1) > 0);

            ViewBag.Collectors = types.SelectMany(m => m.Collectors.Select(c => m.Name + "." + c.Key))
                .ToList();

            ViewBag.BotUsers = _userManager.Users.AsNoTracking().Where(_ => _.Bot).ToList();
        }

        /// <summary>
        ///     Creates the specified collector.
        /// </summary>
        /// <param name="submittedCollector">
        ///     The feed identifier and <i>enabled</i> flag for the feed to update.
        /// </param>
        /// <returns>
        ///     A redirection to the collector detail page if the edit was
        ///     successful. A "Not Found" response if the feed does not exists. A
        ///     "Unauthorized" response if the user does not have the appropriate
        ///     rights.
        /// </returns>
        [HttpPost("Collector/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [FromForm] Collector submittedCollector,
            [Bind(Prefix = "releasableTo")] Guid[] releasableTo,
            [Bind(Prefix = "eyesOnly")] Guid[] eyesOnly,
            [Bind(Prefix = "tags")] Guid[] tags,
            [Bind(Prefix = "Collector")] string collectorId)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                if (ModelState.IsValid)
                {
                    var collector = new Collector(); 

                    var mod = collectorId.Split('.', 2)[0];
                    var colname = collectorId.Split('.', 2)[1];
                    collector.Module = mod;
                    collector.CollectorName = colname;
                    
                    if (!string.IsNullOrEmpty(submittedCollector.Name))
                        collector.Name = submittedCollector.Name;
                    if (!string.IsNullOrEmpty(submittedCollector.Description))
                        collector.Description = submittedCollector.Description;
                    
                    collector.Enabled = submittedCollector.Enabled;
                    collector.SkipInbox = submittedCollector.SkipInbox;
                    collector.Limit = submittedCollector.Limit;
                    collector.CronExpression = submittedCollector.CronExpression;
                    collector.SourceId = submittedCollector.SourceId;
                    collector.ClassificationId = submittedCollector.ClassificationId;
                    await UpdateTags(tags, collector);

                    var filteredRelTo = await _groupRepository
                        .GetAllAsync(AmbientContext, new GroupQuery {Id = releasableTo}).ToListAsync();
                    var filteredEyes = await _groupRepository
                        .GetAllAsync(AmbientContext, new GroupQuery {Id = eyesOnly}).ToListAsync();

                    collector.ReleasableTo = filteredRelTo;
                    collector.EyesOnly = filteredEyes;

                    collector.UserId = collector.UserId;
                    
                    collector = await _collectorRepository.CreateAsync(
                        AmbientContext,
                        collector);

                    await _context.SaveChangesAsync();

                    _logger.Log(LogLevel.Information,
                        EventIDs.CreateCollectorSuccess,
                        new LogEvent(
                                $"User '{currentUser.UserName}' successfully created the collector '{collector.Name}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddCollector(collector),
                        null,
                        LogEvent.Formatter);
                    
                    if (_moduleFactory.HasCollectorSettings(collector.Module, collector.CollectorName))
                        return RedirectToAction(
                            nameof(Configure),
                            new {id = collector.CollectorId});
                    else
                        return RedirectToAction(
                            nameof(Details),
                            new {id = collector.CollectorId});
                }

                return View(submittedCollector);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.CreateCollectorSuccess,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create collector without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddCollector(submittedCollector),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (InvalidArgumentException e)
            {
                _logger.Log(LogLevel.Information,
                    EventIDs.EditCollectorSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create an collector with an invalid model or empty JSON.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddCollector(submittedCollector),
                    null,
                    LogEvent.Formatter);

                ModelState.Clear();
                foreach (var kv in e.Errors)
                foreach (var errorMessage in kv.Value)
                    ModelState.AddModelError(kv.Key, errorMessage);
                await InitializeViewBag(currentUser);

                return View(submittedCollector);
            }
        }

        /// <summary>
        ///     Provides a view to edit the collector.
        /// </summary>
        /// <param name="id">
        ///     The identifier of the feed to edit.
        /// </param>
        /// <returns>
        ///     A view for editing details of the collector. A "Not Found"
        ///     response if the feed does not exists. A "Unauthorized" response
        ///     if the user does not have the appropriate rights.
        /// </returns>
        [HttpGet("Collector/Edit/{id}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var collector = await _collectorRepository.GetAsync(
                    AmbientContext,
                    id,
                    _ => _.AsNoTracking()
                        .Include(__ => __.Classification)
                        .Include(__ => __.Tags).ThenInclude(__ => __.Facet)
                        .Include(__ => __.ReleasableTo)
                        .Include(__ => __.EyesOnly)
                        .Include(__ => __.Source));

                if (collector.Settings != null) ViewData["settings"] = collector.Settings.ToString();

                _logger.Log(LogLevel.Information,
                    EventIDs.EditCollectorSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully requested the view to edit the collector '{collector.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddCollector(collector),
                    null,
                    LogEvent.Formatter);

                await InitializeViewBag(id, collector, currentUser);

                return View(collector);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditCollectorFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit details of collector '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("collector.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditCollectorFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit details of a non-existing collector '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("collector.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        private async Task InitializeViewBag(Guid id, Collector collector, AppUser currentUser)
        {
            ViewBag.HasSettings = _moduleFactory.HasCollectorSettings(collector.Module, collector.CollectorName);
            var settingsType = _moduleFactory.GetCollectorSettings(collector.Module, collector.CollectorName);
            ViewBag.Schema = settingsType;
            ViewBag.DefaultSettings = Activator.CreateInstance(settingsType);
            ViewBag.Settings = collector.Settings;

            await InitializeViewBag(currentUser);
        }

        /// <summary>
        ///     Updates the details for the specified collector.
        /// </summary>
        /// <param name="id">
        ///     The identifier of the feed to edit.
        /// </param>
        /// <param name="submittedCollector">
        ///     The feed identifier and <i>enabled</i> flag for the feed to update.
        /// </param>
        /// <returns>
        ///     A redirection to the collector detail page if the edit was
        ///     successful. A "Not Found" response if the feed does not exists. A
        ///     "Unauthorized" response if the user does not have the appropriate
        ///     rights.
        /// </returns>
        [HttpPost("Collector/Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Guid id,
            [FromForm] Collector submittedCollector,
            [Bind(Prefix = "ImportRuleSet")] string importRuleSets,
            [Bind(Prefix = "releasableTo")] Guid[] releasableTo,
            [Bind(Prefix = "eyesOnly")] Guid[] eyesOnly,
            [Bind(Prefix = "tags")] Guid[] tags)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var collector = await _collectorRepository.GetAsync(
                    AmbientContext,
                    submittedCollector.CollectorId,
                    _ => _.Include(__ => __.Classification)
                        .Include(__ => __.Tags).ThenInclude(__ => __.Facet)
                        .Include(__ => __.ReleasableTo)
                        .Include(__ => __.EyesOnly)
                        .Include(__ => __.Source));

                try
                {
                    if (ModelState.IsValid)
                    {
                        collector.Name = submittedCollector.Name;
                        collector.Description = submittedCollector.Description;
                        collector.Limit = submittedCollector.Limit;
                        collector.CronExpression = submittedCollector.CronExpression;
                        collector.Enabled = submittedCollector.Enabled;
                        collector.SkipInbox = submittedCollector.SkipInbox;
                        collector.SourceId = submittedCollector.SourceId;
                        collector.UserId = submittedCollector.UserId;
                        collector.ClassificationId = submittedCollector.ClassificationId;
                        await UpdateTags(tags, collector);

                        var filteredRelTo = await _groupRepository
                            .GetAllAsync(AmbientContext, new GroupQuery {Id = releasableTo}).ToListAsync();
                        var filteredEyes = await _groupRepository
                            .GetAllAsync(AmbientContext, new GroupQuery {Id = eyesOnly}).ToListAsync();

                        collector.ReleasableTo = filteredRelTo;
                        collector.EyesOnly = filteredEyes;

                        await _collectorRepository.UpdateAsync(
                            AmbientContext,
                            collector);

                        await _context.SaveChangesAsync();

                        _logger.Log(LogLevel.Information,
                            EventIDs.EditCollectorSuccessful,
                            new LogEvent(
                                    $"User '{currentUser.UserName}' successfully edited the collector '{collector.Name}'.")
                                .AddUser(currentUser)
                                .AddHttpContext(_accessor.HttpContext)
                                .AddCollector(collector),
                            null,
                            LogEvent.Formatter);

                        return RedirectToAction(
                            nameof(Details),
                            new {id = collector.CollectorId});
                    }

                    await InitializeViewBag(id, collector, currentUser);
                    return View(collector);
                }
                catch (InvalidArgumentException e)
                {
                    _logger.Log(LogLevel.Information,
                        EventIDs.EditCollectorSuccessful,
                        new LogEvent(
                                $"User '{currentUser.UserName}' attempted to edit the collector '{id}' with an invalid model or empty JSON.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddProperty("collector.id", id),
                        null,
                        LogEvent.Formatter);

                    ModelState.Clear();
                    foreach (var kv in e.Errors)
                    foreach (var errorMessage in kv.Value)
                        ModelState.AddModelError(kv.Key, errorMessage);

                    await InitializeViewBag(id, collector, currentUser);
                    return View(collector);
                }
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditCollectorFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit details of collector '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("collector.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditCollectorFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit details of a non-existing collector '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("collector.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpGet("Collector/Configure/{id}")]
        public async Task<IActionResult> Configure(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var collector = await _collectorRepository.GetAsync(
                    AmbientContext,
                    id,
                    _ => _.Include(__ => __.Source));

                if (!_moduleFactory.HasCollectorSettings(collector.Module, collector.CollectorName))
                    return RedirectToAction((nameof(Details)), new { id });
                
                _logger.Log(LogLevel.Information,
                    EventIDs.EditCollectorSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully requested to configure the collector '{collector.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddCollector(collector),
                    null,
                    LogEvent.Formatter);

                await InitializeViewBag(id, collector, currentUser);

                return View(collector);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditCollectorFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to configure collector '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("collector.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditCollectorFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to configure a non-existing collector '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("collector.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpPost("Collector/Configure/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Configure(
            Guid id,
            [Bind(Prefix = "Settings")] string settings)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var collector = await _collectorRepository.GetAsync(
                    AmbientContext,
                    id, _ => _.Include(__ => __.Source));

                if (!_moduleFactory.HasCollectorSettings(collector.Module, collector.CollectorName))
                    return RedirectToAction((nameof(Details)), new { id });
                
                var json = JsonObject.Parse("{}");
                if (settings != null)
                    try
                    {
                        json = JsonObject.Parse(settings);
                    }
                    catch (JsonReaderException e)
                    {
                        ModelState.AddModelError("Settings", "The provided JSON is invalid.");
                        _logger.Log(LogLevel.Error,
                            EventIDs.EditIncomingFeedError,
                            new LogEvent($"User '{currentUser.UserName}' provided an invalid JSON.")
                                .AddUser(currentUser)
                                .AddHttpContext(_accessor.HttpContext)
                                .AddCollector(collector)
                                .AddException(e),
                            e,
                            LogEvent.Formatter);
                    }

                try
                {
                    if (ModelState.IsValid)
                    {
                        collector.Settings = json.AsObject();

                        await _collectorRepository.UpdateAsync(
                            AmbientContext,
                            collector);

                        await _context.SaveChangesAsync();

                        _logger.Log(LogLevel.Information,
                            EventIDs.EditCollectorSuccessful,
                            new LogEvent(
                                    $"User '{currentUser.UserName}' successfully configured the collector '{collector.Name}'.")
                                .AddUser(currentUser)
                                .AddHttpContext(_accessor.HttpContext)
                                .AddCollector(collector),
                            null,
                            LogEvent.Formatter);

                        return RedirectToAction(
                            nameof(Details),
                            new {id = collector.CollectorId});
                    }

                    await InitializeViewBag(id, collector, currentUser);
                    return View(collector);
                }
                catch (InvalidArgumentException e)
                {
                    _logger.Log(LogLevel.Information,
                        EventIDs.EditCollectorSuccessful,
                        new LogEvent(
                                $"User '{currentUser.UserName}' attempted to configure '{id}' with an invalid model or empty JSON.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddProperty("collector.id", id),
                        null,
                        LogEvent.Formatter);

                    ModelState.Clear();
                    foreach (var kv in e.Errors)
                    foreach (var errorMessage in kv.Value)
                        ModelState.AddModelError(kv.Key, errorMessage);

                    await InitializeViewBag(id, collector, currentUser);
                    return View(collector);
                }
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditCollectorFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to configure collector '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("collector.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditCollectorFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to configure a non-existing collector '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("collector.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpGet("Collector/Uninstall/{id}")]
        public async Task<IActionResult> Uninstall(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var collector = await _collectorRepository.GetAsync(
                    AmbientContext,
                    id);

                _logger.Log(LogLevel.Information,
                    EventIDs.EditCollectorSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully requested the view to uninstall the collector '{collector.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddCollector(collector),
                    null,
                    LogEvent.Formatter);

                return View(collector);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditCollectorFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to uninstall collector '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("collector.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditCollectorFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to uninstall a non-existing collector '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("collector.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpPost("Collector/Uninstall/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, [Bind("CollectorId")] Collector viewModel)
        {
            var currentUser = await GetCurrentUser();
            Collector collector;
            if ((collector = await _collectorRepository.GetAsync(AmbientContext, id)) == null)
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

            if (!await _appAuthorizationService.CanDeleteCollector(User, collector))
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
                    .AddCollector(collector),
                null,
                LogEvent.Formatter);

            await _collectorRepository.RemoveAsync(AmbientContext, collector.CollectorId);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        
        private async Task UpdateTags(Guid[] tags, Collector collector)
        {
            if (tags == null || !tags.Any())
            {
                _logger.LogDebug("Clear all tags");
                collector.Tags?.Clear();
                return;
            }

            var listAsync = await _tagRepository.GetAllAsync(AmbientContext, new TagQuery()
            {
                Ids = tags, Limit = -1
            }).ToListAsync();
            
            if (collector.Tags == null) {
                _logger.LogTrace("Set tags to '" + string.Join(", ", listAsync.Select(_ => _.FriendlyName)) + "' for collector");
                collector.Tags = listAsync;
                return;
            }
        
            var removedTags = collector.Tags.Except(listAsync, _ => _.TagId).ToArray();
            var newTags = listAsync.Except(collector.Tags, _ => _.TagId).ToArray();
            
            foreach (var newTag in newTags)
            {
                collector.Tags.Add(newTag);
                _logger.LogTrace("Add '" + newTag.FriendlyName + "' tag to collector");
            }

            foreach (var removeTag in removedTags)
            {
                collector.Tags.Remove(removeTag);
                _logger.LogTrace("Remove '" + removeTag.FriendlyName+ "' tag from collector");
            }
        }
    }
}