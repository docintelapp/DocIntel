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
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Bogus;
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
    [Route("API/Authentication")]
    [ApiController]
    public class AuthenticationController : DocIntelAPIControllerBase
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IUserClaimsPrincipalFactory<AppUser> _claimPrincipalFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthenticationController> _logger;
        private readonly IUserRepository _userRepository;

        public AuthenticationController(AppUserManager userManager,
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
        /// Login
        /// </summary>
        /// <remarks>
        /// Retrieves an authentication token for the specified user.  
        /// </remarks>
        /// <param name="login">The login credentials</param>
        /// <returns>The authentication token</returns>
        /// <response code="200">Returns the newly created token</response>
        /// <response code="400">The provided credentials are null or empty.</response>
        /// <response code="401">The provided credentials are invalid.</response>
        [AllowAnonymous]
        [HttpPost("Login")]
        [SwaggerOperation(
            OperationId = "Login"
        )]
        [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> LoginAsync([FromBody] LoginInformation login)
        {
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
                return apiKey.User; // TODO user should come from usermanager
            }
            catch (NotFoundEntityException)
            {
                return null;
            }
        }

        public class LoginInformation
        {
            [Required]
            [SwaggerSchema("The username")]
            public string Username { get; set; }
            
            [Required]
            [JsonPropertyName("api_key")]
            [SwaggerSchema("The API key")]
            public string APIKey { get; set; }
        }

        public class LoginResult
        {
            [SwaggerSchema("The authentication token")]
            public string Token { get; set; }
        }

        public class LoginInformationExample : IExamplesProvider<LoginInformation>
        {
            public LoginInformation GetExamples()
            {
                var faker = new Faker("en");
                return new LoginInformation
                {
                    Username = faker.Internet.UserName(),
                    APIKey = Base64Encode(faker.Random.String(64))
                };
            }
            
            private static string Base64Encode(string plainText) {
                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
                return System.Convert.ToBase64String(plainTextBytes);
            }
        }
        
        public class LoginResultExample : IExamplesProvider<LoginResult>
        {
            public LoginResult GetExamples()
            {
                var faker = new Faker("en");
                return new LoginResult
                {
                    Token = faker.Random.String2(2700, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.")
                };
            }
        }
    }
}