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
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

namespace DocIntel.WebApp.Areas.API.Controllers
{
    [Area("API")]
    [Route("API/Authentication")]
    [ApiController]
    public class AuthenticationController : DocIntelAPIControllerBase
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IUserClaimsPrincipalFactory<AppUser> _claimPrincipalFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthenticationController> _logger;
        private readonly IUserRepository _userRepository;

        public AuthenticationController(UserManager<AppUser> userManager,
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

        [AllowAnonymous]
        [HttpPost("Login")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResult))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginInformation login)
        {
            string req_txt;
            _logger.LogInformation(HttpContext.Request.ContentLength.ToString());
            _logger.LogInformation(HttpContext.Request.ContentType);
            _logger.LogInformation(HttpContext.Request.Headers.ToString());
            
            _logger.Log(LogLevel.Information,
                EventIDs.APILoginAttempt,
                new LogEvent($"Login attempt for user '{login.Username}'.")
                    .AddProperty("user.name", login.Username)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            var username = login.Username;
            var apikey = login.APIKey;

            if (string.IsNullOrEmpty(username) | string.IsNullOrEmpty(apikey))
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APILoginFailed,
                    new LogEvent($"User '{login.Username}' attempted to login with an empty API key or username.")
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("user.name", login.Username),
                    null,
                    LogEvent.Formatter);
                return BadRequest();
            }

            IActionResult response = Unauthorized();
            var user = await AuthenticateUserAsync(username, apikey);

            if (user != null)
            {
                var tokenString = GenerateJSONWebToken(user);
                _logger.Log(LogLevel.Information,
                    EventIDs.APILoginSuccessful,
                    new LogEvent($"User '{login.Username}' successfully generated an JWT security token.")
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("user.name", login.Username),
                    null,
                    LogEvent.Formatter);
                response = Ok(new LoginResult {Token = tokenString});
            }
            else
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APILoginFailed,
                    new LogEvent($"User '{login.Username}' attempted to login with invalid credentials.")
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("user.name", login.Username),
                    null,
                    LogEvent.Formatter);
            }

            return response;
        }

        private string GenerateJSONWebToken(AppUser user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = _claimPrincipalFactory.CreateAsync(user).Result.Claims.ToList();

            var token = new JwtSecurityToken(
                _config["Jwt:Issuer"],
                _config["Jwt:Issuer"],
                expires: DateTime.Now.AddHours(3),
                claims: claims,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<AppUser> AuthenticateUserAsync(string username, string apikey)
        {
            try
            {
                var apiKey = await _userRepository.ValidateAPIKey(AmbientContext, username, apikey, new[] {"User"});

                apiKey.LastUsage = DateTime.UtcNow;
                if (HttpContext.Connection.RemoteIpAddress != null)
                    apiKey.LastIP = HttpContext.Connection.RemoteIpAddress.ToString();
                AmbientContext.DatabaseContext.APIKeys.Update(apiKey);
                await AmbientContext.DatabaseContext.SaveChangesAsync();
                return apiKey.User;
            }
            catch (NotFoundEntityException)
            {
                return null;
            }
        }

        public class LoginInformation
        {
            public string Username { get; set; }
            public string APIKey { get; set; }
        }

        public class LoginResult
        {
            public string Token { get; set; }
        }
    }
}