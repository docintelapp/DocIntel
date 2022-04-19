using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DocIntel.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocIntel.Core.Authentication;

public class AppUserManager : AspNetUserManager<AppUser>
{
    public override bool SupportsUserClaim => false;

    public AppUserManager(IUserStore<AppUser> store,
        IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<AppUser> passwordHasher,
        IEnumerable<IUserValidator<AppUser>> userValidators,
        IEnumerable<IPasswordValidator<AppUser>> passwordValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        IServiceProvider services,
        ILogger<UserManager<AppUser>> logger) : base(store,
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

    public override Task<IList<Claim>> GetClaimsAsync(AppUser user)
    {
        try
        {
            return base.GetClaimsAsync(user);
        }
        catch (Exception e)
        {
            Console.WriteLine("ERROR in AppUserManager");
        }

        return Task.FromResult((IList<Claim>)Enumerable.Empty<Claim>().ToList());
    }
}