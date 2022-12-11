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
using System.Linq;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;
using DocIntel.WebApp.ViewModels;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DocIntel.WebApp.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public abstract class BaseController : Controller
    {
        protected readonly IAuthorizationService _authorizationService;
        protected readonly ApplicationSettings _configuration;
        protected readonly DocIntelContext _context;
        protected readonly AppUserManager _userManager;

        public BaseController(DocIntelContext context,
            AppUserManager userManager,
            ApplicationSettings configuration,
            IAuthorizationService authorizationService)
        {
            _userManager = userManager;
            _configuration = configuration;
            _context = context;
            _authorizationService = authorizationService;
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

        public override ViewResult View()
        {
            ProcessViewModel().Wait();
            return base.View();
        }

        public override ViewResult View(object model)
        {
            ProcessViewModel().Wait();
            return base.View(model);
        }

        private async Task ProcessViewModel()
        {
            var currentUser = await GetCurrentUser();
            if (currentUser == null) return;

            await AmbientContext.DatabaseContext.Entry(currentUser).Collection(u => u.Memberships).LoadAsync();
            ViewBag.CurrentUser = currentUser;
            ViewBag.ApplicationName = _configuration.ApplicationName;

            var smartNavigation = await BuildNavigation(currentUser);
            ViewBag.NavigationModel = smartNavigation;
        }

        private async Task<SmartNavigation> BuildNavigation(AppUser currentUser)
        {
            var smartNavigation = NavigationModel.BuildNavigation(Url, currentUser);

            if (currentUser != null) smartNavigation.Lists = await FilterForPermissionsAsync(smartNavigation.Lists);

            return smartNavigation;
        }

        private async Task<List<ListItem>> FilterForPermissionsAsync(List<ListItem> items)
        {
            var filteredItem = new List<ListItem>();

            // Remove actions that are not authorized
            foreach (var item in items)
            {
                var authorized = false;
                if (item.Permissions != null && item.Permissions.Any())
                    foreach (var permission in item.Permissions)
                    {
                        var isAuthorized = await _authorizationService.AuthorizeAsync(User, null,
                            new OperationAuthorizationRequirement {Name = permission}
                        );
                        if (isAuthorized.Succeeded)
                        {
                            authorized = true;
                            break;
                        }
                    }
                else
                    authorized = true;

                var childCount = item.Items.Count();
                if (item.Items != null) item.Items = await FilterForPermissionsAsync(item.Items);

                if (authorized && (childCount == 0 || item.Items.Any())) filteredItem.Add(item);
            }

            return filteredItem;
        }

        public override ViewResult View(string viewName, object model)
        {
            ProcessViewModel().Wait();
            return base.View(viewName, model);
        }
    }
}