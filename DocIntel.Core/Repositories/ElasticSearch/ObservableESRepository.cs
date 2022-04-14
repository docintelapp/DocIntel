/* DocIntel
 * Copyright (C) 2018-2021 Belgian Defense, Antoine Cailliau, Kevin Menten
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

using DocIntel.Core.Exceptions;
using DocIntel.Core.Models;
using DocIntel.Core.Settings;

using Microsoft.Extensions.Logging;

using Nest;

namespace DocIntel.Core.Repositories.ElasticSearch
{
    public class ObservableESRepository : IObservableRepository
    {
        private const int DEFAULT_PAGE_SIZE = 100;
        private readonly ElasticClient _elasticClient;
        private readonly ILogger<ObservableESRepository> _logger;
        private readonly ApplicationSettings _settings;

        public ObservableESRepository(ILogger<ObservableESRepository> logger, ApplicationSettings settings)
        {
            _logger = logger;
            _settings = settings;

            _logger.LogDebug("ObservableESRepository initialized");
            
            // REVIEW Why the other types are not mentioned in the default mapping?
            var esSettings = new ConnectionSettings(new Uri(_settings.ElasticSearch.Uri))
                .DisableAutomaticProxyDetection()
                .DefaultMappingFor<DocumentFileObservables>(m => m
                    .IndexName("observables")
                );

                
            // TODO Should not create elastic client but use dynamic injection
            // TODO Do not initiate network connections in a constructor, error handling if often not working properly with DI
            try
            {
                _elasticClient = new ElasticClient(esSettings);
                CreateIndexes();
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                _logger.LogError(e.StackTrace);
            }
        }

        public async Task<IEnumerable<DocumentFileObservables>> GetObservables(Guid documentId,
            ObservableStatus? status = null)
        {
            int pageSize = 100, page = 0;
            var response = await GetObservables(documentId, status, pageSize, page);
            var results = new List<DocumentFileObservables>();

            while (response.Count() == pageSize)
            {
                results.AddRange(response);
                page++;
                response = await GetObservables(documentId, status, pageSize, page);
            }

            results.AddRange(response); // TODO Use Do/While to avoid code duplicate

            return results;
        }

        public async Task<IEnumerable<DocumentFileObservables>> SearchObservables(string value,
            ObservableType observableType)
        {
            var page = 0;
            var response = await GetDocumentObservables(value, observableType, DEFAULT_PAGE_SIZE, page);
            var results = new List<DocumentFileObservables>();
            while (response.Count() == DEFAULT_PAGE_SIZE)
            {
                results.AddRange(response);
                page++;
                response = await GetDocumentObservables(value, observableType, DEFAULT_PAGE_SIZE, page);
            }
            results.AddRange(response); // TODO Use Do/While to avoid code duplicate
            return results;
        }

        public async Task<IEnumerable<DocumentFileObservables>> ExportDocumentObservables(ObservableType ot)
        {
            var scrollTimeoutMinutes = "2m";
            var scrollPageSize = 1000;
            var searchResponse = await _elasticClient.SearchAsync<DocumentFileObservables>(sd => sd
                .From(0)
                .Take(scrollPageSize)
                .Query(q => q
                                .Term(t => t
                                    .Field(x => x.Type)
                                    .Value(ot.ToString().Replace("_", "-"))
                                ) &&
                            q.Term(t => t
                                .Field(x => x.Status)
                                .Value("Accepted")
                            )
                )
                .Scroll(scrollTimeoutMinutes));

            var results = new List<DocumentFileObservables>();

            while (true)
            {
                if (!searchResponse.IsValid || string.IsNullOrEmpty(searchResponse.ScrollId))
                {
                    // TODO Use application specific exception.
                    throw new Exception($"Search error: {searchResponse.ServerError.Error.Reason}");
                }

                if (!searchResponse.Documents.Any())
                    break; // TODO Bad smell, don't use while(true) and break. 

                results.AddRange(searchResponse.Documents);
                searchResponse =
                    await _elasticClient.ScrollAsync<DocumentFileObservables>(scrollTimeoutMinutes,
                        searchResponse.ScrollId);
            }

            await _elasticClient.ClearScrollAsync(new ClearScrollRequest(searchResponse.ScrollId));

            return results;
        }

        public async Task<IEnumerable<DocumentFileObservables>> SearchExistingObservables(
            IEnumerable<Observable> observables,
            string[] status = null)
        {
            var page = 0;
            status ??= new[] {"Accepted", "Rejected"};
            var response = await SearchExistingObservables(observables, status, DEFAULT_PAGE_SIZE, page);
            var results = new List<DocumentFileObservables>();
            while (response is not null && response.Count() == DEFAULT_PAGE_SIZE)
            {
                results.AddRange(response);
                page++;
                response = await SearchExistingObservables(observables, status, DEFAULT_PAGE_SIZE, page);
            }
            results.AddRange(response); // TODO Use do/while to avoid code duplicate
            
            return results;
        }

        public async Task<bool> UpdateObservables(IEnumerable<DocumentFileObservables> observables)
        {
            var result = true;
            foreach (var observable in observables)
            {
                var asyncUpdateResponse = await _elasticClient.UpdateAsync<DocumentFileObservables>(
                    observable.Id,
                    u => u.Index("observables").Doc(observable)
                );
                if (asyncUpdateResponse.IsValid)
                {
                    // TODO Use structured logging
                    _logger.LogTrace("Successfully updated document");
                }
                else
                {
                    // TODO Use structured logging
                    _logger.LogWarning("Error while updating document " +
                                       asyncUpdateResponse.OriginalException.Message);
                    result = false;
                }
            }

            return result;
        }

        public async Task DeleteAllObservables(Guid documentId)
        {
            await DeleteRelationships(documentId);

            var response = await _elasticClient.DeleteByQueryAsync<DocumentFileObservables>(q => q.Query(q => q
                .Term(t => t
                    .Field(x => x.DocumentId)
                    .Value(documentId.ToString())
                )
            ));
        }

        public async Task<bool> IngestObservables(IEnumerable<DocumentFileObservables> observables)
        {
            var errors = false;
            // TODO Check why we can't use IndexManyAsync (failing for an unknown reason) 
            if (observables is not null)
                foreach (var observable in observables)
                {
                    var response = await _elasticClient.IndexAsync(new IndexRequest<DocumentFileObservables>(observable));
                    _logger.LogDebug(response.ToString());
                    _logger.LogDebug(response.DebugInformation);
                }

            return true;
        }

        public async Task<bool> IngestWhitelistedObservables(Observable observable)
        {
            if (observable.Type == ObservableType.Artefact || observable.Type == ObservableType.File)
            {
                var searchResponse = await _elasticClient.SearchAsync<Observable>(s => s
                    .Index("whitelist")
                    .Query(q => q
                        .Nested(n => n
                            .InnerHits(i => i.Explain())
                            .Path(p => p.Hashes)
                            .Query(q2 => q2
                                .Term(t => t
                                    .Field(f => f.Hashes.First().Value)
                                    .Value(observable.Hashes.First().Value)
                                )
                            )
                        )
                    )
                );
                if (searchResponse.Hits.Count > 0)
                    return true;
            }
            else
            {
                var searchResponse = await _elasticClient.SearchAsync<Observable>(s => s
                    .Index("whitelist")
                    .Query(q => q
                        .Term(t => t
                            .Field(f => f.Value)
                            .Value(observable.Value)
                        )
                    )
                );
                if (searchResponse.Hits.Count > 0)
                    return true;
            }

            var indexResponse = await _elasticClient.IndexAsync(new IndexRequest<Observable>(observable, "whitelist"));

            if (indexResponse.IsValid)
            {
                // TODO Use structured logging
                // _logger.LogInformation("Successfully logged whitelistedobservable");
                return true;
            }

            // TODO Use structured logging
            _logger.LogWarning("Error while indexing whitelistedobservable " +
                               indexResponse.OriginalException.Message);
            return false;
        }

        public async Task<IEnumerable<Observable>> GetWhitelistedObservables()
        {
            return await RockAndScroll<Observable>("whitelist");
        }

        public async Task<bool> DeleteWhitelistedObservable(Guid observableId)
        {
            var deleteResponse = await _elasticClient.DeleteByQueryAsync<Observable>(q => q
                .Index("whitelist")
                .Query(q => q
                    .Term(t => t
                        .Field(x => x.Id)
                        .Value(observableId.ToString())
                    )
                ));

            return deleteResponse.Deleted == 1;
        }

        public async Task<bool> DeleteObservable(Guid observableId)
        {
            await DeleteRelationshipObservable(observableId);

            var response = await _elasticClient.DeleteByQueryAsync<DocumentFileObservables>(q => q.Query(q => q
                .Term(t => t
                    .Field(x => x.Id)
                    .Value(observableId.ToString())
                )
            ));

            return response.Deleted == 1;
        }

        public async Task<bool> IngestRelationships(IEnumerable<Relationship> relationships)
        {
            var response = await _elasticClient.IndexManyAsync(relationships, "relationship");

            if (response.IsValid)
            {
                // TODO Use structured logging
                _logger.LogInformation("Successfully logged relationships");
                return true;
            }

            // TODO Use structured logging
            _logger.LogWarning("Error while indexing relationships " + response.OriginalException.Message);
            return false;
        }

        public async Task<bool> ReplaceRelationships(Guid documentId, IEnumerable<Relationship> relationships)
        {
            if (await DeleteRelationships(documentId))
            {
                if (relationships is not null && relationships.Any())
                    return await IngestRelationships(relationships);
                return true;
            }

            return false;
        }

        public async Task<bool> DeleteRelationshipForTag(Guid documentId, Guid tagId)
        {
            var response = await _elasticClient.DeleteByQueryAsync<Relationship>(q => q
                .Index("relationship")
                .Query(q => q
                                .Term(t => t
                                    .Field(x => x.TagId)
                                    .Value(tagId.ToString())
                                )
                            && q.Term(t => t
                                .Field(x => x.DocumentRef)
                                .Value(documentId.ToString())
                            )
                ));
            return response.IsValid;
        }

        public async Task<IEnumerable<Relationship>> GetRelationships(Guid documentId)
        {
            var page = 0;
            var response = await GetRelationships(DEFAULT_PAGE_SIZE, page, documentId);
            var results = new List<Relationship>();
            while (response.Count() == DEFAULT_PAGE_SIZE)
            {
                results.AddRange(response);
                page++;
                response = await GetRelationships(DEFAULT_PAGE_SIZE, page, documentId);
            }
            results.AddRange(response); // TODO Use do/while to avoid code duplicates
            return results;
        }

        private void CreateIndexes()
        {
            _logger.LogDebug("Create the indexes");
            if (!_elasticClient.Indices.Exists("observables").Exists)
            {
                _logger.LogInformation("Index observables created");
                var response = _elasticClient.Indices.Create("observables", c => c
                    .Map<DocumentFileObservables>(m => m
                        .AutoMap()
                        .Properties(ps => ps
                            .Nested<ObservableHash>(n => n
                                .Name(c => c.Hashes)
                                .AutoMap()
                            )
                        )
                    )
                );
                if (!response.IsValid)
                {
                    _logger.LogDebug(response.ToString());
                    _logger.LogDebug(response.DebugInformation);
                    throw new DocIntelException("Cannot create index observables at " + _settings.ElasticSearch.Uri);
                }
            }
            else
            {
                _logger.LogDebug("Index observables already created");   
            }

            if (!_elasticClient.Indices.Exists("whitelist").Exists)
            {
                _logger.LogInformation("Index whitelist created");
                var response = _elasticClient.Indices.Create("whitelist", c => c
                    .Map<Observable>(m => m
                        .AutoMap()
                        .Properties(ps => ps
                            .Nested<ObservableHash>(n => n
                                .Name(c => c.Hashes)
                                .AutoMap()
                            )
                        )
                    )
                );
                if (!response.IsValid)
                {
                    _logger.LogDebug(response.ToString());
                    _logger.LogDebug(response.DebugInformation);
                    throw new DocIntelException("Cannot create index whitelist at " + _settings.ElasticSearch.Uri);
                }
            }
            else
            {
                _logger.LogDebug("Index whitelist already created");   
            }

            if (!_elasticClient.Indices.Exists("relationship").Exists)
            {
                _logger.LogInformation("Index relationship created");
                var response = _elasticClient.Indices.Create("relationship", c => c
                    .Map<Relationship>(m => m
                        .AutoMap()
                    )
                );
                if (!response.IsValid)
                {
                    _logger.LogDebug(response.ToString());
                    _logger.LogDebug(response.DebugInformation);
                    throw new DocIntelException("Cannot create index relationship at " + _settings.ElasticSearch.Uri);
                }
            }
            else
            {
                _logger.LogDebug("Index relationship already created");   
            }
        }

        private async Task<IEnumerable<DocumentFileObservables>> GetObservables(Guid documentId,
            ObservableStatus? status,
            int pageSize,
            int page)
        {
            Func<QueryContainerDescriptor<DocumentFileObservables>, QueryContainer> qcd;
            if (status.HasValue)
                qcd = q => q.Term(t => t
                    .Field(x => x.DocumentId)
                    .Value(documentId.ToString())
                ) && q.Term(t => t
                    .Field(x => x.Status)
                    .Value(status.ToString())
                );
            else
                qcd = q => q.Term(t => t
                    .Field(x => x.DocumentId)
                    .Value(documentId.ToString())
                );

            var searchResponse = await _elasticClient.SearchAsync<DocumentFileObservables>(s => s
                .From(pageSize * page)
                .Size(pageSize)
                .Query(qcd)
            );
            
            _logger.LogDebug(searchResponse.ToString());

            return searchResponse.Documents.ToArray();
        }

        private async Task<IEnumerable<DocumentFileObservables>> GetDocumentObservables(string value,
            ObservableType observableType, int pageSize, int page)
        {
            var searchResponse = await _elasticClient.SearchAsync<DocumentFileObservables>(s => s
                .From(pageSize * page)
                .Size(pageSize)
                .Query(q => q
                                .Term(t => t
                                    .Field(x => x.Value)
                                    .Value(value))
                            && q.Term(t => t
                                .Field(x => x.Type)
                                .Value(observableType.ToString().Replace("_", "-")))
                )
            );

            return searchResponse.Documents.ToArray();
        }

        /// <summary>
        /// Search for the provides observables without checking for their specific type.
        /// </summary>
        /// <param name="observables">Observables to search for</param>
        /// <param name="status">Status for the observables</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="page">Page</param>
        /// <returns>List of observable matching the criteria.</returns>
        private async Task<IEnumerable<DocumentFileObservables>> SearchExistingObservables(
            IEnumerable<Observable> observables, string[] status, int pageSize, int page)
        {
            var arrValues = observables.Where(p => p.Value != null).Select(item => item.Value).ToList();
            if (arrValues.Any())
            {
                var searchResponse = await _elasticClient.SearchAsync<DocumentFileObservables>(s => s
                    .From(pageSize * page)
                    .Size(pageSize)
                    .Query(q => q
                            .Terms(t => t
                                .Field(f => f.Value)
                                .Terms(arrValues)
                            ) && q.Terms(t => t
                            .Field(x => x.Status)
                            .Terms(status))
                    )
                );

                return searchResponse.Documents;
            }

            return null;
        }

        private async Task<bool> DeleteRelationships(Guid documentId)
        {
            var response = await _elasticClient.DeleteByQueryAsync<Relationship>(q => q
                .Index("relationship")
                .Query(q => q
                    .Term(t => t
                        .Field(x => x.DocumentRef)
                        .Value(documentId.ToString())
                    )
                ));
            return response.IsValid;
        }

        private async Task<bool> DeleteRelationshipObservable(Guid observableId)
        {
            var response = await _elasticClient.DeleteByQueryAsync<Relationship>(q => q
                .Index("relationship")
                .Query(q => q
                    .Term(t => t
                        .Field(x => x.SourceRef)
                        .Value(observableId.ToString())
                    )
                ));
            return response.IsValid;
        }

        private async Task<IEnumerable<Relationship>> GetRelationships(int pageSize, int page, Guid documentId)
        {
            var response = await _elasticClient.SearchAsync<Relationship>(s => s
                .From(pageSize * page)
                .Size(pageSize)
                .Index("relationship")
                .Query(q => q
                    .Term(t => t
                        .Field(x => x.DocumentRef)
                        .Value(documentId.ToString())
                    )
                )
            );
            return response.Documents;
        }

        private async Task<IEnumerable<T>> RockAndScroll<T>(
            string indexName,
            string scrollTimeoutMinutes = "2m",
            int scrollPageSize = 1000
        ) where T : class
        {
            var searchResponse = await _elasticClient.SearchAsync<T>(sd => sd
                .Index(indexName)
                .From(0)
                .Take(scrollPageSize)
                .MatchAll()
                .Scroll(scrollTimeoutMinutes));

            var results = new List<T>();

            while (true)
            {
                if (!searchResponse.IsValid || string.IsNullOrEmpty(searchResponse.ScrollId))
                    throw new Exception($"Search error: {searchResponse.ServerError.Error.Reason}");

                if (!searchResponse.Documents.Any())
                    break;

                results.AddRange(searchResponse.Documents);
                searchResponse = await _elasticClient.ScrollAsync<T>(scrollTimeoutMinutes, searchResponse.ScrollId);
            }

            await _elasticClient.ClearScrollAsync(new ClearScrollRequest(searchResponse.ScrollId));

            return results;
        }
    }
}