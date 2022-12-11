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
using DocIntel.Core.Authentication;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Importers;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.WebApp.Areas.API.Models;
using DocIntel.WebApp.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DocIntel.WebApp.Areas.API.Controllers;

[Area("API")]
[Route("API/Importer")]
[ApiController]
public class ImporterController : DocIntelAPIControllerBase
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IIncomingFeedRepository _importerRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IServiceProvider _serviceProvider;

    public ImporterController(AppUserManager userManager,
        DocIntelContext context,
        ILogger<ImporterController> logger,
        IIncomingFeedRepository importerRepository,
        IHttpContextAccessor accessor,
        IMapper mapper, IGroupRepository groupRepository, IServiceProvider serviceProvider)
        : base(userManager, context)
    {
        _logger = logger;
        _importerRepository = importerRepository;
        _accessor = accessor;
        _mapper = mapper;
        _groupRepository = groupRepository;
        _serviceProvider = serviceProvider;
    }

    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<APIImporterDetails>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Produces("application/json")]
    public async Task<IActionResult> Index()
    {
        var currentUser = await GetCurrentUser();

        try
        {
            return Ok(_mapper.Map<IEnumerable<APIImporterDetails>>(
                await _importerRepository.GetAllAsync(AmbientContext).ToListAsync()
                ));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.ListIncomingFeedFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to list importer without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
    }

    [HttpGet("{importerId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIImporterDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Details(Guid importerId)
    {
        var currentUser = await GetCurrentUser();
        try
        {
            var importer = await _importerRepository.GetAsync(AmbientContext, importerId);
            return Ok(_mapper.Map<APIImporterDetails>(importer));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DetailsIncomingFeedFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to view details of importer '{importerId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importer.id", importerId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DetailsIncomingFeedFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to view details of a non-existing importer '{importerId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importer.id", importerId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }

    [HttpPost("{moduleId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(APIImporterDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> Create([FromRoute] Guid moduleId, [FromBody] APIImporter submittedImporter)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            if (ModelState.IsValid)
            {
                var instance =
                    await ImporterFactory.CreateImporter(moduleId, _serviceProvider, AmbientContext);

                var incomingFeed = instance.Install();
                incomingFeed.Status = submittedImporter.Status;
                incomingFeed.CollectionDelay = submittedImporter.CollectionDelay;
                incomingFeed.FetchingUserId = submittedImporter.FetchingUserId;
                incomingFeed.Limit = submittedImporter.Limit;
                incomingFeed.OverrideClassification = submittedImporter.OverrideClassification;
                incomingFeed.OverrideReleasableTo = submittedImporter.OverrideReleasableTo;
                incomingFeed.OverrideEyesOnly = submittedImporter.OverrideEyesOnly;
                incomingFeed.ClassificationId = submittedImporter.ClassificationId;

                var filteredRelTo = await _groupRepository
                    .GetAllAsync(AmbientContext, new GroupQuery {Id = submittedImporter.ReleasableToId.ToArray()}).ToListAsync();
                var filteredEyes = await _groupRepository
                    .GetAllAsync(AmbientContext, new GroupQuery {Id = submittedImporter.EyesOnlyId.ToArray()}).ToListAsync();

                incomingFeed.ReleasableTo = filteredRelTo;
                incomingFeed.EyesOnly = filteredEyes;

                await _importerRepository.CreateAsync(AmbientContext, incomingFeed);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information, EventIDs.CreateIncomingFeedSuccess,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully created a new importer '{submittedImporter.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddIncomingFeed(incomingFeed),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<APIImporterDetails>(incomingFeed));
            }

            return BadRequest(ModelState);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.CreateIncomingFeedFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to create a new importer '{submittedImporter.Name}' without legitimate rights.")
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
                EventIDs.CreateIncomingFeedFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to create a new importer with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return BadRequest(ModelState);
        }
    }

    [HttpPatch("{importerId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(APIImporterDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Edit([FromRoute] Guid importerId, [FromBody] APIImporter submittedImporter)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var importer = await _importerRepository.GetAsync(AmbientContext, importerId);
            
            if (ModelState.IsValid)
            {
                importer.Status = submittedImporter.Status;
                importer.CollectionDelay = submittedImporter.CollectionDelay;
                importer.FetchingUserId = submittedImporter.FetchingUserId;
                importer.Limit = submittedImporter.Limit;
                importer.OverrideClassification = submittedImporter.OverrideClassification;
                importer.OverrideReleasableTo = submittedImporter.OverrideReleasableTo;
                importer.OverrideEyesOnly = submittedImporter.OverrideEyesOnly;
                importer.ClassificationId = submittedImporter.ClassificationId;

                var filteredRelTo = await _groupRepository
                    .GetAllAsync(AmbientContext, new GroupQuery {Id = submittedImporter.ReleasableToId.ToArray()}).ToListAsync();
                var filteredEyes = await _groupRepository
                    .GetAllAsync(AmbientContext, new GroupQuery {Id = submittedImporter.EyesOnlyId.ToArray()}).ToListAsync();

                importer.ReleasableTo = filteredRelTo;
                importer.EyesOnly = filteredEyes;

                await _importerRepository.UpdateAsync(AmbientContext, importer);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information, EventIDs.EditIncomingFeedSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully edited importer '{importer.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddIncomingFeed(importer),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<APIImporter>(importer));
            }

            throw new InvalidArgumentException(ModelState);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.EditIncomingFeedFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit importer '{importerId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importer.id", importerId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.EditIncomingFeedFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit a non-existing importer '{importerId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importer.id", importerId),
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
                EventIDs.EditIncomingFeedFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit importer '{importerId}' with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importer.id", importerId),
                null,
                LogEvent.Formatter);

            return BadRequest(ModelState);
        }
    }

    [HttpDelete("{importerId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Delete(Guid importerId)
    {
        var currentUser = await GetCurrentUser();
        try
        {
            await _importerRepository.RemoveAsync(AmbientContext, importerId);
            await _context.SaveChangesAsync();

            _logger.Log(LogLevel.Information,
                EventIDs.DeleteIncomingFeedSuccess,
                new LogEvent($"User '{currentUser.UserName}' successfully deleted importer '{importerId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importer.id", importerId),
                null,
                LogEvent.Formatter);

            return Ok();
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DeleteIncomingFeedFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to delete a new importer '{importerId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importer.id", importerId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DeleteIncomingFeedFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to delete a non-existing importer '{importerId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("importer.id", importerId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }
}