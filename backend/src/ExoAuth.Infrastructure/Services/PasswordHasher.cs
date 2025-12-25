using System.Security.Cryptography;
using System.Text;
using ExoAuth.Application.Common.Interfaces;
using Konscious.Security.Cryptography;

namespace ExoAuth.Infrastructure.Services;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int DegreeOfParallelism = 4;
    private const int MemorySize = 65536; // 64 MB
    private const int Iterations = 4;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = HashPassword(password, salt);

        // Combine salt and hash: salt$hash (both base64 encoded)
        return $"{Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string storedHash)
    {
        try
        {
            var parts = storedHash.Split('$');
            if (parts.Length != 2)
                return false;

            var salt = Convert.FromBase64String(parts[0]);
            var hash = Convert.FromBase64String(parts[1]);

            var computedHash = HashPassword(password, salt);

            return CryptographicOperations.FixedTimeEquals(hash, computedHash);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] HashPassword(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            MemorySize = MemorySize,
            Iterations = Iterations
        };

        return argon2.GetBytes(HashSize);
    }
}
