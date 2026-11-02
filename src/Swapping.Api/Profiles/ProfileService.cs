using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Swapping.Api.Data;
using Swapping.Api.Hosting;

namespace Swapping.Api.Profiles;

/// <summary>The profile rules: create, log in, lock out, regenerate the code, change the icon.</summary>
public sealed class ProfileService(GameDbContext db, TimeProvider time, IOptions<ProfilesOptions> options, SecurityLog security)
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

    /// <summary>
    /// Name + code → a new session. Unknown name, invalid name and wrong code give the same answer;
    /// 5 wrong codes in a row lock the profile for 15 minutes (A5).
    /// </summary>
    public async Task<LoginResult> LoginAsync(string? name, string? accessCode, CancellationToken ct)
    {
        if (!ProfileNames.TryNormalize(name, out var cleanName))
            return Refused(null);

        var key = ProfileNames.Key(cleanName);
        var now = time.GetUtcNow();

        // A lock that has ended is cleared first, so wrong codes count from zero again.
        await db.Profiles
            .Where(p => p.NameKey == key && p.LockedUntil != null && p.LockedUntil <= now)
            .ExecuteUpdateAsync(set => set.SetProperty(p => p.FailedAttempts, 0).SetProperty(p => p.LockedUntil, (DateTimeOffset?)null), ct);

        var profile = await db.Profiles.AsNoTracking().SingleOrDefaultAsync(p => p.NameKey == key, ct);
        if (profile is null)
            return Refused(null);

        if (profile.LockedUntil > now)
            return LoginResult.Locked(profile.LockedUntil.Value - now);

        if (!AccessCode.Matches(accessCode, profile.AccessCodeHash))
            return await RegisterFailureAsync(profile.Id, now, ct);

        await db.Profiles.Where(p => p.Id == profile.Id)
            .ExecuteUpdateAsync(set => set.SetProperty(p => p.FailedAttempts, 0), ct);
        var session = SessionEntity.Create(profile.Id, now, Options.TokenLifetime, out var token);
        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);
        return LoginResult.Ok(profile, token, session.ExpiresAt);
    }

    /// <summary>
    /// Counts one wrong code with a single SQL update (S2), so parallel guesses cannot all read
    /// "0 failures". When the counter is already at the limit nothing is counted: the profile is locked.
    /// </summary>
    internal async Task<LoginResult> RegisterFailureAsync(Guid profileId, DateTimeOffset now, CancellationToken ct)
    {
        var max = Options.MaxFailures;
        DateTimeOffset? lockEnd = now + Options.LockoutDuration;

        var counted = await db.Profiles
            .Where(p => p.Id == profileId && p.FailedAttempts < max)
            .ExecuteUpdateAsync(set => set
                .SetProperty(p => p.LockedUntil, p => p.FailedAttempts + 1 >= max ? lockEnd : p.LockedUntil)
                .SetProperty(p => p.FailedAttempts, p => p.FailedAttempts + 1), ct);

        if (counted == 1)
        {
            var attempts = await db.Profiles.Where(p => p.Id == profileId).Select(p => p.FailedAttempts).SingleAsync(ct);
            if (attempts >= max)
                security.Write(SecurityEvent.LockoutStarted, profileId);
            return Refused(profileId);
        }

        var lockedUntil = await db.Profiles.Where(p => p.Id == profileId).Select(p => p.LockedUntil).SingleAsync(ct);
        return LoginResult.Locked((lockedUntil ?? lockEnd.Value) - now);
    }

    /// <summary>
    /// A new access code, only with the current one (A11, S5): the old code stops working and every
    /// session except the calling one is revoked. A wrong or missing current code counts as a failure.
    /// </summary>
    public async Task<RegenerateResult> RegenerateAsync(Guid profileId, Guid sessionId, string? currentCode, CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var profile = await db.Profiles.AsNoTracking().SingleAsync(p => p.Id == profileId, ct);
        if (profile.LockedUntil > now)
            return new RegenerateResult(null, LoginResult.Locked(profile.LockedUntil.Value - now));

        if (!AccessCode.Matches(currentCode, profile.AccessCodeHash))
            return new RegenerateResult(null, await RegisterFailureAsync(profileId, now, ct));

        var code = AccessCode.Generate();
        var hash = AccessCode.Hash(code);
        await db.Profiles.Where(p => p.Id == profileId).ExecuteUpdateAsync(set => set
            .SetProperty(p => p.AccessCodeHash, hash)
            .SetProperty(p => p.FailedAttempts, 0)
            .SetProperty(p => p.LockedUntil, (DateTimeOffset?)null), ct);
        await db.Sessions.Where(s => s.ProfileId == profileId && s.Id != sessionId).ExecuteDeleteAsync(ct);
        security.Write(SecurityEvent.AccessCodeRegenerated, profileId);
        return new RegenerateResult(code, null);
    }

    private LoginResult Refused(Guid? profileId)
    {
        security.Write(SecurityEvent.LoginRefused, profileId);
        return LoginResult.InvalidCredentials();
    }

    public Task<ProfileEntity?> GetAsync(Guid profileId, CancellationToken ct) =>
        db.Profiles.AsNoTracking().SingleOrDefaultAsync(p => p.Id == profileId, ct);

    /// <summary>Changes the icon; null when the icon is not in the list.</summary>
    public async Task<ProfileEntity?> ChangeIconAsync(Guid profileId, string? icon, CancellationToken ct)
    {
        if (icon is null || !Options.IconList.Contains(icon))
            return null;

        var profile = await db.Profiles.SingleAsync(p => p.Id == profileId, ct);
        profile.Icon = icon;
        await db.SaveChangesAsync(ct);
        return profile;
    }

    /// <summary>Ends one session: its token stops working.</summary>
    public Task LogoutAsync(Guid sessionId, CancellationToken ct) =>
        db.Sessions.Where(s => s.Id == sessionId).ExecuteDeleteAsync(ct);
}

public enum ProfileError
{
    InvalidName,
    UnknownIcon,
    NameTaken,
}

public sealed record CreatedProfile(ProfileEntity Profile, string AccessCode, string Token, DateTimeOffset TokenExpiresAt);

public enum LoginOutcome
{
    Ok,
    InvalidCredentials,
    Locked,
}

public sealed record LoginResult(LoginOutcome Outcome, ProfileEntity? Profile, string? Token, DateTimeOffset TokenExpiresAt, TimeSpan RetryAfter)
{
    public static LoginResult Ok(ProfileEntity profile, string token, DateTimeOffset expiresAt) =>
        new(LoginOutcome.Ok, profile, token, expiresAt, TimeSpan.Zero);

    public static LoginResult InvalidCredentials() => new(LoginOutcome.InvalidCredentials, null, null, default, TimeSpan.Zero);

    public static LoginResult Locked(TimeSpan retryAfter) => new(LoginOutcome.Locked, null, null, default, retryAfter);
}

/// <summary>Either the new code, or the refusal (wrong code or locked).</summary>
public sealed record RegenerateResult(string? NewCode, LoginResult? Refusal);

public sealed record CreateResult(CreatedProfile? Value, ProfileError? Error)
{
    public static CreateResult Ok(CreatedProfile value) => new(value, null);

    public static CreateResult Fail(ProfileError error) => new(null, error);
}
