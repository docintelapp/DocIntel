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
using DocIntel.Core.Utils.Search.Sources;
using DocIntel.WebApp.Areas.API.Models;
using DocIntel.WebApp.Helpers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DocIntel.WebApp.Areas.API.Controllers
{
    [Area("API")]
    [Route("API/Source")]
    [ApiController]
    public class SourceController : DocIntelAPIControllerBase
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;

        private readonly ISourceRepository _sourceRepository;

        private readonly ISourceSearchService _sourceSearchEngine;

        public SourceController(DocIntelContext context,
            ILogger<SourceController> logger,
            UserManager<AppUser> userManager,
            ISourceSearchService sourceSearchEngine,
            ISourceRepository sourceRepository,
            IHttpContextAccessor accessor,
            IMapper mapper)
            : base(userManager, context)
        {
            _logger = logger;
            _sourceSearchEngine = sourceSearchEngine;
            _sourceRepository = sourceRepository;
            _accessor = accessor;
            _mapper = mapper;
        }

        [HttpGet("")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<APISource>))]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Index(
            string searchTerm = "",
            SourceSortCriteria sortCriteria = SourceSortCriteria.Title,
            int page = 1, int pageSize = 10)
        {
            var currentUser = await GetCurrentUser();

            var query = new SourceSearchQuery
            {
                SearchTerms = searchTerm,
                SortCriteria = sortCriteria,
                Page = page,
                PageSize = pageSize
            };

            var searchResults = _sourceSearchEngine.Search(query);

            var resultsSource = new List<Source>();
            foreach (var s in searchResults.Hits.OrderBy(_ => _.Position))
                try
                {
                    resultsSource.Add(await _sourceRepository.GetAsync(AmbientContext, s.Source.SourceId));
                }
                catch (NotFoundEntityException e)
                {
                    _logger.Log(LogLevel.Error,
                        EventIDs.APIListSourceFailed,
                        new LogEvent($"Source '{s.Source.SourceId}' was found in the index but not in database.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddException(e),
                        null,
                        LogEvent.Formatter);
                }

            _logger.Log(LogLevel.Information,
                EventIDs.APIListSourceSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully listed sources.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return Ok(_mapper.Map<IEnumerable<APISource>>(resultsSource));
        }
        
        [HttpGet("Suggest")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<APISource>))]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Suggest(string searchTerm = "")
        {
            var currentUser = await GetCurrentUser();

            var query = new SourceSearchQuery
            {
                SearchTerms = searchTerm
            };

            var searchResults = _sourceSearchEngine.Suggest(query);

            var resultsSource = new List<Source>();
            foreach (var s in searchResults.Hits.OrderBy(_ => _.Position))
                try
                {
                    resultsSource.Add(await _sourceRepository.GetAsync(AmbientContext, s.Source.SourceId));
                }
                catch (NotFoundEntityException e)
                {
                    _logger.Log(LogLevel.Error,
                        EventIDs.APIListSourceFailed,
                        new LogEvent($"Source '{s.Source.SourceId}' was found in the index but not in database.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddException(e),
                        null,
                        LogEvent.Formatter);
                }

            _logger.Log(LogLevel.Information,
                EventIDs.APIListSourceSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully listed sources.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return Ok(_mapper.Map<IEnumerable<APISource>>(resultsSource));
        }

        [HttpGet("Details")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APISource))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Details(Guid id)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var source = await _sourceRepository.GetAsync(AmbientContext, id);
                return Ok(_mapper.Map<APISource>(source));
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDetailsSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view details of source '{id}' without legitimate rights.")
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
                    EventIDs.APIDetailsSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view details of a non-existing source '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpPost("")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Source))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Dictionary<string, List<string>>))]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Create([FromBody] APISource submittedSource)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var source = new Source
                {
                    Title = submittedSource.Title,
                    Description = submittedSource.Description,
                    HomePage = submittedSource.HomePage,
                    RSSFeed = submittedSource.RSSFeed,
                    Facebook = submittedSource.Facebook,
                    Twitter = submittedSource.Twitter,
                    Reddit = submittedSource.Reddit,
                    LinkedIn = submittedSource.LinkedIn
                };

                if (submittedSource.Keywords?.Any() ?? false)
                    source.Keywords = string.Join(", ", submittedSource.Keywords.Select(_ => _.Trim()));

                await _sourceRepository.CreateAsync(AmbientContext, source);

                _logger.Log(LogLevel.Information,
                    EventIDs.APICreateSourceSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully created a new source.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddSource(source),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<APISource>(source));
            }
            catch (InvalidArgumentException e)
            {
                _logger.Log(LogLevel.Information,
                    EventIDs.APICreateSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to creat a new source with an invalid model.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source.id", submittedSource.SourceId),
                    null,
                    LogEvent.Formatter);

                return BadRequest(e.Errors);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APICreateSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to create a new source without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
        }

        [HttpPatch("")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Source))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(APISource))]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Edit([FromBody] APISource submittedSource)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var source = await _sourceRepository.GetAsync(AmbientContext, submittedSource.SourceId);

                source.Title = submittedSource.Title;
                source.Description = submittedSource.Description;

                source.Reliability = submittedSource.Reliability;

                source.HomePage = submittedSource.HomePage;
                source.RSSFeed = submittedSource.RSSFeed;
                source.Facebook = submittedSource.Facebook;
                source.Twitter = submittedSource.Twitter;
                source.Reddit = submittedSource.Reddit;
                source.LinkedIn = submittedSource.LinkedIn;
                source.Country = submittedSource.Country;

                if (submittedSource.Keywords != null && submittedSource.Keywords.Any())
                    source.Keywords = string.Join(", ", submittedSource.Keywords.Select(_ => _.Trim()));

                await _sourceRepository.UpdateAsync(AmbientContext, source);

                _logger.Log(LogLevel.Information,
                    EventIDs.APIEditSourceSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully edited source '{source.Title}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddSource(source),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<APISource>(source));
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIEditSourceFailed,
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
                    EventIDs.APIEditSourceFailed,
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
                    EventIDs.APIEditSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit source '{submittedSource.SourceId}' with an invalid model.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source.id", submittedSource.SourceId),
                    null,
                    LogEvent.Formatter);

                return BadRequest(submittedSource);
            }
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                await _sourceRepository.RemoveAsync(AmbientContext, id);

                _logger.Log(LogLevel.Information,
                    EventIDs.APIDeleteSourceSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' deleted source '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source.id", id),
                    null,
                    LogEvent.Formatter);

                return Ok();
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDeleteSourceFailed,
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
                    EventIDs.APIDeleteSourceFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to edit a non-existing source '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpPost("Merge")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Merge(Guid primarySourceId, Guid secondarySourceId)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                if (primarySourceId == secondarySourceId)
                    return Ok();

                await _sourceRepository.MergeAsync(AmbientContext, primarySourceId, secondarySourceId);

                _logger.Log(LogLevel.Information,
                    EventIDs.APIMergeSourceSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully merged source '{primarySourceId}' with '{secondarySourceId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source_primary.id", primarySourceId)
                        .AddProperty("source_secondary.id", secondarySourceId),
                    null,
                    LogEvent.Formatter);

                return Ok();
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIMergeSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to merge source '{primarySourceId}' with '{secondarySourceId}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source_primary.id", primarySourceId)
                        .AddProperty("source_secondary.id", secondarySourceId),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIMergeSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to merge a non-existing source '{primarySourceId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpPut("Subscribe")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Subscribe(Guid id, bool notification = false)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                await _sourceRepository.SubscribeAsync(AmbientContext, AmbientContext.CurrentUser, id, notification);

                _logger.Log(LogLevel.Information,
                    EventIDs.APISubscribeSourceSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully subscribed to source '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source.id", id),
                    null,
                    LogEvent.Formatter);

                return Ok();
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APISubscribeSourceFailed,
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
                    EventIDs.APISubscribeSourceFailed,
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

        [HttpPut("Unsubscribe")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Unsubscribe(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                await _sourceRepository.UnsubscribeAsync(AmbientContext, AmbientContext.CurrentUser, id);

                _logger.Log(LogLevel.Information,
                    EventIDs.APISubscribeSourceSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully unsubscribed to source '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source.id", id),
                    null,
                    LogEvent.Formatter);

                return Ok();
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APISubscribeSourceFailed,
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
                    EventIDs.APISubscribeSourceFailed,
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

        [HttpGet("Statistics")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> GetStatisticsAsync(Guid id)
        {
            // TODO Use SolR to build the statistics, it will be much more efficient.

            var currentUser = await GetCurrentUser();
            var period = (int) (DateTime.UtcNow - DateTime.UtcNow.AddMonths(-6)).TotalDays;
            try
            {
                var source = await _sourceRepository.GetAsync(AmbientContext, id);
                var docStats = source.Documents
                    .Where(_ => (DateTime.UtcNow - _.Files.Min(__ => __.DocumentDate)).TotalDays <= period)
                    .Select(_ =>
                        new
                        {
                            Date = new DateTime(_.Files.Min(__ => __.DocumentDate).Year,
                                _.Files.Min(__ => __.DocumentDate).Month, _.Files.Min(__ => __.DocumentDate).Day)
                        }
                    ).GroupBy(_ => _.Date)
                    .Select(_ => new
                    {
                        Date = _.Key,
                        Value = _.Count()
                    });

                var today = DateTime.UtcNow;
                var start = new DateTime(today.Year, today.Month, today.Day);
                var temp = from d in Enumerable.Range(0, period).Select(_ => start.AddDays(-_))
                    join s in docStats on d.Date equals s.Date into ds
                    from sds in ds.DefaultIfEmpty()
                    select new {Date = d.ToString("yyyy-MM-dd"), Value = sds?.Value ?? 0};

                return Ok(new
                {
                    DataByTopic = new[] {new {Topic = -1, TopicName = "Documents", Dates = temp.OrderBy(_ => _.Date)}}
                });
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APISatisticsSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to get statistics for source '{id}' without legitimate rights.")
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
                    EventIDs.APISatisticsSourceFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to get statistics for a non-existing source '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("source.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }
    }
}