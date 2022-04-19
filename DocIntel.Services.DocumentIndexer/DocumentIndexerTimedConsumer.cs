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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DocIntel.Services.DocumentIndexer;

public class DocumentIndexerTimedConsumer : DynamicContextConsumer, IHostedService, IDisposable
{
    private readonly IDocumentIndexingUtility _indexingUtility;
    private readonly ILogger<DocumentIndexerTimedConsumer> _logger;
    private readonly IDocumentRepository _documentRepository;
    private Timer? _timer;
    private int executionCount;

    public DocumentIndexerTimedConsumer(ILogger<DocumentIndexerTimedConsumer> logger,
        IDocumentRepository documentRepository,
        ApplicationSettings appSettings,
        IServiceProvider serviceProvider,
        AppUserClaimsPrincipalFactory userClaimsPrincipalFactory, IDocumentIndexingUtility indexingUtility)
        : base(appSettings, serviceProvider, userClaimsPrincipalFactory)
    {
        _logger = logger;
        _documentRepository = documentRepository;
        _indexingUtility = indexingUtility;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service running.");

        var fromMinutes = TimeSpan.FromMinutes(2);
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
        var listAsync = await _documentRepository.GetAllAsync(ambientContext,
                _ => _.Include(__ => __.Source).Include(__ => __.Comments).Include(__ => __.Files)
                    .Include(__ => __.DocumentTags).ThenInclude(__ => __.Tag).ThenInclude(__ => __.Facet)
                    .Where(__ => __.LastIndexDate == DateTime.MinValue 
                                 || __.LastIndexDate == DateTime.MaxValue 
                                 || __.ModificationDate - __.LastIndexDate > TimeSpan.FromMinutes(30)))
            .ToListAsync();

        foreach (var document in listAsync)
            try
            {
                if (document.Status != DocumentStatus.Registered) continue;
                _indexingUtility.Update(document);
                document.LastIndexDate = DateTime.UtcNow;
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.Unauthorized,
                    new LogEvent(
                            $"User '{ambientContext.CurrentUser.UserName}' attempted to retrieve document without legitimate rights.")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("document.id", document.DocumentId),
                    null,
                    LogEvent.Formatter);
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EntityNotFound,
                    new LogEvent(
                            $"User '{ambientContext.CurrentUser.UserName}' attempted to retrieve a non-existing document.")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("document.id", document.DocumentId),
                    null,
                    LogEvent.Formatter);
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Warning, EventIDs.EntityNotFound,
                    new LogEvent($"User '{ambientContext.CurrentUser.UserName}' could not index the document: " +
                                 e.GetType() + " (" + e.Message + ")")
                        .AddUser(ambientContext.CurrentUser)
                        .AddProperty("document.id", document.DocumentId)
                        .AddException(e),
                    null,
                    LogEvent.Formatter);
                _logger.LogDebug(e.StackTrace);
            }
        await ambientContext.DatabaseContext.SaveChangesAsync();
    }
}