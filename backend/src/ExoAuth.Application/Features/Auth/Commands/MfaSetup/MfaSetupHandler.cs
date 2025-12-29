using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Auth.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ExoAuth.Application.Features.Auth.Commands.MfaSetup;

public sealed class MfaSetupHandler : ICommandHandler<MfaSetupCommand, MfaSetupResponse>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IMfaService _mfaService;
    private readonly IEncryptionService _encryptionService;
    private readonly IAuditService _auditService;
    private readonly string _issuer;

    public MfaSetupHandler(
        IAppDbContext context,
        ICurrentUserService currentUser,
        IMfaService mfaService,
        IEncryptionService encryptionService,
        IAuditService auditService,
        IConfiguration configuration)
    {
        _context = context;
        _currentUser = currentUser;
        _mfaService = mfaService;
        _encryptionService = encryptionService;
        _auditService = auditService;
        _issuer = configuration.GetValue("Mfa:Issuer", "ExoAuth")!;
    }

    public async ValueTask<MfaSetupResponse> Handle(MfaSetupCommand command, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException();

        var user = await _context.SystemUsers
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new UnauthorizedException();

        if (!user.IsActive)
        {
            throw new UserInactiveException();
        }

        if (user.IsLocked)
        {
            throw new AccountLockedException(user.LockedUntil);
        }

        if (user.MfaEnabled)
        {
            throw new MfaAlreadyEnabledException();
        }

        // Generate new secret
        var secret = _mfaService.GenerateSecret();

        // Encrypt and store temporarily (not enabled yet until confirmed)
        var encryptedSecret = _encryptionService.Encrypt(secret);
        user.SetMfaSecret(encryptedSecret);
        await _context.SaveChangesAsync(ct);

        // Generate QR code URI
        var qrCodeUri = _mfaService.GenerateQrCodeUri(user.Email, secret, _issuer);

        // Format manual entry key with spaces for readability
        var manualEntryKey = FormatManualEntryKey(secret);

        await _auditService.LogAsync(
            AuditActions.MfaSetupStarted,
            userId,
            null,
            "SystemUser",
            userId,
            null,
            ct
        );

        return new MfaSetupResponse(
            Secret: secret,
            QrCodeUri: qrCodeUri,
            ManualEntryKey: manualEntryKey
        );
    }

    private static string FormatManualEntryKey(string secret)
    {
        // Format: XXXX XXXX XXXX XXXX ... for easier manual entry
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < secret.Length; i++)
        {
            if (i > 0 && i % 4 == 0)
                result.Append(' ');
            result.Append(secret[i]);
        }
        return result.ToString();
    }
}
