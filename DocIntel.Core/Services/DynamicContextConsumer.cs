using System;
using System.Linq;
using System.Security.Claims;
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
    protected readonly IServiceProvider _serviceProvider;
    private readonly AppUserClaimsPrincipalFactory _userClaimsPrincipalFactory;
    private readonly ILogger<DynamicContextConsumer> _logger;

    public DynamicContextConsumer(ApplicationSettings appSettings,
        IServiceProvider serviceProvider,
        AppUserClaimsPrincipalFactory userClaimsPrincipalFactory, UserManager<AppUser> userManager)
    {
        _appSettings = appSettings;
        _serviceProvider = serviceProvider;
        _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
        _logger = serviceProvider.GetRequiredService<ILogger<DynamicContextConsumer>>();
    }


    protected async Task<AmbientContext> GetAmbientContext()
    {
        // TODO Refactor. Split up due to a weird bug with DbContext multi-threading
        DocIntelContext dbContext = null;
        try
        {
            var dbContextOptions = _serviceProvider.GetRequiredService<DbContextOptions<DocIntelContext>>();
            var dbContextLogger = _serviceProvider.GetRequiredService<ILogger<DocIntelContext>>();
            var userManager = _serviceProvider.GetRequiredService<AppUserManager>();
            dbContext = new DocIntelContext(dbContextOptions, dbContextLogger);
            var automationUser = await userManager.FindByNameAsync(_appSettings.AutomationAccount);
            
            if (automationUser == null)
                throw new ArgumentNullException($"User '{_appSettings.AutomationAccount}' was not found.");
        
            var claims = await _userClaimsPrincipalFactory.CreateAsync(automationUser);
            
            if (dbContext != null && claims != null && automationUser != null)
            {
                var ambientContext = new AmbientContext
                {
                    DatabaseContext = dbContext,
                    Claims = claims,
                    CurrentUser = automationUser
                };
                return ambientContext;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }
        return null;
    }
}