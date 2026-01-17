using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Dlq.Commands.DeleteDlqEmail;
using ExoAuth.Application.Features.Email.Dlq.Commands.RetryDlqEmail;
using ExoAuth.Application.Features.Email.Dlq.Queries.GetDlqEmails;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Moq;

namespace ExoAuth.UnitTests.Features.Email.Dlq;

/// <summary>
/// Tests for GetDlqEmailsHandler.
/// Note: The full query tests with cursor pagination and projection are better suited
/// for integration tests. These unit tests focus on the handler's core behavior.
/// </summary>
public sealed class GetDlqEmailsHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly List<EmailLog> _logs;

    public GetDlqEmailsHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _logs = new List<EmailLog>();
    }

    [Fact]
    public void Constructor_WithValidContext_CreatesHandler()
    {
        // Arrange & Act
        var handler = new GetDlqEmailsHandler(_mockContext.Object);

        // Assert
        handler.Should().NotBeNull();
    }

    [Fact]
    public void Query_SupportsAllParameters()
    {
        // Arrange
        var query = new GetDlqEmailsQuery(
            Search: "password-reset",
            Cursor: "cursor",
            Limit: 50,
            Sort: "createdAt:desc");

        // Assert
        query.Search.Should().Be("password-reset");
        query.Cursor.Should().Be("cursor");
        query.Limit.Should().Be(50);
        query.Sort.Should().Be("createdAt:desc");
    }

    [Fact]
    public void Query_HasSensibleDefaults()
    {
        // Arrange & Act
        var query = new GetDlqEmailsQuery();

        // Assert
        query.Search.Should().BeNull();
        query.Cursor.Should().BeNull();
        query.Limit.Should().Be(20);
        query.Sort.Should().Be("movedToDlqAt:desc");
    }
}

public sealed class RetryDlqEmailHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IMessageBus> _mockMessageBus;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly RetryDlqEmailHandler _handler;
    private readonly List<EmailLog> _logs;

    public RetryDlqEmailHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _mockMessageBus = new Mock<IMessageBus>();
        _mockAuditService = new Mock<IAuditService>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _logs = new List<EmailLog>();

        _mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid());

        _mockContext.Setup(x => x.EmailLogs)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_logs).Object);

        _handler = new RetryDlqEmailHandler(
            _mockContext.Object,
            _mockMessageBus.Object,
            _mockAuditService.Object,
            _mockCurrentUser.Object);
    }

    [Fact]
    public async Task Handle_ThrowsNotFoundException_WhenLogDoesNotExist()
    {
        // Arrange
        var command = new RetryDlqEmailCommand(Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EmailLogNotFoundException>();
    }

    [Fact]
    public async Task Handle_ThrowsNotInDlqException_WhenEmailNotInDlq()
    {
        // Arrange
        var logId = Guid.NewGuid();
        var log = TestDataFactory.CreateEmailLogWithId(logId, status: EmailStatus.Sent);
        _logs.Add(log);

        _mockContext.Setup(x => x.EmailLogs)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_logs).Object);

        var command = new RetryDlqEmailCommand(logId);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EmailNotInDlqException>();
    }

    [Fact]
    public async Task Handle_RequeuesEmail_WhenInDlq()
    {
        // Arrange
        var logId = Guid.NewGuid();
        var log = TestDataFactory.CreateEmailLogWithId(logId, status: EmailStatus.InDlq);
        _logs.Add(log);

        _mockContext.Setup(x => x.EmailLogs)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_logs).Object);

        var command = new RetryDlqEmailCommand(logId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Requeue() sets status to Queued for re-processing
        result.Status.Should().Be(EmailStatus.Queued);
    }

    [Fact]
    public async Task Handle_SavesChanges()
    {
        // Arrange
        var logId = Guid.NewGuid();
        var log = TestDataFactory.CreateEmailLogWithId(logId, status: EmailStatus.InDlq);
        _logs.Add(log);

        _mockContext.Setup(x => x.EmailLogs)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_logs).Object);

        var command = new RetryDlqEmailCommand(logId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CreatesAuditLog()
    {
        // Arrange
        var logId = Guid.NewGuid();
        var log = TestDataFactory.CreateEmailLogWithId(logId, status: EmailStatus.InDlq);
        _logs.Add(log);

        _mockContext.Setup(x => x.EmailLogs)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_logs).Object);

        var command = new RetryDlqEmailCommand(logId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.EmailDlqRetry,
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            "EmailLog",
            It.IsAny<Guid?>(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}

public sealed class DeleteDlqEmailHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly DeleteDlqEmailHandler _handler;
    private readonly List<EmailLog> _logs;

    public DeleteDlqEmailHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _mockAuditService = new Mock<IAuditService>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _logs = new List<EmailLog>();

        _mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid());

        _mockContext.Setup(x => x.EmailLogs)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_logs).Object);

        _handler = new DeleteDlqEmailHandler(
            _mockContext.Object,
            _mockAuditService.Object,
            _mockCurrentUser.Object);
    }

    [Fact]
    public async Task Handle_ThrowsNotFoundException_WhenLogDoesNotExist()
    {
        // Arrange
        var command = new DeleteDlqEmailCommand(Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EmailLogNotFoundException>();
    }

    [Fact]
    public async Task Handle_ThrowsNotInDlqException_WhenEmailNotInDlq()
    {
        // Arrange
        var logId = Guid.NewGuid();
        var log = TestDataFactory.CreateEmailLogWithId(logId, status: EmailStatus.Sent);
        _logs.Add(log);

        _mockContext.Setup(x => x.EmailLogs)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_logs).Object);

        var command = new DeleteDlqEmailCommand(logId);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EmailNotInDlqException>();
    }

    [Fact]
    public async Task Handle_MarksEmailAsFailed_WhenInDlq()
    {
        // Arrange
        var logId = Guid.NewGuid();
        var log = TestDataFactory.CreateEmailLogWithId(logId, status: EmailStatus.InDlq);
        _logs.Add(log);

        _mockContext.Setup(x => x.EmailLogs)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_logs).Object);

        var command = new DeleteDlqEmailCommand(logId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        log.Status.Should().Be(EmailStatus.Failed);
        log.LastError.Should().Contain("Manually removed from DLQ");
    }

    [Fact]
    public async Task Handle_SavesChanges()
    {
        // Arrange
        var logId = Guid.NewGuid();
        var log = TestDataFactory.CreateEmailLogWithId(logId, status: EmailStatus.InDlq);
        _logs.Add(log);

        _mockContext.Setup(x => x.EmailLogs)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_logs).Object);

        var command = new DeleteDlqEmailCommand(logId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CreatesAuditLog()
    {
        // Arrange
        var logId = Guid.NewGuid();
        var log = TestDataFactory.CreateEmailLogWithId(logId, status: EmailStatus.InDlq);
        _logs.Add(log);

        _mockContext.Setup(x => x.EmailLogs)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_logs).Object);

        var command = new DeleteDlqEmailCommand(logId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockAuditService.Verify(x => x.LogWithContextAsync(
            AuditActions.EmailDlqDeleted,
            It.IsAny<Guid?>(),
            It.IsAny<Guid?>(),
            "EmailLog",
            logId,
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}
