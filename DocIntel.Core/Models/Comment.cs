/* DocIntel
 * Copyright (C) 2018-2023 Belgian Defense, Antoine Cailliau
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
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Nodes;

namespace DocIntel.Core.Models
{
    public class Comment
    {
        public Comment()
        {
            CommentId = Guid.NewGuid();
        }

        [Key] public Guid CommentId { get; set; }

        public string AuthorId { get; set; }
        public AppUser Author { get; set; }

        public Guid DocumentId { get; set; }

        public Document Document { get; set; }

        public DateTime CommentDate { get; set; }

        [Required] public string Body { get; set; }

        public string LastModifiedById { get; set; }
        public AppUser LastModifiedBy { get; set; }

        public DateTime ModificationDate { get; set; }
        
        [Column(TypeName = "jsonb")] public Dictionary<string, JsonObject> MetaData { get; set; }
    }
}