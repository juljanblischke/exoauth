using System.Security.Cryptography;
using System.Text;
using ExoAuth.Domain.Enums;

namespace ExoAuth.Domain.Entities;

/// <summary>
/// Represents a device approval request for risk-based authentication.
/// When a login attempt has a high risk score, a DeviceApprovalRequest is created
/// and the user must approve the device before the login can complete.
/// </summary>
public sealed class DeviceApprovalRequest : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid DeviceSessionId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public string CodeHash { get; private set; } = null!;
    public int RiskScore { get; private set; }
    public string RiskFactors { get; private set; } = null!; // JSON array: ["new_device", "new_country"]
    public ApprovalStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? ResolvedBy { get; private set; } // "email_link", "email_code", "session_trust", "timeout"

    // Navigation properties
    public SystemUser? User { get; set; }
    public DeviceSession? DeviceSession { get; set; }

    private DeviceApprovalRequest() { } // EF Core

    /// <summary>
    /// Creates a new device approval request.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="deviceSessionId">The device session ID that needs approval.</param>
    /// <param name="token">The generated URL token (will be hashed).</param>
    /// <param name="code">The generated XXXX-XXXX code (will be hashed).</param>
    /// <param name="riskScore">The calculated risk score.</param>
    /// <param name="riskFactors">List of risk factors as JSON array.</param>
    /// <param name="expirationMinutes">Expiration time in minutes (default: 30).</param>
    public static DeviceApprovalRequest Create(
        Guid userId,
        Guid deviceSessionId,
        string token,
        string code,
        int riskScore,
        string riskFactors,
        int expirationMinutes = 30)
    {
        return new DeviceApprovalRequest
        {
            UserId = userId,
            DeviceSessionId = deviceSessionId,
            TokenHash = HashValue(token),
            CodeHash = HashValue(NormalizeCode(code)),
            RiskScore = riskScore,
            RiskFactors = riskFactors,
            Status = ApprovalStatus.Pending,
            Attempts = 0,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
        };
    }

    /// <summary>
    /// Checks if the approval request is expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Checks if the approval request is still pending and valid.
    /// </summary>
    public bool IsPending => Status == ApprovalStatus.Pending && !IsExpired;

    /// <summary>
    /// Validates the provided token against the stored hash.
    /// </summary>
    public bool ValidateToken(string token)
    {
        return TokenHash == HashValue(token);
    }

    /// <summary>
    /// Validates the provided code against the stored hash.
    /// </summary>
    public bool ValidateCode(string code)
    {
        return CodeHash == HashValue(NormalizeCode(code));
    }

    /// <summary>
    /// Marks the approval request as approved.
    /// </summary>
    /// <param name="resolvedBy">How the approval was resolved (email_link, email_code, session_trust).</param>
    public void MarkApproved(string resolvedBy)
    {
        Status = ApprovalStatus.Approved;
        ResolvedAt = DateTime.UtcNow;
        ResolvedBy = resolvedBy;
        SetUpdated();
    }

    /// <summary>
    /// Marks the approval request as denied.
    /// </summary>
    public void MarkDenied()
    {
        Status = ApprovalStatus.Denied;
        ResolvedAt = DateTime.UtcNow;
        ResolvedBy = "user_denied";
        SetUpdated();
    }

    /// <summary>
    /// Marks the approval request as expired.
    /// </summary>
    public void MarkExpired()
    {
        Status = ApprovalStatus.Expired;
        ResolvedAt = DateTime.UtcNow;
        ResolvedBy = "timeout";
        SetUpdated();
    }

    /// <summary>
    /// Increments the failed code attempt counter.
    /// </summary>
    /// <returns>The new attempt count.</returns>
    public int IncrementAttempts()
    {
        Attempts++;
        SetUpdated();
        return Attempts;
    }

    /// <summary>
    /// Generates a cryptographically secure URL token.
    /// </summary>
    public static string GenerateToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    /// <summary>
    /// Generates an 8-character alphanumeric code in XXXX-XXXX format.
    /// Uses uppercase letters and digits (no ambiguous chars: 0, O, I, L, 1).
    /// </summary>
    public static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // No 0, O, I, L, 1
        var code = new char[9]; // 8 chars + 1 dash

        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[8];
        rng.GetBytes(bytes);

        for (int i = 0; i < 8; i++)
        {
            var targetIndex = i < 4 ? i : i + 1; // Skip position 4 for dash
            code[targetIndex] = chars[bytes[i] % chars.Length];
        }

        code[4] = '-';
        return new string(code);
    }

    /// <summary>
    /// Normalizes a code by removing dashes and converting to uppercase.
    /// </summary>
    private static string NormalizeCode(string code)
    {
        return code.Replace("-", "").ToUpperInvariant();
    }

    private static string HashValue(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Helper method for checking token hash existence (used by service for collision detection).
    /// </summary>
    public static string HashForCheck(string token)
    {
        return HashValue(token);
    }
}
