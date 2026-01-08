using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Application.Features.Auth.Commands.ResendPasswordReset;

public sealed class ResendPasswordResetHandler : ICommandHandler<ResendPasswordResetCommand, ResendPasswordResetResponse>
{
    private const int ResendCooldownSeconds = 60; // 1 minute cooldown

    private readonly IAppDbContext _dbContext;
    private readonly ISystemUserRepository _userRepository;
    private readonly IPasswordResetService _passwordResetService;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;
    private readonly ICaptchaService _captchaService;
    private readonly ILogger<ResendPasswordResetHandler> _logger;

    public ResendPasswordResetHandler(
        IAppDbContext dbContext,
        ISystemUserRepository userRepository,
        IPasswordResetService passwordResetService,
        IEmailService emailService,
        IAuditService auditService,
        ICaptchaService captchaService,
        ILogger<ResendPasswordResetHandler> logger)
    {
        _dbContext = dbContext;
        _userRepository = userRepository;
        _passwordResetService = passwordResetService;
        _emailService = emailService;
        _auditService = auditService;
        _captchaService = captchaService;
        _logger = logger;
    }

    public async ValueTask<ResendPasswordResetResponse> Handle(
        ResendPasswordResetCommand command,
        CancellationToken ct)
    {
        // Validate CAPTCHA (always required for password reset resend)
        await _captchaService.ValidateRequiredAsync(
            command.CaptchaToken,
            "forgot_password_resend",
            command.IpAddress,
            ct);

        var email = command.Email.ToLowerInvariant();

        // Always return success to prevent email enumeration
        var successResponse = new ResendPasswordResetResponse(
            true,
            "If an account exists with this email and a password reset was requested, you will receive a new reset link.");

        // Find user
        var user = await _userRepository.GetByEmailAsync(email, ct);

        if (user is null)
        {
            _logger.LogDebug("Password reset resend requested for non-existent email: {Email}", email);
            return successResponse;
        }

        if (!user.IsActive)
        {
            _logger.LogDebug("Password reset resend requested for inactive user: {Email}", email);
            return successResponse;
        }

        if (user.IsAnonymized)
        {
            _logger.LogDebug("Password reset resend requested for anonymized user: {Email}", email);
            return successResponse;
        }

        // Check for existing password reset token
        var existingToken = await _dbContext.PasswordResetTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);

        // Check cooldown based on most recent token
        if (existingToken is not null)
        {
            var timeSinceCreated = DateTime.UtcNow - existingToken.CreatedAt;
            if (timeSinceCreated.TotalSeconds < ResendCooldownSeconds)
            {
                var remainingSeconds = (int)Math.Ceiling(ResendCooldownSeconds - timeSinceCreated.TotalSeconds);
                throw new PasswordResetResendCooldownException(remainingSeconds);
            }
        }
        else
        {
            // No existing token - they should use the regular forgot-password endpoint
            _logger.LogDebug("Password reset resend requested but no existing token for user: {Email}", email);
            return successResponse;
        }

        // Invalidate existing tokens and create new one
        await _passwordResetService.InvalidateAllTokensAsync(user.Id, ct);
        var result = await _passwordResetService.CreateResetTokenAsync(user.Id, ct);

        // Send email
        await _emailService.SendPasswordResetAsync(
            email: user.Email,
            firstName: user.FirstName,
            resetToken: result.Token,
            resetCode: result.Code,
            language: user.PreferredLanguage,
            cancellationToken: ct
        );

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.PasswordResetResent,
            user.Id,
            null,
            "SystemUser",
            user.Id,
            new { Email = email },
            ct
        );

        _logger.LogInformation("Password reset email resent to {Email}", email);

        return successResponse;
    }
}
