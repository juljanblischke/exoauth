using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Announcements.Commands.CreateEmailAnnouncement;
using ExoAuth.Application.Features.Email.Announcements.Commands.DeleteEmailAnnouncement;
using ExoAuth.Application.Features.Email.Announcements.Commands.SendEmailAnnouncement;
using ExoAuth.Application.Features.Email.Announcements.Commands.UpdateEmailAnnouncement;
using ExoAuth.Application.Features.Email.Announcements.Queries.GetEmailAnnouncement;
using ExoAuth.Application.Features.Email.Announcements.Queries.GetEmailAnnouncements;
using ExoAuth.Application.Features.Email.Announcements.Queries.PreviewEmailAnnouncement;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using ExoAuth.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExoAuth.UnitTests.Features.Email.Announcements;

public sealed class CreateEmailAnnouncementHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly CreateEmailAnnouncementHandler _handler;
    private readonly List<EmailAnnouncement> _announcements;
    private readonly List<SystemUser> _users;

    public CreateEmailAnnouncementHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _announcements = new List<EmailAnnouncement>();
        _users = new List<SystemUser>();

        _mockContext.Setup(x => x.EmailAnnouncements)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_announcements).Object);
        _mockContext.Setup(x => x.SystemUsers)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_users).Object);

        _handler = new CreateEmailAnnouncementHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_CreatesAnnouncementForAllUsers()
    {
        // Arrange
        var creatorId = Guid.NewGuid();
        var creator = TestDataFactory.CreateSystemUser("creator@example.com", firstName: "Admin", lastName: "User");
        TestDataFactory.SetEntityId(creator, creatorId);
        _users.Add(creator);

        _mockContext.Setup(x => x.SystemUsers)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_users).Object);

        var command = new CreateEmailAnnouncementCommand(
            Subject: "Test Announcement",
            HtmlBody: "<p>Hello everyone!</p>",
            PlainTextBody: "Hello everyone!",
            TargetType: EmailAnnouncementTarget.AllUsers,
            TargetPermission: null,
            TargetUserIds: null,
            CreatedByUserId: creatorId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Subject.Should().Be("Test Announcement");
        result.HtmlBody.Should().Be("<p>Hello everyone!</p>");
        result.PlainTextBody.Should().Be("Hello everyone!");
        result.TargetType.Should().Be(EmailAnnouncementTarget.AllUsers);
        result.Status.Should().Be(EmailAnnouncementStatus.Draft);
    }

    [Fact]
    public async Task Handle_CreatesAnnouncementByPermission()
    {
        // Arrange
        var creatorId = Guid.NewGuid();

        var command = new CreateEmailAnnouncementCommand(
            Subject: "Admin Announcement",
            HtmlBody: "<p>For admins only</p>",
            PlainTextBody: "For admins only",
            TargetType: EmailAnnouncementTarget.ByPermission,
            TargetPermission: "system:admin",
            TargetUserIds: null,
            CreatedByUserId: creatorId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TargetType.Should().Be(EmailAnnouncementTarget.ByPermission);
        result.TargetPermission.Should().Be("system:admin");
    }

    [Fact]
    public async Task Handle_CreatesAnnouncementForSelectedUsers()
    {
        // Arrange
        var creatorId = Guid.NewGuid();
        var selectedUserIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        var command = new CreateEmailAnnouncementCommand(
            Subject: "Selected Users Announcement",
            HtmlBody: "<p>For selected users</p>",
            PlainTextBody: "For selected users",
            TargetType: EmailAnnouncementTarget.SelectedUsers,
            TargetPermission: null,
            TargetUserIds: selectedUserIds,
            CreatedByUserId: creatorId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TargetType.Should().Be(EmailAnnouncementTarget.SelectedUsers);
        result.TargetUserIds.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_SavesChanges()
    {
        // Arrange
        var command = new CreateEmailAnnouncementCommand(
            Subject: "Test",
            HtmlBody: "<p>Test</p>",
            PlainTextBody: "Test",
            TargetType: EmailAnnouncementTarget.AllUsers,
            TargetPermission: null,
            TargetUserIds: null,
            CreatedByUserId: Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public sealed class DeleteEmailAnnouncementHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly DeleteEmailAnnouncementHandler _handler;
    private readonly List<EmailAnnouncement> _announcements;

    public DeleteEmailAnnouncementHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _announcements = new List<EmailAnnouncement>();

        _mockContext.Setup(x => x.EmailAnnouncements)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_announcements).Object);

        _handler = new DeleteEmailAnnouncementHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_ThrowsNotFoundException_WhenAnnouncementDoesNotExist()
    {
        // Arrange
        var command = new DeleteEmailAnnouncementCommand(Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EmailAnnouncementNotFoundException>();
    }

    [Fact]
    public async Task Handle_ThrowsAlreadySentException_WhenAnnouncementSent()
    {
        // Arrange
        var announcementId = Guid.NewGuid();
        var announcement = TestDataFactory.CreateEmailAnnouncementWithId(announcementId);
        announcement.StartSending(100); // Start sending to make it non-deletable
        _announcements.Add(announcement);

        _mockContext.Setup(x => x.EmailAnnouncements)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_announcements).Object);

        var command = new DeleteEmailAnnouncementCommand(announcementId);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EmailAnnouncementAlreadySentException>();
    }

    [Fact]
    public async Task Handle_DeletesDraftAnnouncement()
    {
        // Arrange
        var announcementId = Guid.NewGuid();
        var announcement = TestDataFactory.CreateEmailAnnouncementWithId(announcementId);
        _announcements.Add(announcement);

        _mockContext.Setup(x => x.EmailAnnouncements)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_announcements).Object);

        var command = new DeleteEmailAnnouncementCommand(announcementId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public sealed class UpdateEmailAnnouncementHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly UpdateEmailAnnouncementHandler _handler;
    private readonly List<EmailAnnouncement> _announcements;

    public UpdateEmailAnnouncementHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _announcements = new List<EmailAnnouncement>();

        _mockContext.Setup(x => x.EmailAnnouncements)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_announcements).Object);

        _handler = new UpdateEmailAnnouncementHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_ThrowsNotFoundException_WhenAnnouncementDoesNotExist()
    {
        // Arrange
        var command = new UpdateEmailAnnouncementCommand(
            Id: Guid.NewGuid(),
            Subject: "Updated",
            HtmlBody: "<p>Updated</p>",
            PlainTextBody: "Updated",
            TargetType: EmailAnnouncementTarget.AllUsers,
            TargetPermission: null,
            TargetUserIds: null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EmailAnnouncementNotFoundException>();
    }

    [Fact]
    public async Task Handle_ThrowsAlreadySentException_WhenAnnouncementSent()
    {
        // Arrange
        var announcementId = Guid.NewGuid();
        var announcement = TestDataFactory.CreateEmailAnnouncementWithId(announcementId);
        announcement.StartSending(100);
        _announcements.Add(announcement);

        _mockContext.Setup(x => x.EmailAnnouncements)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_announcements).Object);

        var command = new UpdateEmailAnnouncementCommand(
            Id: announcementId,
            Subject: "Updated",
            HtmlBody: "<p>Updated</p>",
            PlainTextBody: "Updated",
            TargetType: EmailAnnouncementTarget.AllUsers,
            TargetPermission: null,
            TargetUserIds: null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EmailAnnouncementAlreadySentException>();
    }

    [Fact]
    public async Task Handle_UpdatesDraftAnnouncement()
    {
        // Arrange
        var announcementId = Guid.NewGuid();
        var announcement = TestDataFactory.CreateEmailAnnouncementWithId(announcementId, "Original Subject");
        _announcements.Add(announcement);

        _mockContext.Setup(x => x.EmailAnnouncements)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_announcements).Object);

        var command = new UpdateEmailAnnouncementCommand(
            Id: announcementId,
            Subject: "Updated Subject",
            HtmlBody: "<p>Updated Body</p>",
            PlainTextBody: "Updated Body",
            TargetType: EmailAnnouncementTarget.AllUsers,
            TargetPermission: null,
            TargetUserIds: null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Subject.Should().Be("Updated Subject");
        result.HtmlBody.Should().Be("<p>Updated Body</p>");
        result.PlainTextBody.Should().Be("Updated Body");
    }
}

