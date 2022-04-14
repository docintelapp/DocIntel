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
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;

using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Helpers;
using DocIntel.Core.Messages;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Search.Documents;

using MassTransit;

using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;

using Group = DocIntel.Core.Models.Group;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace DocIntel.Core.Repositories.EFCore
{
    public class DocumentEFRepository : IDocumentRepository
    {
        private readonly IAppAuthorizationService _appAuthorizationService;
        private readonly IPublishEndpoint _busClient;
        private readonly ApplicationSettings _configuration;

        private readonly ILogger<DocumentEFRepository> _logger;
        private readonly SHA256 _sha256Calculator;
        private readonly ApplicationSettings _settings;

        public DocumentEFRepository(IPublishEndpoint busClient,
            IAppAuthorizationService appAuthorizationService,
            ApplicationSettings configuration,
            ILogger<DocumentEFRepository> logger,
            ApplicationSettings settings)
        {
            _busClient = busClient;
            _appAuthorizationService = appAuthorizationService;
            _sha256Calculator = SHA256.Create();
            _configuration = configuration;
            _logger = logger;
            _settings = settings;
        }

        public async Task<Document> AddAsync(AmbientContext context,
            Document document,
            Tag[] tags = null,
            ISet<Group> releasableTo = null,
            ISet<Group> eyesOnly = null)
        {
            if (!await _appAuthorizationService.CanCreateDocument(context.Claims, document))
                throw new UnauthorizedOperationException();

            var now = DateTime.UtcNow;

            var url = GenerateDocumentURL(context, document);
            document.URL = url;
            
            if (IsValid(context, document, out var modelErrors))
            {
                if (document.DocumentDate == DateTime.MinValue)
                    document.DocumentDate = now;
                document.RegistrationDate = now;
                document.ModificationDate = now;
                if (context.CurrentUser != null)
                {
                    document.RegisteredById = context.CurrentUser.Id;
                    document.LastModifiedById = context.CurrentUser.Id;
                }

                document.SequenceId = GetSequence(context, document.RegistrationDate);
                document.Reference = GetReference(document.RegistrationDate, document.SequenceId);
                
                var trackingEntity = await context.DatabaseContext.AddAsync(document);
                
                if (tags != null)
                    foreach (var tag in tags)
                    {
                        var dt = new DocumentTag(document, tag);
                        await context.DatabaseContext.DocumentTag.AddAsync(dt);
                    }

                context.DatabaseContext.OnSaveCompleteTasks.Add(
                    () => _busClient.Publish(new DocumentCreatedMessage
                    {
                        DocumentId = trackingEntity.Entity.DocumentId,
                        UserId = context.CurrentUser.Id
                    }));

                return trackingEntity.Entity;
            }

            throw new InvalidArgumentException(modelErrors);
        }

        private static string GenerateDocumentURL(AmbientContext context, Document document)
        {
            string url;
            int i = 0;
            do
            {
                url = GenerateDocumentUrl(document, i);
                i++;
            } while (context.DatabaseContext.Documents.Any(_ =>
                (document.DocumentId == default || _.DocumentId != document.DocumentId) && _.URL == url));

            return url;
        }

        private static string GenerateDocumentUrl(Document document, int i = 0)
        {
            var url = Regex.Replace(Regex.Replace(document.Title, @"[^A-Za-z0-9_\.~]+", "-"), "-{2,}", "-")
                .ToLowerInvariant().Trim('-') + (i == 0 ? "" : ("-" + i));
            return url;
        }

        public async Task UpdateStatusAsync(AmbientContext context, Guid documentId, DocumentStatus status)
        {
            var retrievedDocument = await context.DatabaseContext.Documents
                .AsQueryable()
                .SingleOrDefaultAsync(_ => _.DocumentId == documentId);

            retrievedDocument.Status = status;
            
            await context.DatabaseContext.SingleUpdateAsync(retrievedDocument);
        }

        public async Task<Document> UpdateAsync(AmbientContext context,
            Document document,
            ISet<Tag> tags = null,
            ISet<Group> releasableTo = null,
            ISet<Group> eyesOnly = null)
        {
            var retrievedDocument = context.DatabaseContext.Documents
                .Include(_ => _.Files)
                .AsQueryable()
                .SingleOrDefault(_ => _.DocumentId == document.DocumentId);

            if (retrievedDocument == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanEditDocument(context.Claims, retrievedDocument))
                throw new UnauthorizedOperationException();

            document.URL = GenerateDocumentURL(context, document);
            
            if (IsValid(context, document, out var modelErrors))
            {
                var currentTags = context.DatabaseContext.DocumentTag
                    .AsQueryable()
                    .Where(_ => _.DocumentId == document.DocumentId)
                    .Select(_ => _.Tag)
                    .ToHashSet();

                var tagsToAdd = tags.Except(currentTags, _ => _.TagId).ToArray();
                var tagsToRemove = currentTags.Except(tags, _ => _.TagId).ToArray();

                if (tagsToRemove.Any())
                {
                    var hashset = tagsToRemove.Select(_ => _.TagId).ToArray();
                    context.DatabaseContext.DocumentTag.RemoveRange(
                        from dt in context.DatabaseContext.DocumentTag.AsQueryable()
                        where (dt.DocumentId == document.DocumentId) & hashset.Contains(dt.TagId)
                        select dt);
                    
                    // TODO Do NOT introduce dependencies between the repositories
                    // foreach (var t in tagsToRemove)
                    //    _observableRepository.DeleteRelationshipForTag(document.DocumentId, t.TagId);

                }

                if (tagsToAdd.Any())
                {
                    var hashset = tagsToAdd.Select(_ => _.TagId).ToArray();
                    foreach (var tagToAdd in hashset)
                        if (!context.DatabaseContext.DocumentTag.Any(_ => _.DocumentId == document.DocumentId
                                                                          && _.TagId == tagToAdd))
                        {
                            context.DatabaseContext.DocumentTag.Add(new DocumentTag
                            {
                                DocumentId = document.DocumentId,
                                TagId = tagToAdd
                            });
                        }
                }

                document.ReleasableTo = releasableTo;
                document.EyesOnly = eyesOnly;
                
                document.ModificationDate = DateTime.UtcNow;
                if (context.CurrentUser != null) document.LastModifiedById = context.CurrentUser.Id;

                var propertyEntry = context.DatabaseContext.Entry(document).Property(_ => _.Status);
                if (retrievedDocument.Status != DocumentStatus.Registered
                    && document.Status == DocumentStatus.Registered
                    || propertyEntry.IsModified
                    && propertyEntry.OriginalValue != DocumentStatus.Registered
                    && propertyEntry.CurrentValue == DocumentStatus.Registered)
                {
                    document.SequenceId = GetSequence(context, document.RegistrationDate);
                    document.Reference = GetReference(document.RegistrationDate, document.SequenceId);
                }

                var trackingEntity = context.DatabaseContext.Update(document);
                context.DatabaseContext.OnSaveCompleteTasks.Add(
                    () =>
                    {
                        _logger.LogDebug("Sending DocumentUpdatedMessage message");
                        return _busClient.Publish(new DocumentUpdatedMessage
                        {
                            DocumentId = trackingEntity.Entity.DocumentId,
                            UserId = context.CurrentUser.Id,
                            TagsAdded = tagsToAdd.Select(_ => _.TagId),
                            TagsRemoved = tagsToRemove.Select(_ => _.TagId)
                        });
                    });

                return trackingEntity.Entity;
            }

            throw new InvalidArgumentException(modelErrors);
        }

        public async Task<Document> RemoveAsync(AmbientContext context,
            Guid documentId)
        {
            var document = context.DatabaseContext.Documents.Include(_ => _.Files)
                .Single(_ => _.DocumentId == documentId);
            if (document == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanDeleteDocument(context.Claims, document))
                throw new UnauthorizedOperationException();

            foreach (var file in document.Files)
                if (File.Exists(Path.Combine(_configuration.DocFolder, file.Filepath)))
                    File.Delete(Path.Combine(_configuration.DocFolder, file.Filepath));
            var files = document.Files.Select(_ => _.FileId).ToArray();

            EntityEntry<Document> trackingEntity;
            
            using (var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                document.Thumbnail = null;
                await context.DatabaseContext.SaveChangesAsync();

                var urls = document.Files.Select(_ => _.SourceUrl).Union(new[] {document.SourceUrl})
                    .Where(_ => !string.IsNullOrEmpty(_)).ToArray();

                // EF Core does not support GroupBy -> OrderBy -> First, sadly...
                var temp = context.DatabaseContext.SubmittedDocuments.AsQueryable().Where(_ => urls.Contains(_.URL))
                    .Select(u => new {Key = u.URL, Submission = u});
                
                var temp2 = temp.Select(_ => _.Key).Distinct().SelectMany(_ => temp.Where(__ => __.Key == _).Select(_ => _.Submission).OrderByDescending(_ => _.SubmissionDate).Take(1)).ToArray();
                
                foreach (var submittedDocument in temp2)
                {
                    submittedDocument.Status = SubmissionStatus.Discarded;
                }
                
                context.DatabaseContext.RemoveRange(document.Files);
                trackingEntity = context.DatabaseContext.Documents.Remove(document);
                
                context.DatabaseContext.OnSaveCompleteTasks.Add(
                    () => _busClient.Publish(new DocumentRemovedMessage
                    {
                        DocumentId = trackingEntity.Entity.DocumentId,
                        FileIDs = files,
                        UserId = context.CurrentUser.Id
                    })
                );
                ts.Complete();
            }
            
            return trackingEntity.Entity;
        }

        public Task<int> CountAsync(AmbientContext context,
            DocumentQuery documentQuery = null)
        {
            var query = BuildQuery(context.DatabaseContext.Documents, documentQuery, context);
            return query.CountAsync();
        }

        public async Task<bool> ExistsAsync(AmbientContext context,
            Guid documentId)
        {
            var document = await context.DatabaseContext.Documents.FindAsync(documentId);
            if (document != null) return await _appAuthorizationService.CanReadDocument(context.Claims, document);

            return false;
        }

        public async Task<bool> ExistsAsync(AmbientContext context,
            DocumentQuery query)
        {
            var filteredDocuments = BuildQuery(context.DatabaseContext.Documents, query, context);

            var result = filteredDocuments.Any();
            if (result)
                foreach (var doc in filteredDocuments)
                    if (await _appAuthorizationService.CanReadDocument(context.Claims, doc))
                        return true;

            return false;
        }
        
        public async Task<bool> ExistsAsync(AmbientContext context, Func<IQueryable<Document>, IQueryable<Document>> query)
        {
            var filteredDocuments = BuildQuery(context.DatabaseContext.Documents, query);

            var result = filteredDocuments.Any();
            if (result)
                foreach (var doc in filteredDocuments)
                    if (await _appAuthorizationService.CanReadDocument(context.Claims, doc))
                        return true;

            return false;
        }
        
        
        public async IAsyncEnumerable<Document> GetAllAsync(AmbientContext context, Func<IQueryable<Document>, IQueryable<Document>> query)
        {
            IQueryable<Document> enumerable = context.DatabaseContext.Documents;

            enumerable = enumerable.Include(nameof(Document.ReleasableTo));
            enumerable = enumerable.Include(nameof(Document.EyesOnly));
            enumerable = enumerable.Include(nameof(Document.Classification));

            var filteredDocuments = BuildQuery(enumerable, query);

            foreach (var document in filteredDocuments)
                if (await _appAuthorizationService.CanReadDocument(context.Claims, document))
                    yield return document;

        }


        public async IAsyncEnumerable<Document> GetAllAsync(AmbientContext context,
            DocumentQuery query = null,
            string[] includeRelatedData = null)
        {
            IQueryable<Document> enumerable = context.DatabaseContext.Documents;

            if (includeRelatedData != default)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            enumerable = enumerable.Include(nameof(Document.ReleasableTo));
            enumerable = enumerable.Include(nameof(Document.EyesOnly));
            enumerable = enumerable.Include(nameof(Document.Classification));

            var filteredDocuments = BuildQuery(enumerable, query, context);

            foreach (var document in filteredDocuments)
                if (await _appAuthorizationService.CanReadDocument(context.Claims, document))
                    yield return document;
        }

        public async Task<Document> GetAsync(AmbientContext context,
            Guid id,
            string[] includeRelatedData = null)
        {
            IQueryable<Document> enumerable = context.DatabaseContext.Documents;

            if (includeRelatedData != default)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            enumerable = enumerable.Include(nameof(Document.ReleasableTo));
            enumerable = enumerable.Include(nameof(Document.EyesOnly));
            enumerable = enumerable.Include(nameof(Document.Classification));

            enumerable = enumerable.Include(_ => _.Files).ThenInclude(_ => _.Classification);
            enumerable = enumerable.Include(_ => _.Files).ThenInclude(_ => _.EyesOnly);
            enumerable = enumerable.Include(_ => _.Files).ThenInclude(_ => _.ReleasableTo);

            var document = await enumerable.SingleOrDefaultAsync(_ => _.DocumentId == id);

            if (document == null)
                throw new NotFoundEntityException();

            if (await _appAuthorizationService.CanReadDocument(context.Claims, document))
            {
                return document;
            }
            
            throw new UnauthorizedOperationException();
        }

        public async Task<Document> GetAsync(AmbientContext context,
            DocumentQuery query,
            string[] includeRelatedData = null)
        {
            IQueryable<Document> enumerable = context.DatabaseContext.Documents;

            if (includeRelatedData != default)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            enumerable = enumerable.Include(nameof(Document.ReleasableTo));
            enumerable = enumerable.Include(nameof(Document.EyesOnly));
            enumerable = enumerable.Include(nameof(Document.Classification));
            
            enumerable = enumerable.Include(_ => _.Files).ThenInclude(_ => _.Classification);
            enumerable = enumerable.Include(_ => _.Files).ThenInclude(_ => _.EyesOnly);
            enumerable = enumerable.Include(_ => _.Files).ThenInclude(_ => _.ReleasableTo);

            enumerable = BuildQuery(enumerable, query, context);

            if (enumerable.Count() > 1)
                throw new NotFoundEntityException(); // FIXME

            var document = await enumerable.FirstOrDefaultAsync();
            if (document == null)
                throw new NotFoundEntityException();

            if (await _appAuthorizationService.CanReadDocument(context.Claims, document))
                return document;
            
            throw new UnauthorizedOperationException();
        }

        public IAsyncEnumerable<(Document document, SubscriptionStatus status)> GetSubscriptionsAsync(
            AmbientContext context,
            int page = 0,
            int limit = 10)
        {
            var queryable = context.DatabaseContext.UserDocumentSubscription.Include(_ => _.Document)
                .Where(_ => _.UserId == context.CurrentUser.Id);

            if ((page > 0) & (limit > 0))
                queryable = queryable.Skip((page - 1) * limit).Take(limit);

            return queryable
                .AsAsyncEnumerable()
                .Select(_ => (document: _.Document, status: new SubscriptionStatus
                {
                    Subscribed = true,
                    Notification = _.Notify
                }));
        }

        public async Task<SubscriptionStatus> IsSubscribedAsync(AmbientContext context,
            Guid documentId)
        {
            var retrievedDocument = await context.DatabaseContext.Documents.FindAsync(documentId);
            if (retrievedDocument == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanSubscribeDocument(context.Claims, retrievedDocument))
                return new SubscriptionStatus {Subscribed = false};

            var subscription = context.DatabaseContext.UserDocumentSubscription.FirstOrDefault(_ =>
                (_.DocumentId == documentId) & (_.UserId == context.CurrentUser.Id));
            if (subscription == null)
                return new SubscriptionStatus {Subscribed = false};
            return new SubscriptionStatus {Subscribed = true, Notification = subscription.Notify};
        }

        public async Task SubscribeAsync(AmbientContext context,
            Guid documentId,
            bool notification = false)
        {
            var retrievedDocument = await context.DatabaseContext.Documents.FindAsync(documentId);
            if (retrievedDocument == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanSubscribeDocument(context.Claims, retrievedDocument))
                throw new UnauthorizedOperationException();

            var subscription = context.DatabaseContext.UserDocumentSubscription
                .FirstOrDefault(_ => (_.DocumentId == documentId) & (_.UserId == context.CurrentUser.Id));

            if (subscription != null)
            {
                subscription.Notify = notification;
                context.DatabaseContext.UserDocumentSubscription.Update(subscription);
            }
            else
            {
                subscription = new UserDocumentSubscription
                {
                    DocumentId = documentId,
                    UserId = context.CurrentUser.Id,
                    Notify = notification
                };
                await context.DatabaseContext.UserDocumentSubscription.AddAsync(subscription);
            }
        }

        public async Task UnsubscribeAsync(AmbientContext context,
            Guid documentId)
        {
            var retrievedDocument = await context.DatabaseContext.Documents.FindAsync(documentId);
            if (retrievedDocument == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanSubscribeDocument(context.Claims, retrievedDocument))
                throw new UnauthorizedOperationException();

            context.DatabaseContext.UserDocumentSubscription.RemoveRange(
                context.DatabaseContext.UserDocumentSubscription.AsQueryable().Where(_ =>
                    (_.DocumentId == documentId) & (_.UserId == context.CurrentUser.Id))
            );
        }

        public async Task<DocumentFile> GetFileAsync(AmbientContext ambientContext, Guid id,
            string[] includeRelatedData = null)
        {
            IQueryable<DocumentFile> enumerable = ambientContext.DatabaseContext.Files;
            if (includeRelatedData != default)
                foreach (var relatedData in includeRelatedData)
                    enumerable = enumerable.Include(relatedData);

            enumerable = enumerable.Include(_ => _.Classification);
            enumerable = enumerable.Include(_ => _.EyesOnly);
            enumerable = enumerable.Include(_ => _.ReleasableTo);
            
            enumerable = enumerable.Include(_ => _.Document).ThenInclude(_ => _.Classification);
            enumerable = enumerable.Include(_ => _.Document).ThenInclude(_ => _.EyesOnly);
            enumerable = enumerable.Include(_ => _.Document).ThenInclude(_ => _.ReleasableTo);
            
            var file = enumerable.SingleOrDefault(_ => _.FileId == id);
            if (!await _appAuthorizationService.CanViewDocumentFile(ambientContext.Claims, file))
                throw new UnauthorizedOperationException();
            
            return file;
        }

        public async Task<DocumentFile> AddFile(AmbientContext ambientContext, DocumentFile file, Stream stream, ISet<Group> releasableTo = null, ISet<Group> eyesOnly = null)
        {
            if (stream.Length == 0)
                return null;

            var now = DateTime.UtcNow;
            file.Sha256Hash = HashFile(stream);
            file.DocumentDate = now;
            file.RegistrationDate = now;
            file.ModificationDate = now;
            file.RegisteredById = ambientContext.CurrentUser.Id;
            file.LastModifiedById = ambientContext.CurrentUser.Id;
            
            file.EyesOnly = eyesOnly
                ?.Where(_ => ambientContext.CurrentUser.Memberships.Any(__ => __.GroupId == _.GroupId))
                .ToHashSet();
            
            file.ReleasableTo = 
                (releasableTo ?? Enumerable.Empty<Group>())
                .Union(file.EyesOnly ?? Enumerable.Empty<Group>()).ToHashSet();

            if (!CheckFileContents(stream, out var extension))
            {
                throw new InvalidArgumentException(new List<ValidationResult>
                {
                    new ValidationResult("File type is not supported", new[] {"file"})
                });                
            }

            if (IsValid(ambientContext, file, out var modelErrors))
            {
                var newFilePath =
                    Path.Combine(GetFileFolder(file.Document.RegistrationDate),
                        file.FileId + extension);
                stream.Position = 0; // Rewind to copy
                await using (var destFile = File.Open(Path.Combine(GetDocumentsFolder(), newFilePath), FileMode.Create))
                {
                    await stream.CopyToAsync(destFile);
                }

                file.Filepath = newFilePath;
                new FileExtensionContentTypeProvider().TryGetContentType(file.Filepath, out var contentType);
                file.MimeType = contentType;
                
                var trackingEntity = await ambientContext.DatabaseContext.AddAsync(file);
                return trackingEntity.Entity;
            }

            throw new InvalidArgumentException(modelErrors);
        }

        public async Task<DocumentFile> UpdateFile(AmbientContext ambientContext, DocumentFile file,
            Stream stream = null, ISet<Group> releasableTo = null, ISet<Group> eyesOnly = null)
        {
            Document document;
            if (file.Document == null)
                document = await ambientContext.DatabaseContext.Documents.FindAsync(file.DocumentId);
            else
                document = file.Document;

            var now = DateTime.UtcNow;
            if (stream != null)
                file.Sha256Hash = HashFile(stream);
            file.ModificationDate = now;
            file.LastModifiedById = ambientContext.CurrentUser.Id;
            
            file.EyesOnly = eyesOnly
                ?.Where(_ => ambientContext.CurrentUser.Memberships.Any(__ => __.GroupId == _.GroupId))
                .ToHashSet();
            
            file.ReleasableTo = 
                (releasableTo ?? Enumerable.Empty<Group>())
                .Union(file.EyesOnly ?? Enumerable.Empty<Group>()).ToHashSet();
            
            if (IsValid(ambientContext, file, out var modelErrors))
            {
                var newFilePath =
                    Path.Combine(GetFileFolder(document.RegistrationDate),
                        GetFilename(file.FileId));
                if (File.Exists(newFilePath))
                    File.Delete(newFilePath);

                if (stream != null)
                {
                    stream.Position = 0; // Rewind to copy
                    using (var destFile = File.Open(Path.Combine(GetDocumentsFolder(), newFilePath), FileMode.Create))
                    {
                        await stream.CopyToAsync(destFile);
                    }

                    file.Filepath = newFilePath;
                }

                var trackingEntity = ambientContext.DatabaseContext.Update(file);
                return trackingEntity.Entity;
            }

            throw new InvalidArgumentException(modelErrors);
        }

        public async Task<DocumentFile> DeleteFile(AmbientContext ambientContext, Guid id)
        {
            var file = await ambientContext.DatabaseContext.Files.FindAsync(id);
            
            var newFilePath =
                Path.Combine(GetFileFolder(file.RegistrationDate),
                    GetFilename(file.FileId));
            if (File.Exists(newFilePath))
                File.Delete(newFilePath);

            var trackingEntity = ambientContext.DatabaseContext.Remove(file);
            return trackingEntity.Entity;
        }
        
        public async Task<SubmittedDocument> SubmitDocument(AmbientContext ambientContext, SubmittedDocument doc)
        {
            doc.SubmitterId = ambientContext.CurrentUser.Id;
            doc.SubmissionDate = DateTime.UtcNow;
            var trackingEntity = await ambientContext.DatabaseContext.AddAsync(doc);
            return trackingEntity.Entity;
        }
        
        public IAsyncEnumerable<SubmittedDocument> GetSubmittedDocuments(AmbientContext ambientContext, Func<IQueryable<SubmittedDocument>, IQueryable<SubmittedDocument>> query = null, int page = 0, int limit = 100)
        {
            var queryable = ambientContext.DatabaseContext.SubmittedDocuments.AsQueryable();
            if (query != null) queryable = query(queryable);
            if (page > 1 && limit > 0)
                queryable = queryable.Skip((page - 1) * limit);

            if (page <= 1 && limit > 0)
                queryable = queryable.Take(limit);
            
            return queryable.AsAsyncEnumerable();
        }
        
        public void DeleteSubmittedDocument(AmbientContext ambientContext, Guid id, SubmissionStatus status = SubmissionStatus.Processed, bool hard = false)
        {
            if (!hard)
            {
                var submittedDocument = ambientContext.DatabaseContext.SubmittedDocuments.AsQueryable().SingleOrDefault(_ => _.SubmittedDocumentId == id);
                if (submittedDocument != null) submittedDocument.Status = status;
            }
            else
            {
                var submittedDocuments = ambientContext.DatabaseContext.SubmittedDocuments.AsQueryable().Where(__ => __.URL == ambientContext.DatabaseContext.SubmittedDocuments.AsQueryable().SingleOrDefault(_ => _.SubmittedDocumentId == id).URL);
                ambientContext.DatabaseContext.RemoveRange(submittedDocuments);
            }
        }

        public Task<SubmittedDocument> GetSubmittedDocument(AmbientContext ambientContext, Guid submissionId, Func<IQueryable<SubmittedDocument>, IQueryable<SubmittedDocument>> query = null)
        {
            IQueryable<SubmittedDocument> queryable = ambientContext.DatabaseContext.SubmittedDocuments;
            if (query != null)
            {
                queryable = query(queryable);
            }

            return Task.FromResult(queryable.SingleOrDefault(_ => _.SubmittedDocumentId == submissionId));
        }

        
        
        private IQueryable<Document> BuildQuery(IQueryable<Document> databaseContextDocuments, Func<IQueryable<Document>, IQueryable<Document>> query)
        {
            return query(databaseContextDocuments);
        }


        private static IQueryable<Document> BuildQuery(IQueryable<Document> documents,
            DocumentQuery query, AmbientContext context)
        {
            if (query.Source != null)
                documents = documents.Where(_ => _.SourceId == query.Source.SourceId).AsQueryable();

            if (query.SourceId != null)
                documents = documents.Where(_ => _.SourceId == query.SourceId).AsQueryable();

            if (!string.IsNullOrEmpty(query.SourceUrl))
            {
                documents = documents.Where(_ => _.SourceUrl == query.SourceUrl || _.Files.Any(__ => __.SourceUrl == query.SourceUrl)).AsQueryable();
            }

            if (!string.IsNullOrEmpty(query.Reference))
                documents = documents.Where(_ => _.Reference == query.Reference).AsQueryable();

            if (!string.IsNullOrEmpty(query.ReferencePrefix))
                documents = documents.Where(_ => _.Reference.StartsWith(query.ReferencePrefix)).AsQueryable();

            if (query.TagIds != null)
            {
                var array = query.TagIds.ToHashSet().ToArray();
                documents = documents.Where(_ => _.DocumentTags.Any(__ => array.Contains(__.TagId))).AsQueryable();
            }

            if (!string.IsNullOrEmpty(query.URL)) documents = documents.Where(_ => _.URL == query.URL).AsQueryable();

            if (query.DocumentId != null)
                documents = documents.Where(_ => _.DocumentId == (Guid) query.DocumentId).AsQueryable();

            if (query.Statuses != null && query.Statuses.Any())
                documents = documents.Where(_ => query.Statuses.ToArray().Contains(_.Status));

            if (!string.IsNullOrEmpty(query.RegisteredBy))
                documents = documents.Where(_ => _.RegisteredById == query.RegisteredBy).AsQueryable();

            if (query.OrderBy == SortCriteria.DocumentDate)
                documents = documents.OrderByDescending(_ => _.Files.Min(__ => __.DocumentDate));
            else if (query.OrderBy == SortCriteria.ModificationDate)
                documents = documents.OrderByDescending(_ => _.ModificationDate);
            else if (query.OrderBy == SortCriteria.RegistrationDate)
                documents = documents.OrderByDescending(_ => _.RegistrationDate);

            if ((query.RegisteredAfter != DateTime.MinValue))
                documents = documents.Where(x => x.RegistrationDate > query.RegisteredAfter);
            
            if ((query.ModifiedAfter != DateTime.MinValue))
                documents = documents.Where(x => x.ModificationDate > query.ModifiedAfter);
            
            if ((query.ModifiedBefore != DateTime.MinValue))
                documents = documents.Where(x => x.ModificationDate < query.ModifiedBefore);

            if (!string.IsNullOrEmpty(query.ExternalReference))
                documents = documents.Where(_ => _.ExternalReference == query.ExternalReference);

            if (query.ExcludeMuted)
            {
                documents = documents.Where(d =>
                    !d.DocumentTags.Any(dt =>
                        context.DatabaseContext.UserTagSubscriptions.Any(s =>
                            s.Muted & s.UserId == context.CurrentUser.Id & s.TagId == dt.TagId)));
                documents = documents.Where(d =>
                    !context.DatabaseContext.UserSourceSubscription.Any(s =>
                            s.Muted & s.UserId == context.CurrentUser.Id & s.SourceId == d.SourceId));
            }
            
            if (query.Page > 1 && query.Limit > 0)
                documents = documents.Skip((query.Page - 1) * query.Limit);

            if (query.Limit > 0)
                documents = documents.Take(query.Limit);

            return documents;
        }

        private bool IsValid(AmbientContext context,
            Document document,
            out List<ValidationResult> modelErrors)
        {
            var validationContext = new ValidationContext(document);
            modelErrors = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(document,
                validationContext,
                modelErrors);

            if (document.SourceId != null
                && !string.IsNullOrEmpty(document.ExternalReference)
                && context.DatabaseContext.Documents.Any(_ => _.DocumentId != document.DocumentId
                                                              && _.SourceId == document.SourceId
                                                              && _.ExternalReference == document.ExternalReference))
            {
                modelErrors.Add(new ValidationResult("The external reference already exists for the source.",
                    new[] {"ExternalReference", "SourceId"}));
                isValid = false;
            }

            return isValid;
        }

        private bool IsValid(AmbientContext context,
            DocumentFile file,
            out List<ValidationResult> modelErrors)
        {
            var validationContext = new ValidationContext(file);
            modelErrors = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(file,
                validationContext,
                modelErrors);

            if (string.IsNullOrEmpty(file.Sha256Hash))
                throw new ArgumentNullException(nameof(file.Sha256Hash));

            var docWithFileSameHash = context.DatabaseContext.Files.Include(_ => _.Document).AsQueryable()
                .Where(_ => _.DocumentId != file.DocumentId && _.Sha256Hash == file.Sha256Hash);
            if (docWithFileSameHash.Any())
            {
                throw new FileAlreadyKnownException
                {
                    Hash = file.Sha256Hash,
                    ExistingReference = docWithFileSameHash.First().Document.Reference,
                    Document = docWithFileSameHash.First().Document
                };
            }

            var docs = context.DatabaseContext.Files.Include(_ => _.Document).AsQueryable()
                .Where(_ => _.DocumentId != file.DocumentId && _.SourceUrl == file.SourceUrl);
            if (!string.IsNullOrEmpty(file.SourceUrl)
                && docs.Any())
                throw new TitleAlreadyExistsException
                {
                    ExistingReference = docs.First().Document.Reference
                };

            return isValid;
        }

        private string HashFile(Stream file)
        {
            file.Position = 0; // Rewind to compute the hash
            var hash = _sha256Calculator.ComputeHash(file);
            var v = string.Join("", hash.Select(x => x.ToString("x2")));
            return v;
        }

        public int GetSequence(AmbientContext context, DateTime documentDate)
        {
            var queryables = context.DatabaseContext.Documents
                .AsQueryable()
                .Where(_ => (_.RegistrationDate.Year == documentDate.Year)
                            & (_.RegistrationDate.Month == documentDate.Month));

            var max = 1;
            if (queryables.Any()) max = queryables.Max(_ => _.SequenceId) + 1;

            return max;
        }

        public string GetReference(DateTime documentDate, int sequenceId)
        {
            var prefix = _configuration.DocumentPrefix + "-" + documentDate.ToString("yyyy-MM");
            return prefix + "-" + sequenceId.ToString().PadLeft(3, '0');
        }

        private async Task SaveFile(Document document, Dictionary<DocumentFile, Stream> files)
        {
            foreach (var file in files)
            {
                var newFilePath =
                    Path.Combine(GetFileFolder(document.RegistrationDate),
                        GetFilename(file.Key.FileId));
                file.Value.Position = 0; // Rewind to copy
                using (var destFile = File.Open(Path.Combine(GetDocumentsFolder(), newFilePath), FileMode.Create))
                {
                    await file.Value.CopyToAsync(destFile);
                }

                file.Key.Filepath = newFilePath;
            }
        }

        private string GetDocumentsFolder()
        {
            var docFolder = _configuration.DocFolder;
            _logger.LogDebug("DocFolder : " + docFolder);
            if (!Directory.Exists(docFolder)) Directory.CreateDirectory(docFolder);

            return docFolder;
        }

        private string GetFileFolder(DateTime documentDate)
        {
            var year = documentDate.Year.ToString();
            var month = documentDate.Month.ToString();

            var documentFolder = Path.Combine(year, month);

            // Check if folder exists, otherwise, just create it.
            var completePath = Path.Combine(GetDocumentsFolder(), documentFolder);
            if (!Directory.Exists(completePath)) Directory.CreateDirectory(completePath);

            return documentFolder;
        }

        private string GetFilename(Guid fileReference)
        {
            var filename = fileReference + ".pdf";
            return filename;
        }
        
        public static bool CheckFileContents(Stream stream, out string extension)
        {
            if (IsBinary(stream))
            {
                (int offset, List<byte[]> data) signatures;

                stream.Position = 0;
                using var reader = new BinaryReader(stream, Encoding.Default, true);
                foreach (var signature in _fileSignature2)
                {
                    var headerBytes = reader.ReadBytes(signature.offset + signature.data.Length);
                    if (headerBytes.Skip(signature.offset).Take(signature.data.Length).SequenceEqual(signature.data))
                    {
                        extension = signature.extension;
                        return true;
                    }
                    reader.BaseStream.Position = 0;
                }

                extension = null;
                return false;
            }
            else
            {
                stream.Position = 0;
                using var reader = new StreamReader(stream, leaveOpen:true);
                var buffer = new char[512];
                reader.Read(buffer, 0, 512);
                var s = new string(buffer).ToLower();
                if (s.IndexOf("<!DOCTYPE HTML".ToLower(), StringComparison.Ordinal) >= 0 | s.IndexOf("<HTML".ToLower(), StringComparison.Ordinal) >= 0)
                {
                    extension = ".html";
                    return true;
                }

                extension = ".txt";
                return true;
            }
        }
        
        // Not perfect, but good enough and used in GIT. See https://stackoverflow.com/questions/4744890/c-sharp-check-if-file-is-text-based
        // Could be updated with https://dev.w3.org/html5/cts/html5-type-sniffing.html
        public static bool IsBinary(Stream stream, int requiredConsecutiveNul = 1)
        {
            const int charsToCheck = 8000;
            const char nulChar = '\0';
        
            int nulCount = 0;
            stream.Position = 0;
        
            using (var streamReader = new StreamReader(stream, leaveOpen: true))
            {
                for (var i = 0; i < charsToCheck; i++)
                {
                    if (streamReader.EndOfStream)
                        return false;
        
                    if ((char) streamReader.Read() == nulChar)
                    {
                        nulCount++;
        
                        if (nulCount >= requiredConsecutiveNul)
                            return true;
                    }
                    else
                    {
                        nulCount = 0;
                    }
                }
            }
        
            return false;
        }

        // See https://www.garykessler.net/library/file_sigs.html for reference
        private static readonly List<(int offset, byte[] data, string extension)> _fileSignature2 =
            new List<(int offset, byte[] data, string extension)>()
            {
                {(0, new byte[] {0xFF, 0xD8, 0xFF, 0xE0}, ".jpeg")},
                {(0, new byte[] {0xFF, 0xD8, 0xFF, 0xE2}, ".jpeg")},
                {(0, new byte[] {0xFF, 0xD8, 0xFF, 0xE3}, ".jpeg")},
                {(0, new byte[] {0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A}, ".png")},
                {(0, new byte[] { 0x25, 0x50, 0x44, 0x46 }, ".pdf")},
                {(0, new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00 }, ".docx")},
                {(0, new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }, ".doc")},
            };
        
    }
}