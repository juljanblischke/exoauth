using ExoAuth.Application.Common.Interfaces;
using Mediator;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Application.Features.Auth.Commands.RequestMagicLink;

public sealed class RequestMagicLinkHandler : ICommandHandler<RequestMagicLinkCommand, RequestMagicLinkResponse>
{
    private readonly ISystemUserRepository _userRepository;
    private readonly IMagicLinkService _magicLinkService;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;
    private readonly ICaptchaService _captchaService;
    private readonly ILogger<RequestMagicLinkHandler> _logger;

    public RequestMagicLinkHandler(
        ISystemUserRepository userRepository,
        IMagicLinkService magicLinkService,
        IEmailService emailService,
        IAuditService auditService,
        ICaptchaService captchaService,
        ILogger<RequestMagicLinkHandler> logger)
    {
        _userRepository = userRepository;
        _magicLinkService = magicLinkService;
        _emailService = emailService;
        _auditService = auditService;
        _captchaService = captchaService;
        _logger = logger;
    }

    public async ValueTask<RequestMagicLinkResponse> Handle(RequestMagicLinkCommand command, CancellationToken ct)
    {
        // Validate CAPTCHA (always required for magic link)
        await _captchaService.ValidateRequiredAsync(
            command.CaptchaToken,
            "magic_link",
            command.IpAddress,
            ct);

        var email = command.Email.ToLowerInvariant();

        // Always return success to prevent email enumeration
        var successResponse = new RequestMagicLinkResponse(true, "If an account exists with this email, you will receive a magic link.");

        // Find user
        var user = await _userRepository.GetByEmailAsync(email, ct);

        if (user is null)
        {
            _logger.LogDebug("Magic link requested for non-existent email: {Email}", email);
            return successResponse;
        }

        if (!user.IsActive)
        {
            _logger.LogDebug("Magic link requested for inactive user: {Email}", email);
            return successResponse;
        }

        if (user.IsAnonymized)
        {
            _logger.LogDebug("Magic link requested for anonymized user: {Email}", email);
            return successResponse;
        }

        // Create magic link token
        var result = await _magicLinkService.CreateMagicLinkAsync(user.Id, ct);

        // Send email
        await _emailService.SendMagicLinkAsync(
            email: user.Email,
            firstName: user.FirstName,
            magicLinkToken: result.Token,
            userId: user.Id,
            language: user.PreferredLanguage,
            cancellationToken: ct
        );

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.MagicLinkRequested,
            user.Id,
            null,
            "SystemUser",
            user.Id,
            new { Email = email },
            ct
        );

        _logger.LogInformation("Magic link email sent to {Email}", email);

        return successResponse;
    }
}
