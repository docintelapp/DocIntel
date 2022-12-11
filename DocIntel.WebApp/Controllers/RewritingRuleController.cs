/* DocIntel
 * Copyright (C) 2018-2022 Belgian Defense, Antoine Cailliau
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
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;
using DocIntel.WebApp.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DocIntel.WebApp.Controllers
{
    /// <summary>
    ///     Provides the functionalities for managing the rewriting rules.
    /// </summary>
    public class RewritingRuleController : BaseController
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IAppAuthorizationService _appAuthorizationService;
        private readonly IImportRuleRepository _importRuleRepository;
        private readonly ILogger _logger;

        public RewritingRuleController(IAppAuthorizationService appAuthorizationService,
            DocIntelContext context,
            ILogger<RewritingRuleController> logger,
            ApplicationSettings configuration,
            AppUserManager userManager,
            IAuthorizationService authorizationService,
            IImportRuleRepository importRuleRepository,
            IHttpContextAccessor accessor)
            : base(context,
                userManager,
                configuration,
                authorizationService)
        {
            _logger = logger;
            _appAuthorizationService = appAuthorizationService;
            _importRuleRepository = importRuleRepository;
            _accessor = accessor;
        }

        /// <summary>
        ///     Provides the details about a rewriting rule.
        /// </summary>
        /// <param name="id">
        ///     The identifier of the rewritingrule to display details of.
        /// </param>
        /// <returns>
        ///     A page with the details about the rewriting rule.
        /// </returns>
        [HttpGet("ImportRule/Details/{id}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var currentUser = await GetCurrentUser();
            var rule = _importRuleRepository.Get(AmbientContext, id);
            if (rule == null)
            {
                _logger.Log(LogLevel.Information,
                    EventIDs.ListImportRuleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view the details of a non-existing rewriting rule '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }

            if (!await _appAuthorizationService.CanViewImportRule(User, null)) // TODO split permission
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.ListImportRuleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view the details of the import rewriting rule '{rule.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddImportRule(rule),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            _logger.Log(LogLevel.Information,
                EventIDs.ListImportRuleSuccessful,
                new LogEvent(
                        $"User '{currentUser.UserName}' successfully viewed the details of the rewriting rule '{rule.Name}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddImportRule(rule),
                null,
                LogEvent.Formatter);

            return View(rule);
        }

        /// <summary>
        ///     Provides a page to create a new rewriting rule.
        /// </summary>
        /// <returns>
        ///     A view to create a new rule.
        /// </returns>
        [HttpGet("ImportRule/Create/{id}")]
        public async Task<IActionResult> Create(Guid id)
        {
            var currentUser = await GetCurrentUser();
            if (!await _appAuthorizationService.CanCreateImportRule(User, null))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.CreateImportRuleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new rewriting rule without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            var ruleSet = _importRuleRepository.GetSet(AmbientContext, id);

            return View(new ImportRule() { ImportRuleSetId = id, ImportRuleSet = ruleSet });
        }

        /// <summary>
        ///     Creates a new rewriting rule with the specified details.
        /// </summary>
        /// <param name="submittedRuleSet">
        ///     The name and description of the rewriting rule to create.
        /// </param>
        /// <param name="importRules">
        ///     The list of rewriting rule rules
        /// </param>
        /// <returns></returns>
        [HttpPost("ImportRule/Create/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid id, [Bind("Name,Description,Position,SearchPattern,Replacement")] ImportRule submittedRule)
        {
            var currentUser = await GetCurrentUser();
            if (!await _appAuthorizationService.CanCreateImportRule(User, null)) // TODO Split permission?
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.CreateImportRuleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new rewriting rule '{submittedRule.Name}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            var rule = new ImportRule();

            if (ModelState.IsValid)
            {
                rule.Name = submittedRule.Name;
                rule.Description = submittedRule.Description;
                rule.Position = submittedRule.Position;
                rule.SearchPattern = submittedRule.SearchPattern;
                rule.Replacement = submittedRule.Replacement;
                rule.ImportRuleSetId = id;
                await _importRuleRepository.Create(AmbientContext, rule);

                _logger.Log(LogLevel.Information,
                    EventIDs.CreateImportRuleSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully created a new import ruleset '{rule.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return RedirectToAction("Details", "RewritingRuleSet", new {id = rule.ImportRuleSetId});
            }

            return View(rule);
        }

        /// <summary>
        ///     Provides the view for editing a rewriting rule.
        /// </summary>
        /// <param name="id">
        ///     The identifier of the rule to edit.
        /// </param>
        /// <returns>
        ///     A view to edit the specified rule.
        /// </returns>
        [HttpGet("ImportRule/Edit/{id}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var currentUser = await GetCurrentUser();

            var rule = _importRuleRepository.Get(AmbientContext, id);
            if (rule == null)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.UpdateImportRuleFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to edit a non-existing rewriting rule.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }

            if (!await _appAuthorizationService.CanEditImportRule(User, null)) // TODO split permissions
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.UpdateImportRuleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit the rewriting rule '{rule.Name}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddImportRule(rule),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            _logger.Log(LogLevel.Information,
                EventIDs.UpdateImportRuleSuccessful,
                new LogEvent(
                        $"User '{currentUser.UserName}' successfully requested to edit the rewriting rule '{rule.Name}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddImportRule(rule),
                null,
                LogEvent.Formatter);

            return View(rule);
        }

        /// <summary>
        ///     Updates the rewriting rule.
        /// </summary>
        /// <param name="id">
        ///     The identifier of the rule to edit.
        /// </param>
        /// <param name="submittedRule">
        ///     The identifier, name and description of the rule to update.
        /// </param>
        /// <returns>
        ///     A redirection to the details page if the edit was successful.
        /// </returns>
        [HttpPost("ImportRule/Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Guid id,
            [Bind("Name,Description,SearchPattern,Replacement,Position")]
            ImportRule submittedRule)
        {
            var currentUser = await GetCurrentUser();

            var rule = _importRuleRepository.Get(AmbientContext, id);
            if (rule == null)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.UpdateImportRuleFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to edit a non-existing rewriting rule.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }

            if (!await _appAuthorizationService.CanEditImportRule(User, null)) // TODO Split permissions
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.UpdateImportRuleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit the rewriting rule '{rule.Name}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddImportRule(rule),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                rule.Name = submittedRule.Name;
                rule.Description = submittedRule.Description;
                rule.SearchPattern = submittedRule.SearchPattern;
                rule.Replacement = submittedRule.Replacement;
                rule.Position = submittedRule.Position;

                await AmbientContext.DatabaseContext.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.UpdateImportRuleSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully edited the rewriting rule '{rule.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddImportRule(rule),
                    null,
                    LogEvent.Formatter);

                return RedirectToAction(nameof(Details), "RewritingRuleSet", new {id = rule.ImportRuleSetId});
            }

            return View(submittedRule);
        }

        /// <summary>
        ///     Provides the view for deleting a rewriting rule.
        /// </summary>
        /// <param name="id">
        ///     The identifier of the rule to delete.
        /// </param>
        /// <returns>
        ///     A view to delete the specified rule. 
        /// </returns>
        [HttpGet("ImportRule/Delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentUser = await GetCurrentUser();
            var rule = _importRuleRepository.Get(AmbientContext, id);
            if (rule == null)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DeleteImportRuleFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to delete a non-existing rewriting rule.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }

            if (!await _appAuthorizationService.CanDeleteImportRule(User, null)) // TODO split permission
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DeleteImportRuleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to delete the rewriting rule '{rule.Name}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddImportRule(rule),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            _logger.Log(LogLevel.Information,
                EventIDs.DeleteImportRuleSuccessful,
                new LogEvent(
                        $"User '{currentUser.UserName}' successfully requested to delete the rewriting rule '{rule.Name}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddImportRule(rule),
                null,
                LogEvent.Formatter);

            return View(rule);
        }

        /// <summary>
        ///     Delete the specified rewriting rule
        /// </summary>
        /// <param name="id">
        ///     The identifier of the rewriting rule to delete.
        /// </param>
        /// <returns>
        ///     A redirection to the index page if the edit was successful. 
        /// </returns>
        [HttpPost("ImportRule/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDelete(Guid id)
        {
            var currentUser = await GetCurrentUser();
            var rule = _importRuleRepository.Get(AmbientContext, id);
            if (rule == null)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DeleteImportRuleFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to delete a non-existing rewriting rule.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }

            if (!await _appAuthorizationService.CanDeleteImportRule(User, null)) // TODO Split permission
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DeleteImportRuleFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to delete the rewriting rule '{rule.Name}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddImportRule(rule),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            _logger.Log(LogLevel.Information,
                EventIDs.DeleteImportRuleSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully deleted the rewriting rule '{rule.Name}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddImportRule(rule),
                null,
                LogEvent.Formatter);

            await _importRuleRepository.Remove(AmbientContext, rule.ImportRuleId);

            return RedirectToAction("Details", "RewritingRuleSet", new { id = rule.ImportRuleSetId });
        }
    }
}