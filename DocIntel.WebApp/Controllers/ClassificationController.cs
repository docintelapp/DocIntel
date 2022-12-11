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
using System.Linq;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
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
    ///     Provides functionalities for managing classifications. Classifications can be assigned to
    ///     users (a user can have multiple classifications). A user is also a member of all
    ///     the parent classifications of classifications he is a member.
    /// </summary>
    public class ClassificationController : BaseController
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IAppAuthorizationService _appAuthorizationService;
        private readonly ApplicationSettings _appSettings;
        private readonly IClassificationRepository _classificationRepository;
        private readonly ILogger _logger;

        public ClassificationController(IAppAuthorizationService appAuthorizationService,
            IClassificationRepository classificationRepository,
            AppUserManager userManager,
            ApplicationSettings configuration,
            ILogger<ClassificationController> logger,
            DocIntelContext context,
            IAuthorizationService authorizationService,
            IHttpContextAccessor accessor, ApplicationSettings appSettings)
            : base(context,
                userManager,
                configuration,
                authorizationService)
        {
            _logger = logger;
            _appAuthorizationService = appAuthorizationService;
            _classificationRepository = classificationRepository;
            _accessor = accessor;
            _appSettings = appSettings;
        }

        /// <summary>
        ///     Provides the listing of classifications
        /// </summary>
        /// <returns>
        ///     A view with the listing of the classifications. A "Unauthorized" response
        ///     if the user is not allowed to view the results.
        /// </returns>
        public async Task<IActionResult> Index()
        {
            var currentUser = await GetCurrentUser();
            try
            {
                _logger.Log(LogLevel.Information, EventIDs.ListClassificationSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully list the classifications.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                ViewBag.DefaultClassification = _classificationRepository.GetDefault(AmbientContext);

                return View(_classificationRepository
                    .GetAllAsync(AmbientContext, new[] {"ParentClassification"}).ToEnumerable());
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.ListClassificationFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to list classifications without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
        }

        [HttpGet("Classification/Details/{id}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var classification = await _classificationRepository.GetAsync(AmbientContext,
                    id,
                    new[] {"ParentClassification"});

                _logger.Log(LogLevel.Information, EventIDs.DetailsClassificationSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully viewed details of '{classification.Title}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddClassification(classification),
                    null,
                    LogEvent.Formatter);

                return View(classification);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.DetailsClassificationFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view details of classification '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("classification.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.DetailsClassificationFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view details of a non-existing classification '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("classification.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        public async Task<IActionResult> Create()
        {
            var currentUser = await GetCurrentUser();

            if (!await _appAuthorizationService.CanAddClassification(User, null))
            {
                _logger.Log(LogLevel.Warning, EventIDs.CreateClassificationFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new classification without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }

            ViewBag.AllClassifications =
                await _classificationRepository.GetAllAsync(AmbientContext).ToListAsync();

            return View(new Classification());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Title", "Subtitle", "Abbreviation", "ParentClassificationId", "Description", "Color", "Default")]
            Classification viewModel)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                if (ModelState.IsValid)
                {
                    var classification = new Classification
                    {
                        Title = viewModel.Title,
                        Subtitle = viewModel.Subtitle,
                        ParentClassificationId = viewModel.ParentClassificationId,
                        Description = viewModel.Description,
                        Color = viewModel.Color,
                        Abbreviation = viewModel.Abbreviation,
                        Default = viewModel.Default
                    };

                    await _classificationRepository.AddAsync(AmbientContext, classification);
                    await _context.SaveChangesAsync();

                    _logger.Log(LogLevel.Information, EventIDs.CreateClassificationSuccessful,
                        new LogEvent(
                                $"User '{currentUser.UserName}' successfully created a new classification '{viewModel.Title}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddClassification(classification),
                        null,
                        LogEvent.Formatter);

                    return RedirectToAction(nameof(Index));
                }

                throw new InvalidArgumentException(ModelState);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.CreateClassificationFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new classification '{viewModel.Title}' without legitimate rights.")
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

                _logger.Log(LogLevel.Information, EventIDs.CreateClassificationFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new classification '{viewModel.Title}' with an invalid model.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddClassification(viewModel),
                    null,
                    LogEvent.Formatter);

                ViewBag.AllClassifications =
                    await _classificationRepository.GetAllAsync(AmbientContext).ToListAsync();
                return View(viewModel);
            }
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var classification = await _classificationRepository.GetAsync(AmbientContext,
                    id,
                    new string[] { });

                _logger.Log(LogLevel.Information, EventIDs.EditClassificationSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully displayed form for editing '{classification.Title}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                ViewBag.AllClassifications = await _classificationRepository.GetAllAsync(AmbientContext)
                    .Where(_ => _.ClassificationId != id).ToListAsync();

                return View(classification);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditClassificationFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to display form for editing classification '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("classification.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditClassificationFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to display form for editing a non-existing classification '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("classification.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            [Bind("ClassificationId", "Title", "Subtitle", "Abbreviation", "ParentClassificationId", "Description",
                "Color", "Default")]
            Classification viewModel)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var classification = await _classificationRepository.GetAsync(AmbientContext,
                    viewModel.ClassificationId
                );

                if (ModelState.IsValid)
                {
                    classification.Title = viewModel.Title;
                    classification.Subtitle = viewModel.Subtitle;
                    classification.Abbreviation = viewModel.Abbreviation;
                    classification.ParentClassificationId = viewModel.ParentClassificationId;
                    classification.Description = viewModel.Description;
                    classification.Color = viewModel.Color;
                    classification.Default = viewModel.Default;

                    await _classificationRepository.UpdateAsync(AmbientContext, classification);
                    await _context.SaveChangesAsync();

                    _logger.Log(LogLevel.Information, EventIDs.EditClassificationSuccessful,
                        new LogEvent(
                                $"User '{currentUser.UserName}' successfully edited classification '{viewModel.ClassificationId}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddClassification(classification),
                        null,
                        LogEvent.Formatter);

                    return RedirectToAction(nameof(Index));
                }

                throw new InvalidArgumentException(ModelState);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EditClassificationFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit classification '{viewModel.ClassificationId}' without legitimate rights.")
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

                _logger.Log(LogLevel.Information, EventIDs.EditClassificationFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit classification '{viewModel.ClassificationId}' with an invalid model.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddClassification(viewModel),
                    null,
                    LogEvent.Formatter);
                ViewBag.AllClassifications = await _classificationRepository.GetAllAsync(AmbientContext)
                    .Where(_ => _.ClassificationId != viewModel.ClassificationId).ToListAsync();

                return View(viewModel);
            }
        }

        [HttpGet("Classification/Delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var classification = await _classificationRepository.GetAsync(AmbientContext,
                    id
                );

                _logger.Log(LogLevel.Information, EventIDs.DeleteClassificationSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully requested to delete '{classification.Title}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return View(classification);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.DeleteClassificationFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to request delete classification '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("classification.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.DeleteClassificationFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to request delete a non-existing classification '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("classification.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpPost("Classification/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, [Bind("ClassificationId")] Classification viewModel)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var classification = await _classificationRepository.GetAsync(AmbientContext,
                    id
                );

                await _classificationRepository.RemoveAsync(AmbientContext, id);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information, EventIDs.DeleteClassificationSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully created a new classification '{viewModel.Title}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddClassification(classification),
                    null,
                    LogEvent.Formatter);

                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.DeleteClassificationFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to delete classification '{viewModel.ClassificationId}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("classification.id", viewModel.ClassificationId),
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

                _logger.Log(LogLevel.Information, EventIDs.DeleteClassificationFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to delete classification '{viewModel.ClassificationId}' with an invalid model.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("classification.id", viewModel.ClassificationId),
                    null,
                    LogEvent.Formatter);

                return View(viewModel);
            }
        }
    }
}