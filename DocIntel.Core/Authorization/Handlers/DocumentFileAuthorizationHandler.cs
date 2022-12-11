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
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Authorization.Handlers
{
    public class DocumentFileAuthorizationHandler : CustomAuthorizationHandler<DocumentFile>
    {
        private readonly ApplicationSettings _settings;
        private readonly IServiceProvider _serviceProvider;

        public DocumentFileAuthorizationHandler(
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            ILogger<DocumentFileAuthorizationHandler> logger,
            ApplicationSettings settings, IServiceProvider serviceProvider) : base(signInManager,
            userManager,
            logger)
        {
            _settings = settings;
            _serviceProvider = serviceProvider;
        }
        
        protected async Task<AmbientContext> GetContext(string registeredBy = null)
        {
            var userClaimsPrincipalFactory = _serviceProvider.GetService<AppUserClaimsPrincipalFactory>();
            if (userClaimsPrincipalFactory == null) throw new ArgumentNullException(nameof(userClaimsPrincipalFactory));

            var options =
                (DbContextOptions<DocIntelContext>) _serviceProvider.GetService(
                    typeof(DbContextOptions<DocIntelContext>));
            var context = new DocIntelContext(options,
                (ILogger<DocIntelContext>) _serviceProvider.GetService(typeof(ILogger<DocIntelContext>)));

            var automationUser = !string.IsNullOrEmpty(registeredBy)
                ? await _userManager.FindByIdAsync(registeredBy)
                : await _userManager.FindByNameAsync(_settings.AutomationAccount);
            if (automationUser == null)
                return null;

            var claims = await userClaimsPrincipalFactory.CreateAsync(automationUser);
            return new AmbientContext
            {
                DatabaseContext = context,
                Claims = claims,
                CurrentUser = automationUser
            };
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OperationAuthorizationRequirement requirement,
            DocumentFile resource)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            if (requirement is null) throw new ArgumentNullException(nameof(requirement));

            if (resource != null)
            {
                _logger.LogDebug("Resource is not null");

                if (resource.EyesOnly?.Any() ?? false)
                {
                    _logger.LogDebug("Evaluating EyesOnly");
                    var eyesOnly = resource.EyesOnly.Select(_ => _.GroupId).ToHashSet();
                    if (context.User.Claims.Any(_ => _.Type == "Group" && eyesOnly.Contains(Guid.Parse(_.Value))))
                    {
                        context.Succeed(requirement);
                    }
                    else
                    {
                        _logger.LogWarning("User has no eyes on the document");
                        context.Fail();
                    }
                }

                if ((resource.ReleasableTo?.Any() ?? false))
                {
                    // TODO Use user and role claims to avoid querying the database unnecessarily
                    var ambientContext = await GetContext();
                    var groupRepository = _serviceProvider.GetRequiredService<IGroupRepository>();
                    var defaultGroups = groupRepository.GetDefaultGroups(ambientContext).Select(_ => _.GroupId).ToArray();
                    
                    _logger.LogDebug("Evaluating ReleasableTo");
                    var relTo = (resource.ReleasableTo ?? Enumerable.Empty<Group>()).Select(_ => _.GroupId).Union(defaultGroups).ToHashSet();
                    if (context.User.Claims.Any(_ => _.Type == "Group" && relTo.Contains(Guid.Parse(_.Value))))
                    {
                        context.Succeed(requirement);
                    }
                    else
                    {
                        _logger.LogWarning("User has no rel to");
                        context.Fail();
                    }
                }

                if (!context.HasFailed) base.HandleRequirementAsync(context, requirement, resource);
            }
        }
    }
}