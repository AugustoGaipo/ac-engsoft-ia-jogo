namespace Swapping.Api.Profiles;

/// <summary>A player profile. Holds the hash of the access code, never the code.</summary>
public sealed class ProfileEntity
{
    public Guid Id { get; set; }

    /// <summary>The name as the player typed it (trimmed), public in the rankings.</summary>
    public string Name { get; set; } = "";

    /// <summary>The unique key: <see cref="ProfileNames.Key"/> of the name.</summary>
    public string NameKey { get; set; } = "";

    public string Icon { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>SHA-256 of the normalized access code (A3).</summary>
    public byte[] AccessCodeHash { get; set; } = [];

    /// <summary>Wrong codes in a row since the last good login (A5).</summary>
    public int FailedAttempts { get; set; }

    public DateTimeOffset? LockedUntil { get; set; }
}
