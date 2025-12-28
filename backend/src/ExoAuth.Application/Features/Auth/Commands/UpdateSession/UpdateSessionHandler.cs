using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.UpdateSession;

public sealed class UpdateSessionHandler : ICommandHandler<UpdateSessionCommand, DeviceSessionDto>
{
    private readonly IDeviceSessionService _sessionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public UpdateSessionHandler(
        IDeviceSessionService sessionService,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _sessionService = sessionService;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async ValueTask<DeviceSessionDto> Handle(UpdateSessionCommand command, CancellationToken ct)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException();

        var currentSessionId = _currentUserService.SessionId;

        // Get the session and verify ownership
        var session = await _sessionService.GetSessionByIdAsync(command.SessionId, ct);

        if (session is null || session.UserId != userId)
        {
            throw new SessionNotFoundException();
        }

        // Update name if provided
        if (command.Name is not null)
        {
            await _sessionService.SetSessionNameAsync(command.SessionId, command.Name, ct);

            await _auditService.LogWithContextAsync(
                AuditActions.SessionRenamed,
                userId,
                null,
                "DeviceSession",
                command.SessionId,
                new { Name = command.Name },
                ct
            );
        }

        // Update trust status if provided
        if (command.IsTrusted.HasValue)
        {
            await _sessionService.SetTrustStatusAsync(command.SessionId, command.IsTrusted.Value, ct);

            await _auditService.LogWithContextAsync(
                AuditActions.SessionTrusted,
                userId,
                null,
                "DeviceSession",
                command.SessionId,
                new { IsTrusted = command.IsTrusted.Value },
                ct
            );
        }

        // Reload session to get updated values
        session = await _sessionService.GetSessionByIdAsync(command.SessionId, ct);

        return new DeviceSessionDto(
            Id: session!.Id,
            DeviceId: session.DeviceId,
            DisplayName: session.DisplayName,
            DeviceName: session.DeviceName,
            Browser: session.Browser,
            BrowserVersion: session.BrowserVersion,
            OperatingSystem: session.OperatingSystem,
            OsVersion: session.OsVersion,
            DeviceType: session.DeviceType,
            IpAddress: session.IpAddress,
            Country: session.Country,
            CountryCode: session.CountryCode,
            City: session.City,
            LocationDisplay: session.LocationDisplay,
            IsTrusted: session.IsTrusted,
            IsCurrent: session.Id == currentSessionId,
            LastActivityAt: session.LastActivityAt,
            CreatedAt: session.CreatedAt
        );
    }
}
