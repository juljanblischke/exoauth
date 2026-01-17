using System.Net;
using System.Net.Http.Json;
using System.Text;
using ExoAuth.Application.Common.Models;
using ExoAuth.Infrastructure.Services.Captcha;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace ExoAuth.UnitTests.Services.Captcha;

public sealed class TurnstileProviderTests
{
    private readonly CaptchaSettings _settings;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<ILogger<TurnstileProvider>> _mockLogger;

    public TurnstileProviderTests()
    {
        _settings = new CaptchaSettings
        {
            Enabled = true,
            Provider = "Turnstile",
            Turnstile = new TurnstileSettings
            {
                SiteKey = "test-site-key",
                SecretKey = "test-secret-key"
            }
        };
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _mockLogger = new Mock<ILogger<TurnstileProvider>>();
    }

    private TurnstileProvider CreateProvider()
    {
        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://challenges.cloudflare.com")
        };
        var options = Options.Create(_settings);
        return new TurnstileProvider(httpClient, options, _mockLogger.Object);
    }

    private void SetupJsonResponse(HttpStatusCode statusCode, string jsonContent)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    [Fact]
    public void ProviderName_ReturnsTurnstile()
    {
        // Arrange
        var provider = CreateProvider();

        // Assert
        provider.ProviderName.Should().Be("turnstile");
    }

    [Fact]
    public async Task ValidateAsync_WithSuccessResponse_ReturnsSuccess()
    {
        // Arrange
        SetupJsonResponse(HttpStatusCode.OK, """
            {
                "success": true,
                "hostname": "example.com",
                "challenge_ts": "2024-01-01T00:00:00Z"
            }
            """);
        var provider = CreateProvider();

        // Act
        var result = await provider.ValidateAsync("valid-token", "127.0.0.1");

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorCode.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_WithFailureResponse_ReturnsFailure()
    {
        // Arrange
        SetupJsonResponse(HttpStatusCode.OK, """
            {
                "success": false,
                "error-codes": ["invalid-input-response"]
            }
            """);
        var provider = CreateProvider();

        // Act
        var result = await provider.ValidateAsync("invalid-token", "127.0.0.1");

        // Assert
        result.Success.Should().BeFalse();
        // Error is mapped from "invalid-input-response" to "invalid-token"
        result.ErrorCode.Should().Be("invalid-token");
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyToken_ReturnsFailure()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var result = await provider.ValidateAsync("", "127.0.0.1");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("missing-input-response");
    }

    [Fact]
    public async Task ValidateAsync_WithNullToken_ReturnsFailure()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var result = await provider.ValidateAsync(null!, "127.0.0.1");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("missing-input-response");
    }

    [Fact]
    public async Task ValidateAsync_WhenHttpRequestFails_ReturnsFailure()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));
        var provider = CreateProvider();

        // Act
        var result = await provider.ValidateAsync("valid-token", "127.0.0.1");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("validation-error");
    }

    [Fact]
    public async Task ValidateAsync_WithTimeoutExpiredError_ReturnsExpiredMessage()
    {
        // Arrange
        SetupJsonResponse(HttpStatusCode.OK, """
            {
                "success": false,
                "error-codes": ["timeout-or-duplicate"]
            }
            """);
        var provider = CreateProvider();

        // Act
        var result = await provider.ValidateAsync("expired-token", "127.0.0.1");

        // Assert
        result.Success.Should().BeFalse();
        // Error is mapped from "timeout-or-duplicate" to "token-expired"
        result.ErrorCode.Should().Be("token-expired");
    }

    [Fact]
    public async Task ValidateAsync_IncludesSecretKeyInRequest()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"success": true}""", Encoding.UTF8, "application/json")
            });
        var provider = CreateProvider();

        // Act
        await provider.ValidateAsync("test-token", "127.0.0.1");

        // Assert
        capturedRequest.Should().NotBeNull();
        var content = await capturedRequest!.Content!.ReadAsStringAsync();
        content.Should().Contain("secret=test-secret-key");
    }

    [Fact]
    public async Task ValidateAsync_IncludesTokenInRequest()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"success": true}""", Encoding.UTF8, "application/json")
            });
        var provider = CreateProvider();

        // Act
        await provider.ValidateAsync("my-captcha-token", "127.0.0.1");

        // Assert
        capturedRequest.Should().NotBeNull();
        var content = await capturedRequest!.Content!.ReadAsStringAsync();
        content.Should().Contain("response=my-captcha-token");
    }

    [Fact]
    public async Task ValidateAsync_IncludesRemoteIpInRequest_WhenProvided()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"success": true}""", Encoding.UTF8, "application/json")
            });
        var provider = CreateProvider();

        // Act
        await provider.ValidateAsync("test-token", "192.168.1.100");

        // Assert
        capturedRequest.Should().NotBeNull();
        var content = await capturedRequest!.Content!.ReadAsStringAsync();
        content.Should().Contain("remoteip=192.168.1.100");
    }
}
