using DocIntel.Core.Authentication;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace DocIntel.Core.Tests.Authentication;

[TestFixture]
public class AppSignInManagerTests
{
    [Test]
    public async Task PasswordSignInAsync_DisabledUser_ReturnsNotAllowed()
    {
        // Arrange
        var user = new AppUser { Enabled = false };
        var signInManager = CreateAppSignInManager();

        // Act
        var result = await signInManager.PasswordSignInAsync(user, "password", true, true);

        // Assert
        Assert.AreEqual(SignInResult.NotAllowed, result);
    }

    private static AppSignInManager CreateAppSignInManager()
    {
        var storeMock = Mock.Of<IUserStore<AppUser>>();
        var optionsAccessorMock1 = Mock.Of<IOptions<IdentityOptions>>();
        var passwordHasherMock = Mock.Of<IPasswordHasher<AppUser>>();
        var userValidatorsMock = new List<IUserValidator<AppUser>>();
        var passwordValidatorsMock = new List<IPasswordValidator<AppUser>>();
        var keyNormalizerMock = Mock.Of<ILookupNormalizer>();
        var errorsMock = Mock.Of<IdentityErrorDescriber>();
        var servicesMock = Mock.Of<IServiceProvider>();
        var loggerMock1 = Mock.Of<ILogger<UserManager<AppUser>>>();

        var userManager = new Mock<UserManager<AppUser>>(storeMock,
            optionsAccessorMock1,
            passwordHasherMock,
            userValidatorsMock,
            passwordValidatorsMock,
            keyNormalizerMock,
            errorsMock,
            servicesMock,
            loggerMock1);
        var userManagerMock = userManager;
        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
        var optionsAccessorMock = new Mock<IOptions<IdentityOptions>>();
        var loggerMock = new Mock<ILogger<AppSignInManager>>();
        var schemesMock = new Mock<IAuthenticationSchemeProvider>();
        var confirmationMock = new Mock<IUserConfirmation<AppUser>>();
        
        var signInManager = new AppSignInManager(userManagerMock.Object,
            contextAccessorMock.Object,
            claimsFactoryMock.Object,
            optionsAccessorMock.Object,
            loggerMock.Object,
            schemesMock.Object,
            confirmationMock.Object);
        return signInManager;
    }

