using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Swapping.Api.Data;

namespace Swapping.Api.Profiles;

/// <summary>The profile rules: create, log in, lock out, regenerate the code, change the icon.</summary>
public sealed class ProfileService(GameDbContext db, TimeProvider time, IOptions<ProfilesOptions> options)
{
    private ProfilesOptions Options => options.Value;

    public async Task<CreateResult> CreateAsync(string? name, string? icon, CancellationToken ct)
    {
        if (!ProfileNames.TryNormalize(name, out var cleanName))
            return CreateResult.Fail(ProfileError.InvalidName);
        if (icon is null || !Options.IconList.Contains(icon))
            return CreateResult.Fail(ProfileError.UnknownIcon);

        var key = ProfileNames.Key(cleanName);
        if (await db.Profiles.AnyAsync(p => p.NameKey == key, ct))
            return CreateResult.Fail(ProfileError.NameTaken);

        var now = time.GetUtcNow();
        var code = AccessCode.Generate();
        var profile = new ProfileEntity
        {
            Id = Guid.NewGuid(),
            Name = cleanName,
            NameKey = key,
            Icon = icon,
            CreatedAt = now,
            AccessCodeHash = AccessCode.Hash(code),
        };
        var session = SessionEntity.Create(profile.Id, now, Options.TokenLifetime, out var token);
        db.Profiles.Add(profile);
        db.Sessions.Add(session);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Two creations of the same name at once: the unique index decides.
            return CreateResult.Fail(ProfileError.NameTaken);
        }

        return CreateResult.Ok(new CreatedProfile(profile, code, token, session.ExpiresAt));
    }
}

public enum ProfileError
{
    InvalidName,
    UnknownIcon,
    NameTaken,
}

public sealed record CreatedProfile(ProfileEntity Profile, string AccessCode, string Token, DateTimeOffset TokenExpiresAt);

public sealed record CreateResult(CreatedProfile? Value, ProfileError? Error)
{
    public static CreateResult Ok(CreatedProfile value) => new(value, null);

    public static CreateResult Fail(ProfileError error) => new(null, error);
}
