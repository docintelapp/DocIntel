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
using System.Threading.Tasks;
using DocIntel.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Authorization.Handlers
{
    public class NullAuthorizationHandler : CustomAuthorizationHandler<object>
    {
        public NullAuthorizationHandler(
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            ILogger<NullAuthorizationHandler> logger) : base(signInManager, userManager, logger)
        {
        }

        public override async Task HandleAsync(AuthorizationHandlerContext context)
        {
            if (context.User == null) throw new ArgumentNullException();

            if (context.Resource == null)
            {
                foreach (var req in context.Requirements.OfType<OperationAuthorizationRequirement>())
                    await HandleRequirementAsync(context, req, null);
            }
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OperationAuthorizationRequirement requirement,
            object resource)
        {
            if (context.User.HasClaim(DocIntelConstants.ClaimPermissionType, requirement.Name))
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}