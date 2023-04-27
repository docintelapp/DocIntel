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

using System.ComponentModel.DataAnnotations;

namespace DocIntel.WebApp.ViewModels.Account
{
    public class ExternalLoginViewModel
    {
        public string ProviderDisplayName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        [Display(Name = "User name")]
        public string UserName { get; set; }
    }
}