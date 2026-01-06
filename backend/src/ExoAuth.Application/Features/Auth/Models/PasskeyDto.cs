using ExoAuth.Domain.Entities;

namespace ExoAuth.Application.Features.Auth.Models;

/// <summary>
/// DTO for passkey information.
/// </summary>
public sealed record PasskeyDto(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    DateTime? LastUsedAt
)
{
    /// <summary>
    /// Maps a Passkey entity to a PasskeyDto.
    /// </summary>
    /// <param name="passkey">The passkey entity.</param>
    /// <returns>The mapped DTO.</returns>
    public static PasskeyDto FromEntity(Passkey passkey)
    {
        return new PasskeyDto(
            Id: passkey.Id,
            Name: passkey.Name,
            CreatedAt: passkey.CreatedAt,
            LastUsedAt: passkey.LastUsedAt
        );
    }
}
