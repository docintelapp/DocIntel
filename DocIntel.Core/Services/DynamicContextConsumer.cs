using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DocIntel.Core.Authorization;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Settings;
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
        AppUserClaimsPrincipalFactory userClaimsPrincipalFactory)
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
            var dbContextOptions = ServiceProviderServiceExtensions.GetRequiredService<DbContextOptions<DocIntelContext>>(_serviceProvider);
            var dbContextLogger = ServiceProviderServiceExtensions.GetRequiredService<ILogger<DocIntelContext>>(_serviceProvider);
            dbContext = new DocIntelContext(dbContextOptions, dbContextLogger);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }

        ClaimsPrincipal claims = null;
        AppUser automationUser = null;
        try
        {
            var dbContextOptions = ServiceProviderServiceExtensions.GetRequiredService<DbContextOptions<DocIntelContext>>(_serviceProvider);
            var dbContextLogger = ServiceProviderServiceExtensions.GetRequiredService<ILogger<DocIntelContext>>(_serviceProvider);
            var dbContext2 = new DocIntelContext(dbContextOptions, dbContextLogger);
            automationUser = Queryable.FirstOrDefault(dbContext2.Users.AsNoTracking(), _ => _.UserName == _appSettings.AutomationAccount);
            
            if (automationUser == null)
                throw new ArgumentNullException($"User '{_appSettings.AutomationAccount}' was not found.");
            dbContext2.Dispose();
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }

        try
        {
            var dbContextOptions = ServiceProviderServiceExtensions.GetRequiredService<DbContextOptions<DocIntelContext>>(_serviceProvider);
            var dbContextLogger = ServiceProviderServiceExtensions.GetRequiredService<ILogger<DocIntelContext>>(_serviceProvider);
            var dbContext2 = new DocIntelContext(dbContextOptions, dbContextLogger);
            claims = await _userClaimsPrincipalFactory.CreateAsync(dbContext2, automationUser);
            dbContext2.Dispose();
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }
            
        try
        {
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