    [Test]
    public async Task PasswordSignInAsync_SuccessfulSignIn_LogsSuccess()
    {
        // Arrange
        var user = new AppUser { UserName = "testuser", Enabled = true };
        var storeMock = Mock.Of<IUserStore<AppUser>>();
        var optionsAccessorMock1 = Mock.Of<IOptions<IdentityOptions>>();
        var passwordHasherMock = Mock.Of<IPasswordHasher<AppUser>>();
        var userValidatorsMock = new List<IUserValidator<AppUser>>();
        var passwordValidatorsMock = new List<IPasswordValidator<AppUser>>();
        var keyNormalizerMock = Mock.Of<ILookupNormalizer>();
        var errorsMock = Mock.Of<IdentityErrorDescriber>();
        var servicesMock = Mock.Of<IServiceProvider>();
        var loggerMock1 = Mock.Of<ILogger<UserManager<AppUser>>>();

        var userManager = new Mock<UserManager<AppUser>>(storeMock,
            optionsAccessorMock1,
            passwordHasherMock,
            userValidatorsMock,
            passwordValidatorsMock,
            keyNormalizerMock,
            errorsMock,
            servicesMock,
            loggerMock1);
        var userManagerMock = userManager;
        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
        var optionsAccessorMock = new Mock<IOptions<IdentityOptions>>();
        var loggerMock = new Mock<ILogger<AppSignInManager>>();
        var schemesMock = new Mock<IAuthenticationSchemeProvider>();
        var confirmationMock = new Mock<IUserConfirmation<AppUser>>();

        var signInManagerMock = new Mock<AppSignInManager>(
            userManagerMock.Object,
            contextAccessorMock.Object,
            claimsFactoryMock.Object,
            optionsAccessorMock.Object,
            loggerMock.Object,
            schemesMock.Object,
            confirmationMock.Object) { CallBase = true };
        
        userManagerMock
            .Setup(m => m.CheckPasswordAsync(user, "password"))
            .ReturnsAsync(true);

        signInManagerMock
            .Setup(_ => _.CheckPasswordSignInAsync(user, "password", It.IsAny<bool>()))
            .ReturnsAsync(SignInResult.Success);
        
        signInManagerMock
            .Protected().As<IInterface>()
            .Setup(t => t.SignInOrTwoFactorAsync(user, It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(SignInResult.Success);
        
        // Act
        var result = await signInManagerMock.Object.PasswordSignInAsync(user, "password", true, true);

        // Assert
        Assert.AreEqual(SignInResult.Success, result);
        loggerMock.Verify(m => m.Log(LogLevel.Information, EventIDs.UserLogOnSuccess, It.IsAny<LogEvent>(), null, LogEvent.Formatter), Times.Once);
    }

    interface IInterface
    {
        Task<SignInResult> SignInOrTwoFactorAsync(AppUser user, bool isPersistent, string? loginProvider = null,
            bool bypassTwoFactor = false);
    }

    [Test]
    public async Task PasswordSignInAsync_FailedSignIn_WrongPassword()
    {
        // Arrange
        var user = new AppUser { UserName = "testuser", Enabled = true };
        var storeMock = Mock.Of<IUserStore<AppUser>>();
        var optionsAccessorMock1 = Mock.Of<IOptions<IdentityOptions>>();
        var passwordHasherMock = Mock.Of<IPasswordHasher<AppUser>>();
        var userValidatorsMock = new List<IUserValidator<AppUser>>();
        var passwordValidatorsMock = new List<IPasswordValidator<AppUser>>();
        var keyNormalizerMock = Mock.Of<ILookupNormalizer>();
        var errorsMock = Mock.Of<IdentityErrorDescriber>();
        var servicesMock = Mock.Of<IServiceProvider>();
        var loggerMock1 = Mock.Of<ILogger<UserManager<AppUser>>>();

        var userManager = new Mock<UserManager<AppUser>>(storeMock,
            optionsAccessorMock1,
            passwordHasherMock,
            userValidatorsMock,
            passwordValidatorsMock,
            keyNormalizerMock,
            errorsMock,
            servicesMock,
            loggerMock1);
        var userManagerMock = userManager;
        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
        var optionsAccessorMock = new Mock<IOptions<IdentityOptions>>();
        var loggerMock = new Mock<ILogger<AppSignInManager>>();
        var schemesMock = new Mock<IAuthenticationSchemeProvider>();
        var confirmationMock = new Mock<IUserConfirmation<AppUser>>();

        var signInManagerMock = new Mock<AppSignInManager>(
            userManagerMock.Object,
            contextAccessorMock.Object,
            claimsFactoryMock.Object,
            optionsAccessorMock.Object,
            loggerMock.Object,
            schemesMock.Object,
            confirmationMock.Object) { CallBase = true };
        
        userManagerMock
            .Setup(m => m.CheckPasswordAsync(user, "password"))
            .ReturnsAsync(false);

        signInManagerMock
            .Setup(_ => _.CheckPasswordSignInAsync(user, "password", It.IsAny<bool>()))
            .ReturnsAsync(SignInResult.Failed);

        // Act
        var result = await signInManagerMock.Object.PasswordSignInAsync(user, "password", true, true);

        // Assert
        Assert.AreEqual(SignInResult.Failed, result);
        loggerMock.Verify(m => m.Log(LogLevel.Information, EventIDs.UserLogOnFailed, It.IsAny<LogEvent>(), null, LogEvent.Formatter), Times.Once);
    }
    
    [Test]
    public async Task PasswordSignInAsync_FailedSignIn_LogsFailure()
    {
        // Arrange
        var user = new AppUser { UserName = "testuser", Enabled = true };
        var storeMock = Mock.Of<IUserStore<AppUser>>();
        var optionsAccessorMock1 = Mock.Of<IOptions<IdentityOptions>>();
        var passwordHasherMock = Mock.Of<IPasswordHasher<AppUser>>();
        var userValidatorsMock = new List<IUserValidator<AppUser>>();
        var passwordValidatorsMock = new List<IPasswordValidator<AppUser>>();
        var keyNormalizerMock = Mock.Of<ILookupNormalizer>();
        var errorsMock = Mock.Of<IdentityErrorDescriber>();
        var servicesMock = Mock.Of<IServiceProvider>();
        var loggerMock1 = Mock.Of<ILogger<UserManager<AppUser>>>();

        var userManager = new Mock<UserManager<AppUser>>(storeMock,
            optionsAccessorMock1,
            passwordHasherMock,
            userValidatorsMock,
            passwordValidatorsMock,
            keyNormalizerMock,
            errorsMock,
            servicesMock,
            loggerMock1);
        var userManagerMock = userManager;
        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
        var optionsAccessorMock = new Mock<IOptions<IdentityOptions>>();
        var loggerMock = new Mock<ILogger<AppSignInManager>>();
        var schemesMock = new Mock<IAuthenticationSchemeProvider>();
        var confirmationMock = new Mock<IUserConfirmation<AppUser>>();

        var signInManagerMock = new Mock<AppSignInManager>(
            userManagerMock.Object,
            contextAccessorMock.Object,
            claimsFactoryMock.Object,
            optionsAccessorMock.Object,
            loggerMock.Object,
            schemesMock.Object,
            confirmationMock.Object) { CallBase = true };
        
        userManagerMock
            .Setup(m => m.CheckPasswordAsync(user, "password"))
            .ReturnsAsync(true);

        signInManagerMock
            .Setup(_ => _.CheckPasswordSignInAsync(user, "password", It.IsAny<bool>()))
            .ReturnsAsync(SignInResult.Failed);

        // Act
        var result = await signInManagerMock.Object.PasswordSignInAsync(user, "password", true, true);

        // Assert
        Assert.AreEqual(SignInResult.Failed, result);
        loggerMock.Verify(m => m.Log(LogLevel.Information, EventIDs.UserLogOnFailed, It.IsAny<LogEvent>(), null, LogEvent.Formatter), Times.Once);
    }

    [Test]
    public async Task PasswordSignInAsync_NotAllowedSignIn_LogsNotAllowed()
    {
        // Arrange
        var user = new AppUser { UserName = "testuser", Enabled = true };
        var storeMock = Mock.Of<IUserStore<AppUser>>();
        var optionsAccessorMock1 = Mock.Of<IOptions<IdentityOptions>>();
        var passwordHasherMock = Mock.Of<IPasswordHasher<AppUser>>();
        var userValidatorsMock = new List<IUserValidator<AppUser>>();
        var passwordValidatorsMock = new List<IPasswordValidator<AppUser>>();
        var keyNormalizerMock = Mock.Of<ILookupNormalizer>();
        var errorsMock = Mock.Of<IdentityErrorDescriber>();
        var servicesMock = Mock.Of<IServiceProvider>();
        var loggerMock1 = Mock.Of<ILogger<UserManager<AppUser>>>();

        var userManager = new Mock<UserManager<AppUser>>(storeMock,
            optionsAccessorMock1,
            passwordHasherMock,
            userValidatorsMock,
            passwordValidatorsMock,
            keyNormalizerMock,
            errorsMock,
            servicesMock,
            loggerMock1);
        var userManagerMock = userManager;
        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
        var optionsAccessorMock = new Mock<IOptions<IdentityOptions>>();
        var loggerMock = new Mock<ILogger<AppSignInManager>>();
        var schemesMock = new Mock<IAuthenticationSchemeProvider>();
        var confirmationMock = new Mock<IUserConfirmation<AppUser>>();

        var signInManagerMock = new Mock<AppSignInManager>(
            userManagerMock.Object,
            contextAccessorMock.Object,
            claimsFactoryMock.Object,
            optionsAccessorMock.Object,
            loggerMock.Object,
            schemesMock.Object,
            confirmationMock.Object) { CallBase = true };
        
        userManagerMock
            .Setup(m => m.CheckPasswordAsync(user, "password"))
            .ReturnsAsync(true);

        signInManagerMock
            .Setup(_ => _.CheckPasswordSignInAsync(user, "password", It.IsAny<bool>()))
            .ReturnsAsync(SignInResult.NotAllowed);

        // Act
        var result = await signInManagerMock.Object.PasswordSignInAsync(user, "password", true, true);

        // Assert
        Assert.AreEqual(SignInResult.NotAllowed, result);
        loggerMock.Verify(m => m.Log(LogLevel.Information, EventIDs.UserLogOnNotAllowed, It.IsAny<LogEvent>(), null, LogEvent.Formatter), Times.Once);
    }
}
