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
using System.Text.Json.Serialization;
using DocIntel.Core.Models;
using Newtonsoft.Json;

namespace DocIntel.WebApp.Areas.API.Models
{
    public class ApiDocument
    {
        /// <summary>
        /// The title
        /// </summary>
        /// <example>Hardware-based threat defense against increasingly complex cryptojackers</example>
        [Required]
        public string Title { get; set; }
        
        /// <summary>
        /// A short summary
        /// </summary>
        /// <example><![CDATA[<p>Even with the dip in the value of cryptocurrencies in the past few months, cryptojackers – trojanized coin miners that attackers distribute to use compromised devices’ computing power for their objectives – continue to be widespread. In the past several months, Microsoft Defender Antivirus detected cryptojackers on hundreds of thousands of devices every month. These threats also continue to evolve: recent cryptojackers have become stealthier, leveraging living-off-the-land binaries (LOLBins) to evade detection.</p>]]></example>
        [JsonPropertyName("summary")]
        public string Summary { get; set; }
        
        /// <summary>
        /// The reference as used by the external source
        /// </summary>
        /// <example>18-12450234</example>
        [JsonPropertyName("external_reference")]
        public string ExternalReference { get; set; }
        
        /// <summary>
        /// The URL from which the document was retrieved
        /// </summary>
        /// <example>https://blog.example.org/my-new-apt-report</example>
        [JsonPropertyName("source_url")]
        public string SourceUrl { get; set; }
        
        /// <summary>
        /// The publication date of the document
        /// </summary>
        /// <example>2022-08-18T17:00:00</example>
        [JsonPropertyName("document_date")]
        public DateTime? DocumentDate { get; set; }
        
        /// <summary>
        /// Notes, in HTML.
        /// </summary>
        public string Note { get; set; }
        
        /// <summary>
        /// A set of tags
        /// </summary>
        public List<string> Tags { get; set; }
        
        /// <summary>
        /// The identifier for the source
        /// </summary>
        /// <example>fa654749-fdd1-4f57-9ac5-9fcad4cca406</example>
        [JsonPropertyName("source_id")]
        public Guid? SourceId { get; set; }
        
        /// <summary>
        /// The identifier for the classification
        /// </summary>
        /// <example>ad153066-0c0a-4ecc-914b-e5876c8d3787</example>
        [JsonPropertyName("classification_id")]
        public Guid? ClassificationId { get; set; }
    }
    
    public class ApiDocumentDetails : ApiDocument
    {
        /// <summary>
        /// The document identifier
        /// </summary>
        /// <example>ca913805-ae9f-47fe-8e90-b20fb29fb72f</example>
        [JsonPropertyName("document_id")]
        public Guid DocumentId { get; set; }

        /// <summary>
        /// The reference, given by DocIntel
        /// </summary>
        /// <example>DI-2022-09-084</example>
        public string Reference { get; set; }

        /// <summary>
        /// The source
        /// </summary>
        public ApiSourceDetails Source { get; set; }

        /// <summary>
        /// The classification
        /// </summary>
        public APIClassification Classification { get; set; }

        /// <summary>
        /// The registration date 
        /// </summary>
        /// <example>2022-09-28T15:26:04.347412</example>
        [JsonPropertyName("registration_date")]
        public DateTime RegistrationDate { get; set; }

        /// <summary>
        /// The last modification date
        /// </summary>
        /// <example>2022-09-19T19:01:02.824604</example>
        [JsonPropertyName("modification_date")]
        public DateTime ModificationDate { get; set; }

        /// <summary>
        /// The user who registered the document
        /// </summary>
        [JsonPropertyName("registered_by")]
        public APIAppUser RegisteredBy { get; set; }

        /// <summary>
        /// The user who last modified the document
        /// </summary>
        [JsonPropertyName("last_modified_by")]
        public APIAppUser LastModifiedBy { get; set; }

        /// <summary>
        /// The status of the document
        /// </summary>
        public DocumentStatus Status { get; set; }

        /// <summary>
        /// The slug at which the document is available in DocIntel
        /// </summary>
        /// <example>hardware-based-threat-defense-against-increasingly-complex-cryptojackers</example>
        public string URL { get; set; }
        
        /// <summary>
        /// The tags
        /// </summary>
        public new List<APITag> Tags { get; set; }
    }
}