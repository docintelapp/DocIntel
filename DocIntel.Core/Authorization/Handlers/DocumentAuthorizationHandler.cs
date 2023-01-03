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
using System.Linq;
using System.Threading.Tasks;

using DocIntel.Core.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Authorization.Handlers
{
    public class DocumentAuthorizationHandler : CustomAuthorizationHandler<Document>
    {
        private readonly Guid[] defaultGroups;

        public DocumentAuthorizationHandler(
            DocIntelContext dbContext,
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            ILogger<DocumentAuthorizationHandler> logger) : base(signInManager, userManager, logger)
        {
            defaultGroups = dbContext.Groups.Where(_ => _.Default).Select(_ => _.GroupId).ToArray();
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OperationAuthorizationRequirement requirement,
            Document resource)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            if (requirement is null) throw new ArgumentNullException(nameof(requirement));

            if (resource == null)
            {
                _logger.LogTrace("Resource for handling DocumentAuthorization is null");
                return Task.CompletedTask;
            }

            // Bot needs to access documents for indexing for example
            // TODO Investigate if '!= default' is the best way to check, use pattern?
            if (context.User.Claims.All(_ => _.Type != "Bot") & (resource.EyesOnly != default) &&
                resource.EyesOnly.Any())
            {
                var eyesOnly = resource.EyesOnly.Select(_ => _.GroupId).ToHashSet();
                if (context.User.Claims.Any(_ => _.Type == "Group" && eyesOnly.Contains(Guid.Parse(_.Value))))
                {
                    context.Succeed(requirement);
                }
                else
                {
                    // TODO Use structured logging
                    _logger.LogWarning("User has no eyes on the document");
                    context.Fail();
                }
            }

            // Bot needs to access documents for indexing for example
            // TODO Investigate if '!= default' is the best way to check, use pattern?
            if (context.User.Claims.All(_ => _.Type != "Bot"))
            {
                if (defaultGroups.Length > 0 && !context.User.Claims.Any(_ => _.Type == "Group" && defaultGroups.Contains(Guid.Parse(_.Value))))
                {
                    if ((resource.ReleasableTo != default && resource.ReleasableTo.Any())
                        || (resource.EyesOnly != default && resource.EyesOnly.Any()))
                    {
                        _logger.LogTrace("Evaluating ReleasableTo");
                        var relTo = resource.ReleasableTo.Select(_ => _.GroupId)
                            .Union(resource.EyesOnly.Select(_ => _.GroupId)).ToHashSet();
                        if (!context.User.Claims.Any(_ => _.Type == "Group" && relTo.Contains(Guid.Parse(_.Value))))
                        {
                            _logger.LogWarning("User has no rel to");
                            context.Fail();
                        }
                    }
                    else
                    {
                        context.Fail();
                    }
                }
            }

            if (!context.HasFailed) base.HandleRequirementAsync(context, requirement, resource);

            return Task.CompletedTask;
        }
    }
}