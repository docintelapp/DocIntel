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
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using DocIntel.Core.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Authorization.Handlers
{
    public abstract class CustomAuthorizationHandler<T> : AuthorizationHandler<OperationAuthorizationRequirement, T>
    {
        protected readonly ILogger _logger;
        protected readonly SignInManager<AppUser> _signInManager;
        protected readonly UserManager<AppUser> _userManager;

        protected CustomAuthorizationHandler(
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            ILogger<CustomAuthorizationHandler<T>> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        public override async Task HandleAsync(AuthorizationHandlerContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            if (context.User == null) throw new ArgumentNullException(nameof(context.User));

            if (context.Resource != null && context.Resource is T t)
            {
                // TODO may be check only the appropriate authorization handlers?
                foreach (var req in context.Requirements.OfType<OperationAuthorizationRequirement>())
                    await HandleRequirementAsync(context, req, t);
            }
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OperationAuthorizationRequirement requirement,
            T resource)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (requirement is null) throw new ArgumentNullException(nameof(requirement));

            var userId = context.User.Claims.Single(claim => claim.Type == ClaimTypes.NameIdentifier).Value;

            if (context.User.HasClaim(DocIntelConstants.ClaimPermissionType, requirement.Name))
            {
                context.Succeed(requirement);
            }
            else
            {
                // TODO Use structured logging
                _logger.LogWarning($"User {userId} does not have permission '{requirement.Name}'.");
            }

            return Task.CompletedTask;
        }
    }
}