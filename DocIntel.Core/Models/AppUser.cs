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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json.Linq;

namespace DocIntel.Core.Models
{
    public class AppUser : IdentityUser
    {
        [Display(Name = "First Name")] public string FirstName { get; set; }

        [Display(Name = "Last Name")] public string LastName { get; set; }

        public string Function { get; set; }
        public string ProfilePicture { get; set; }

        public DateTime LastActivity { get; set; }
        public DateTime LastLogin { get; set; }

        public DateTime RegistrationDate { get; set; }

        [DefaultValue(true)] public bool Enabled { get; set; } = true;

        [DefaultValue(false)] public bool Bot { get; set; } = false;

        [Column(TypeName = "jsonb")]
        public UserPreferences Preferences { get; set; }

        public ICollection<UserTagSubscription> SubscribedTags { get; set; }
        public ICollection<UserFacetSubscription> SubscribedFacets { get; set; }

        public ICollection<APIKey> APIKeys { get; set; }

        public ICollection<Member> Memberships { get; set; }

        [NotMapped]
        public string FriendlyName
        {
            get
            {
                if (string.IsNullOrEmpty(FirstName) & string.IsNullOrEmpty(LastName))
                    return UserName;
                return $"{FirstName} {LastName}";
            }
        }

        public AppUser()
        {
            Preferences = new UserPreferences();
        }
        
        [Column(TypeName = "jsonb")] public Dictionary<string, JsonObject> MetaData { get; set; }
    }
}