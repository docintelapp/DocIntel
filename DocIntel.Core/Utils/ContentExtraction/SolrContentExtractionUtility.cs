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

// 

using System.IO;

using DocIntel.Core.Models;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Indexation.SolR;

using SolrNet;

namespace DocIntel.Core.Utils.ContentExtraction
{
    public class SolrContentExtractionUtility : IContentExtractionUtility
    {
        private readonly ApplicationSettings _appSettings;
        private readonly ISolrOperations<IndexedDocument> _solr;

        public SolrContentExtractionUtility(ApplicationSettings appSettings, ISolrOperations<IndexedDocument> solr)
        {
            _appSettings = appSettings;
            _solr = solr;
        }

        public string ExtractText(Document document, DocumentFile file)
        {
            var filename = Path.Combine(_appSettings.DocFolder, file.Filepath);
            if (!File.Exists(filename)) return null;
            using var f = File.OpenRead(filename);
            var response = _solr.Extract(new ExtractParameters(f, file.FileId.ToString())
            {
                ExtractOnly = true,
                ExtractFormat = ExtractFormat.Text,
                StreamType = file.MimeType
            });
            return response?.Content;
        }
    }
}