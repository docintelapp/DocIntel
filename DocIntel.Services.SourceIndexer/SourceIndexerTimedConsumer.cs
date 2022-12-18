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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DocIntel.Services.SourceIndexer;

public class SourceIndexerTimedConsumer : DynamicContextConsumer, IHostedService, IDisposable
{
    private readonly ISourceIndexingUtility _indexingUtility;
    private readonly ILogger<SourceIndexerTimedConsumer> _logger;
    private readonly ISourceRepository _sourceRepository;
    private Timer? _timer;
    private int executionCount;

    public SourceIndexerTimedConsumer(ILogger<SourceIndexerTimedConsumer> logger,
        ISourceRepository sourceRepository,
        ApplicationSettings appSettings,
        IServiceProvider serviceProvider,
        AppUserClaimsPrincipalFactory userClaimsPrincipalFactory, ISourceIndexingUtility indexingUtility,
        UserManager<AppUser> userManager)
        : base(appSettings, serviceProvider, userClaimsPrincipalFactory, userManager)
    {
        _logger = logger;
        _sourceRepository = sourceRepository;
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

        using var scope = _serviceProvider.CreateScope();
        using var ambientContext = await GetAmbientContext(scope.ServiceProvider);
        var listAsync = await _sourceRepository.GetAllAsync(ambientContext,
                _ => _.Include(__ => __.Documents)
                    .Where(__ => __.LastIndexDate == DateTime.MinValue 
                                 || __.LastIndexDate == DateTime.MaxValue 
                                 || __.Documents.Max(___ => ___.ModificationDate) - __.LastIndexDate > TimeSpan.FromMinutes(_appSettings.Schedule.MaxIndexingDelay)
                                 || __.ModificationDate - __.LastIndexDate > TimeSpan.FromMinutes(_appSettings.Schedule.MaxIndexingDelay)))
            .ToListAsync();

        foreach (var source in listAsync)
            try
            {
                _indexingUtility.Update(source);
                source.LastIndexDate = DateTime.UtcNow;
                _logger.LogInformation("Index updated for the source '{0}'", source.SourceId);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.Unauthorized,
                    new LogEvent(
                            $"User '{ambientContext.CurrentUser.UserName}' attempted to retreive source without legitimate rights.")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("source.id", source.SourceId),
                    null,
                    LogEvent.Formatter);
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EntityNotFound,
                    new LogEvent(
                            $"User '{ambientContext.CurrentUser.UserName}' attempted to retreive a non-existing source.")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("source.id", source.SourceId),
                    null,
                    LogEvent.Formatter);
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.EntityNotFound,
                    new LogEvent($"User '{ambientContext.CurrentUser.UserName}' could not index the source: " +
                                 e.GetType() + " (" + e.Message + ")")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("source.id", source.SourceId)
                        .AddException(e),
                    null,
                    LogEvent.Formatter);
                _logger.LogDebug(e.StackTrace);
            }
        await ambientContext.DatabaseContext.SaveChangesAsync();
    }
}