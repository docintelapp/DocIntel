using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Services;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Indexation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DocIntel.Services.TagIndexer;

public class TagIndexerTimedConsumer : DynamicContextConsumer, IHostedService, IDisposable
{
    private readonly ITagIndexingUtility _indexingUtility;
    private readonly ILogger<TagIndexerTimedConsumer> _logger;
    private readonly ITagRepository _tagRepository;
    private Timer? _timer;
    private int executionCount;

    public TagIndexerTimedConsumer(ILogger<TagIndexerTimedConsumer> logger,
        ITagRepository tagRepository,
        ApplicationSettings appSettings,
        IServiceProvider serviceProvider,
        AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
        ITagIndexingUtility indexingUtility, UserManager<AppUser> userManager)
        : base(appSettings, serviceProvider, userClaimsPrincipalFactory, userManager)
    {
        _logger = logger;
        _tagRepository = tagRepository;
        _indexingUtility = indexingUtility;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service running.");

        var fromMinutes = TimeSpan.FromMinutes(_appSettings.Schedule.IndexingFrequencyCheck);
        _timer = new Timer(DoWork, null, fromMinutes, fromMinutes);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service is stopping.");

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        var count = Interlocked.Increment(ref executionCount);
        _logger.LogInformation(
            "Timed Hosted Service is working. Count: {Count}", count);

        using var ambientContext = await GetAmbientContext();
        var listAsync = await _tagRepository.GetAllAsync(ambientContext,
                _ => _.Include(__ => __.Facet).Include(__ => __.Documents).ThenInclude(__ => __.Document)
                    .Where(__ => __.LastIndexDate == DateTime.MinValue 
                                 || __.LastIndexDate == DateTime.MaxValue 
                                 || __.Documents.Max(___ => ___.Document.DocumentDate) - __.LastIndexDate > TimeSpan.FromMinutes(_appSettings.Schedule.MaxIndexingDelay)
                                 || __.ModificationDate - __.LastIndexDate > TimeSpan.FromMinutes(_appSettings.Schedule.MaxIndexingDelay)))
            .ToListAsync();

        foreach (var tag in listAsync)
            try
            {
                _indexingUtility.Update(tag);
                tag.LastIndexDate = DateTime.UtcNow;
                await ambientContext.DatabaseContext.SaveChangesAsync();
                _logger.LogInformation("Index updated for the tag {0}", tag.TagId);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    TagIndexerMessageConsumer.Unauthorized,
                    new LogEvent(
                            $"User '{ambientContext.CurrentUser.UserName}' attempted to retrieve tag without legitimate rights.")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("tag.id", tag.TagId),
                    null,
                    LogEvent.Formatter);
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    TagIndexerMessageConsumer.EntityNotFound,
                    new LogEvent(
                            $"User '{ambientContext.CurrentUser.UserName}' attempted to retrieve a non-existing tag.")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("tag.id", tag.TagId),
                    null,
                    LogEvent.Formatter);
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Warning,
                    TagIndexerMessageConsumer.EntityNotFound,
                    new LogEvent($"User '{ambientContext.CurrentUser.UserName}' could not index the tag: " +
                                 e.GetType() + " (" + e.Message + ")")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("tag.id", tag.TagId)
                        .AddException(e),
                    null,
                    LogEvent.Formatter);
                _logger.LogDebug(e.StackTrace);
            }
        await ambientContext.DatabaseContext.SaveChangesAsync();
    }
}