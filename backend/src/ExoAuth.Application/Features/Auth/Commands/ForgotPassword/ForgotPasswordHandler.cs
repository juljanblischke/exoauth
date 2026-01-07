using ExoAuth.Application.Common.Interfaces;
using Mediator;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Application.Features.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordHandler : ICommandHandler<ForgotPasswordCommand, ForgotPasswordResponse>
{
    private readonly ISystemUserRepository _userRepository;
    private readonly IPasswordResetService _passwordResetService;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;
    private readonly ICaptchaService _captchaService;
    private readonly ILogger<ForgotPasswordHandler> _logger;

    public ForgotPasswordHandler(
        ISystemUserRepository userRepository,
        IPasswordResetService passwordResetService,
        IEmailService emailService,
        IAuditService auditService,
        ICaptchaService captchaService,
        ILogger<ForgotPasswordHandler> logger)
    {
        _userRepository = userRepository;
        _passwordResetService = passwordResetService;
        _emailService = emailService;
        _auditService = auditService;
        _captchaService = captchaService;
        _logger = logger;
    }

    public async ValueTask<ForgotPasswordResponse> Handle(ForgotPasswordCommand command, CancellationToken ct)
    {
        // Validate CAPTCHA (always required for forgot password)
        await _captchaService.ValidateRequiredAsync(
            command.CaptchaToken,
            "forgot_password",
            command.IpAddress,
            ct);

        var email = command.Email.ToLowerInvariant();

        // Always return success to prevent email enumeration
        var successResponse = new ForgotPasswordResponse(true, "If an account exists with this email, you will receive a password reset link.");

        // Find user
        var user = await _userRepository.GetByEmailAsync(email, ct);

        if (user is null)
        {
            _logger.LogDebug("Password reset requested for non-existent email: {Email}", email);
            return successResponse;
        }

        if (!user.IsActive)
        {
            _logger.LogDebug("Password reset requested for inactive user: {Email}", email);
            return successResponse;
        }

        if (user.IsAnonymized)
        {
            _logger.LogDebug("Password reset requested for anonymized user: {Email}", email);
            return successResponse;
        }

        // Create reset token
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
            AuditActions.PasswordResetRequested,
            user.Id,
            null,
            "SystemUser",
            user.Id,
            new { Email = email },
            ct
        );

        _logger.LogInformation("Password reset email sent to {Email}", email);

        return successResponse;
    }
}
