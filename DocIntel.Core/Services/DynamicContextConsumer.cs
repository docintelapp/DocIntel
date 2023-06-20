using System;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Services;

public class DynamicContextConsumer
{
    protected readonly ApplicationSettings _appSettings;
    private readonly ILogger<DynamicContextConsumer> _logger;
    protected readonly IServiceProvider _serviceProvider;

    public DynamicContextConsumer(ApplicationSettings appSettings,
        IServiceProvider serviceProvider,
        AppUserClaimsPrincipalFactory userClaimsPrincipalFactory, UserManager<AppUser> userManager)
    {
        _appSettings = appSettings;
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<DynamicContextConsumer>>();
    }

    protected async Task<AmbientContext> GetAmbientContext(IServiceProvider scopeServiceProvider)
    {
        // TODO Refactor. Split up due to a weird bug with DbContext multi-threading
        DocIntelContext dbContext = null;
        try
        {
            var dbContextOptions = scopeServiceProvider.GetRequiredService<DbContextOptions<DocIntelContext>>();
            var dbContextLogger = scopeServiceProvider.GetRequiredService<ILogger<DocIntelContext>>();
            var userManager = scopeServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var userClaimsPrincipalFactory = scopeServiceProvider.GetRequiredService<AppUserClaimsPrincipalFactory>();
            
            dbContext = new DocIntelContext(dbContextOptions, dbContextLogger);
            var automationUser = await userManager.FindByNameAsync(_appSettings.AutomationAccount);
            
            if (automationUser == null)
                throw new ArgumentNullException($"User '{_appSettings.AutomationAccount}' was not found.");
        
            var claims = await userClaimsPrincipalFactory.CreateAsync(automationUser);
            
            var ambientContext = new AmbientContext
            {
                DatabaseContext = dbContext,
                Claims = claims,
                CurrentUser = automationUser
            };
            return ambientContext;
        }
        catch (Exception e)
        {
            _logger.LogError("ERROR in DynamicContextConsumer.GetAmbientContext: " + e.Message + "\n" + e.StackTrace);
        }
        return null;
    }
}