using System.Security.Claims;
using DocIntel.Core.Authentication;
using DocIntel.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocIntel.Core.Tests.Authentication;

[TestFixture]
public class AppRoleManagerTests
{
    [Test]
    public async Task CreateAsync_SetsCreationAndModificationDate()
    {
        // Arrange
        var role = new AppRole();
        var manager = CreateAppRoleManager();

        // Act
        await manager.CreateAsync(role);

        // Assert
        Assert.IsTrue(role.CreationDate != default);
        Assert.AreEqual(role.CreationDate, role.ModificationDate);
    }

    [Test]
    public async Task UpdateAsync_SetsModificationDate()
    {
        // Arrange
        var role = new AppRole();
        var manager = CreateAppRoleManager(role);
        
        // Act
        await manager.UpdateAsync(role);

        // Assert
        Assert.IsTrue(role.ModificationDate != default);
    }

    [Test]
    public async Task SetPermissionAsync_AddsAndRemovesPermissions()
    {
        // Arrange
        var role = new AppRole();
        var permissions = new[] { "ViewDocument", "SearchDocument" };
        var existingPermissions = new[] { "SearchDocument", "RegisterDocument" };
        var manager = CreateAppRoleManager(role, existingPermissions);

        // Act
        var result = await manager.SetPermissionAsync(role, permissions);

        // Assert
        var retreivedPermissions = await manager.GetPermissionsAsync(role);
        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(permissions.Length, retreivedPermissions.Count);
        Assert.IsTrue(retreivedPermissions.All(p => permissions.Contains(p)));
        Assert.IsFalse(retreivedPermissions.Any(p => !permissions.Contains(p)));
    }

    [Test]
    public async Task GetPermissionsAsync_ReturnsValidPermissions()
    {
        // Arrange
        var role = new AppRole();
        var permissions = new[] { "ViewDocument", "SearchDocument" };
        var manager = CreateAppRoleManager(role, permissions);

        // Act
        var result = await manager.GetPermissionsAsync(role);

        // Assert
        Assert.AreEqual(permissions.Length, result.Count);
        Assert.IsTrue(result.All(p => permissions.Contains(p)));
        Assert.IsFalse(result.Any(p => !permissions.Contains(p)));
    }

    private AppRoleManager CreateAppRoleManager(AppRole existingRole = null, IEnumerable<string> existingPermissions = null)
    {
        var roleStoreMock = new Mock<IRoleClaimStore<AppRole>>();
        var roleValidatorsMock = new List<IRoleValidator<AppRole>>();
        var keyNormalizerMock = new Mock<ILookupNormalizer>();
        var errorsMock = new Mock<IdentityErrorDescriber>();
        var loggerMock = new Mock<ILogger<AppRoleManager>>();
        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        
        var manager = new AppRoleManager(
            roleStoreMock.Object,
            roleValidatorsMock,
            keyNormalizerMock.Object,
            errorsMock.Object,
            loggerMock.Object,
            contextAccessorMock.Object
        );

        if (existingRole != null && existingPermissions != null)
        {
            var claims = existingPermissions.Select(p => new Claim(DocIntelConstants.ClaimPermissionType, p)).ToList();
            roleStoreMock
                .Setup(_ => _.GetClaimsAsync(It.Is<AppRole>(role => role == existingRole), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IList<Claim>>(claims));

            roleStoreMock
                .Setup(_ => _.RemoveClaimAsync(
                        It.Is<AppRole>(role => role == existingRole),
                        It.Is<Claim>(_ =>
                            _.Type == DocIntelConstants.ClaimPermissionType
                            && existingPermissions.Contains(_.Value)
                        ),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Callback<AppRole, Claim, CancellationToken>((r, c, t) =>
                {
                    claims.RemoveAll(_ => _.Type == c.Type && _.Value == c.Value);
                })
                .Returns(Task.CompletedTask);
            
            roleStoreMock
                .Setup(_ => _.AddClaimAsync(
                        It.Is<AppRole>(role => role == existingRole),
                        It.Is<Claim>(_ =>
                            _.Type == DocIntelConstants.ClaimPermissionType
                        ),
                        It.IsAny<CancellationToken>()
                    )
                )
                .Callback<AppRole, Claim, CancellationToken>((r, c, t) =>
                {
                    claims.Add(c);
                })
                .Returns(Task.CompletedTask);

            roleStoreMock
                .Setup(_ => _.UpdateAsync(It.Is<AppRole>(role => role == existingRole), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(IdentityResult.Success));

        }

        return manager;
    }
}
