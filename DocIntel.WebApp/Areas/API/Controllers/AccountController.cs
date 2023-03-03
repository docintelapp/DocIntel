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
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using DocIntel.Core;
using DocIntel.Core.Authentication;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.WebApp.Helpers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace DocIntel.WebApp.Areas.API.Controllers
{
    /// <summary>
    /// For authenticating with the API you must include the `Authentication` header with a `Bearer` token in all your
    /// requests. The format for the `Authentication` header is `Authentication: Bearer TOKEN` where `TOKEN` is the
    /// actual token obtained with a request to `/API/Authentication/Login`. Bearer tokens are like temporary passwords
    /// for your account. You generate a `Bearer` token using your personal API key. Your API key can be found in your
    /// DocIntel account user menu, under the item *Manage API Keys*. An account may have multiple API keys, last usage
    /// is tracked separately for each.
    ///
    /// Your API keys and tokens carry all your privileges, so keep it secure and don't share it with anyone. Always use
    /// HTTPS instead of HTTP for making your requests. Avoid storing these credentials in plaintext.
    /// </summary>
    [Area("API")]
    [Route("API/Account")]
    [ApiController]
    public class AccountController : DocIntelAPIControllerBase
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IUserClaimsPrincipalFactory<AppUser> _claimPrincipalFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthenticationController> _logger;
        private readonly IUserRepository _userRepository;

        public AccountController(AppUserManager userManager,
            DocIntelContext context,
            IConfiguration config,
            IUserRepository userRepository,
            ILogger<AuthenticationController> logger,
            IHttpContextAccessor accessor,
            IUserClaimsPrincipalFactory<AppUser> claimPrincipalFactory)
            : base(userManager, context)
        {
            _config = config;
            _userRepository = userRepository;
            _logger = logger;
            _accessor = accessor;
            _claimPrincipalFactory = claimPrincipalFactory;
        }

        /// <summary>
        /// Get users permission
        /// </summary>
        /// <remarks>
        /// Returns the permissions of the currently logged in user
        ///
        /// For example, with cURL
        /// 
        ///     curl --request GET \
        ///         --url http://localhost:5001/API/Authentication/Permissions \
        ///         --header 'Authorization: Bearer $TOKEN'
        /// 
        /// </remarks>
        /// <returns>The permissions</returns>
        /// <response code="201">Returns the permissions</response>
        /// <response code="401">Action is not authorized</response>
        [HttpGet("Permissions")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string[]))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        public async Task<IActionResult> GetPermissions()
        {
            var currentUser = await GetCurrentUser();
            var enumerable = (await _claimPrincipalFactory.CreateAsync(currentUser)).Claims
                .Where(claim => claim.Type == DocIntelConstants.ClaimPermissionType)
                .Select(claim => claim.Value)
                .ToList();
            return Ok(enumerable);
        }

        /// <summary>
        /// Get user's role
        /// </summary>
        /// <remarks>
        /// Returns the roles of the currently logged in user
        ///
        /// For example, with cURL
        /// 
        ///     curl --request GET \
        ///         --url http://localhost:5001/API/Authentication/Roles \
        ///         --header 'Authorization: Bearer $TOKEN'
        /// 
        /// </remarks>
        /// <returns>The permissions</returns>
        /// <response code="201">Returns the permissions</response>
        /// <response code="401">Action is not authorized</response>
        [HttpGet("Roles")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string[]))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        public async Task<IActionResult> GetRoles()
        {
            var currentUser = await GetCurrentUser();
            return Ok(await _userManager.GetRolesAsync(currentUser));
        }
    }
}