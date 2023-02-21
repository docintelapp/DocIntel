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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json.Linq;

namespace DocIntel.Core.Models
{
    /// <summary>
    ///     Represents an plugin
    /// </summary>
    public class Importer
    {
        /// <summary>
        ///     The global, unique, identifier for the plugin.
        /// </summary>
        public Guid ImporterId { get; init; }

        /// <summary>
        ///     The name of the plugin.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     A brief description of the plugin
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///     Whether the plugin is enabled or disabled.
        /// </summary>
        /// <value>
        ///     <c>True</c> if the plugin is enabled, <c>False</c> otherwise.
        /// </value>
        public ImporterStatus Status { get; set; }

        public TimeSpan CollectionDelay { get; set; }

        public DateTime? LastCollection { get; set; }

        public string FetchingUserId { get; set; }
        public AppUser FetchingUser { get; set; }
        
        /// <summary>
        ///     The specific settings for the plugin.
        /// </summary>
        [Column(TypeName = "jsonb")]
        public JObject Settings { get; set; }
        public Guid ReferenceClass { get; set; }
        
        [DefaultValue(10)]
        public int Limit { get; set; } = 10;
        public int Priority { get; set; }

        public bool OverrideClassification { get; set; }
        public Classification Classification { get; set; }
        public Guid? ClassificationId { get; set; }

        public bool OverrideReleasableTo { get; set; }
        public ICollection<Group> ReleasableTo { get; set; }
        public bool OverrideEyesOnly { get; set; }
        public ICollection<Group> EyesOnly { get; set; }
        public Guid? SourceId { get; set; }
        public Source Source { get; set; }
        
        public ICollection<Tag> Tags { get; set; }
        public bool OverrideSource { get; set; }
        public bool SkipInbox { get; set; }
    }

    public enum ImporterStatus
    {
        Disabled, Enabled, Error
    }
    
    public class Scraper {
        public Guid ScraperId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Enabled { get; set; }
        [Column(TypeName = "jsonb")]
        public JObject Settings { get; set; }
        
        public Guid ReferenceClass { get; set; }
        
        public bool OverrideSource { get; set; }
        public Guid? SourceId { get; set; }
        public Source Source { get; set; }
        public bool SkipInbox { get; set; }
        public int Position { get; set; }

        public bool OverrideClassification { get; set; }
        public Classification Classification { get; set; }
        public Guid? ClassificationId { get; set; }

        public ICollection<Group> ReleasableTo { get; set; }
        public ICollection<Group> EyesOnly { get; set; }
        public bool OverrideReleasableTo { get; set; }
        public bool OverrideEyesOnly { get; set; }
        
        public ICollection<Tag> Tags { get; set; }
    }
}