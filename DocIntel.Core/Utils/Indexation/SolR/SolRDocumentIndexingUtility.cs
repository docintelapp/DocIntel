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
using System.IO;
using System.Linq;

using AutoMapper;

using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Observables;
using Microsoft.Extensions.Logging;

using SolrNet;

namespace DocIntel.Core.Utils.Indexation.SolR
{
    public class SolRDocumentIndexingUtility : IDocumentIndexingUtility
    {
        private readonly ApplicationSettings _settings;
        private readonly ILogger<SolRDocumentIndexingUtility> _logger;
        private readonly IMapper _mapper;
        private readonly ISolrOperations<IndexedDocument> _solr;
        private readonly ISynapseRepository _observableRepository;

        public SolRDocumentIndexingUtility(ISolrOperations<IndexedDocument> solr,
            ILogger<SolRDocumentIndexingUtility> logger, IMapper mapper, ApplicationSettings settings, ISynapseRepository observableRepository)
        {
            _solr = solr;
            _logger = logger;
            _mapper = mapper;
            _settings = settings;
            _observableRepository = observableRepository;
        }

        public void Add(Document document)
        {
            _logger.LogDebug("Add " + document.DocumentId);
            var indexedDocument = _mapper.Map<IndexedDocument>(document);
            indexedDocument.FileContents = ExtractFileContent(document);
            indexedDocument.Observables = (_observableRepository.GetObservables(document).ToListAsync().Result).Select(_ => _.GetCoreValue());
            _solr.Add(indexedDocument, new AddParameters()
            {
                CommitWithin = _settings.Schedule.CommitWithin,
                Overwrite = true
            });
        }

        public void Remove(Guid documentId)
        {
            _logger.LogDebug("Delete " + documentId);
            _solr.Delete(documentId.ToString(), new DeleteParameters()
            {
                CommitWithin = _settings.Schedule.CommitWithin
            });
        }

        public void RemoveAll()
        {
            _solr.Delete(SolrQuery.All, new DeleteParameters()
            {
                CommitWithin = _settings.Schedule.CommitWithin
            });
        }

        public void Update(Document document)
        {
            _logger.LogDebug("Update " + document.DocumentId);
            var indexedDocument = _mapper.Map<IndexedDocument>(document);
            indexedDocument.FileContents = ExtractFileContent(document);
            indexedDocument.Observables = _observableRepository.GetObservables(document).ToListAsync().Result.Select(_ => _.GetCoreValue());
            _solr.Add(indexedDocument, new AddParameters()
            {
                CommitWithin = _settings.Schedule.CommitWithin,
                Overwrite = true
            });
        }

        public void Commit()
        {
            _logger.LogDebug("Commit");
            _solr.Commit();
        }

        private List<string> ExtractFileContent(Document document)
        {
            var fileContents = new List<string>();
            foreach (var file in document.Files.Where(_ =>
                _.MimeType == "application/pdf" || _.MimeType.StartsWith("text")))
            {
                var filename = Path.Combine(_settings.DocFolder, file.Filepath);
                if (File.Exists(filename))
                {
                    using var f = File.OpenRead(filename);
                    var response = _solr.Extract(new ExtractParameters(f, file.FileId.ToString())
                    {
                        ExtractOnly = true,
                        ExtractFormat = ExtractFormat.Text,
                        StreamType = file.MimeType
                    });
                    fileContents.Add(response.Content);
                }
            }

            return fileContents;
        }
    }
}