public sealed class SendEmailAnnouncementHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly Mock<IMessageBus> _mockMessageBus;
    private readonly Mock<ILogger<SendEmailAnnouncementHandler>> _mockLogger;
    private readonly SendEmailAnnouncementHandler _handler;
    private readonly List<EmailAnnouncement> _announcements;
    private readonly List<SystemUser> _users;
    private readonly List<EmailLog> _emailLogs;

    public SendEmailAnnouncementHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _mockMessageBus = new Mock<IMessageBus>();
        _mockLogger = new Mock<ILogger<SendEmailAnnouncementHandler>>();
        _announcements = new List<EmailAnnouncement>();
        _users = new List<SystemUser>();
        _emailLogs = new List<EmailLog>();

        _mockContext.Setup(x => x.EmailAnnouncements)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_announcements).Object);
        _mockContext.Setup(x => x.SystemUsers)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_users).Object);
        _mockContext.Setup(x => x.EmailLogs)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_emailLogs).Object);

        _handler = new SendEmailAnnouncementHandler(_mockContext.Object, _mockMessageBus.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ThrowsNotFoundException_WhenAnnouncementDoesNotExist()
    {
        // Arrange
        var command = new SendEmailAnnouncementCommand(Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EmailAnnouncementNotFoundException>();
    }

    [Fact]
    public async Task Handle_ThrowsAlreadySentException_WhenAlreadySending()
    {
        // Arrange
        var announcementId = Guid.NewGuid();
        var announcement = TestDataFactory.CreateEmailAnnouncementWithId(announcementId);
        announcement.StartSending(100);
        _announcements.Add(announcement);

        _mockContext.Setup(x => x.EmailAnnouncements)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_announcements).Object);

        var command = new SendEmailAnnouncementCommand(announcementId);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EmailAnnouncementAlreadySentException>();
    }

    [Fact]
    public async Task Handle_ThrowsNoRecipientsException_WhenNoRecipients()
    {
        // Arrange
        var announcementId = Guid.NewGuid();
        var announcement = TestDataFactory.CreateEmailAnnouncementWithId(announcementId);
        _announcements.Add(announcement);
        // No users in database

        _mockContext.Setup(x => x.EmailAnnouncements)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_announcements).Object);

        var command = new SendEmailAnnouncementCommand(announcementId);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EmailAnnouncementNoRecipientsException>();
    }

    [Fact]
    public async Task Handle_StartsSending_WhenRecipientsExist()
    {
        // Arrange
        var announcementId = Guid.NewGuid();
        var announcement = TestDataFactory.CreateEmailAnnouncementWithId(announcementId);
        _announcements.Add(announcement);

        // Add active users
        var user1 = TestDataFactory.CreateSystemUser("user1@example.com");
        TestDataFactory.SetEntityId(user1, Guid.NewGuid());
        var user2 = TestDataFactory.CreateSystemUser("user2@example.com");
        TestDataFactory.SetEntityId(user2, Guid.NewGuid());
        _users.AddRange(new[] { user1, user2 });

        _mockContext.Setup(x => x.EmailAnnouncements)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_announcements).Object);
        _mockContext.Setup(x => x.SystemUsers)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_users).Object);

        var command = new SendEmailAnnouncementCommand(announcementId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(EmailAnnouncementStatus.Sending);
        result.TotalRecipients.Should().Be(2);
    }
}

public sealed class PreviewEmailAnnouncementHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly PreviewEmailAnnouncementHandler _handler;
    private readonly List<SystemUser> _users;
    private readonly List<SystemUserPermission> _userPermissions;
    private readonly List<SystemPermission> _permissions;

    public PreviewEmailAnnouncementHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _users = new List<SystemUser>();
        _userPermissions = new List<SystemUserPermission>();
        _permissions = new List<SystemPermission>();

        _mockContext.Setup(x => x.SystemUsers)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_users).Object);
        _mockContext.Setup(x => x.SystemUserPermissions)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_userPermissions).Object);
        _mockContext.Setup(x => x.SystemPermissions)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_permissions).Object);

        _handler = new PreviewEmailAnnouncementHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_ReturnsPreview_WithEstimatedRecipients()
    {
        // Arrange
        var user1 = TestDataFactory.CreateSystemUser("user1@example.com");
        TestDataFactory.SetEntityId(user1, Guid.NewGuid());
        var user2 = TestDataFactory.CreateSystemUser("user2@example.com");
        TestDataFactory.SetEntityId(user2, Guid.NewGuid());
        _users.AddRange(new[] { user1, user2 });

        _mockContext.Setup(x => x.SystemUsers)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_users).Object);

        var query = new PreviewEmailAnnouncementQuery(
            Subject: "Test Preview",
            HtmlBody: "<p>Preview content</p>",
            PlainTextBody: "Preview content",
            TargetType: EmailAnnouncementTarget.AllUsers,
            TargetPermission: null,
            TargetUserIds: null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Subject.Should().Be("Test Preview");
        result.HtmlBody.Should().Be("<p>Preview content</p>");
        result.PlainTextBody.Should().Be("Preview content");
        result.EstimatedRecipients.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ReturnsZeroRecipients_WhenNoActiveUsers()
    {
        // Arrange
        var query = new PreviewEmailAnnouncementQuery(
            Subject: "Test",
            HtmlBody: "<p>Test</p>",
            PlainTextBody: "Test",
            TargetType: EmailAnnouncementTarget.AllUsers,
            TargetPermission: null,
            TargetUserIds: null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.EstimatedRecipients.Should().Be(0);
    }

    [Fact]
    public async Task Handle_CountsSelectedUsers()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var userId3 = Guid.NewGuid();

        var user1 = TestDataFactory.CreateSystemUser("user1@example.com");
        TestDataFactory.SetEntityId(user1, userId1);
        var user2 = TestDataFactory.CreateSystemUser("user2@example.com");
        TestDataFactory.SetEntityId(user2, userId2);
        _users.AddRange(new[] { user1, user2 });

        _mockContext.Setup(x => x.SystemUsers)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_users).Object);

        var query = new PreviewEmailAnnouncementQuery(
            Subject: "Test",
            HtmlBody: "<p>Test</p>",
            PlainTextBody: "Test",
            TargetType: EmailAnnouncementTarget.SelectedUsers,
            TargetPermission: null,
            TargetUserIds: new List<Guid> { userId1, userId2, userId3 }); // Third one doesn't exist

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.EstimatedRecipients.Should().Be(2); // Only 2 valid users
    }
}

