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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Messages;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Services;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Indexation;

using MassTransit;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocIntel.Services.DocumentIndexer;

public class DocumentIndexerMessageConsumer :
    DynamicContextConsumer,
    IConsumer<DocumentCreatedMessage>,
    IConsumer<DocumentUpdatedMessage>,
    IConsumer<DocumentRemovedMessage>,
    IConsumer<DocumentIndexMessage>,
    IConsumer<DocumentClearIndexMessage>,
    IConsumer<CommentCreatedMessage>,
    IConsumer<CommentUpdatedMessage>,
    IConsumer<CommentRemovedMessage>,
    IConsumer<TagUpdatedMessage>,
    IConsumer<TagRemovedMessage>,
    IConsumer<TagMergedMessage>,
    IConsumer<SourceUpdatedMessage>,
    IConsumer<SourceRemovedMessage>,
    IConsumer<SourceMergedMessage>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentIndexingUtility _indexingUtility;
    private readonly ILogger<DocumentIndexerMessageConsumer> _logger;

    private readonly IUserClaimsPrincipalFactory<AppUser> _userClaimsPrincipalFactory;
    private readonly ApplicationSettings _appSettings;
    private readonly IServiceProvider _serviceProvider;

    public DocumentIndexerMessageConsumer(ILogger<DocumentIndexerMessageConsumer> logger,
        IDocumentRepository documentRepository,
        IDocumentIndexingUtility indexingUtility,
        AppUserClaimsPrincipalFactory userClaimsPrincipalFactory, 
        ApplicationSettings appSettings,
        IServiceProvider serviceProvider)
        : base(appSettings, serviceProvider, userClaimsPrincipalFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _documentRepository = documentRepository;
        _indexingUtility = indexingUtility;
        _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
        _appSettings = appSettings;
    }

    public async Task Consume(ConsumeContext<CommentCreatedMessage> context)
    {
        using var ambientContext = await GetAmbientContext();
        var documentId = ambientContext.DatabaseContext.Comments
            .AsQueryable()
            .SingleOrDefault(_ => _.CommentId == context.Message.CommentId)
            ?.DocumentId;
        if (documentId != null) await UpdateIndex((Guid) documentId, ambientContext);
    }

    public async Task Consume(ConsumeContext<CommentRemovedMessage> context)
    {
        using var ambientContext = await GetAmbientContext();
        var documentId = ambientContext.DatabaseContext.Comments
            .AsQueryable()
            .SingleOrDefault(_ => _.CommentId == context.Message.CommentId)
            ?.DocumentId;
        if (documentId != null) await UpdateIndex((Guid) documentId, ambientContext);
    }

    public async Task Consume(ConsumeContext<CommentUpdatedMessage> context)
    {
        using var ambientContext = await GetAmbientContext();
        var documentId = ambientContext.DatabaseContext.Comments
            .AsQueryable()
            .SingleOrDefault(_ => _.CommentId == context.Message.CommentId)
            ?.DocumentId;
        if (documentId != null) await UpdateIndex((Guid) documentId, ambientContext);
    }

    public Task Consume(ConsumeContext<DocumentClearIndexMessage> context)
    {
        _logger.LogDebug("DocumentIndexMessage");
        _indexingUtility.RemoveAll();
        return Task.CompletedTask;
    }

    public async Task Consume(ConsumeContext<DocumentCreatedMessage> context)
    {
        _logger.LogDebug("DocumentCreatedMessage: {0}", context.Message.DocumentId);
        using var ambientContext = await GetAmbientContext();
        await AddToIndex(context.Message.DocumentId, ambientContext);
    }

    public async Task Consume(ConsumeContext<DocumentIndexMessage> context)
    {
        _logger.LogDebug("DocumentIndexMessage: {0}", context.Message.DocumentId);

        using var ambientContext = await GetAmbientContext();
        await UpdateIndex(context.Message.DocumentId, ambientContext);
    }

    public async Task Consume(ConsumeContext<DocumentRemovedMessage> context)
    {
        _logger.LogDebug("DocumentRemovedMessage: {0}", context.Message.DocumentId);
        using var ambientContext = await GetAmbientContext();
        RemoveFromIndex(context.Message.DocumentId, ambientContext);
    }

    public async Task Consume(ConsumeContext<DocumentUpdatedMessage> context)
    {
        _logger.LogDebug("DocumentUpdatedMessage: {0}", context.Message.DocumentId);
        using var ambientContext = await GetAmbientContext();
        await UpdateIndex(context.Message.DocumentId, ambientContext);
    }

    public async Task Consume(ConsumeContext<SourceMergedMessage> context)
    {
        var documentIds = context.Message.Documents.ToArray();
        using var ambientContext = await GetAmbientContext();
        var documents = ambientContext.DatabaseContext.DocumentTag
            .AsQueryable()
            .Where(_ => documentIds.Contains(_.TagId))
            .Select(_ => _.DocumentId)
            .Distinct()
            .ToList();

        foreach (var docId in documents) await UpdateIndex(docId, ambientContext);
    }

    public async Task Consume(ConsumeContext<SourceRemovedMessage> context)
    {
        var documentIds = context.Message.Documents.ToArray();

        using var ambientContext = await GetAmbientContext();
        var documents = ambientContext.DatabaseContext.DocumentTag
            .AsQueryable()
            .Where(_ => documentIds.Contains(_.TagId))
            .Select(_ => _.DocumentId)
            .Distinct()
            .ToList();

        foreach (var docId in documents) await UpdateIndex(docId, ambientContext);
    }

    public async Task Consume(ConsumeContext<SourceUpdatedMessage> context)
    {
        _logger.LogDebug("SourceUpdatedMessage: {0}", context.Message.SourceId);

        using var ambientContext = await GetAmbientContext();
        var documents = ambientContext.DatabaseContext.Documents
            .AsQueryable()
            .Where(_ => _.SourceId == context.Message.SourceId)
            .Select(_ => _.DocumentId)
            .Distinct()
            .ToList();

        foreach (var docId in documents) await UpdateIndex(docId, ambientContext);
    }

    public async Task Consume(ConsumeContext<TagMergedMessage> context)
    {
        _logger.LogDebug("TagMergedMessage: {0}", context.Message.RetainedTagId);

        var documentIds = context.Message.Documents.ToArray();
        using var ambientContext = await GetAmbientContext();
        var documents = ambientContext.DatabaseContext.DocumentTag
            .AsQueryable()
            .Where(_ => documentIds.Contains(_.TagId))
            .Select(_ => _.DocumentId)
            .Distinct()
            .ToList();

        foreach (var docId in documents) await UpdateIndex(docId, ambientContext);
    }

    public async Task Consume(ConsumeContext<TagRemovedMessage> context)
    {
        _logger.LogDebug("TagRemovedMessage: {0}", context.Message.TagId);

        var documentIds = context.Message.Documents.ToArray();
        using var ambientContext = await GetAmbientContext();
        var documents = ambientContext.DatabaseContext.DocumentTag
            .AsQueryable()
            .Where(_ => documentIds.Contains(_.TagId))
            .Select(_ => _.DocumentId)
            .Distinct()
            .ToList();

        foreach (var docId in documents) await UpdateIndex(docId, ambientContext);
    }

    public async Task Consume(ConsumeContext<TagUpdatedMessage> context)
    {
        _logger.LogDebug("TagUpdatedMessage: {0}", context.Message.TagId);

        using var ambientContext = await GetAmbientContext();
        var documents = ambientContext.DatabaseContext.DocumentTag
            .AsQueryable()
            .Where(_ => _.TagId == context.Message.TagId)
            .Select(_ => _.DocumentId)
            .Distinct()
            .ToList();

        foreach (var docId in documents) await UpdateIndex(docId, ambientContext);
    }

    private void RemoveFromIndex(Guid documentId, AmbientContext ambientContext)
    {
        try
        {
            _indexingUtility.Remove(documentId);
            _logger.LogInformation($"Document '{documentId}' removed from index.");
        }
        catch (Exception e)
        {
            _logger.Log(LogLevel.Warning, EventIDs.EntityNotFound,
                new LogEvent($"User '{ambientContext.CurrentUser.UserName}' could not remove the document '" +
                             documentId + "' from index: " + e.GetType() + " (" + e.Message + ")")
                    .AddUser(ambientContext.CurrentUser)
                    .AddProperty("document.id", documentId)
                    .AddException(e),
                null,
                LogEvent.Formatter);
            _logger.LogDebug(e.StackTrace);
        }
    }

    private async Task AddToIndex(Guid documentId, AmbientContext ambientContext)
    {
        try
        {
            var document = await _documentRepository.GetAsync(ambientContext,
                documentId,
                new[]
                {
                    nameof(Document.DocumentTags),
                    nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag),
                    nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag) + "." + nameof(Tag.Facet),
                    nameof(Document.Source),
                    nameof(Document.Comments),
                    nameof(Document.Files)
                });

            if (document.Status != DocumentStatus.Registered)
            {
                _logger.LogDebug("Document {0} status is not 'registered' but '{2}' ({1})",
                    document.Reference, document.DocumentId, document.Status);
                return;
            }

            _indexingUtility.Add(document);
            document.LastIndexDate = DateTime.UtcNow;
            await ambientContext.DatabaseContext.SaveChangesAsync();
            _logger.LogInformation("Index updated for the document {0} ({1})",
                document.Reference, document.DocumentId);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning, EventIDs.Unauthorized,
                new LogEvent(
                        $"User '{ambientContext.CurrentUser.UserName}' attempted to retrieve document without legitimate rights.")
                    .AddUser(ambientContext.CurrentUser)
                    .AddProperty("document.id", documentId),
                null,
                LogEvent.Formatter);
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning, EventIDs.EntityNotFound,
                new LogEvent(
                        $"User '{ambientContext.CurrentUser.UserName}' attempted to retrieve a non-existing document.")
                    .AddUser(ambientContext.CurrentUser)
                    .AddProperty("document.id", documentId),
                null,
                LogEvent.Formatter);
        }
        catch (Exception e)
        {
            _logger.Log(LogLevel.Warning, EventIDs.EntityNotFound,
                new LogEvent(
                        $"User '{ambientContext.CurrentUser.UserName}' could not add the document '{documentId}' to the index: {e.GetType()} ({e.Message})")
                    .AddUser(ambientContext.CurrentUser)
                    .AddProperty("document.id", documentId)
                    .AddException(e),
                null,
                LogEvent.Formatter);
            _logger.LogDebug(e.StackTrace);
        }
    }

    private async Task UpdateIndex(Guid documentId, AmbientContext ambientContext)
    {
        try
        {
            _logger.LogDebug("documentId = " + documentId);
            var document = await _documentRepository.GetAsync(ambientContext,
                documentId,
                new[]
                {
                    nameof(Document.DocumentTags),
                    nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag),
                    nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag) + "." + nameof(Tag.Facet),
                    nameof(Document.Source),
                    nameof(Document.Comments),
                    nameof(Document.Files)
                });
            _logger.LogDebug("Document.Status = " + document.Status);

            if (document.Status != DocumentStatus.Registered)
            {
                _logger.LogDebug("Document {0} status is not '" + DocumentStatus.Registered + "' but '{2}' ({1}). Last update: {3}",
                    document.Reference, document.DocumentId, document.Status, document.ModificationDate);
                return;
            }

            _indexingUtility.Update(document);
            document.LastIndexDate = DateTime.UtcNow;
            await ambientContext.DatabaseContext.SaveChangesAsync();
            _logger.LogInformation("Index updated for the document {0} ({1})",
                document.Reference, document.DocumentId);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning, EventIDs.Unauthorized,
                new LogEvent(
                        $"User '{ambientContext.CurrentUser.UserName}' attempted to retrieve document without legitimate rights.")
                    .AddUser(ambientContext.CurrentUser)
                    .AddProperty("document.id", documentId),
                null,
                LogEvent.Formatter);
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning, EventIDs.EntityNotFound,
                new LogEvent(
                        $"User '{ambientContext.CurrentUser.UserName}' attempted to retrieve a non-existing document.")
                    .AddUser(ambientContext.CurrentUser)
                    .AddProperty("document.id", documentId),
                null,
                LogEvent.Formatter);
        }
        catch (Exception e)
        {
            _logger.Log(LogLevel.Warning, EventIDs.EntityNotFound,
                new LogEvent($"User '{ambientContext.CurrentUser.UserName}' could not index the document: " +
                             e.GetType() + " (" + e.Message + ")")
                    .AddUser(ambientContext.CurrentUser)
                    .AddProperty("document.id", documentId)
                    .AddException(e),
                null,
                LogEvent.Formatter);
            _logger.LogDebug(e.StackTrace);
        }
    }
}