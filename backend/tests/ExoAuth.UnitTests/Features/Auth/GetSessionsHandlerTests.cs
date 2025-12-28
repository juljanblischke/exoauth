using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Queries.GetSessions;
using ExoAuth.Domain.Entities;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.Auth;

public sealed class GetSessionsHandlerTests
{
    private readonly Mock<IDeviceSessionService> _mockSessionService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly GetSessionsHandler _handler;

    public GetSessionsHandlerTests()
    {
        _mockSessionService = new Mock<IDeviceSessionService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        _handler = new GetSessionsHandler(
            _mockSessionService.Object,
            _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task Handle_WithValidUser_ReturnsSessions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentSessionId = Guid.NewGuid();
        var otherSessionId = Guid.NewGuid();

        var sessions = new List<DeviceSession>
        {
            TestDataFactory.CreateDeviceSession(userId, "device-1"),
            TestDataFactory.CreateDeviceSession(userId, "device-2")
        };

        // Set IDs via reflection
        SetSessionId(sessions[0], currentSessionId);
        SetSessionId(sessions[1], otherSessionId);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.SessionId).Returns(currentSessionId);
        _mockSessionService.Setup(x => x.GetActiveSessionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        var query = new GetSessionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First(s => s.Id == currentSessionId).IsCurrent.Should().BeTrue();
        result.First(s => s.Id == otherSessionId).IsCurrent.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithNoSessions_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.SessionId).Returns((Guid?)null);
        _mockSessionService.Setup(x => x.GetActiveSessionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeviceSession>());

        var query = new GetSessionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithNoUserId_ThrowsUnauthorizedException()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.UserId).Returns((Guid?)null);

        var query = new GetSessionsQuery();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => _handler.Handle(query, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Handle_MapsSessionPropertiesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var session = TestDataFactory.CreateDeviceSession(userId, "test-device", "My Laptop", "Chrome", "Windows");
        SetSessionId(session, sessionId);

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.SessionId).Returns(sessionId);
        _mockSessionService.Setup(x => x.GetActiveSessionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeviceSession> { session });

        var query = new GetSessionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var dto = result.First();
        dto.Id.Should().Be(sessionId);
        dto.DeviceId.Should().Be("test-device");
        dto.Browser.Should().Be("Chrome");
        dto.OperatingSystem.Should().Be("Windows");
        dto.IsCurrent.Should().BeTrue();
    }

    private static void SetSessionId(DeviceSession session, Guid id)
    {
        var property = typeof(DeviceSession).GetProperty("Id");
        var backingField = typeof(DeviceSession).BaseType?.GetField("<Id>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        backingField?.SetValue(session, id);
    }
}
