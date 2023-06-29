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
using DocIntel.Core.Utils.Thumbnail;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DocIntel.Services.Thumbnailer;

public class ThumbnailerTimedConsumer : DynamicContextConsumer, IHostedService, IDisposable
{
    private readonly ILogger<ThumbnailerTimedConsumer> _logger;
    private readonly IDocumentRepository _documentRepository;
    private readonly IThumbnailUtility _utility;
    private Timer? _timer;
    private int executionCount;

    private static int currentlyRunning = 0;
    
    public ThumbnailerTimedConsumer(ILogger<ThumbnailerTimedConsumer> logger,
        IDocumentRepository documentRepository,
        AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
        IThumbnailUtility utility, ApplicationSettings appSettings, IServiceProvider serviceProvider,
        UserManager<AppUser> userManager)
        : base(appSettings, serviceProvider, userClaimsPrincipalFactory, userManager)
    {
        _logger = logger;
        _documentRepository = documentRepository;
        _utility = utility;
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
        if (0 == Interlocked.Exchange(ref currentlyRunning, 1))
        {
        var count = Interlocked.Increment(ref executionCount);
        _logger.LogInformation(
            "Timed Hosted Service is working. Count: {Count}", count);

        using var scope = _serviceProvider.CreateScope();
        using var ambientContext = await GetAmbientContext(scope.ServiceProvider);
        var listAsync = await _documentRepository.GetAllAsync(ambientContext,
                _ => _.Include(__ => __.Files).Where(__ => __.ThumbnailId == null))
            .ToListAsync();

        foreach (var document in listAsync)
            try
            {
                await _utility.GenerateThumbnail(ambientContext, document);
                await ambientContext.DatabaseContext.SaveChangesAsync();
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    ThumbnailerMessageConsumer.Unauthorized,
                    new LogEvent(
                            $"User '{ambientContext.CurrentUser.UserName}' attempted to retreive document without legitimate rights.")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("document.id", document.DocumentId),
                    null,
                    LogEvent.Formatter);
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    ThumbnailerMessageConsumer.EntityNotFound,
                    new LogEvent(
                            $"User '{ambientContext.CurrentUser.UserName}' attempted to retreive a non-existing document.")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("document.id", document.DocumentId),
                    null,
                    LogEvent.Formatter);
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Warning,
                    ThumbnailerMessageConsumer.EntityNotFound,
                    new LogEvent($"User '{ambientContext.CurrentUser.UserName}' could not thumbnail the document: " +
                                 e.GetType() + " (" + e.Message + ")")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("document.id", document.DocumentId)
                        .AddException(e),
                    null,
                    LogEvent.Formatter);
                _logger.LogDebug(e.StackTrace);
            }

        Interlocked.Exchange(ref currentlyRunning, 0);
        }
        else
        {
            _logger.LogInformation(
                $"Timed Hosted Service is still running. Skipping this beat.");   
        }
    }
}