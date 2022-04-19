using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocIntel.Core.Authorization.Operations;
using DocIntel.Core.Helpers;
using DocIntel.Core.Models;
using DocIntel.Core.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RunMethodsSequentially;

namespace DocIntel.Core.Services;

public class BaseDataDbService : IStartupServiceToRunSequentially
{
    public int OrderNum { get; }
 
    public async ValueTask ApplyYourChangeAsync(
        IServiceProvider scopedServices)
    {
        var logger = scopedServices
            .GetRequiredService<ILogger<BaseDataDbService>>();
        var settings = scopedServices
            .GetRequiredService<ApplicationSettings>();
        var userManager = scopedServices
            .GetRequiredService<UserManager<AppUser>>();
            
        var context = scopedServices.GetRequiredService<DocIntelContext>();

        AppUser automation = null;
        if (await context.Users.CountAsync() == 0)
        {
            // Create automation account
            if (string.IsNullOrEmpty(settings.AutomationAccount))
                throw new ArgumentException("Automation account in configuration is invalid.");

            var user = new AppUser
            {
                UserName = settings.AutomationAccount,
                RegistrationDate = DateTime.UtcNow,
                Bot = true
            };
            var password = UserHelper.GenerateRandomPassword();
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidProgramException("Could not create automation account");
            }

            automation = user;
        }
        
        // Create administrator role
        var role = await context.Roles.SingleOrDefaultAsync();
        if (role == null)
        {
            // Get all permissions
            var type = typeof(IOperationConstants);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p)).ToArray();
            var permissions = new HashSet<string>();
            foreach (var i in types.SelectMany(t => t.GetFields().Where(f => f.IsPublic).Select(x => (string) x.GetValue(null)))) 
                permissions.Add(i);
            foreach (var i in types.SelectMany(t => t.GetProperties().Select(x => (string) x.GetValue(null))))
                permissions.Add(i);
                
            role = new AppRole
            {
                Name = "Administrator", 
                NormalizedName = "ADMINISTRATOR",
                PermissionList = string.Join(",", permissions)
            };
            context.Add(role);
            
            // Assign the automation account to the 'Administrator' role
            if (automation == null)
                automation = context.Users.Single(_ => _.UserName == settings.AutomationAccount);
            context.Add(new AppUserRole {User = automation, Role = role});
        }
            
        if (await context.Classifications.CountAsync() == 0)
        {
            var classification = context.Classifications.Add(new Classification()
            {
                Title = "Unclassified",
                Abbreviation = "U",
                Default = true
            });

            if (logger != null) logger.LogDebug($"Adding default classification '{classification.Entity.ClassificationId}' to database.");
        }

        await context.SaveChangesAsync();
    }
}