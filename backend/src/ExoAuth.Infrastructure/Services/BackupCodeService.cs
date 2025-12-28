using System.Security.Cryptography;
using System.Text;
using ExoAuth.Application.Common.Interfaces;

namespace ExoAuth.Infrastructure.Services;

/// <summary>
/// Service for generating and validating MFA backup codes.
/// Format: XXXX-XXXX (8 alphanumeric characters, uppercase)
/// </summary>
public sealed class BackupCodeService : IBackupCodeService
{
    // Characters used for backup codes (avoiding ambiguous characters like 0/O, 1/I/L)
    private const string AllowedCharacters = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public List<string> GenerateCodes(int count = 10)
    {
        var codes = new List<string>(count);

        for (int i = 0; i < count; i++)
        {
            codes.Add(GenerateSingleCode());
        }

        return codes;
    }

    public string HashCode(string code)
    {
        var normalized = NormalizeCode(code);
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    public bool VerifyCode(string code, string hash)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(hash))
            return false;

        var computedHash = HashCode(code);
        return computedHash == hash;
    }

    public string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        // Remove hyphens, spaces, and convert to uppercase
        return code.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();
    }

    private string GenerateSingleCode()
    {
        var codeBuilder = new StringBuilder(9); // 8 chars + 1 hyphen

        using var rng = RandomNumberGenerator.Create();
        var randomBytes = new byte[8];
        rng.GetBytes(randomBytes);

        for (int i = 0; i < 8; i++)
        {
            if (i == 4)
                codeBuilder.Append('-');

            var index = randomBytes[i] % AllowedCharacters.Length;
            codeBuilder.Append(AllowedCharacters[index]);
        }

        return codeBuilder.ToString();
    }
}
