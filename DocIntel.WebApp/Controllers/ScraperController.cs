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
using System.Reflection;
using System.Threading.Tasks;

using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Scrapers;
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
    ///     Provides the functionalities for managing scrapers.
    /// </summary>
    public class ScraperController : BaseController
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IAppAuthorizationService _appAuthorizationService;
        private readonly IClassificationRepository _classificationRepository;

        private readonly IGroupRepository _groupRepository;
        private readonly IImportRuleRepository _importRulesRepository;
        private readonly ILogger _logger;
        private readonly IScraperRepository _scraperRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly ApplicationSettings _setting;

        public ScraperController(IAppAuthorizationService appAuthorizationService,
            DocIntelContext context,
            ILogger<DocumentController> logger,
            ApplicationSettings configuration,
            UserManager<AppUser> userManager,
            IAuthorizationService authorizationService,
            IScraperRepository scraperRepository,
            IHttpContextAccessor accessor, IServiceProvider serviceProvider,
            IImportRuleRepository importRulesRepository, IGroupRepository groupRepository, ApplicationSettings setting,
            IClassificationRepository classificationRepository)
            : base(context,
                userManager,
                configuration,
                authorizationService)
        {
            _logger = logger;
            _appAuthorizationService = appAuthorizationService;
            _scraperRepository = scraperRepository;
            _accessor = accessor;
            _serviceProvider = serviceProvider;
            _importRulesRepository = importRulesRepository;
            _groupRepository = groupRepository;
            _setting = setting;
            _classificationRepository = classificationRepository;
        }

        /// <summary>
        ///     Provides the listing for the scrapers.
        /// </summary>
        /// <returns>
        ///     A view listing the scrapers. An "Unauthorized" response if
        ///     the user does not have the appropriate rights.
        /// </returns>
        [HttpGet("Scraper")]
        [HttpGet("Scraper/Index")]
        public async Task<IActionResult> Index()
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var enumerable = _scraperRepository.GetAllAsync(
                    AmbientContext);

                _logger.Log(LogLevel.Information,
                    EventIDs.ListScraperSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully list scrapers.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return View(await enumerable.ToListAsync());
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.ListScraperFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to list scrapers without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
        }

        /// <summary>
        ///     Provides a view to display the detail of an scraper.
        /// </summary>
        /// <param name="id">
        ///     The identifier of the scraper to display details of.
        /// </param>
        /// <returns>
        ///     A view for displaying details of the scraper. A "Not Found"
        ///     response if the feed does not exists. A "Unauthorized" response
        ///     if the user does not have the appropriate rights.
        /// </returns>
        [HttpGet("Scraper/Details/{id}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var scraper = await _scraperRepository.GetAsync(
                    AmbientContext,
                    id,
                    _ => _.Include(__ => __.Classification)
                        .Include(__ => __.ReleasableTo)
                        .Include(__ => __.EyesOnly)
                        .Include(__ => __.Source)
                        .Include(__ => __.ImportRuleSets)
                        .ThenInclude(__ => __.ImportRuleSet)
                        .ThenInclude(__ => __.ImportRules));

                await InitializeViewBag(id, scraper, currentUser);

                _logger.Log(LogLevel.Information,
                    EventIDs.DetailsScraperSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully requested the view for the scraper '{scraper.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddScraper(scraper),
                    null,
                    LogEvent.Formatter);

                return View(scraper);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DetailsScraperFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view details of scraper '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DetailsScraperFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view details of a non-existing scraper '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        /// <summary>
        ///     Provides a view to create a new scraper.
        /// </summary>
        /// <param name="id">
        ///     The identifier of the feed to create.
        /// </param>
        /// <returns>
        ///     A view for creating details of the scraper. A "Not Found"
        ///     response if the feed does not exists. A "Unauthorized" response
        ///     if the user does not have the appropriate rights.
        /// </returns>
        [HttpGet("Scraper/Create")]
        public async Task<IActionResult> Create()
        {
            var currentUser = await GetCurrentUser();

            if (!await _appAuthorizationService.CanCreateScraper(User, null))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.CreateScraperFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create an scraper without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            await InitializeViewBag(currentUser);

            _logger.Log(LogLevel.Information,
                EventIDs.CreateScraperSuccess,
                new LogEvent($"User '{currentUser.UserName}' successfully requested the view to create an scraper.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return View(new Scraper());
        }

        private async Task InitializeViewBag(AppUser currentUser)
        {
            ViewBag.DefaultClassification = _classificationRepository.GetDefault(AmbientContext);
            ViewBag.AllClassifications = AmbientContext.DatabaseContext.Classifications.ToList();
            var allGroups = await _groupRepository.GetAllAsync(AmbientContext).ToListAsync();
            ViewBag.AllGroups = allGroups;
            ViewBag.OwnGroups = allGroups.Where(_ =>
                currentUser.Memberships.Any(__ => __.GroupId == _.GroupId));

            var installedScrapers =
                await _scraperRepository.GetAllAsync(AmbientContext).Select(_ => _.ScraperId).ToArrayAsync();

            // Search for classes with Scrapers
            var types = ScraperFactory.GetAllScrapers();

            ViewBag.Scrapers = types.Select(type =>
                    ScraperFactory.CreateScraper(type, _serviceProvider, AmbientContext).Result)
                .Where(instance => instance != null)
                .Select(instance => instance.Get())
                .ToList();

            ViewBag.BotUsers = AmbientContext.DatabaseContext.Users.AsQueryable()
                .Where(_ => _.Bot)
                .ToList();
        }

        /// <summary>
        ///     Creates the specified scraper.
        /// </summary>
        /// <param name="submittedScraper">
        ///     The feed identifier and <i>enabled</i> flag for the feed to update.
        /// </param>
        /// <param name="settings">
        ///     A string representing the JSON for the settings of the plugin.
        /// </param>
        /// <returns>
        ///     A redirection to the scraper detail page if the edit was
        ///     successful. A "Not Found" response if the feed does not exists. A
        ///     "Unauthorized" response if the user does not have the appropriate
        ///     rights.
        /// </returns>
        [HttpPost("Scraper/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind(
                "Name,Description,Enabled,ReferenceClass,SkipInbox,SourceId,Position,OverrideSource,OverrideClassification,OverrideReleasableTo,OverrideEyesOnly,ClassificationId")]
            Scraper submittedScraper,
            [Bind(Prefix = "releasableTo")] Guid[] releasableTo,
            [Bind(Prefix = "eyesOnly")] Guid[] eyesOnly,
            [Bind(Prefix = "Scraper")] string scraperId)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                if (ModelState.IsValid)
                {
                    var instance =
                        await ScraperFactory.CreateScraper(Guid.Parse(scraperId), _serviceProvider, AmbientContext);

                    var scraper = instance.Install();
                    scraper.Enabled = submittedScraper.Enabled;
                    scraper.SkipInbox = submittedScraper.SkipInbox;
                    scraper.OverrideSource = submittedScraper.OverrideSource;
                    scraper.SourceId = submittedScraper.SourceId;
                    scraper.Position = submittedScraper.Position;
                    scraper.OverrideClassification = submittedScraper.OverrideClassification;
                    scraper.OverrideReleasableTo = submittedScraper.OverrideReleasableTo;
                    scraper.OverrideEyesOnly = submittedScraper.OverrideEyesOnly;
                    scraper.ClassificationId = submittedScraper.ClassificationId;

                    var filteredRelTo = await _groupRepository
                        .GetAllAsync(AmbientContext, new GroupQuery {Id = releasableTo}).ToListAsync();
                    var filteredEyes = await _groupRepository
                        .GetAllAsync(AmbientContext, new GroupQuery {Id = eyesOnly}).ToListAsync();

                    scraper.ReleasableTo = filteredRelTo;
                    scraper.EyesOnly = filteredEyes;

                    scraper = await _scraperRepository.CreateAsync(
                        AmbientContext,
                        scraper);

                    await _context.SaveChangesAsync();

                    _logger.Log(LogLevel.Information,
                        EventIDs.CreateScraperSuccess,
                        new LogEvent(
                                $"User '{currentUser.UserName}' successfully created the scraper '{scraper.Name}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddScraper(scraper),
                        null,
                        LogEvent.Formatter);

                    return RedirectToAction(
                        nameof(Configure),
                        new {id = scraper.ScraperId});
                }

                return View(submittedScraper);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.CreateScraperSuccess,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create scraper without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddScraper(submittedScraper),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (InvalidArgumentException e)
            {
                _logger.Log(LogLevel.Information,
                    EventIDs.EditScraperSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create an scraper with an invalid model or empty JSON.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddScraper(submittedScraper),
                    null,
                    LogEvent.Formatter);

                ModelState.Clear();
                foreach (var kv in e.Errors)
                foreach (var errorMessage in kv.Value)
                    ModelState.AddModelError(kv.Key, errorMessage);
                await InitializeViewBag(currentUser);

                return View(submittedScraper);
            }
        }

        /// <summary>
        ///     Provides a view to edit the scraper.
        /// </summary>
        /// <param name="id">
        ///     The identifier of the feed to edit.
        /// </param>
        /// <returns>
        ///     A view for editing details of the scraper. A "Not Found"
        ///     response if the feed does not exists. A "Unauthorized" response
        ///     if the user does not have the appropriate rights.
        /// </returns>
        [HttpGet("Scraper/Edit/{id}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var scraper = await _scraperRepository.GetAsync(
                    AmbientContext,
                    id,
                    _ => _.Include(__ => __.Classification).Include(__ => __.ReleasableTo)
                        .Include(__ => __.EyesOnly).Include(__ => __.Source).Include(__ => __.ImportRuleSets));

                if (scraper.Settings != null) ViewData["settings"] = scraper.Settings.ToString();

                _logger.Log(LogLevel.Information,
                    EventIDs.EditScraperSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully requested the view to edit the scraper '{scraper.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddScraper(scraper),
                    null,
                    LogEvent.Formatter);

                await InitializeViewBag(id, scraper, currentUser);

                return View(scraper);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditScraperFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit details of scraper '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditScraperFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit details of a non-existing scraper '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        private async Task InitializeViewBag(Guid id, Scraper scraper, AppUser currentUser)
        {
            await InitializeViewBag(currentUser);

            var instance = await ScraperFactory.CreateScraper(scraper.ReferenceClass, _serviceProvider, AmbientContext);
            var stringProperties = instance.GetType().GetProperties()
                .Where(property =>
                {
                    var attribute = property.GetCustomAttribute<ScraperSettingAttribute>();
                    return attribute != null;
                });

            var dict = new List<FormField>();
            foreach (var property in stringProperties)
            {
                var attribute = property.GetCustomAttribute<ScraperSettingAttribute>();
                var attributeValue = attribute.DefaultValue;
                if (scraper.Settings != null)
                    attributeValue = scraper.Settings[attribute.Name]?.ToString() ?? attribute.DefaultValue;
                dict.Add(new FormField
                {
                    DataType = property.PropertyType, Key = attribute.Name, Value = attributeValue,
                    Type = attribute.Type, PossibleValue = attribute.PossibleValues
                });
            }

            ViewBag.Settings = dict;

            ViewBag.ImportRules = _importRulesRepository.GetAll(AmbientContext);

            await InitializeViewBag(currentUser);
        }

        /// <summary>
        ///     Updates the details for the specified scraper.
        /// </summary>
        /// <param name="id">
        ///     The identifier of the feed to edit.
        /// </param>
        /// <param name="submittedScraper">
        ///     The feed identifier and <i>enabled</i> flag for the feed to update.
        /// </param>
        /// <param name="settings">
        ///     A string representing the JSON for the settings of the plugin.
        /// </param>
        /// <returns>
        ///     A redirection to the scraper detail page if the edit was
        ///     successful. A "Not Found" response if the feed does not exists. A
        ///     "Unauthorized" response if the user does not have the appropriate
        ///     rights.
        /// </returns>
        [HttpPost("Scraper/Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Guid id,
            [Bind(
                "ScraperId,Name,Description,Enabled,ReferenceClass,SkipInbox,SourceId,Position,OverrideSource,OverrideClassification,OverrideReleasableTo,OverrideEyesOnly,ClassificationId")]
            Scraper submittedScraper,
            [Bind(Prefix = "ImportRuleSet")] string importRuleSets,
            [Bind(Prefix = "releasableTo")] Guid[] releasableTo,
            [Bind(Prefix = "eyesOnly")] Guid[] eyesOnly)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var scraper = await _scraperRepository.GetAsync(
                    AmbientContext,
                    submittedScraper.ScraperId,
                    _ => _.Include(__ => __.Classification)
                        .Include(__ => __.ReleasableTo)
                        .Include(__ => __.EyesOnly)
                        .Include(__ => __.Source)
                        .Include(__ => __.ImportRuleSets));

                try
                {
                    if (ModelState.IsValid)
                    {
                        scraper.Enabled = submittedScraper.Enabled;
                        scraper.SkipInbox = submittedScraper.SkipInbox;
                        scraper.OverrideSource = submittedScraper.OverrideSource;
                        scraper.SourceId = submittedScraper.SourceId;
                        scraper.Position = submittedScraper.Position;
                        scraper.OverrideClassification = submittedScraper.OverrideClassification;
                        scraper.OverrideReleasableTo = submittedScraper.OverrideReleasableTo;
                        scraper.OverrideEyesOnly = submittedScraper.OverrideEyesOnly;
                        scraper.ClassificationId = submittedScraper.ClassificationId;

                        var filteredRelTo = await _groupRepository
                            .GetAllAsync(AmbientContext, new GroupQuery {Id = releasableTo}).ToListAsync();
                        var filteredEyes = await _groupRepository
                            .GetAllAsync(AmbientContext, new GroupQuery {Id = eyesOnly}).ToListAsync();

                        scraper.ReleasableTo = filteredRelTo;
                        scraper.EyesOnly = filteredEyes;

                        scraper.ImportRuleSets?.Clear();

                        await _scraperRepository.UpdateAsync(
                            AmbientContext,
                            scraper);

                        await _context.SaveChangesAsync();

                        scraper.ImportRuleSets = new List<OrderedImportRuleSet>();
                        if (!string.IsNullOrEmpty(importRuleSets))
                        {
                            var split = importRuleSets.Split(Environment.NewLine);
                            for (var index = 0; index < split.Length; index++)
                            {
                                var lines = split[index];
                                var importRuleSetId = Guid.Parse(lines);
                                if (_importRulesRepository.SetExists(AmbientContext, importRuleSetId))
                                    scraper.ImportRuleSets.Add(new OrderedImportRuleSet
                                    {
                                        Scraper = scraper,
                                        ImportRuleSetId = importRuleSetId,
                                        Position = index
                                    });
                                else
                                    _logger.LogDebug("Could not find " + lines);
                            }
                        }

                        await _scraperRepository.UpdateAsync(
                            AmbientContext,
                            scraper);

                        await _context.SaveChangesAsync();

                        _logger.Log(LogLevel.Information,
                            EventIDs.EditScraperSuccessful,
                            new LogEvent(
                                    $"User '{currentUser.UserName}' successfully edited the scraper '{scraper.Name}'.")
                                .AddUser(currentUser)
                                .AddHttpContext(_accessor.HttpContext)
                                .AddScraper(scraper),
                            null,
                            LogEvent.Formatter);

                        return RedirectToAction(
                            nameof(Details),
                            new {id = scraper.ScraperId});
                    }

                    await InitializeViewBag(id, scraper, currentUser);
                    return View(scraper);
                }
                catch (InvalidArgumentException e)
                {
                    _logger.Log(LogLevel.Information,
                        EventIDs.EditScraperSuccessful,
                        new LogEvent(
                                $"User '{currentUser.UserName}' attempted to edit the scraper '{id}' with an invalid model or empty JSON.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddProperty("incoming_feed.id", id),
                        null,
                        LogEvent.Formatter);

                    ModelState.Clear();
                    foreach (var kv in e.Errors)
                    foreach (var errorMessage in kv.Value)
                        ModelState.AddModelError(kv.Key, errorMessage);

                    await InitializeViewBag(id, scraper, currentUser);
                    return View(scraper);
                }
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditScraperFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit details of scraper '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditScraperFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit details of a non-existing scraper '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpGet("Scraper/Configure/{id}")]
        public async Task<IActionResult> Configure(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var scraper = await _scraperRepository.GetAsync(
                    AmbientContext,
                    id,
                    _ => _.Include(__ => __.Source).Include(__ => __.ImportRuleSets));

                _logger.Log(LogLevel.Information,
                    EventIDs.EditScraperSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully requested to configure the scraper '{scraper.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddScraper(scraper),
                    null,
                    LogEvent.Formatter);

                await InitializeViewBag(id, scraper, currentUser);

                return View(scraper);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditScraperFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to configure scraper '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditScraperFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to configure a non-existing scraper '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpPost("Scraper/Configure/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Configure(
            Guid id,
            [Bind(Prefix = "Settings")] Dictionary<string, string> settings)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var scraper = await _scraperRepository.GetAsync(
                    AmbientContext,
                    id, _ => _.Include(__ => __.Source).Include(__ => __.ImportRuleSets));

                var json = JObject.Parse("{}");
                if (settings != null)
                    try
                    {
                        json = JObject.FromObject(settings);
                    }
                    catch (JsonReaderException e)
                    {
                        ModelState.AddModelError("Settings", "The provided JSON is invalid.");
                        _logger.Log(LogLevel.Error,
                            EventIDs.EditIncomingFeedError,
                            new LogEvent($"User '{currentUser.UserName}' provided an invalid JSON.")
                                .AddUser(currentUser)
                                .AddHttpContext(_accessor.HttpContext)
                                .AddScraper(scraper)
                                .AddException(e),
                            e,
                            LogEvent.Formatter);
                    }

                try
                {
                    if (ModelState.IsValid)
                    {
                        scraper.Settings = json;

                        await _scraperRepository.UpdateAsync(
                            AmbientContext,
                            scraper);

                        await _context.SaveChangesAsync();

                        _logger.Log(LogLevel.Information,
                            EventIDs.EditScraperSuccessful,
                            new LogEvent(
                                    $"User '{currentUser.UserName}' successfully configured the scraper '{scraper.Name}'.")
                                .AddUser(currentUser)
                                .AddHttpContext(_accessor.HttpContext)
                                .AddScraper(scraper),
                            null,
                            LogEvent.Formatter);

                        return RedirectToAction(
                            nameof(Details),
                            new {id = scraper.ScraperId});
                    }

                    await InitializeViewBag(id, scraper, currentUser);
                    return View(scraper);
                }
                catch (InvalidArgumentException e)
                {
                    _logger.Log(LogLevel.Information,
                        EventIDs.EditScraperSuccessful,
                        new LogEvent(
                                $"User '{currentUser.UserName}' attempted to configure '{id}' with an invalid model or empty JSON.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddProperty("incoming_feed.id", id),
                        null,
                        LogEvent.Formatter);

                    ModelState.Clear();
                    foreach (var kv in e.Errors)
                    foreach (var errorMessage in kv.Value)
                        ModelState.AddModelError(kv.Key, errorMessage);

                    await InitializeViewBag(id, scraper, currentUser);
                    return View(scraper);
                }
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditScraperFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to configure scraper '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditScraperFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to configure a non-existing scraper '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpGet("Scraper/Uninstall/{id}")]
        public async Task<IActionResult> Uninstall(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var scraper = await _scraperRepository.GetAsync(
                    AmbientContext,
                    id);

                _logger.Log(LogLevel.Information,
                    EventIDs.EditScraperSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully requested the view to uninstall the scraper '{scraper.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddScraper(scraper),
                    null,
                    LogEvent.Formatter);

                return View(scraper);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditScraperFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to uninstall scraper '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EditScraperFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to uninstall a non-existing scraper '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("incoming_feed.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpPost("Scraper/Uninstall/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, [Bind("ScraperId")] Scraper viewModel)
        {
            var currentUser = await GetCurrentUser();
            Scraper scraper;
            if ((scraper = await _scraperRepository.GetAsync(AmbientContext, id)) == null)
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

            if (!await _appAuthorizationService.CanDeleteScraper(User, scraper))
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
                    .AddScraper(scraper),
                null,
                LogEvent.Formatter);

            await _scraperRepository.RemoveAsync(AmbientContext, scraper.ScraperId);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}