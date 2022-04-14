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

using System.Threading.Tasks;

using DocIntel.Core.Models;
using DocIntel.Core.Repositories;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DocIntel.WebApp.Areas.API.Controllers
{
    [Authorize]
    public abstract class DocIntelAPIControllerBase : ControllerBase
    {
        protected readonly DocIntelContext _context;
        protected readonly UserManager<AppUser> _userManager;

        protected DocIntelAPIControllerBase(UserManager<AppUser> userManager,
            DocIntelContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        protected AmbientContext AmbientContext => new()
        {
            DatabaseContext = _context,
            Claims = User,
            CurrentUser = GetCurrentUser().Result
        };

        protected async Task<AppUser> GetCurrentUser()
        {
            return await _userManager.GetUserAsync(HttpContext.User);
        }
    }
}