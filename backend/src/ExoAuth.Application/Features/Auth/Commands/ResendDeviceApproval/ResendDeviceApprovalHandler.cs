using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Application.Features.Auth.Commands.ResendDeviceApproval;

public sealed class ResendDeviceApprovalHandler : ICommandHandler<ResendDeviceApprovalCommand, ResendDeviceApprovalResponse>
{
    private const int ResendCooldownSeconds = 60; // 1 minute cooldown

    private readonly IAppDbContext _dbContext;
    private readonly ISystemUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;
    private readonly ICaptchaService _captchaService;
    private readonly ILogger<ResendDeviceApprovalHandler> _logger;

    public ResendDeviceApprovalHandler(
        IAppDbContext dbContext,
        ISystemUserRepository userRepository,
        IEmailService emailService,
        IAuditService auditService,
        ICaptchaService captchaService,
        ILogger<ResendDeviceApprovalHandler> logger)
    {
        _dbContext = dbContext;
        _userRepository = userRepository;
        _emailService = emailService;
        _auditService = auditService;
        _captchaService = captchaService;
        _logger = logger;
    }

    public async ValueTask<ResendDeviceApprovalResponse> Handle(
        ResendDeviceApprovalCommand command,
        CancellationToken ct)
    {
        // Validate CAPTCHA (optional - can be required based on configuration)
        if (!string.IsNullOrEmpty(command.CaptchaToken))
        {
            await _captchaService.ValidateRequiredAsync(
                command.CaptchaToken,
                "device_approval_resend",
                command.IpAddress,
                ct);
        }

        // Find the device by approval token
        var tokenHash = Device.HashForCheck(command.ApprovalToken);
        var device = await _dbContext.Devices
            .Include(d => d.User)
            .FirstOrDefaultAsync(d =>
                d.ApprovalTokenHash == tokenHash &&
                d.Status == DeviceStatus.PendingApproval,
                ct);

        if (device is null)
        {
            throw new DeviceApprovalNotPendingException();
        }

        var user = device.User;
        if (user is null || !user.IsActive || user.IsAnonymized)
        {
            throw new DeviceApprovalNotPendingException();
        }

        // Check cooldown based on device's UpdatedAt (which is set when approval is resent)
        var lastActivity = device.UpdatedAt ?? device.CreatedAt;
        var timeSinceLastActivity = DateTime.UtcNow - lastActivity;
        if (timeSinceLastActivity.TotalSeconds < ResendCooldownSeconds)
        {
            var remainingSeconds = (int)Math.Ceiling(ResendCooldownSeconds - timeSinceLastActivity.TotalSeconds);
            throw new DeviceApprovalResendCooldownException(remainingSeconds);
        }

        // Generate new approval credentials
        var newApprovalToken = Device.GenerateApprovalToken();
        var newApprovalCode = Device.GenerateApprovalCode();

        // Update the device with new credentials (keeps existing risk score and factors)
        device.ResetToPending(
            newApprovalToken,
            newApprovalCode,
            device.RiskScore ?? 0,
            device.RiskFactors ?? string.Empty,
            expirationMinutes: 30
        );

        await _dbContext.SaveChangesAsync(ct);

        // Send device approval email
        await _emailService.SendDeviceApprovalRequiredAsync(
            email: user.Email,
            firstName: user.FirstName,
            approvalToken: newApprovalToken,
            approvalCode: newApprovalCode,
            deviceName: device.DisplayName,
            browser: device.Browser,
            operatingSystem: device.OperatingSystem,
            location: device.LocationDisplay,
            ipAddress: device.IpAddress,
            riskScore: device.RiskScore ?? 0,
            userId: user.Id,
            language: user.PreferredLanguage,
            cancellationToken: ct
        );

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.DeviceApprovalResent,
            user.Id,
            null,
            "Device",
            device.Id,
            new
            {
                device.RiskScore,
                DeviceId = device.DeviceId,
                Reason = "Device approval email resent"
            },
            ct
        );

        _logger.LogInformation(
            "Device approval email resent for user {UserId}, device {DeviceId}",
            user.Id, device.Id);

        return new ResendDeviceApprovalResponse(
            Success: true,
            Message: "Device approval email has been resent.",
            NewApprovalToken: newApprovalToken
        );
    }
}
