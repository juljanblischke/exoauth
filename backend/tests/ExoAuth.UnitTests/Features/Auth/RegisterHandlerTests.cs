using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Commands.Register;
using ExoAuth.Domain.Constants;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth;

public sealed class RegisterHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<ISystemUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IMfaService> _mockMfaService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ICaptchaService> _mockCaptchaService;
    private readonly RegisterHandler _handler;

    public RegisterHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _mockUserRepository = new Mock<ISystemUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockTokenService = new Mock<ITokenService>();
        _mockMfaService = new Mock<IMfaService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockCaptchaService = new Mock<ICaptchaService>();

        // Default token service setup
        _mockTokenService.Setup(x => x.RefreshTokenExpiration).Returns(TimeSpan.FromDays(30));

        // Default MFA service setup
        _mockMfaService.Setup(x => x.GenerateMfaToken(It.IsAny<Guid>(), It.IsAny<Guid?>()))
            .Returns("test-setup-token");

        // Default CAPTCHA service setup - always valid in tests
        _mockCaptchaService.Setup(x => x.ValidateRequiredAsync(
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new RegisterHandler(
            _mockContext.Object,
            _mockUserRepository.Object,
            _mockPasswordHasher.Object,
            _mockTokenService.Object,
            _mockMfaService.Object,
            _mockAuditService.Object,
            _mockCaptchaService.Object);
    }

    [Fact]
    public async Task Handle_FirstUser_CreatesUserWithAllPermissions()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "admin@example.com",
            Password: "Password123!",
            FirstName: "Admin",
            LastName: "User");

        var permissions = SystemPermissions.All.Select(p =>
            SystemPermission.Create(p.Name, p.Description, p.Category)).ToList();

        _mockUserRepository.Setup(x => x.AnyExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // No existing users
        _mockUserRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockPasswordHasher.Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed-password");
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<SystemUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemUser user, CancellationToken _) => user);

        // Setup permissions DbSet
        var permissionsDbSet = MockDbContext.CreateMockDbSet(permissions);
        _mockContext.Setup(x => x.SystemPermissions).Returns(permissionsDbSet.Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - First user requires MFA setup before getting access
        result.Should().NotBeNull();
        result.MfaSetupRequired.Should().BeTrue();
        result.SetupToken.Should().Be("test-setup-token");
        result.AccessToken.Should().BeNull(); // No tokens until MFA setup complete
        result.RefreshToken.Should().BeNull();
        result.User.Should().BeNull();

        // Verify all permissions were assigned
        _mockUserRepository.Verify(x => x.SetUserPermissionsAsync(
            It.IsAny<Guid>(),
            It.Is<List<Guid>>(ids => ids.Count == permissions.Count),
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify audit log for registration
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.UserRegistered,
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            "SystemUser",
            It.IsAny<Guid?>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify MFA setup required audit log
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.MfaSetupRequiredSent,
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            "SystemUser",
            It.IsAny<Guid?>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify MFA token was generated
        _mockMfaService.Verify(x => x.GenerateMfaToken(
            It.IsAny<Guid>(),
            null), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUsersExist_ThrowsRegistrationClosedException()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "user@example.com",
            Password: "Password123!",
            FirstName: "New",
            LastName: "User");

        _mockUserRepository.Setup(x => x.AnyExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Users already exist

        // Act & Assert
        await Assert.ThrowsAsync<RegistrationClosedException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        // Verify no user was created
        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<SystemUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmailExists_ThrowsEmailExistsException()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "existing@example.com",
            Password: "Password123!",
            FirstName: "New",
            LastName: "User");

        _mockUserRepository.Setup(x => x.AnyExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Email exists

        // Act & Assert
        await Assert.ThrowsAsync<EmailExistsException>(
            () => _handler.Handle(command, CancellationToken.None).AsTask());

        // Verify no user was created
        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<SystemUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_HashesPasswordBeforeSaving()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "admin@example.com",
            Password: "PlainTextPassword123!",
            FirstName: "Admin",
            LastName: "User");

        var permissions = SystemPermissions.All.Select(p =>
            SystemPermission.Create(p.Name, p.Description, p.Category)).ToList();

        _mockUserRepository.Setup(x => x.AnyExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockPasswordHasher.Setup(x => x.Hash("PlainTextPassword123!"))
            .Returns("hashed-password-123");
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<SystemUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemUser user, CancellationToken _) => user);

        var permissionsDbSet = MockDbContext.CreateMockDbSet(permissions);
        _mockContext.Setup(x => x.SystemPermissions).Returns(permissionsDbSet.Object);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockPasswordHasher.Verify(x => x.Hash("PlainTextPassword123!"), Times.Once);
        _mockUserRepository.Verify(x => x.AddAsync(
            It.Is<SystemUser>(u => u.PasswordHash == "hashed-password-123"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DoesNotCreateSessionOrTokensBeforeMfaSetup()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "admin@example.com",
            Password: "Password123!",
            FirstName: "Admin",
            LastName: "User");

        var permissions = SystemPermissions.All.Select(p =>
            SystemPermission.Create(p.Name, p.Description, p.Category)).ToList();

        _mockUserRepository.Setup(x => x.AnyExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockPasswordHasher.Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed-password");
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<SystemUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemUser user, CancellationToken _) => user);

        var permissionsDbSet = MockDbContext.CreateMockDbSet(permissions);
        _mockContext.Setup(x => x.SystemPermissions).Returns(permissionsDbSet.Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - No tokens or session until MFA setup completes
        result.MfaSetupRequired.Should().BeTrue();
        _mockTokenService.Verify(x => x.GenerateAccessToken(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<UserType>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<Guid?>()), Times.Never);
        _mockContext.Verify(x => x.RefreshTokens.AddAsync(
            It.IsAny<RefreshToken>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmailIsNormalizedToLowercase()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "ADMIN@EXAMPLE.COM",
            Password: "Password123!",
            FirstName: "Admin",
            LastName: "User");

        var permissions = SystemPermissions.All.Select(p =>
            SystemPermission.Create(p.Name, p.Description, p.Category)).ToList();

        _mockUserRepository.Setup(x => x.AnyExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockPasswordHasher.Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed-password");
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<SystemUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemUser user, CancellationToken _) => user);

        var permissionsDbSet = MockDbContext.CreateMockDbSet(permissions);
        _mockContext.Setup(x => x.SystemPermissions).Returns(permissionsDbSet.Object);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - User is created with lowercase email
        _mockUserRepository.Verify(x => x.AddAsync(
            It.Is<SystemUser>(u => u.Email == "admin@example.com"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
