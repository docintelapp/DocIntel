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
using System.Linq;
using System.Threading.Tasks;
using DocIntel.Core.Authorization;
using DocIntel.Core.Messages;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Services;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocIntel.Services.DocumentAnalyzer
{
    public class DocumentAnalyzerMessageConsumer : DynamicContextConsumer, 
        IConsumer<DocumentCreatedMessage>/*, 
        IConsumer<FileCreatedMessage>, 
        IConsumer<FileUpdatedMessage>*/
    {
        private readonly ILogger<DocumentAnalyzerMessageConsumer> _logger;
        private readonly DocumentAnalyzerUtility _documentAnalyzerUtility;
        private readonly IDocumentRepository _documentRepository;

        public DocumentAnalyzerMessageConsumer(ILogger<DocumentAnalyzerMessageConsumer> logger,
            DocumentAnalyzerUtility documentAnalyzerUtility,
            ApplicationSettings appSettings,
            IServiceProvider serviceProvider,
            AppUserClaimsPrincipalFactory userClaimsPrincipalFactory, IDocumentRepository documentRepository,
            UserManager<AppUser> userManager)
            : base(appSettings, serviceProvider, userClaimsPrincipalFactory, userManager)
        {
            _logger = logger;
            _documentAnalyzerUtility = documentAnalyzerUtility;
            _documentRepository = documentRepository;
        }

        public async Task Consume(ConsumeContext<DocumentCreatedMessage> context)
        {
            _logger.LogDebug("DocumentCreatedMessage: {0}", context.Message.DocumentId);
            
            try
            {
                await Analyze(context.Message.DocumentId);
            }
            catch (Exception e)
            {
                _logger.LogError($"Document {context.Message.DocumentId} could not be analyzed ({e.Message}).");
                _logger.LogError(e.StackTrace);
            }
        }

        private async Task Analyze(Guid documentId)
        {
            using var scope = _serviceProvider.CreateScope();
            using var ambientContext = await GetAmbientContext(scope.ServiceProvider);
            await _documentAnalyzerUtility.Analyze(documentId, ambientContext);
        }

        /*
        public async Task Consume(ConsumeContext<FileCreatedMessage> context)
        {
            _logger.LogDebug("FileCreatedMessage: {0}", context.Message.FileId);
            var ambientContext = await GetAmbientContext();

            try
            {
                var file = await _documentRepository.GetFileAsync(ambientContext, context.Message.FileId, new [] { "Document" });
                if (file.Document.Status != DocumentStatus.Registered)
                    await Analyze(file.DocumentId);
            }
            catch (Exception e)
            {
                _logger.LogError($"Document {context.Message.FileId} could not be analyzed ({e.Message}).");
                _logger.LogError(e.StackTrace);
            }
            
            ambientContext.Dispose();
        }

        public async Task Consume(ConsumeContext<FileUpdatedMessage> context)
        {
            _logger.LogDebug("FileCreatedMessage: {0}", context.Message.FileId);
            var ambientContext = await GetAmbientContext();

            try
            {
                var file = await _documentRepository.GetFileAsync(ambientContext, context.Message.FileId, new [] { "Document" });
                if (file.Document.Status != DocumentStatus.Registered)
                    await Analyze(file.DocumentId);
            }
            catch (Exception e)
            {
                _logger.LogError($"Document {context.Message.FileId} could not be analyzed ({e.Message}).");
                _logger.LogError(e.StackTrace);
            }
            
            ambientContext.Dispose();
        }
        */
    }
}