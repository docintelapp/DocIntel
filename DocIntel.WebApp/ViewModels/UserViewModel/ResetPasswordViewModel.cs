/* DocIntel
 * Copyright (C) 2018-2022 Belgian Defense, Antoine Cailliau
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
using System.ComponentModel.DataAnnotations;

using DocIntel.Core.Models;

using Microsoft.AspNetCore.Identity;

namespace DocIntel.WebApp.ViewModels.UserViewModel
{
    public class ResetPasswordViewModel
    {
        public string UserId { get; set; }
        public AppUser User { get; set; }
        
        [Display(Name = "Password")]
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password")]        
        [Display(Name = "Password Confirmation")]
        public string PasswordConfirmation { get; set; }

        public PasswordOptions PasswordOptions { get; set; }
        
        public string Code { get; set; }
    }
}