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
using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Scrapers;
using DocIntel.WebApp.Areas.API.Models;
using DocIntel.WebApp.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DocIntel.WebApp.Areas.API.Controllers;

[Area("API")]
[Route("API/Scraper")]
[ApiController]
public class ScraperController : DocIntelAPIControllerBase
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IScraperRepository _scraperRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IServiceProvider _serviceProvider;

    public ScraperController(UserManager<AppUser> userManager,
        DocIntelContext context,
        ILogger<ScraperController> logger,
        IScraperRepository scraperRepository,
        IHttpContextAccessor accessor,
        IMapper mapper, IGroupRepository groupRepository, IServiceProvider serviceProvider)
        : base(userManager, context)
    {
        _logger = logger;
        _scraperRepository = scraperRepository;
        _accessor = accessor;
        _mapper = mapper;
        _groupRepository = groupRepository;
        _serviceProvider = serviceProvider;
    }

    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<APIScraperDetails>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Produces("application/json")]
    public async Task<IActionResult> Index()
    {
        var currentUser = await GetCurrentUser();

        try
        {
            return Ok(_mapper.Map<IEnumerable<APIScraperDetails>>(
                await _scraperRepository.GetAllAsync(AmbientContext).ToListAsync()
                ));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.ListScraperFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to list scraper without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
    }

    [HttpGet("{scraperId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIScraperDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Details(Guid scraperId)
    {
        var currentUser = await GetCurrentUser();
        try
        {
            var scraper = await _scraperRepository.GetAsync(AmbientContext, scraperId);
            return Ok(_mapper.Map<APIScraperDetails>(scraper));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DetailsScraperFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to view details of scraper '{scraperId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("scraper.id", scraperId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DetailsScraperFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to view details of a non-existing scraper '{scraperId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("scraper.id", scraperId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }

    [HttpPost("{moduleId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(APIScraperDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> Create([FromRoute] Guid moduleId, [FromBody] APIScraper submittedScraper)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            if (ModelState.IsValid)
            {
                var instance =
                    await ScraperFactory.CreateScraper(moduleId, _serviceProvider, AmbientContext);

                var scraper = instance.Install();
                scraper.Name = submittedScraper.Name;
                scraper.Description = submittedScraper.Description;
                scraper.Enabled = submittedScraper.Enabled;
                scraper.Settings = submittedScraper.Settings;
                scraper.ReferenceClass = submittedScraper.ReferenceClass;
                scraper.OverrideSource = submittedScraper.OverrideSource;
                scraper.SourceId = submittedScraper.SourceId;
                scraper.SkipInbox = submittedScraper.SkipInbox;
                scraper.Position = submittedScraper.Position;
                scraper.OverrideClassification = submittedScraper.OverrideClassification;
                scraper.ClassificationId = submittedScraper.ClassificationId;
                scraper.OverrideReleasableTo = submittedScraper.OverrideReleasableTo;
                scraper.OverrideEyesOnly = submittedScraper.OverrideEyesOnly;

                var filteredRelTo = await _groupRepository
                    .GetAllAsync(AmbientContext, new GroupQuery {Id = submittedScraper.ReleasableToId.ToArray()}).ToListAsync();
                var filteredEyes = await _groupRepository
                    .GetAllAsync(AmbientContext, new GroupQuery {Id = submittedScraper.EyesOnlyId.ToArray()}).ToListAsync();

                scraper.ReleasableTo = filteredRelTo;
                scraper.EyesOnly = filteredEyes;

                await _scraperRepository.CreateAsync(AmbientContext, scraper);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information, EventIDs.CreateScraperSuccess,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully created a new scraper '{submittedScraper.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddScraper(scraper),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<APIScraperDetails>(scraper));
            }

            return BadRequest(ModelState);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.CreateScraperFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to create a new scraper '{submittedScraper.Name}' without legitimate rights.")
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
                EventIDs.CreateScraperFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to create a new scraper with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return BadRequest(ModelState);
        }
    }

    [HttpPatch("{scraperId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(APIScraperDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Edit([FromRoute] Guid scraperId, [FromBody] APIScraper submittedScraper)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var scraper = await _scraperRepository.GetAsync(AmbientContext, scraperId);
            
            if (ModelState.IsValid)
            {
                scraper.Name = submittedScraper.Name;
                scraper.Description = submittedScraper.Description;
                scraper.Enabled = submittedScraper.Enabled;
                scraper.Settings = submittedScraper.Settings;
                scraper.ReferenceClass = submittedScraper.ReferenceClass;
                scraper.OverrideSource = submittedScraper.OverrideSource;
                scraper.SourceId = submittedScraper.SourceId;
                scraper.SkipInbox = submittedScraper.SkipInbox;
                scraper.Position = submittedScraper.Position;
                scraper.OverrideClassification = submittedScraper.OverrideClassification;
                scraper.ClassificationId = submittedScraper.ClassificationId;
                scraper.OverrideReleasableTo = submittedScraper.OverrideReleasableTo;
                scraper.OverrideEyesOnly = submittedScraper.OverrideEyesOnly;
                
                var filteredRelTo = await _groupRepository
                    .GetAllAsync(AmbientContext, new GroupQuery {Id = submittedScraper.ReleasableToId.ToArray()}).ToListAsync();
                var filteredEyes = await _groupRepository
                    .GetAllAsync(AmbientContext, new GroupQuery {Id = submittedScraper.EyesOnlyId.ToArray()}).ToListAsync();

                scraper.ReleasableTo = filteredRelTo;
                scraper.EyesOnly = filteredEyes;
                
                await _scraperRepository.UpdateAsync(AmbientContext, scraper);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information, EventIDs.EditScraperSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully edited scraper '{scraper.Name}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddScraper(scraper),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<APIScraper>(scraper));
            }

            throw new InvalidArgumentException(ModelState);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.EditScraperFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit scraper '{scraperId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("scraper.id", scraperId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.EditScraperFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit a non-existing scraper '{scraperId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("scraper.id", scraperId),
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
                EventIDs.EditScraperFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit scraper '{scraperId}' with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("scraper.id", scraperId),
                null,
                LogEvent.Formatter);

            return BadRequest(ModelState);
        }
    }

    [HttpDelete("{scraperId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> Delete(Guid scraperId)
    {
        var currentUser = await GetCurrentUser();
        try
        {
            await _scraperRepository.RemoveAsync(AmbientContext, scraperId);
            await _context.SaveChangesAsync();

            _logger.Log(LogLevel.Information,
                EventIDs.DeleteScraperSuccess,
                new LogEvent($"User '{currentUser.UserName}' successfully deleted scraper '{scraperId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("scraper.id", scraperId),
                null,
                LogEvent.Formatter);

            return Ok();
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DeleteScraperFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to delete a new scraper '{scraperId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("scraper.id", scraperId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DeleteScraperFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to delete a non-existing scraper '{scraperId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("scraper.id", scraperId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }
}