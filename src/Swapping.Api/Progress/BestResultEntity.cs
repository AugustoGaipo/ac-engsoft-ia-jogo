namespace Swapping.Api.Progress;

/// <summary>
/// The best result of one profile in one level for one ranking mode (B1): at most 2 rows per
/// (profile, level), whatever the number of uploads. Nothing else is stored about a run.
/// </summary>
public sealed class BestResultEntity
{
    public Guid ProfileId { get; set; }

    public string LevelId { get; set; } = "";

    public RankingMode Mode { get; set; }

    public int TimeMs { get; set; }

    public int Swaps { get; set; }

    /// <summary>Stored for information, never ranked (B7).</summary>
    public int Deaths { get; set; }

    /// <summary>Stored for information, never ranked (B7).</summary>
    public int RoomRestarts { get; set; }

    /// <summary>The client's clock (informational only).</summary>
    public DateTimeOffset FinishedAt { get; set; }

    /// <summary>Server time of the first upload of this result: the last tie-break (B5).</summary>
    public DateTimeOffset AchievedAt { get; set; }

    /// <summary>Slot for the ghost recording (B8). Always null in this version.</summary>
    public Guid? GhostId { get; set; }

    public ResultScore Score => new(TimeMs, Swaps, AchievedAt);
}
