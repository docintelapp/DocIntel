﻿/* DocIntel
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
using System.Linq;
using System.Threading.Tasks;

using DocIntel.Core.Authorization;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;
using DocIntel.WebApp.Helpers;
using DocIntel.WebApp.ViewModels.ImportRule;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DocIntel.WebApp.Controllers
{
    /// <summary>
    ///     Provides the functionalities for managing the import rules and filters
    ///     applied to incoming feeds.
    ///     Rules and filters are organized in sets. The sets contains the
    ///     prioritized rules and filters that are regexes to be applied to the tags
    ///     of the incoming feeds. A feed can have multiple, prioritized, sets.
    /// </summary>
    public class ImportRuleController : BaseController
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IAppAuthorizationService _appAuthorizationService;
        private readonly IImportRuleRepository _importRuleRepository;
        private readonly IIncomingFeedRepository _incomingFeedRepository;
        private readonly ILogger _logger;

        public ImportRuleController(IAppAuthorizationService appAuthorizationService,
            DocIntelContext context,
            ILogger<DocumentController> logger,
            ApplicationSettings configuration,
            IIncomingFeedRepository incomingFeedRepository,
            UserManager<AppUser> userManager,
            IAuthorizationService authorizationService,
            IIncomingFeedRepository pluginRepository,
            IImportRuleRepository importRuleRepository,
            IHttpContextAccessor accessor)
            : base(context,
                userManager,
                configuration,
                authorizationService)
        {
            _logger = logger;
            _incomingFeedRepository = incomingFeedRepository;
            _appAuthorizationService = appAuthorizationService;
            _importRuleRepository = importRuleRepository;
            _accessor = accessor;
        }

        /// <summary>
        ///     Provides the list of all import rules/filter sets.
        /// </summary>
        /// <returns>
        ///     A view with the listing of all import rules and filters. An
        ///     "Unauthorized" response if the user does not have the legitimate
        ///     rights.
        /// </returns>
        [HttpGet("ImportRuleSet")]
        [HttpGet("ImportRuleSet/Index")]
        public async Task<IActionResult> Index()
        {
            // REFACTOR to avoid view model
            var importRules = _importRuleRepository.GetAllSets(AmbientContext);
            var vm = new IndexViewModel
            {
                ImportRuleSets = importRules,
                ImportRuleSetsCount = importRules.Count()
            };
            return View(vm);
        }

        /// <summary>
        ///     Provides the details about an import ruleset.
        /// </summary>
        /// <param name="id">
        ///     The identifier of the import ruleset to display details of.
        /// </param>
        /// <returns>
        ///     A page with the details about the ruleset. A "Not Found" response
        ///     if the ruleset does not exist. A "Unauthorized" response if the
        ///     current user does not have the correct rights for viewing the
        ///     details.
        /// </returns>
        [HttpGet("ImportRuleSet/Details/{id}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var currentUser = await GetCurrentUser();
            var importRuleSet = _importRuleRepository.GetSet(AmbientContext, id);
            if (importRuleSet == null)
            {
                _logger.Log(LogLevel.Information,
                    EventIDs.ListImportRuleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view the details of a non-existing ruleset '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }

            if (!await _appAuthorizationService.CanViewImportRule(User, importRuleSet))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.ListImportRuleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view the details of the import ruleset '{importRuleSet.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddImportRuleSet(importRuleSet),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            _logger.Log(LogLevel.Information,
                EventIDs.ListImportRuleSuccessful,
                new LogEvent(
                        $"User '{currentUser.UserName}' successfully viewed the details of the import ruleset '{importRuleSet.Name}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddImportRuleSet(importRuleSet),
                null,
                LogEvent.Formatter);

            return View(importRuleSet);
        }

        /// <summary>
        ///     Provides a page to create a new import ruleset.
        /// </summary>
        /// <returns>
        ///     A view to create a new ruleset. An "Unauthorized" response if the
        ///     current user does not have the right to create a new ruleset.
        /// </returns>
        [HttpGet("ImportRuleSet/Create")]
        public async Task<IActionResult> Create()
        {
            var currentUser = await GetCurrentUser();
            if (!await _appAuthorizationService.CanCreateImportRule(User, null))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.CreateImportRuleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new import ruleset without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            return View();
        }

        /// <summary>
        ///     Creates a new ruleset with the specified details.
        /// </summary>
        /// <param name="submittedRuleSet">
        ///     The name and description of the ruleset to create.
        /// </param>
        /// <param name="importRules">
        ///     The list of import rules, formatted as
        ///     <c>name;description;pattern;replacement</c>.
        /// </param>
        /// <returns></returns>
        [HttpPost("ImportRuleSet/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Description")] ImportRuleSet submittedRuleSet,
            [Bind("ImportRules")] string importRules)
        {
            var currentUser = await GetCurrentUser();
            if (!await _appAuthorizationService.CanCreateImportRule(User, submittedRuleSet))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.CreateImportRuleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new import ruleset '{submittedRuleSet.Name}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            var ruleSet = new ImportRuleSet();

            if (ModelState.IsValid)
            {
                ruleSet.Name = submittedRuleSet.Name;
                ruleSet.Description = submittedRuleSet.Description;
                await _importRuleRepository.Create(AmbientContext, ruleSet, await GetCurrentUser());

                if (!string.IsNullOrEmpty(importRules))
                {
                    var i = 0;
                    foreach (var rule in importRules.Split("\n"))
                    {
                        if (string.IsNullOrEmpty(rule.Trim()))
                            continue;
                        var row = rule.Trim().Split(";");

                        var name = row[0].Trim();
                        var description = row[1].Trim();
                        var pattern = row[2].Trim();
                        var replacement = row[3].Trim();
                        var r = new ImportRule
                        {
                            Name = name,
                            Description = description,
                            SearchPattern = pattern,
                            Replacement = replacement,
                            Position = i,
                            ImportRuleSetId = ruleSet.ImportRuleSetId
                        };
                        await _importRuleRepository.Create(AmbientContext, r, await GetCurrentUser());
                        i++;
                    }
                }

                _logger.Log(LogLevel.Information,
                    EventIDs.CreateImportRuleSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully created a new import ruleset '{submittedRuleSet.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return RedirectToAction(nameof(Details), new {id = ruleSet.ImportRuleSetId});
            }

            return View(submittedRuleSet);
        }

        /// <summary>
        ///     Provides the view for editing an import ruleset.
        /// </summary>
        /// <param name="id">
        ///     The identifier of the ruleset to edit.
        /// </param>
        /// <returns>
        ///     A view to edit the specified ruleset. A "Not Found" response if the
        ///     ruleset does exists. A "Unauthorized" response if the user does not
        ///     have sufficient rights.
        /// </returns>
        [HttpGet("ImportRuleSet/Edit/{id}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var currentUser = await GetCurrentUser();

            var importRuleSet = _importRuleRepository.GetSet(AmbientContext, id);
            if (importRuleSet == null)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.UpdateImportRuleFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to edit a non-existing import ruleset.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }

            if (!await _appAuthorizationService.CanEditImportRule(User, importRuleSet))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.UpdateImportRuleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit the import ruleset '{importRuleSet.Name}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddImportRuleSet(importRuleSet),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            _logger.Log(LogLevel.Information,
                EventIDs.UpdateImportRuleSuccessful,
                new LogEvent(
                        $"User '{currentUser.UserName}' successfully requested to edit the import ruleset '{importRuleSet.Name}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddImportRuleSet(importRuleSet),
                null,
                LogEvent.Formatter);

            ViewBag.IncomingFeeds = _incomingFeedRepository.GetAllAsync(AmbientContext).ToEnumerable();

            return View(importRuleSet);
        }

        /// <summary>
        ///     Updates the import ruleset.
        /// </summary>
        /// <param name="id">
        ///     The identifier of the ruleset to edit.
        /// </param>
        /// <param name="submittedRuleSet">
        ///     The identifier, name and description of the ruleset to create.
        /// </param>
        /// <param name="importRules">
        ///     The list of import rules, formatted as
        ///     <c>name;description;pattern;replacement</c>.
        /// </param>
        /// <returns>
        ///     A redirection to the details page if the edit was successful. A
        ///     "Not Found" response if the ruleset does exists. A "Unauthorized"
        ///     response if the user does not have sufficient rights.
        /// </returns>
        [HttpPost("ImportRuleSet/Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Guid id,
            [Bind("ImportRuleSetId,Name,Description")]
            ImportRuleSet submittedRuleSet,
            [Bind("ImportRules")] string importRules)
        {
            var currentUser = await GetCurrentUser();

            var ruleSet = _importRuleRepository.GetSet(AmbientContext, submittedRuleSet.ImportRuleSetId);
            if (ruleSet == null)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.UpdateImportRuleFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to edit a non-existing import ruleset.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }

            if (!await _appAuthorizationService.CanEditImportRule(User, ruleSet))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.UpdateImportRuleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit the import ruleset '{ruleSet.Name}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddImportRuleSet(ruleSet),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                ruleSet.Name = submittedRuleSet.Name;
                ruleSet.Description = submittedRuleSet.Description;
                if (ruleSet.ImportRules != null)
                    ruleSet.ImportRules.Clear();

                if (ruleSet.IncomingFeeds != null)
                    ruleSet.IncomingFeeds.Clear();

                await _importRuleRepository.Update(AmbientContext, ruleSet, currentUser);

                if (!string.IsNullOrEmpty(importRules))
                {
                    var i = 0;
                    foreach (var rule in importRules.Split("\n"))
                    {
                        if (string.IsNullOrEmpty(rule.Trim()))
                            continue;
                        var row = rule.Trim().Split(";");

                        var name = row[0].Trim();
                        var description = row[1].Trim();
                        var pattern = row[2].Trim();
                        var replacement = row[3].Trim();
                        var r = new ImportRule
                        {
                            Name = name,
                            Description = description,
                            SearchPattern = pattern,
                            Replacement = replacement,
                            Position = i,
                            ImportRuleSetId = ruleSet.ImportRuleSetId
                        };
                        await _importRuleRepository.Create(AmbientContext, r, await GetCurrentUser());
                        i++;
                    }
                }

                await AmbientContext.DatabaseContext.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.UpdateImportRuleSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully edited the import ruleset '{ruleSet.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddImportRuleSet(ruleSet),
                    null,
                    LogEvent.Formatter);

                return RedirectToAction(nameof(Details), new {id = ruleSet.ImportRuleSetId});
            }

            return View(submittedRuleSet);
        }

        /// <summary>
        ///     Provides the view for deleting an import ruleset.
        /// </summary>
        /// <param name="id">
        ///     The identifier of the ruleset to delete.
        /// </param>
        /// <returns>
        ///     A view to delete the specified ruleset. A "Not Found" response if
        ///     the ruleset does exists. A "Unauthorized" response if the user does
        ///     not have sufficient rights.
        /// </returns>
        [HttpGet("ImportRuleSet/Delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentUser = await GetCurrentUser();
            var ruleSet = _importRuleRepository.GetSet(AmbientContext, id);
            if (ruleSet == null)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DeleteImportRuleFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to delete a non-existing import ruleset.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }

            if (!await _appAuthorizationService.CanDeleteImportRule(User, ruleSet))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DeleteImportRuleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to delete the import ruleset '{ruleSet.Name}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddImportRuleSet(ruleSet),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            _logger.Log(LogLevel.Information,
                EventIDs.DeleteImportRuleSuccessful,
                new LogEvent(
                        $"User '{currentUser.UserName}' successfully requested to delete the import ruleset '{ruleSet.Name}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddImportRuleSet(ruleSet),
                null,
                LogEvent.Formatter);

            return View(ruleSet);
        }

        /// <summary>
        ///     Updates the import ruleset.
        /// </summary>
        /// <param name="id">
        ///     The identifier of the ruleset to edit.
        /// </param>
        /// <param name="viewModel">
        ///     The identifier of the ruleset to create.
        /// </param>
        /// <returns>
        ///     A redirection to the index page if the edit was successful. A
        ///     "Not Found" response if the ruleset does exists. A "Unauthorized"
        ///     response if the user does not have sufficient rights.
        /// </returns>
        [HttpPost("ImportRuleSet/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id,
            [Bind("ImportRuleSetId")] ImportRuleSet viewModel)
        {
            var currentUser = await GetCurrentUser();
            var ruleSet = _importRuleRepository.GetSet(AmbientContext, id);
            if (ruleSet == null)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DeleteImportRuleFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to delete a non-existing import ruleset.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }

            if (!await _appAuthorizationService.CanDeleteImportRule(User, ruleSet))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DeleteImportRuleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to delete the import ruleset '{ruleSet.Name}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddImportRuleSet(ruleSet),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            _logger.Log(LogLevel.Information,
                EventIDs.DeleteImportRuleSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully deleted the import ruleset '{ruleSet.Name}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddImportRuleSet(ruleSet),
                null,
                LogEvent.Formatter);

            await _importRuleRepository.Remove(AmbientContext, ruleSet, currentUser);

            return RedirectToAction(nameof(Index));
        }
    }
}