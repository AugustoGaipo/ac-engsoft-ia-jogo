using Microsoft.EntityFrameworkCore;
using Swapping.Api.Data;
using Swapping.Api.Hosting;

namespace Swapping.Api.Progress;

public enum SubmitError
{
    UnknownLevel,
    LevelLocked,
    ImplausibleResult,
}

/// <summary>A finished run as the client sends it (already bound to integers by the strict JSON).</summary>
public sealed record SubmittedResult(string LevelId, ResultValues Values, DateTimeOffset FinishedAt);

public sealed record SubmitOutcome(bool ImprovedTime, bool ImprovedMoves, bool UnlockedNext);

public sealed record SubmitResult(SubmitOutcome? Outcome, SubmitError? Error)
{
    public static SubmitResult Ok(SubmitOutcome outcome) => new(outcome, null);

    public static SubmitResult Fail(SubmitError error) => new(null, error);
}

public sealed record BestView(int TimeMs, int Swaps);

/// <summary>One public ranking line: nothing but these five fields (P6).</summary>
public sealed record RankingEntry(int Rank, string Name, string Icon, int TimeMs, int Swaps);

public sealed record RankingView(string LevelId, string Mode, IReadOnlyList<RankingEntry> Entries);

public sealed record MyRankView(int Rank, int TimeMs, int Swaps, int TotalPlayers);

public sealed record LevelProgressView(string LevelId, int Number, string Name, bool Unlocked, BestView? BestTime, BestView? BestMoves);

/// <summary>The progress rules: unlocking in catalog order, best result per mode, my progress.</summary>
public sealed class ProgressService(GameDbContext db, LevelDirectory levels, TimeProvider time, SecurityLog security)
{
    private static readonly RankingMode[] Modes = [RankingMode.Time, RankingMode.Moves];

    /// <summary>
    /// Checks a result (level known → unlocked → plausible) and keeps it for each mode where it is
    /// strictly better (B1, B3). The profile id comes only from the token (P2).
    /// </summary>
    public async Task<SubmitResult> SubmitAsync(Guid profileId, SubmittedResult result, CancellationToken ct)
    {
        if (levels.Find(result.LevelId) is not { } level)
            return SubmitResult.Fail(SubmitError.UnknownLevel);

        if (!await IsUnlockedAsync(profileId, level.LevelId, ct))
            return Refused(profileId, level, result.Values, SubmitError.LevelLocked);

        if (!ResultCheck.IsPlausible(result.Values, level.Rooms))
            return Refused(profileId, level, result.Values, SubmitError.ImplausibleResult);

        try
        {
            return SubmitResult.Ok(await KeepBestAsync(profileId, level, result, ct));
        }
        catch (DbUpdateException)
        {
            // Two first uploads of the same level at once: the primary key let only one insert in.
            // Read again and compare with the row that won.
            db.ChangeTracker.Clear();
            return SubmitResult.Ok(await KeepBestAsync(profileId, level, result, ct));
        }
    }

    private async Task<SubmitOutcome> KeepBestAsync(Guid profileId, LevelInfo level, SubmittedResult result, CancellationToken ct)
    {
        var rows = await db.BestResults
            .Where(b => b.ProfileId == profileId && b.LevelId == level.LevelId)
            .ToDictionaryAsync(b => b.Mode, ct);
        var firstResult = rows.Count == 0;
        var now = time.GetUtcNow();
        var candidate = new ResultScore(result.Values.TimeMs, result.Values.Swaps, now);

        var improved = new Dictionary<RankingMode, bool>();
        foreach (var mode in Modes)
        {
            var current = rows.GetValueOrDefault(mode);
            improved[mode] = RankingOrder.IsBetter(mode, candidate, current?.Score);
            if (!improved[mode])
                continue;

            if (current is null)
            {
                current = new BestResultEntity { ProfileId = profileId, LevelId = level.LevelId, Mode = mode };
                db.BestResults.Add(current);
            }
            current.TimeMs = result.Values.TimeMs;
            current.Swaps = result.Values.Swaps;
            current.Deaths = result.Values.Deaths;
            current.RoomRestarts = result.Values.RoomRestarts;
            current.FinishedAt = result.FinishedAt;
            current.AchievedAt = now;
        }

        await db.SaveChangesAsync(ct);
        // The next level opens on the first result of this one (B2); later uploads unlock nothing new.
        var unlockedNext = firstResult && levels.Next(level.LevelId) is not null;
        return new SubmitOutcome(improved[RankingMode.Time], improved[RankingMode.Moves], unlockedNext);
    }

