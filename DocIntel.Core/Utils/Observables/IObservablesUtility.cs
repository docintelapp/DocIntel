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

using System.Collections.Generic;
using System.Threading.Tasks;
using DocIntel.Core.Models;
using Synsharp.Telepath.Messages;

namespace DocIntel.Core.Utils.Observables
{
    public interface IObservablesUtility
    {
        /// <summary>
        /// Extract synapse objects from the content.
        /// </summary>
        /// <param name="document">The source document</param>
        /// <param name="file">The source file</param>
        /// <param name="content">The text content of the file</param>
        /// <returns>The objects contained in the text</returns>
        IAsyncEnumerable<SynapseNode> ExtractDataAsync(Document document, DocumentFile file, string content);

        /// <summary>
        /// Annotate the synapse objects with tags.
        /// 
        /// For example, <c>_di.workflow.malicious</c>, <c>_di.workflow.legit</c>, <c>_di.workflow.review</c>
        /// </summary>
        /// <param name="document">The source document</param>
        /// <param name="file">The source file</param>
        /// <param name="objects">The objects</param>
        Task AnnotateAsync(Document document, DocumentFile file, IEnumerable<SynapseNode> objects);
    }
}