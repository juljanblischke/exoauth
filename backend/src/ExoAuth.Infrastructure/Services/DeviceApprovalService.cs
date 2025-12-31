using System.Text.Json;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Domain.Entities;
using ExoAuth.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services;

/// <summary>
/// Service for managing device approval requests.
/// Follows the same pattern as PasswordResetService for token/code generation with collision prevention.
/// </summary>
public sealed class DeviceApprovalService : IDeviceApprovalService
{
    private readonly IAppDbContext _context;
    private readonly ILogger<DeviceApprovalService> _logger;
    private readonly int _expirationMinutes;
    private readonly int _maxCodeAttempts;
    private const int MaxRetries = 3;

    public DeviceApprovalService(
        IAppDbContext context,
        IConfiguration configuration,
        ILogger<DeviceApprovalService> logger)
    {
        _context = context;
        _logger = logger;

        var deviceTrust = configuration.GetSection("DeviceTrust");
        _expirationMinutes = deviceTrust.GetValue("ApprovalExpiryMinutes", 30);
        _maxCodeAttempts = deviceTrust.GetValue("MaxCodeAttempts", 3);
    }

    public async Task<DeviceApprovalResult> CreateApprovalRequestAsync(
        Guid userId,
        Guid deviceSessionId,
        int riskScore,
        IEnumerable<string> riskFactors,
        CancellationToken cancellationToken = default)
    {
        // Invalidate any existing pending requests for this device session
        await InvalidatePendingRequestsAsync(deviceSessionId, cancellationToken);

        // Serialize risk factors to JSON
        var riskFactorsJson = JsonSerializer.Serialize(riskFactors.ToList());

        // Generate with collision prevention (same pattern as PasswordResetService)
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var token = DeviceApprovalRequest.GenerateToken();
            var code = DeviceApprovalRequest.GenerateCode();

            // Check for token hash collision (extremely unlikely but we check anyway)
            var tokenHash = DeviceApprovalRequest.HashForCheck(token);
            var exists = await _context.DeviceApprovalRequests
                .AnyAsync(x => x.TokenHash == tokenHash, cancellationToken);

            if (exists)
            {
                _logger.LogWarning("Device approval token collision detected on attempt {Attempt}, regenerating", attempt + 1);
                continue;
            }

            var entity = DeviceApprovalRequest.Create(
                userId,
                deviceSessionId,
                token,
                code,
                riskScore,
                riskFactorsJson,
                _expirationMinutes);

            await _context.DeviceApprovalRequests.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Device approval request created for user {UserId}, device session {DeviceSessionId}, risk score {RiskScore}",
                userId, deviceSessionId, riskScore);

            return new DeviceApprovalResult(entity, token, code);
        }

        // This should never happen given the entropy of our tokens
        throw new InvalidOperationException("Failed to generate unique device approval token after maximum retries");
    }

    public async Task<DeviceApprovalRequest?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var tokenHash = DeviceApprovalRequest.HashForCheck(token);

        var request = await _context.DeviceApprovalRequests
            .Include(x => x.User)
            .Include(x => x.DeviceSession)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (request is null)
        {
            _logger.LogDebug("Device approval token not found");
            return null;
        }

        // Check if expired
        if (request.IsExpired)
        {
            _logger.LogDebug("Device approval token is expired");
            // Mark as expired if still pending
            if (request.Status == ApprovalStatus.Pending)
            {
                request.MarkExpired();
                await _context.SaveChangesAsync(cancellationToken);
            }
            return null;
        }

        // Check if not pending
        if (!request.IsPending)
        {
            _logger.LogDebug("Device approval request is not pending (status: {Status})", request.Status);
            return null;
        }

        return request;
    }

    public async Task<CodeValidationResult> ValidateCodeAsync(
        string approvalToken,
        string code,
        CancellationToken cancellationToken = default)
    {
        // First validate the token to get the request
        var request = await ValidateTokenAsync(approvalToken, cancellationToken);

        if (request is null)
        {
            return CodeValidationResult.NotFound();
        }

        // Check if max attempts reached
        if (request.Attempts >= _maxCodeAttempts)
        {
            _logger.LogWarning(
                "Device approval max attempts reached for request {RequestId}",
                request.Id);
            return CodeValidationResult.Failed(ErrorCodes.ApprovalMaxAttempts, 0);
        }

        // Validate the code
        if (!request.ValidateCode(code))
        {
            // Increment attempts
            var newAttemptCount = request.IncrementAttempts();
            await _context.SaveChangesAsync(cancellationToken);

            var remainingAttempts = _maxCodeAttempts - newAttemptCount;

            _logger.LogDebug(
                "Invalid device approval code for request {RequestId}, attempt {Attempt}/{MaxAttempts}",
                request.Id, newAttemptCount, _maxCodeAttempts);

            if (remainingAttempts <= 0)
            {
                return CodeValidationResult.Failed(ErrorCodes.ApprovalMaxAttempts, 0);
            }

            return CodeValidationResult.Failed(ErrorCodes.ApprovalCodeInvalid, remainingAttempts);
        }

        _logger.LogDebug("Device approval code validated successfully for request {RequestId}", request.Id);
        return CodeValidationResult.Success(request);
    }

    public async Task ApproveAsync(DeviceApprovalRequest request, string resolvedBy, CancellationToken cancellationToken = default)
    {
        request.MarkApproved(resolvedBy);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Device approval request {RequestId} approved via {ResolvedBy} for user {UserId}",
            request.Id, resolvedBy, request.UserId);
    }

    public async Task DenyAsync(DeviceApprovalRequest request, CancellationToken cancellationToken = default)
    {
        request.MarkDenied();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Device approval request {RequestId} denied for user {UserId}",
            request.Id, request.UserId);
    }

    public async Task ResolveBySessionTrustAsync(Guid deviceSessionId, CancellationToken cancellationToken = default)
    {
        var pendingRequests = await _context.DeviceApprovalRequests
            .Where(x => x.DeviceSessionId == deviceSessionId && x.Status == ApprovalStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var request in pendingRequests)
        {
            request.MarkApproved("session_trust");
        }

        if (pendingRequests.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Resolved {Count} pending device approval request(s) via session trust for device session {DeviceSessionId}",
                pendingRequests.Count, deviceSessionId);
        }
    }

    public async Task<DeviceApprovalRequest?> GetPendingByDeviceSessionAsync(
        Guid deviceSessionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.DeviceApprovalRequests
            .Include(x => x.User)
            .Include(x => x.DeviceSession)
            .Where(x => x.DeviceSessionId == deviceSessionId && x.Status == ApprovalStatus.Pending)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task InvalidatePendingRequestsAsync(Guid deviceSessionId, CancellationToken cancellationToken = default)
    {
        var pendingRequests = await _context.DeviceApprovalRequests
            .Where(x => x.DeviceSessionId == deviceSessionId && x.Status == ApprovalStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var request in pendingRequests)
        {
            request.MarkExpired();
        }

        if (pendingRequests.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug(
                "Invalidated {Count} pending device approval request(s) for device session {DeviceSessionId}",
                pendingRequests.Count, deviceSessionId);
        }
    }
}
