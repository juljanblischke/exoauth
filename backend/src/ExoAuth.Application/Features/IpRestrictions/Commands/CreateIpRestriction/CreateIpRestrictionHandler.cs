using System.Net;
using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.IpRestrictions.Models;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.IpRestrictions.Commands.CreateIpRestriction;

public sealed class CreateIpRestrictionHandler : ICommandHandler<CreateIpRestrictionCommand, IpRestrictionDto>
{
    private readonly IAppDbContext _dbContext;
    private readonly IIpRestrictionService _ipRestrictionService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuditService _auditService;

    public CreateIpRestrictionHandler(
        IAppDbContext dbContext,
        IIpRestrictionService ipRestrictionService,
        IDateTimeProvider dateTimeProvider,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _ipRestrictionService = ipRestrictionService;
        _dateTimeProvider = dateTimeProvider;
        _auditService = auditService;
    }

    public async ValueTask<IpRestrictionDto> Handle(CreateIpRestrictionCommand command, CancellationToken ct)
    {
        // Validate IP address or CIDR notation
        if (!IsValidIpOrCidr(command.IpAddress))
        {
            throw new IpRestrictionInvalidCidrException(command.IpAddress);
        }

        var now = _dateTimeProvider.UtcNow;

        // Check for existing active restriction with the same IP (regardless of type)
        var existing = await _dbContext.IpRestrictions
            .FirstOrDefaultAsync(x =>
                x.IpAddress == command.IpAddress &&
                (x.ExpiresAt == null || x.ExpiresAt > now),
                ct);

        if (existing != null)
        {
            throw new IpRestrictionAlreadyExistsException(command.IpAddress);
        }

        // Create the restriction
        var restriction = IpRestriction.CreateManual(
            command.IpAddress,
            command.Type,
            command.Reason,
            command.ExpiresAt,
            command.CurrentUserId);

        _dbContext.IpRestrictions.Add(restriction);
        await _dbContext.SaveChangesAsync(ct);

        // Invalidate cache
        await _ipRestrictionService.InvalidateCacheAsync(ct);

        // Audit log
        await _auditService.LogWithContextAsync(
            command.Type == IpRestrictionType.Whitelist
                ? AuditActions.IpWhitelisted
                : AuditActions.IpBlacklisted,
            userId: command.CurrentUserId,
            targetUserId: null,
            entityType: "IpRestriction",
            entityId: restriction.Id,
            details: new
            {
                restriction.IpAddress,
                Type = command.Type.ToString(),
                restriction.Reason,
                restriction.ExpiresAt
            },
            cancellationToken: ct);

        // Get created by user info
        var createdByUser = await _dbContext.SystemUsers
            .Where(x => x.Id == command.CurrentUserId)
            .Select(x => new { x.Email, x.FirstName, x.LastName })
            .FirstOrDefaultAsync(ct);

        return new IpRestrictionDto(
            restriction.Id,
            restriction.IpAddress,
            restriction.Type,
            restriction.Reason,
            restriction.Source,
            restriction.ExpiresAt,
            restriction.CreatedAt,
            restriction.CreatedByUserId,
            createdByUser?.Email,
            createdByUser != null ? $"{createdByUser.FirstName} {createdByUser.LastName}" : null);
    }

    private static bool IsValidIpOrCidr(string ipOrCidr)
    {
        // Check for CIDR notation
        if (ipOrCidr.Contains('/'))
        {
            var parts = ipOrCidr.Split('/');
            if (parts.Length != 2)
                return false;

            if (!IPAddress.TryParse(parts[0], out var ip))
                return false;

            if (!int.TryParse(parts[1], out var prefixLength))
                return false;

            // Validate prefix length based on IP version
            var maxPrefix = ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;
            return prefixLength >= 0 && prefixLength <= maxPrefix;
        }

        // Plain IP address
        return IPAddress.TryParse(ipOrCidr, out _);
    }
}
