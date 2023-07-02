/* DocIntel
 * Copyright (C) 2018-2023 Belgian Defense, Antoine Cailliau, Kevin Menten
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
using System.Threading.Tasks;
using DocIntel.Core.Authorization;
using DocIntel.Core.Messages;
using DocIntel.Core.Models;
using DocIntel.Core.Services;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils;
using DocIntel.Core.Utils.Observables;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Synsharp.Telepath;

namespace DocIntel.Services.DocumentAnalyzer;

public class DocumentAnalyzerMessageConsumer : DynamicContextConsumer, 
    IConsumer<DocumentCreatedMessage>,
    IConsumer<DocumentAnalysisRequest>
{
    private readonly ILogger<DocumentAnalyzerMessageConsumer> _logger;
    private readonly DocumentAnalyzerUtility _documentAnalyzerUtility;
    private readonly TelepathClient _telepathClient;

    public DocumentAnalyzerMessageConsumer(ILogger<DocumentAnalyzerMessageConsumer> logger,
        DocumentAnalyzerUtility documentAnalyzerUtility,
        ApplicationSettings appSettings,
        IServiceProvider serviceProvider,
        AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
        UserManager<AppUser> userManager)
        : base(appSettings, serviceProvider, userClaimsPrincipalFactory, userManager)
    {
        _logger = logger;
        _documentAnalyzerUtility = documentAnalyzerUtility;
        _logger?.LogTrace("A new instance of DocumentAnalyzerMessageConsumer was created");

        var settings = serviceProvider.GetRequiredService<SynapseSettings>();
        var uriBuilder = new UriBuilder(settings.URL);
                
        if (string.IsNullOrEmpty(uriBuilder.UserName) && !string.IsNullOrEmpty(settings.UserName)) 
            uriBuilder.UserName = settings.UserName;
        if (string.IsNullOrEmpty(uriBuilder.Password) && !string.IsNullOrEmpty(settings.Password)) 
            uriBuilder.Password = settings.Password;
                
        _telepathClient = new TelepathClient(uriBuilder.ToString());
        _telepathClient.OnConnect += (_, _) => 
            _logger?.LogTrace("TelepathClient for DocumentAnalyzerMessageConsumer connected");
        _telepathClient.OnDisconnect += (_, _) => 
            _logger?.LogTrace("TelepathClient for DocumentAnalyzerMessageConsumer disconnected");
    }

    public async Task Consume(ConsumeContext<DocumentCreatedMessage> context)
    {
        _logger.LogDebug("DocumentCreatedMessage received by {MessageReceiver} for document {DocumentId}", 
            nameof(DocumentAnalyzerMessageConsumer),
            context.Message.DocumentId);
            
        try
        {
            await Analyze(context.Message.DocumentId);
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "Document {DocumentId} could not be analyzed ({ErrorMessage})",
                context.Message.DocumentId,
                e.Message);
            _logger.LogDebug("{ErrorMessage}\n{StackTrace}", e.Message, e.StackTrace);
        }
    }

    public async Task Consume(ConsumeContext<DocumentAnalysisRequest> context)
    {
        _logger.LogDebug("DocumentAnalysisRequest received by {MessageReceiver} for document {DocumentId}", 
            nameof(DocumentAnalyzerMessageConsumer),
            context.Message.DocumentId);
            
        try
        {
            await Analyze(context.Message.DocumentId);
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "Document {DocumentId} could not be analyzed ({ErrorMessage})",
                context.Message.DocumentId,
                e.Message);
            _logger.LogDebug("{ErrorMessage}\n{StackTrace}", e.Message, e.StackTrace);
        }
    }

    private async Task Analyze(Guid documentId)
    {
        using var scope = _serviceProvider.CreateScope();
        using var ambientContext = await GetAmbientContext(scope.ServiceProvider);

        ISynapseRepository observablesRepository = new SynapseRepository(_telepathClient,
            scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
        await _documentAnalyzerUtility.Analyze(documentId, ambientContext, observablesRepository);
    }
}