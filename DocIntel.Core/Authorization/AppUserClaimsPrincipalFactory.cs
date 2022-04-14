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

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DocIntel.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DocIntel.Core.Authorization
{
    public class AppUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<AppUser, AppRole>
    {
        private readonly DocIntelContext _context;

        public AppUserClaimsPrincipalFactory(
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager,
            IOptions<IdentityOptions> options, DocIntelContext context)
            : base(userManager, roleManager, options)
        {
            _context = context;
        }
        
        public override async Task<ClaimsPrincipal> CreateAsync(AppUser user)
        {
            var principal = await base.CreateAsync(user);

            var claimsIdentity = (ClaimsIdentity) principal.Identity;

            if (claimsIdentity != null)
            {
                claimsIdentity.AddClaim(new Claim("UserId", user.Id));
                if (user.Bot)
                    claimsIdentity.AddClaim(new Claim("Bot", user.Bot.ToString()));

                var permissions = _context.UserRoles.AsQueryable()
                    .Where(x => x.UserId == user.Id)
                    .Select(_ => _.Role)
                    .AsEnumerable()
                    .SelectMany(x => x.Permissions);

                foreach (var permission in permissions) 
                    claimsIdentity.AddClaim(new Claim("Permission", permission));

                var groups = _context.Members.Include(_ => _.Group).AsQueryable()
                    .Where(x => x.UserId == user.Id)
                    .Select(_ => _.Group)
                    .ToList();

                foreach (var group in groups)
                {
                    claimsIdentity.AddClaim(new Claim("Group", @group.GroupId.ToString()));
                    AddParentGroup(claimsIdentity, @group);
                }
            }

            return principal;
        }

        private void AddParentGroup(ClaimsIdentity claimsIdentity, Group group)
        {
            if (group.ParentGroupId != default)
            {
                var parentGroup = _context.Groups.SingleOrDefault(_ => _.GroupId == group.ParentGroupId);
                if (parentGroup != null)
                {
                    claimsIdentity.AddClaim(new Claim("Group", parentGroup.GroupId.ToString()));
                    AddParentGroup(claimsIdentity, parentGroup);
                }
            }
        }
    }
}