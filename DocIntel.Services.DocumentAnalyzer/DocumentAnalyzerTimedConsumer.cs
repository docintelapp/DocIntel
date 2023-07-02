using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocIntel.Core.Authorization;
using DocIntel.Core.Messages;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Services;
using DocIntel.Core.Settings;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DocIntel.Services.DocumentAnalyzer;

public class DocumentAnalyzerTimedConsumer : DynamicContextConsumer, IHostedService, IDisposable
{
    private int _executionCount;
    private Timer _timer;
    
    private readonly ILogger<DocumentAnalyzerTimedConsumer> _logger;
    private readonly IDocumentRepository _documentRepository;
    private readonly IPublishEndpoint _busClient;

    private static int _currentlyRunning;

    public DocumentAnalyzerTimedConsumer(ILogger<DocumentAnalyzerTimedConsumer> logger,
        IDocumentRepository documentRepository,
        ApplicationSettings appSettings,
        IServiceProvider serviceProvider,
        AppUserClaimsPrincipalFactory userClaimsPrincipalFactory, 
        UserManager<AppUser> userManager, 
        IPublishEndpoint busClient)
        : base(appSettings, serviceProvider, userClaimsPrincipalFactory, userManager)
    {
        _logger = logger;
        _documentRepository = documentRepository;
        _busClient = busClient;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service running");
        var period = TimeSpan.FromMinutes(_appSettings.Schedule.AnalyzerFrequencyCheck);
        var dueTime = TimeSpan.FromMinutes(_appSettings.Schedule.AnalyzerWaitCheck);
        _timer = new Timer(async _ => await DoWork(), null, dueTime, period);
        return Task.CompletedTask;
    }

    private async Task DoWork()
    {
        if (0 == Interlocked.Exchange(ref _currentlyRunning, 1))
        {
            var count = Interlocked.Increment(ref _executionCount);
            _logger.LogInformation(
                "Timed Hosted Service got the lock. Count: {Count}", count);
                            
            using var scope = _serviceProvider.CreateScope();
            using var ambientContext = await GetAmbientContext(scope.ServiceProvider);

            foreach (var doc in await _documentRepository.GetAllAsync(ambientContext,
                         _ => _.Where(__ => __.Status == DocumentStatus.Submitted)).ToListAsync())
            {
                await _busClient.Publish(new DocumentAnalysisRequest
                {
                    DocumentId = doc.DocumentId,
                    UserId = ambientContext.CurrentUser.Id
                });
            }
            /*
            while (await _documentRepository.GetAllAsync(ambientContext,
                       _ => _.Where(__ => __.Status == DocumentStatus.Submitted)).CountAsync() > 0)
            {
                // using var documentScope = _serviceProvider.CreateScope();
                // using var documentAmbientContext = await GetAmbientContext(documentScope.ServiceProvider);
                
                /*
                var submitted = await _documentRepository.GetAllAsync(documentAmbientContext,
                        _ => _.Where(__ => __.Status == DocumentStatus.Submitted))
                    .OrderByDescending(__ => __.RegistrationDate).FirstAsync();
                if (!await _documentAnalyzerUtility.Analyze(submitted.DocumentId, documentAmbientContext))
                {
                    _logger.LogError("Could not analyze document. Skipping forever.");
                    submitted.Status = DocumentStatus.Error;
                }
                */

                // await documentAmbientContext.DatabaseContext.SaveChangesAsync();
            //}

            Interlocked.Exchange(ref _currentlyRunning, 0);
        }
        else
        {
            _logger.LogInformation(
                $"Timed Hosted Service is still running. Skipping this beat.");   
        }
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service is stopping");

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
