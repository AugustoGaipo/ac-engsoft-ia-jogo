using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Swapping.Api.Profiles;

/// <summary>A login session. Holds the hash of the bearer token, never the token (A4).</summary>
public sealed class SessionEntity
{
    public Guid Id { get; set; }

    public Guid ProfileId { get; set; }

    /// <summary>SHA-256 of the token text.</summary>
    public byte[] TokenHash { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>A new session with 32 random bytes as the token (Base64url).</summary>
    public static SessionEntity Create(Guid profileId, DateTimeOffset now, TimeSpan lifetime, out string token)
    {
        token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        return new SessionEntity
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            TokenHash = HashToken(token),
            CreatedAt = now,
            ExpiresAt = now + lifetime,
        };
    }

    public static byte[] HashToken(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));
}
