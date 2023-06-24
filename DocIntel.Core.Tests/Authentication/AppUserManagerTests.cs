using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq.Protected;

namespace DocIntel.Core.Tests.Authentication;

[TestFixture]
public class AppUserManagerTests
{
    
    [Test]
    public async Task ResetPassword_WithUserResetTokenAndNewPassword_Succeeds()
    {
        // Arrange
        var user = new AppUser();
        var resetToken = "reset-token";
        var newPassword = "new-password";
        
        var storeMock = new Mock<IUserStore<AppUser>>();
        var identityOptionsMock = new Mock<IOptions<IdentityOptions>>();
        var passwordHasherMock = new Mock<IPasswordHasher<AppUser>>();
        var userValidatorsMock = new List<IUserValidator<AppUser>>();
        var passwordValidatorsMock = new List<IPasswordValidator<AppUser>>();
        var keyNormalizerMock = new Mock<ILookupNormalizer>();
        var errorsMock = new Mock<IdentityErrorDescriber>();
        var servicesMock = new Mock<IServiceProvider>();
        var loggerMock = new Mock<ILogger<AppUserManager>>();

        var appUserManager = new Mock<AppUserManager>(
            storeMock.Object,
            identityOptionsMock.Object,
            passwordHasherMock.Object,
            userValidatorsMock,
            passwordValidatorsMock,
            keyNormalizerMock.Object,
            errorsMock.Object,
            servicesMock.Object,
            loggerMock.Object) { CallBase = true };
        
        appUserManager
            .Setup(m => m.GenerateUserTokenAsync(user, It.IsAny<string>(), UserManager<AppUser>.ResetPasswordTokenPurpose))
            .ReturnsAsync(resetToken);

        appUserManager
            .Setup(m => m.VerifyUserTokenAsync(user, It.IsAny<string>(), UserManager<AppUser>.ResetPasswordTokenPurpose,
                resetToken))
            .ReturnsAsync(true);
                
        appUserManager
            .Protected()
            .As<ITestUserManager>()
            .Setup(m => m.UpdatePasswordHash(user, newPassword, It.IsAny<bool>()))
            .ReturnsAsync(IdentityResult.Success);
                
        appUserManager
            .Protected()
            .As<ITestUserManager>()
            .Setup(m => m.UpdateUserAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await appUserManager.Object.ResetPassword(
            Mock.Of<ClaimsPrincipal>(),
            user,
            resetToken,
            newPassword
        );

        // Assert
        Assert.IsTrue(result);
    }

    [Test]
    public async Task ResetPassword_WithUserResetTokenAndNewPassword_WrongResetToken()
    {
        // Arrange
        var user = new AppUser();
        var resetToken = "reset-token";
        var newPassword = "new-password";
        
        var storeMock = new Mock<IUserStore<AppUser>>();
        var identityOptionsMock = new Mock<IOptions<IdentityOptions>>();
        var passwordHasherMock = new Mock<IPasswordHasher<AppUser>>();
        var userValidatorsMock = new List<IUserValidator<AppUser>>();
        var passwordValidatorsMock = new List<IPasswordValidator<AppUser>>();
        var keyNormalizerMock = new Mock<ILookupNormalizer>();
        var errorsMock = new Mock<IdentityErrorDescriber>();
        var servicesMock = new Mock<IServiceProvider>();
        var loggerMock = new Mock<ILogger<AppUserManager>>();

        var appUserManager = new Mock<AppUserManager>(
            storeMock.Object,
            identityOptionsMock.Object,
            passwordHasherMock.Object,
            userValidatorsMock,
            passwordValidatorsMock,
            keyNormalizerMock.Object,
            errorsMock.Object,
            servicesMock.Object,
            loggerMock.Object) { CallBase = true };
        
        appUserManager
            .Setup(m => m.GenerateUserTokenAsync(user, It.IsAny<string>(), UserManager<AppUser>.ResetPasswordTokenPurpose))
            .ReturnsAsync(resetToken);

        appUserManager
            .Setup(m => m.VerifyUserTokenAsync(user, It.IsAny<string>(), UserManager<AppUser>.ResetPasswordTokenPurpose,
                It.IsAny<string>()))
            .ReturnsAsync(false);
                
        appUserManager
            .Protected()
            .As<ITestUserManager>()
            .Setup(m => m.UpdatePasswordHash(user, newPassword, It.IsAny<bool>()))
            .ReturnsAsync(IdentityResult.Success);
                
        appUserManager
            .Protected()
            .As<ITestUserManager>()
            .Setup(m => m.UpdateUserAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await appUserManager.Object.ResetPassword(
            Mock.Of<ClaimsPrincipal>(),
            user,
            "incorrect-token",
            newPassword
        );

        // Assert
        Assert.IsFalse(result);
    }

    [Test]
    public async Task ResetPassword_WithUserResetTokenAndNewPassword_WrongPassword()
    {
        // Arrange
        var user = new AppUser();
        var resetToken = "reset-token";
        var newPassword = "new-password";
        
        var storeMock = new Mock<IUserStore<AppUser>>();
        var identityOptionsMock = new Mock<IOptions<IdentityOptions>>();
        var passwordHasherMock = new Mock<IPasswordHasher<AppUser>>();
        var userValidatorsMock = new List<IUserValidator<AppUser>>();
        var passwordValidatorsMock = new List<IPasswordValidator<AppUser>>();
        var keyNormalizerMock = new Mock<ILookupNormalizer>();
        var errorsMock = new Mock<IdentityErrorDescriber>();
        var servicesMock = new Mock<IServiceProvider>();
        var loggerMock = new Mock<ILogger<AppUserManager>>();

        var appUserManager = new Mock<AppUserManager>(
            storeMock.Object,
            identityOptionsMock.Object,
            passwordHasherMock.Object,
            userValidatorsMock,
            passwordValidatorsMock,
            keyNormalizerMock.Object,
            errorsMock.Object,
            servicesMock.Object,
            loggerMock.Object) { CallBase = true };
        
        appUserManager
            .Setup(m => m.GenerateUserTokenAsync(user, It.IsAny<string>(), UserManager<AppUser>.ResetPasswordTokenPurpose))
            .ReturnsAsync(resetToken);

        appUserManager
            .Setup(m => m.VerifyUserTokenAsync(user, It.IsAny<string>(), UserManager<AppUser>.ResetPasswordTokenPurpose,
                It.IsAny<string>()))
            .ReturnsAsync(false);
                
        appUserManager
            .Protected()
            .As<ITestUserManager>()
            .Setup(m => m.UpdatePasswordHash(user, newPassword, It.IsAny<bool>()))
            .ReturnsAsync(IdentityResult.Failed());
                
        appUserManager
            .Protected()
            .As<ITestUserManager>()
            .Setup(m => m.UpdateUserAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await appUserManager.Object.ResetPassword(
            Mock.Of<ClaimsPrincipal>(),
            user,
            resetToken,
            newPassword
        );

        // Assert
        Assert.IsFalse(result);
    }

    [Test]
    public async Task ResetPassword_WithUserResetTokenAndNewPassword_CannotUpdateUser()
    {
        // Arrange
        var user = new AppUser();
        var resetToken = "reset-token";
        var newPassword = "new-password";
        
        var storeMock = new Mock<IUserStore<AppUser>>();
        var identityOptionsMock = new Mock<IOptions<IdentityOptions>>();
        var passwordHasherMock = new Mock<IPasswordHasher<AppUser>>();
        var userValidatorsMock = new List<IUserValidator<AppUser>>();
        var passwordValidatorsMock = new List<IPasswordValidator<AppUser>>();
        var keyNormalizerMock = new Mock<ILookupNormalizer>();
        var errorsMock = new Mock<IdentityErrorDescriber>();
        var servicesMock = new Mock<IServiceProvider>();
        var loggerMock = new Mock<ILogger<AppUserManager>>();

        var appUserManager = new Mock<AppUserManager>(
            storeMock.Object,
            identityOptionsMock.Object,
            passwordHasherMock.Object,
            userValidatorsMock,
            passwordValidatorsMock,
            keyNormalizerMock.Object,
            errorsMock.Object,
            servicesMock.Object,
            loggerMock.Object) { CallBase = true };
        
        appUserManager
            .Setup(m => m.GenerateUserTokenAsync(user, It.IsAny<string>(), UserManager<AppUser>.ResetPasswordTokenPurpose))
            .ReturnsAsync(resetToken);

        appUserManager
            .Setup(m => m.VerifyUserTokenAsync(user, It.IsAny<string>(), UserManager<AppUser>.ResetPasswordTokenPurpose,
                It.IsAny<string>()))
            .ReturnsAsync(false);
                
        appUserManager
            .Protected()
            .As<ITestUserManager>()
            .Setup(m => m.UpdatePasswordHash(user, newPassword, It.IsAny<bool>()))
            .ReturnsAsync(IdentityResult.Success);
                
        appUserManager
            .Protected()
            .As<ITestUserManager>()
            .Setup(m => m.UpdateUserAsync(user))
            .ReturnsAsync(IdentityResult.Failed());

        // Act
        var result = await appUserManager.Object.ResetPassword(
            Mock.Of<ClaimsPrincipal>(),
            user,
            resetToken,
            newPassword
        );

        // Assert
        Assert.IsFalse(result);
    }
    
    [Test]
    public async Task ResetPassword_WithUserAndNewPassword_Succeeds()
    {
        // Arrange
        var user = new AppUser();
        var resetToken = "reset-token";
        var newPassword = "new-password";
        
        var storeMock = new Mock<IUserStore<AppUser>>();
        var identityOptionsMock = new Mock<IOptions<IdentityOptions>>();
        var passwordHasherMock = new Mock<IPasswordHasher<AppUser>>();
        var userValidatorsMock = new List<IUserValidator<AppUser>>();
        var passwordValidatorsMock = new List<IPasswordValidator<AppUser>>();
        var keyNormalizerMock = new Mock<ILookupNormalizer>();
        var errorsMock = new Mock<IdentityErrorDescriber>();
        var servicesMock = new Mock<IServiceProvider>();
        var loggerMock = new Mock<ILogger<AppUserManager>>();

        var appUserManager = new Mock<AppUserManager>(
            storeMock.Object,
            identityOptionsMock.Object,
            passwordHasherMock.Object,
            userValidatorsMock,
            passwordValidatorsMock,
            keyNormalizerMock.Object,
            errorsMock.Object,
            servicesMock.Object,
            loggerMock.Object) { CallBase = true };
        
        appUserManager
            .Setup(m => m.GenerateUserTokenAsync(user, It.IsAny<string>(), UserManager<AppUser>.ResetPasswordTokenPurpose))
            .ReturnsAsync(resetToken);

        appUserManager
            .Setup(m => m.VerifyUserTokenAsync(user, It.IsAny<string>(), UserManager<AppUser>.ResetPasswordTokenPurpose,
                resetToken))
            .ReturnsAsync(true);
                
        appUserManager
            .Protected()
            .As<ITestUserManager>()
            .Setup(m => m.UpdatePasswordHash(user, newPassword, It.IsAny<bool>()))
            .ReturnsAsync(IdentityResult.Success);
                
        appUserManager
            .Protected()
            .As<ITestUserManager>()
            .Setup(m => m.UpdateUserAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await appUserManager.Object.ResetPassword(
            Mock.Of<ClaimsPrincipal>(),
            user,
            newPassword
        );

        // Assert
        Assert.IsTrue(result);
    }

    interface ITestUserManager
    {
        Task<IdentityResult> UpdatePasswordHash(AppUser user, string newPassword, bool validatePassword);
        Task<IdentityResult> UpdateUserAsync(AppUser user);
    }

    [Test]
    public async Task ResetPassword_WithUserAndNewPassword_WrongResetToken()
    {
        // Arrange
        var user = new AppUser();
        var resetToken = "reset-token";
        var newPassword = "new-password";
        
        var storeMock = new Mock<IUserStore<AppUser>>();
        var identityOptionsMock = new Mock<IOptions<IdentityOptions>>();
        var passwordHasherMock = new Mock<IPasswordHasher<AppUser>>();
        var userValidatorsMock = new List<IUserValidator<AppUser>>();
        var passwordValidatorsMock = new List<IPasswordValidator<AppUser>>();
        var keyNormalizerMock = new Mock<ILookupNormalizer>();
        var errorsMock = new Mock<IdentityErrorDescriber>();
        var servicesMock = new Mock<IServiceProvider>();
        var loggerMock = new Mock<ILogger<AppUserManager>>();

        var appUserManager = new Mock<AppUserManager>(
            storeMock.Object,
            identityOptionsMock.Object,
            passwordHasherMock.Object,
            userValidatorsMock,
            passwordValidatorsMock,
            keyNormalizerMock.Object,
            errorsMock.Object,
            servicesMock.Object,
            loggerMock.Object) { CallBase = true };
        
        appUserManager
            .Setup(m => m.GenerateUserTokenAsync(user, It.IsAny<string>(), UserManager<AppUser>.ResetPasswordTokenPurpose))
            .ReturnsAsync(resetToken);

        appUserManager
            .Setup(m => m.VerifyUserTokenAsync(user, It.IsAny<string>(), UserManager<AppUser>.ResetPasswordTokenPurpose,
                It.IsAny<string>()))
            .ReturnsAsync(false);
                
        appUserManager
            .Protected()
            .As<ITestUserManager>()
            .Setup(m => m.UpdatePasswordHash(user, newPassword, It.IsAny<bool>()))
            .ReturnsAsync(IdentityResult.Success);
                
        appUserManager
            .Protected()
            .As<ITestUserManager>()
            .Setup(m => m.UpdateUserAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await appUserManager.Object.ResetPassword(
            Mock.Of<ClaimsPrincipal>(),
            user,
            newPassword
        );

        // Assert
        Assert.IsFalse(result);
    }

    [Test]
    public async Task ResetPassword_WithUserAndNewPassword_WrongPassword()
    {
        // Arrange
        var user = new AppUser();
        var resetToken = "reset-token";
        var newPassword = "new-password";
        
        var storeMock = new Mock<IUserStore<AppUser>>();
        var identityOptionsMock = new Mock<IOptions<IdentityOptions>>();
        var passwordHasherMock = new Mock<IPasswordHasher<AppUser>>();
        var userValidatorsMock = new List<IUserValidator<AppUser>>();
        var passwordValidatorsMock = new List<IPasswordValidator<AppUser>>();
        var keyNormalizerMock = new Mock<ILookupNormalizer>();
        var errorsMock = new Mock<IdentityErrorDescriber>();
        var servicesMock = new Mock<IServiceProvider>();
        var loggerMock = new Mock<ILogger<AppUserManager>>();

        var appUserManager = new Mock<AppUserManager>(
            storeMock.Object,
            identityOptionsMock.Object,
            passwordHasherMock.Object,
            userValidatorsMock,
            passwordValidatorsMock,
            keyNormalizerMock.Object,
            errorsMock.Object,
            servicesMock.Object,
            loggerMock.Object) { CallBase = true };
        
        appUserManager
            .Setup(m => m.GenerateUserTokenAsync(user, It.IsAny<string>(), UserManager<AppUser>.ResetPasswordTokenPurpose))
            .ReturnsAsync(resetToken);

        appUserManager
            .Setup(m => m.VerifyUserTokenAsync(user, It.IsAny<string>(), UserManager<AppUser>.ResetPasswordTokenPurpose,
                It.IsAny<string>()))
            .ReturnsAsync(false);
                
        appUserManager
            .Protected()
            .As<ITestUserManager>()
            .Setup(m => m.UpdatePasswordHash(user, newPassword, It.IsAny<bool>()))
            .ReturnsAsync(IdentityResult.Failed());
                
        appUserManager
            .Protected()
            .As<ITestUserManager>()
            .Setup(m => m.UpdateUserAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await appUserManager.Object.ResetPassword(
            Mock.Of<ClaimsPrincipal>(),
            user,
            newPassword
        );

        // Assert
        Assert.IsFalse(result);
    }

    [Test]
    public async Task ResetPassword_WithUserAndNewPassword_CannotUpdateUser()
    {
        // Arrange
        var user = new AppUser();
        var resetToken = "reset-token";
        var newPassword = "new-password";
        
        var storeMock = new Mock<IUserStore<AppUser>>();
        var identityOptionsMock = new Mock<IOptions<IdentityOptions>>();
        var passwordHasherMock = new Mock<IPasswordHasher<AppUser>>();
        var userValidatorsMock = new List<IUserValidator<AppUser>>();
        var passwordValidatorsMock = new List<IPasswordValidator<AppUser>>();
        var keyNormalizerMock = new Mock<ILookupNormalizer>();
        var errorsMock = new Mock<IdentityErrorDescriber>();
        var servicesMock = new Mock<IServiceProvider>();
        var loggerMock = new Mock<ILogger<AppUserManager>>();

        var appUserManager = new Mock<AppUserManager>(
            storeMock.Object,
            identityOptionsMock.Object,
            passwordHasherMock.Object,
            userValidatorsMock,
            passwordValidatorsMock,
            keyNormalizerMock.Object,
            errorsMock.Object,
            servicesMock.Object,
            loggerMock.Object) { CallBase = true };
        
        appUserManager
            .Setup(m => m.GenerateUserTokenAsync(user, It.IsAny<string>(), UserManager<AppUser>.ResetPasswordTokenPurpose))
            .ReturnsAsync(resetToken);

        appUserManager
            .Setup(m => m.VerifyUserTokenAsync(user, It.IsAny<string>(), UserManager<AppUser>.ResetPasswordTokenPurpose,
                It.IsAny<string>()))
            .ReturnsAsync(false);
                
        appUserManager
            .Protected()
            .As<ITestUserManager>()
            .Setup(m => m.UpdatePasswordHash(user, newPassword, It.IsAny<bool>()))
            .ReturnsAsync(IdentityResult.Success);
                
        appUserManager
            .Protected()
            .As<ITestUserManager>()
            .Setup(m => m.UpdateUserAsync(user))
            .ReturnsAsync(IdentityResult.Failed());

        // Act
        var result = await appUserManager.Object.ResetPassword(
            Mock.Of<ClaimsPrincipal>(),
            user,
            newPassword
        );

        // Assert
        Assert.IsFalse(result);
    }

    [Test]
    public async Task CreateAsync_WithUserAndPassword_Succeeds()
    {
        // Arrange
        var user = new AppUser();
        string? password = "password";

        var passwordValidator = new Mock<IPasswordValidator<AppUser>>();
        passwordValidator
            .Setup(_ => _.ValidateAsync(It.IsAny<UserManager<AppUser>>(), user, It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var storeMock = new Mock<IUserPasswordStore<AppUser>>();
        var identityOptionsMock = new Mock<IOptions<IdentityOptions>>();
        var passwordHasherMock = new Mock<IPasswordHasher<AppUser>>();
        var userValidatorsMock = new List<IUserValidator<AppUser>>();
        var passwordValidatorsMock = new List<IPasswordValidator<AppUser>>() { passwordValidator.Object };
        var keyNormalizerMock = new Mock<ILookupNormalizer>();
        var errorsMock = new Mock<IdentityErrorDescriber>();
        var servicesMock = new Mock<IServiceProvider>();
        var loggerMock = new Mock<ILogger<AppUserManager>>();

        var appUserManager = new Mock<AppUserManager>(
            storeMock.Object,
            identityOptionsMock.Object,
            passwordHasherMock.Object,
            userValidatorsMock,
            passwordValidatorsMock,
            keyNormalizerMock.Object,
            errorsMock.Object,
            servicesMock.Object,
            loggerMock.Object) { CallBase = true };
        
        appUserManager
            .Setup(m => m.CreateAsync(user))
            .ReturnsAsync(IdentityResult.Success);
        
        // Act
        var result = await appUserManager.Object.CreateAsync(
            Mock.Of<ClaimsPrincipal>(),
            user,
            password
        );

        // Assert
        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(user.RegistrationDate != default);
    }

    [Test]
    public async Task CreateAsync_WithUserAndPassword_WrongPassword()
    {
        // Arrange
        var user = new AppUser();
        var password = "password";

        var passwordValidator = new Mock<IPasswordValidator<AppUser>>();
        passwordValidator
            .Setup(_ => _.ValidateAsync(It.IsAny<UserManager<AppUser>>(), user, It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed());
        
        var storeMock = new Mock<IUserPasswordStore<AppUser>>();
        var identityOptionsMock = new Mock<IOptions<IdentityOptions>>();
        var passwordHasherMock = new Mock<IPasswordHasher<AppUser>>();
        var userValidatorsMock = new List<IUserValidator<AppUser>>();
        var passwordValidatorsMock = new List<IPasswordValidator<AppUser>>() { passwordValidator.Object };
        var keyNormalizerMock = new Mock<ILookupNormalizer>();
        var errorsMock = new Mock<IdentityErrorDescriber>();
        var servicesMock = new Mock<IServiceProvider>();
        var loggerMock = new Mock<ILogger<AppUserManager>>();

        var appUserManager = new Mock<AppUserManager>(
            storeMock.Object,
            identityOptionsMock.Object,
            passwordHasherMock.Object,
            userValidatorsMock,
            passwordValidatorsMock,
            keyNormalizerMock.Object,
            errorsMock.Object,
            servicesMock.Object,
            loggerMock.Object) { CallBase = true };
        
        appUserManager
            .Setup(m => m.CreateAsync(user))
            .ReturnsAsync(IdentityResult.Success);
        
        // Act
        var result = await appUserManager.Object.CreateAsync(
            Mock.Of<ClaimsPrincipal>(),
            user,
            password
        );

        // Assert
        Assert.IsFalse(result.Succeeded);
    }

    [Test]
    public async Task GetClaimsAsync_WithUser_ReturnsClaims()
    {
        // Arrange
        var user = new AppUser();
        var claims = new List<Claim>
        {
            new Claim("claim1", "value1"),
            new Claim("claim2", "value2")
        };
        
        var storeMock = new Mock<IUserClaimStore<AppUser>>();
        var identityOptionsMock = new Mock<IOptions<IdentityOptions>>();
        var passwordHasherMock = new Mock<IPasswordHasher<AppUser>>();
        var userValidatorsMock = new List<IUserValidator<AppUser>>();
        var passwordValidatorsMock = new List<IPasswordValidator<AppUser>>();
        var keyNormalizerMock = new Mock<ILookupNormalizer>();
        var errorsMock = new Mock<IdentityErrorDescriber>();
        var servicesMock = new Mock<IServiceProvider>();
        var loggerMock = new Mock<ILogger<AppUserManager>>();

        var appUserManager = new Mock<AppUserManager>(
            storeMock.Object,
            identityOptionsMock.Object,
            passwordHasherMock.Object,
            userValidatorsMock,
            passwordValidatorsMock,
            keyNormalizerMock.Object,
            errorsMock.Object,
            servicesMock.Object,
            loggerMock.Object) { CallBase = true };

        storeMock
            .Setup(_ => _.GetClaimsAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(claims);

        // Act
        var result = await appUserManager.Object.GetClaimsAsync(user);

        // Assert
        CollectionAssert.AreEqual(claims, result);
    }

    private Mock<UserManager<AppUser>> CreateMockUserManager(AppUser user, string resetToken, bool resetPasswordSucceeded)
    {
        var userManagerMock = new Mock<UserManager<AppUser>>(
            Mock.Of<IUserStore<AppUser>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<AppUser>>(),
            Mock.Of<IEnumerable<IUserValidator<AppUser>>>(),
            Mock.Of<IEnumerable<IPasswordValidator<AppUser>>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<AppUser>>>()
        );
        userManagerMock.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync(resetToken);
        userManagerMock.Setup(m => m.ResetPasswordAsync(user, resetToken, It.IsAny<string>()))
            .ReturnsAsync(resetPasswordSucceeded ? IdentityResult.Success : IdentityResult.Failed());

        return userManagerMock;
    }
}
