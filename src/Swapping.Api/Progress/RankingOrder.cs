namespace Swapping.Api.Progress;

public enum RankingMode
{
    /// <summary>Lowest time first; ties by fewer swaps, then by who got it first.</summary>
    Time,

    /// <summary>Fewest swaps first; ties by lower time, then by who got it first.</summary>
    Moves,
}

/// <summary>What a ranking compares. <see cref="AchievedAt"/> is the server time of the first upload.</summary>
public sealed record ResultScore(int TimeMs, int Swaps, DateTimeOffset AchievedAt);

/// <summary>The two ranking orders as data, not two code paths.</summary>
public static class RankingOrder
{
    private static readonly IComparer<ResultScore> TimeFirst = Comparer<ResultScore>.Create((a, b) =>
        (a.TimeMs, a.Swaps, a.AchievedAt).CompareTo((b.TimeMs, b.Swaps, b.AchievedAt)));

    private static readonly IComparer<ResultScore> MovesFirst = Comparer<ResultScore>.Create((a, b) =>
        (a.Swaps, a.TimeMs, a.AchievedAt).CompareTo((b.Swaps, b.TimeMs, b.AchievedAt)));

    public static IComparer<ResultScore> For(RankingMode mode) => mode switch
    {
        RankingMode.Time => TimeFirst,
        RankingMode.Moves => MovesFirst,
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    /// <summary>
    /// Strictly better in that mode's order. The date is left out, so an equal result arriving later
    /// (or with an earlier client clock) never replaces the old one.
    /// </summary>
    public static bool IsBetter(RankingMode mode, ResultScore candidate, ResultScore? current) =>
        current is null || For(mode).Compare(candidate with { AchievedAt = current.AchievedAt }, current) < 0;

    /// <summary>
    /// The same orders for the database: the keys of <see cref="For"/>, then the profile id so the
    /// answer is stable even if two uploads had the very same server time.
    /// </summary>
    public static IOrderedQueryable<BestResultEntity> InOrder(this IQueryable<BestResultEntity> rows, RankingMode mode) => mode switch
    {
        RankingMode.Time => rows.OrderBy(b => b.TimeMs).ThenBy(b => b.Swaps).ThenBy(b => b.AchievedAt).ThenBy(b => b.ProfileId),
        RankingMode.Moves => rows.OrderBy(b => b.Swaps).ThenBy(b => b.TimeMs).ThenBy(b => b.AchievedAt).ThenBy(b => b.ProfileId),
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    /// <summary>
    /// The rows strictly ahead of <paramref name="score"/> in that mode's order, for "my rank" as a
    /// <c>COUNT</c> (P7). Must agree with <see cref="For"/>.
    /// </summary>
    public static System.Linq.Expressions.Expression<Func<BestResultEntity, bool>> AheadOf(RankingMode mode, ResultScore score)
    {
        var (time, swaps, at) = (score.TimeMs, score.Swaps, score.AchievedAt);
        return mode switch
        {
            RankingMode.Time => b => b.TimeMs < time || (b.TimeMs == time && (b.Swaps < swaps || (b.Swaps == swaps && b.AchievedAt < at))),
            RankingMode.Moves => b => b.Swaps < swaps || (b.Swaps == swaps && (b.TimeMs < time || (b.TimeMs == time && b.AchievedAt < at))),
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };
    }
}
