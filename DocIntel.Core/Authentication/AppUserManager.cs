using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DocIntel.Core.Helpers;
using DocIntel.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocIntel.Core.Authentication;

public class AppUserManager : AspNetUserManager<AppUser>
{
    public AppUserManager(IUserStore<AppUser> store,
        IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<AppUser> passwordHasher,
        IEnumerable<IUserValidator<AppUser>> userValidators,
        IEnumerable<IPasswordValidator<AppUser>> passwordValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        IServiceProvider services,
        ILogger<AppUserManager> logger) : base(store,
        optionsAccessor,
        passwordHasher,
        userValidators,
        passwordValidators,
        keyNormalizer,
        errors,
        services,
        logger)
    {
    }

    public override bool SupportsUserClaim => true;
    public override bool SupportsUserRole => true;

    /// <summary>
    ///     Resets the password of the user by generating and directly using a
    ///     new reset token via
    ///     <see
    ///         cref="Microsoft.AspNetCore.Identity.UserManager{TUser}" />
    ///     .
    /// </summary>
    /// <param name="user">The user</param>
    /// <param name="newPassword">The password</param>
    /// <returns>
    ///     <c>True</c> if the password was successfully resets, <c>False</c>
    ///     otherwise.
    /// </returns>
    public async Task<bool> ResetPassword(ClaimsPrincipal claimsPrincipal, AppUser user,
        string newPassword)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        //if (!await _appAuthorizationService.CanResetPassword(claimsPrincipal, user))
            // throw new UnauthorizedOperationException();

        var resetToken = await base.GeneratePasswordResetTokenAsync(user);

        var passwordChangeResult
            = await base.ResetPasswordAsync(user,
                resetToken,
                newPassword);
        return passwordChangeResult.Succeeded;
    }


    /// <summary>
    ///     Resets the password via
    ///     <see
    ///         cref="Microsoft.AspNetCore.Identity.UserManager{TUser}" />
    ///     with the
    ///     provided reset token.
    /// </summary>
    /// <param name="user">The user</param>
    /// <param name="resetToken">The reset token</param>
    /// <param name="newPassword">The password</param>
    /// <returns>
    ///     <c>True</c> if the password was successfully reset, <c>False</c>
    ///     otherwise.
    /// </returns>
    public async Task<bool> ResetPassword(ClaimsPrincipal claimsPrincipal, AppUser user,
        string resetToken,
        string newPassword)
    {
        //if (!await _appAuthorizationService.CanResetPassword(claimsPrincipal, user))
          //  throw new UnauthorizedOperationException();

        var passwordChangeResult
            = await base.ResetPasswordAsync(user,
                resetToken,
                newPassword);
        return passwordChangeResult.Succeeded;
    }

    public async Task<IdentityResult> CreateAsync(ClaimsPrincipal claimsPrincipal, AppUser user, string password)
    {
        //if (!await _appAuthorizationService.CanCreateUser(claimsPrincipal, user))
          //  throw new UnauthorizedOperationException();
        
        user.RegistrationDate = DateTime.UtcNow;
        
        if (string.IsNullOrEmpty(password))
        {
            password = UserHelper.GenerateRandomPassword();
        }
        
        return await base.CreateAsync(user, password);
    }

    public override Task<IList<Claim>> GetClaimsAsync(AppUser user)
    {
        try
        {
            return base.GetClaimsAsync(user);
        }
        catch (Exception e)
        {
            Logger.LogError("Error while getting user claims: {EMessage}", e.Message);
        }

        return Task.FromResult((IList<Claim>)Enumerable.Empty<Claim>().ToList());
    }
}