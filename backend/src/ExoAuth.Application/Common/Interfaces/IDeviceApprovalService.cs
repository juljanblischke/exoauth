using ExoAuth.Application.Common.Models;
using ExoAuth.Domain.Entities;

namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for managing device approval requests for risk-based authentication.
/// </summary>
public interface IDeviceApprovalService
{
    /// <summary>
    /// Creates a new device approval request.
    /// Generates both a URL token and an 8-char code (XXXX-XXXX).
    /// Includes collision prevention for tokens.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="deviceSessionId">The device session ID that needs approval.</param>
    /// <param name="riskScore">The calculated risk score.</param>
    /// <param name="riskFactors">List of risk factors.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created approval request and the plain text values (token, code).</returns>
    Task<DeviceApprovalResult> CreateApprovalRequestAsync(
        Guid userId,
        Guid deviceSessionId,
        int riskScore,
        IEnumerable<string> riskFactors,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an approval token (URL token) and returns the request if valid.
    /// </summary>
    /// <param name="token">The plain text token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The approval request if valid, null otherwise.</returns>
    Task<DeviceApprovalRequest?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an approval code (XXXX-XXXX format) for a specific approval token.
    /// Increments attempt counter on failure.
    /// </summary>
    /// <param name="approvalToken">The approval token (used to identify the request).</param>
    /// <param name="code">The plain text code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The validation result with the request if valid.</returns>
    Task<CodeValidationResult> ValidateCodeAsync(
        string approvalToken,
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a device by marking the approval request as approved.
    /// </summary>
    /// <param name="request">The approval request.</param>
    /// <param name="resolvedBy">How the approval was resolved (email_link, email_code, session_trust).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ApproveAsync(DeviceApprovalRequest request, string resolvedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Denies a device approval request.
    /// </summary>
    /// <param name="request">The approval request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DenyAsync(DeviceApprovalRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves approval requests when a device session is trusted via session management.
    /// </summary>
    /// <param name="deviceSessionId">The device session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ResolveBySessionTrustAsync(Guid deviceSessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a pending approval request for a device session.
    /// </summary>
    /// <param name="deviceSessionId">The device session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The pending approval request if exists, null otherwise.</returns>
    Task<DeviceApprovalRequest?> GetPendingByDeviceSessionAsync(
        Guid deviceSessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all pending approval requests for a device session.
    /// </summary>
    /// <param name="deviceSessionId">The device session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidatePendingRequestsAsync(Guid deviceSessionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of creating a device approval request.
/// </summary>
public sealed record DeviceApprovalResult(
    DeviceApprovalRequest Entity,
    string Token,
    string Code
);

/// <summary>
/// Result of validating an approval code.
/// </summary>
public sealed record CodeValidationResult
{
    /// <summary>
    /// Whether the code is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// The approval request (if found).
    /// </summary>
    public DeviceApprovalRequest? Request { get; init; }

    /// <summary>
    /// Error message if validation failed.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Number of remaining attempts (if applicable).
    /// </summary>
    public int? RemainingAttempts { get; init; }

    public static CodeValidationResult Success(DeviceApprovalRequest request) =>
        new() { IsValid = true, Request = request };

    public static CodeValidationResult Failed(string errorCode, int? remainingAttempts = null) =>
        new() { IsValid = false, ErrorCode = errorCode, RemainingAttempts = remainingAttempts };

    public static CodeValidationResult NotFound() =>
        new() { IsValid = false, ErrorCode = "APPROVAL_TOKEN_INVALID" };
}