public sealed class GetEmailAnnouncementsHandlerTests
{
    private readonly Mock<IAppDbContext> _mockContext;
    private readonly GetEmailAnnouncementsHandler _handler;
    private readonly List<EmailAnnouncement> _announcements;

    public GetEmailAnnouncementsHandlerTests()
    {
        _mockContext = MockDbContext.Create();
        _announcements = new List<EmailAnnouncement>();

        _mockContext.Setup(x => x.EmailAnnouncements)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_announcements).Object);

        _handler = new GetEmailAnnouncementsHandler(_mockContext.Object);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoAnnouncements()
    {
        // Arrange
        var query = new GetEmailAnnouncementsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsAnnouncements()
    {
        // Arrange
        var ann1 = TestDataFactory.CreateEmailAnnouncementWithId(Guid.NewGuid(), "First");
        var ann2 = TestDataFactory.CreateEmailAnnouncementWithId(Guid.NewGuid(), "Second");
        _announcements.AddRange(new[] { ann1, ann2 });

        _mockContext.Setup(x => x.EmailAnnouncements)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_announcements).Object);

        var query = new GetEmailAnnouncementsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_FiltersByStatus()
    {
        // Arrange
        var ann1 = TestDataFactory.CreateEmailAnnouncementWithId(Guid.NewGuid(), "Draft");
        var ann2 = TestDataFactory.CreateEmailAnnouncementWithId(Guid.NewGuid(), "Sending");
        ann2.StartSending(100);
        _announcements.AddRange(new[] { ann1, ann2 });

        _mockContext.Setup(x => x.EmailAnnouncements)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_announcements).Object);

        var query = new GetEmailAnnouncementsQuery(Status: EmailAnnouncementStatus.Draft);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be(EmailAnnouncementStatus.Draft);
    }

    [Fact]
    public async Task Handle_SearchesBySubject()
    {
        // Arrange
        var ann1 = TestDataFactory.CreateEmailAnnouncementWithId(Guid.NewGuid(), "Important Update");
        var ann2 = TestDataFactory.CreateEmailAnnouncementWithId(Guid.NewGuid(), "Weekly Newsletter");
        _announcements.AddRange(new[] { ann1, ann2 });

        _mockContext.Setup(x => x.EmailAnnouncements)
            .Returns(MockDbContext.CreateAsyncMockDbSet(_announcements).Object);

        var query = new GetEmailAnnouncementsQuery(Search: "important");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Subject.Should().Be("Important Update");
    }
}
