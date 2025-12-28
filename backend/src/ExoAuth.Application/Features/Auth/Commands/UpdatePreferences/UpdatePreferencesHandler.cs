using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Auth.Commands.UpdatePreferences;

public sealed class UpdatePreferencesHandler : ICommandHandler<UpdatePreferencesCommand, UpdatePreferencesResponse>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public UpdatePreferencesHandler(
        IAppDbContext context,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _context = context;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async ValueTask<UpdatePreferencesResponse> Handle(UpdatePreferencesCommand command, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException();

        var user = await _context.SystemUsers
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new UnauthorizedException();

        var oldLanguage = user.PreferredLanguage;
        user.SetPreferredLanguage(command.Language);

        await _context.SaveChangesAsync(ct);

        // Audit log
        await _auditService.LogAsync(
            AuditActions.PreferencesUpdated,
            userId,
            null,
            "SystemUser",
            userId,
            new { OldLanguage = oldLanguage, NewLanguage = command.Language },
            ct
        );

        return new UpdatePreferencesResponse(true, command.Language);
    }
}
