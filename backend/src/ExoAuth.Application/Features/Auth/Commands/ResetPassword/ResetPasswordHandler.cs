using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Domain.Entities;
using Mediator;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Application.Features.Auth.Commands.ResetPassword;

public sealed class ResetPasswordHandler : ICommandHandler<ResetPasswordCommand, ResetPasswordResponse>
{
    private readonly IPasswordResetService _passwordResetService;
    private readonly ISystemUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;
    private readonly IForceReauthService _forceReauthService;
    private readonly ILogger<ResetPasswordHandler> _logger;

    public ResetPasswordHandler(
        IPasswordResetService passwordResetService,
        ISystemUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        IAuditService auditService,
        IForceReauthService forceReauthService,
        ILogger<ResetPasswordHandler> logger)
    {
        _passwordResetService = passwordResetService;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _auditService = auditService;
        _forceReauthService = forceReauthService;
        _logger = logger;
    }

    public async ValueTask<ResetPasswordResponse> Handle(ResetPasswordCommand command, CancellationToken ct)
    {
        PasswordResetToken? resetToken;

        // Validate token or code
        if (!string.IsNullOrEmpty(command.Token))
        {
            resetToken = await _passwordResetService.ValidateTokenAsync(command.Token, ct);
        }
        else if (!string.IsNullOrEmpty(command.Email) && !string.IsNullOrEmpty(command.Code))
        {
            resetToken = await _passwordResetService.ValidateCodeAsync(command.Email, command.Code, ct);
        }
        else
        {
            throw new PasswordResetTokenInvalidException();
        }

        if (resetToken is null)
        {
            throw new PasswordResetTokenInvalidException();
        }

        // Get the user
        var user = await _userRepository.GetByIdAsync(resetToken.UserId, ct);

        if (user is null || !user.IsActive || user.IsLocked)
        {
            throw new PasswordResetTokenInvalidException();
        }

        // Update password
        var newPasswordHash = _passwordHasher.Hash(command.NewPassword);
        user.SetPasswordHash(newPasswordHash);
        await _userRepository.UpdateAsync(user, ct);

        // Mark token as used
        await _passwordResetService.MarkAsUsedAsync(resetToken, ct);

        // Force re-auth: Set flag for ALL sessions of this user (session-based reauth)
        await _forceReauthService.SetFlagForAllSessionsAsync(user.Id, ct);

        // Send confirmation email
        await _emailService.SendPasswordChangedAsync(
            email: user.Email,
            firstName: user.FirstName,
            userId: user.Id,
            language: user.PreferredLanguage,
            cancellationToken: ct
        );

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.PasswordResetCompleted,
            user.Id,
            null,
            "SystemUser",
            user.Id,
            null,
            ct
        );

        _logger.LogInformation("Password reset completed for user {UserId}", user.Id);

        return new ResetPasswordResponse(true, "Password has been reset successfully. Please login with your new password.");
    }
}
