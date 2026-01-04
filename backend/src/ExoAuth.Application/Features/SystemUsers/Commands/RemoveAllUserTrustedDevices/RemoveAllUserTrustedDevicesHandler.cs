using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.SystemUsers.Commands.RemoveAllUserTrustedDevices;

public sealed class RemoveAllUserTrustedDevicesHandler : ICommandHandler<RemoveAllUserTrustedDevicesCommand, int>
{
    private readonly ITrustedDeviceService _trustedDeviceService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppDbContext _context;
    private readonly IAuditService _auditService;

    public RemoveAllUserTrustedDevicesHandler(
        ITrustedDeviceService trustedDeviceService,
        ICurrentUserService currentUserService,
        IAppDbContext context,
        IAuditService auditService)
    {
        _trustedDeviceService = trustedDeviceService;
        _currentUserService = currentUserService;
        _context = context;
        _auditService = auditService;
    }

    public async ValueTask<int> Handle(RemoveAllUserTrustedDevicesCommand command, CancellationToken ct)
    {
        var adminUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException();

        // Verify the target user exists
        var userExists = await _context.SystemUsers.AnyAsync(u => u.Id == command.UserId, ct);
        if (!userExists)
        {
            throw new SystemUserNotFoundException(command.UserId);
        }

        var removedCount = await _trustedDeviceService.RemoveAllAsync(command.UserId, ct);

        if (removedCount > 0)
        {
            await _auditService.LogWithContextAsync(
                AuditActions.TrustedDevicesRemovedByAdmin,
                adminUserId,
                command.UserId,
                "TrustedDevice",
                null,
                new { RemovedCount = removedCount },
                ct
            );
        }

        return removedCount;
    }
}
