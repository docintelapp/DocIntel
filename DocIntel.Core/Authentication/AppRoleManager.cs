using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DocIntel.Core.Authorization.Operations;
using DocIntel.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Authentication;

public class AppRoleManager : AspNetRoleManager<AppRole>
{
    public AppRoleManager(IRoleStore<AppRole> store,
        IEnumerable<IRoleValidator<AppRole>> roleValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        ILogger<AppRoleManager> logger,
        IHttpContextAccessor contextAccessor) 
        : base(store,
            roleValidators,
            keyNormalizer,
            errors,
            logger,
            contextAccessor)
    {
    }

    public override bool SupportsRoleClaims => true;

    public override Task<IdentityResult> CreateAsync(AppRole role)
    {
        role.CreationDate = DateTime.UtcNow;
        role.ModificationDate = role.CreationDate;
        return base.CreateAsync(role);
    }

    public override Task<IdentityResult> UpdateAsync(AppRole role)
    {
        role.ModificationDate = DateTime.UtcNow;
        return base.UpdateAsync(role);
    }

    public Task<IdentityResult> AddPermissionAsync(AppRole role, string permission)
    {
        return AddClaimAsync(role, new Claim(DocIntelConstants.ClaimPermissionType, permission));
    }

    public Task<IdentityResult> RemovePermissionAsync(AppRole role, string permission)
    {
        return base.RemoveClaimAsync(role, new Claim(DocIntelConstants.ClaimPermissionType, permission));
    }

    public async Task<IdentityResult> SetPermissionAsync(AppRole role, string[] permissions)
    {
        var existingPermissions = (await GetClaimsAsync(role))
            .Where(c => c.Type == DocIntelConstants.ClaimPermissionType)
            .Select(c => c.Value)
            .ToArray();
        permissions = FilterLegitimatePermissions(permissions);
        
        foreach (var permission in existingPermissions.Except(permissions))
        {
            var result = await RemovePermissionAsync(role, permission);
            if (!result.Succeeded)
                return result;
        }
        
        foreach (var permission in permissions.Except(existingPermissions))
        {
            var result = await AddPermissionAsync(role, permission);
            if (!result.Succeeded)
                return result;
        }

        return IdentityResult.Success;
    }

    public async Task<IList<string>> GetPermissionsAsync(AppRole role)
    {
        return (await base.GetClaimsAsync(role))
            .Where(c => c.Type == DocIntelConstants.ClaimPermissionType)
            .Select(c => c.Value)
            .ToList();
    }

    private string[] FilterLegitimatePermissions(IEnumerable<string> permissions)
    {
        // Get possible permissions using reflection.
        var type = typeof(IOperationConstants);
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => type.IsAssignableFrom(p)).ToArray();
        var possiblePermissions = types
            .SelectMany(t => t.GetFields().Where(f => f.IsPublic).Select(x => (string) x.GetValue(null)))
            .Union(types.SelectMany(t => t.GetProperties().Select(x => (string) x.GetValue(null))));

        return permissions.Intersect(possiblePermissions).ToArray();
    }
}