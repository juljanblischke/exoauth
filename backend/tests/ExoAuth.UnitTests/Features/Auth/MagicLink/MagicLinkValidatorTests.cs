using ExoAuth.Application.Features.Auth.Commands.LoginWithMagicLink;
using ExoAuth.Application.Features.Auth.Commands.RequestMagicLink;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace ExoAuth.UnitTests.Features.Auth.MagicLink;

public sealed class MagicLinkValidatorTests
{
    private readonly RequestMagicLinkValidator _requestValidator;
    private readonly LoginWithMagicLinkValidator _loginValidator;

    public MagicLinkValidatorTests()
    {
        _requestValidator = new RequestMagicLinkValidator();
        _loginValidator = new LoginWithMagicLinkValidator();
    }

    #region RequestMagicLinkValidator Tests

    [Fact]
    public void RequestMagicLinkValidator_WithValidEmail_PassesValidation()
    {
        // Arrange
        var command = new RequestMagicLinkCommand("test@example.com");

        // Act
        var result = _requestValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RequestMagicLinkValidator_WithEmptyEmail_FailsValidation()
    {
        // Arrange
        var command = new RequestMagicLinkCommand("");

        // Act
        var result = _requestValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is required");
    }

    [Fact]
    public void RequestMagicLinkValidator_WithNullEmail_FailsValidation()
    {
        // Arrange
        var command = new RequestMagicLinkCommand(null!);

        // Act
        var result = _requestValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void RequestMagicLinkValidator_WithInvalidEmailFormat_FailsValidation()
    {
        // Arrange
        var command = new RequestMagicLinkCommand("not-an-email");

        // Act
        var result = _requestValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Invalid email format");
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("user+tag@example.org")]
    public void RequestMagicLinkValidator_WithVariousValidEmails_PassesValidation(string email)
    {
        // Arrange
        var command = new RequestMagicLinkCommand(email);

        // Act
        var result = _requestValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("plaintext")]
    [InlineData("@nodomain")]
    public void RequestMagicLinkValidator_WithVariousInvalidEmails_FailsValidation(string email)
    {
        // Arrange
        var command = new RequestMagicLinkCommand(email);

        // Act
        var result = _requestValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    #endregion

    #region LoginWithMagicLinkValidator Tests

    [Fact]
    public void LoginWithMagicLinkValidator_WithValidToken_PassesValidation()
    {
        // Arrange
        var command = new LoginWithMagicLinkCommand("valid-token-value");

        // Act
        var result = _loginValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void LoginWithMagicLinkValidator_WithEmptyToken_FailsValidation()
    {
        // Arrange
        var command = new LoginWithMagicLinkCommand("");

        // Act
        var result = _loginValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Token)
            .WithErrorMessage("Magic link token is required");
    }

    [Fact]
    public void LoginWithMagicLinkValidator_WithNullToken_FailsValidation()
    {
        // Arrange
        var command = new LoginWithMagicLinkCommand(null!);

        // Act
        var result = _loginValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Fact]
    public void LoginWithMagicLinkValidator_WithWhitespaceToken_FailsValidation()
    {
        // Arrange
        var command = new LoginWithMagicLinkCommand("   ");

        // Act
        var result = _loginValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Fact]
    public void LoginWithMagicLinkValidator_AllowsOptionalDeviceId()
    {
        // Arrange
        var command = new LoginWithMagicLinkCommand("valid-token", DeviceId: "device-123");

        // Act
        var result = _loginValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void LoginWithMagicLinkValidator_AllowsOptionalDeviceFingerprint()
    {
        // Arrange
        var command = new LoginWithMagicLinkCommand("valid-token", DeviceFingerprint: "fingerprint-xyz");

        // Act
        var result = _loginValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void LoginWithMagicLinkValidator_AllowsOptionalUserAgent()
    {
        // Arrange
        var command = new LoginWithMagicLinkCommand("valid-token", UserAgent: "Mozilla/5.0");

        // Act
        var result = _loginValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void LoginWithMagicLinkValidator_AllowsRememberMeFlag()
    {
        // Arrange
        var command = new LoginWithMagicLinkCommand("valid-token", RememberMe: true);

        // Act
        var result = _loginValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