    /// <summary>Every catalog level, in catalog order, with the profile's state.</summary>
    public async Task<IReadOnlyList<LevelProgressView>> GetProgressAsync(Guid profileId, CancellationToken ct)
    {
        var rows = await db.BestResults.AsNoTracking().Where(b => b.ProfileId == profileId).ToListAsync(ct);
        var best = rows.ToDictionary(b => (b.LevelId, b.Mode), b => new BestView(b.TimeMs, b.Swaps));
        var played = rows.Select(b => b.LevelId).ToHashSet(StringComparer.Ordinal);

        return levels.Levels.Select((level, index) => new LevelProgressView(
            level.LevelId, level.Number, level.Name,
            Unlocked: index == 0 || played.Contains(levels.Levels[index - 1].LevelId),
            BestTime: best.GetValueOrDefault((level.LevelId, RankingMode.Time)),
            BestMoves: best.GetValueOrDefault((level.LevelId, RankingMode.Moves)))).ToList();
    }

    /// <summary>
    /// The top <paramref name="top"/> of one level in one mode. Ranks are plain positions (B5); the
    /// entries carry only public fields (P6). The level id was already checked against the catalog (P4).
    /// </summary>
    public async Task<IReadOnlyList<RankingEntry>> GetRankingAsync(string levelId, RankingMode mode, int top, CancellationToken ct)
    {
        var rows = await db.BestResults.AsNoTracking()
            .Where(b => b.LevelId == levelId && b.Mode == mode)
            .InOrder(mode)
            .Take(top)
            .Select(b => new { b.ProfileId, b.TimeMs, b.Swaps })
            .ToListAsync(ct);

        var ids = rows.Select(r => r.ProfileId).ToList();
        var profiles = await db.Profiles.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => (p.Name, p.Icon), ct);

        return rows.Select((r, index) =>
        {
            var (name, icon) = profiles[r.ProfileId];
            return new RankingEntry(index + 1, name, icon, r.TimeMs, r.Swaps);
        }).ToList();
    }

    /// <summary>My row in one level and mode with its rank, or null when I have no result there.</summary>
    public async Task<MyRankView?> GetMyRankAsync(Guid profileId, string levelId, RankingMode mode, CancellationToken ct)
    {
        var level = db.BestResults.AsNoTracking().Where(b => b.LevelId == levelId && b.Mode == mode);
        if (await level.FirstOrDefaultAsync(b => b.ProfileId == profileId, ct) is not { } mine)
            return null;

        var ahead = await level.CountAsync(RankingOrder.AheadOf(mode, mine.Score), ct);
        var total = await level.CountAsync(ct);
        return new MyRankView(ahead + 1, mine.TimeMs, mine.Swaps, total);
    }

    /// <summary>The first level is always open; any other once the previous one has a result (B2).</summary>
    private async Task<bool> IsUnlockedAsync(Guid profileId, string levelId, CancellationToken ct) =>
        levels.Previous(levelId) is not { } previous ||
        await db.BestResults.AnyAsync(b => b.ProfileId == profileId && b.LevelId == previous.LevelId, ct);

    /// <summary>P11: a refused result leaves a trace with the profile, the level and the numbers, never the body.</summary>
    private SubmitResult Refused(Guid profileId, LevelInfo level, ResultValues values, SubmitError error)
    {
        security.Write(SecurityEvent.ResultRefused, profileId,
            $"level={level.LevelId} code={error} timeMs={values.TimeMs} swaps={values.Swaps} deaths={values.Deaths} roomRestarts={values.RoomRestarts}");
        return SubmitResult.Fail(error);
    }
}
