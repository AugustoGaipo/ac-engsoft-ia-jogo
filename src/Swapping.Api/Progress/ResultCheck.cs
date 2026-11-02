namespace Swapping.Api.Progress;

/// <summary>The numbers of a finished run sent by the client (untrusted).</summary>
public sealed record ResultValues(int TimeMs, int Swaps, int Deaths, int RoomRestarts);

/// <summary>
/// Plausibility of a result (B4). The server cannot replay a run (P1), so it only refuses numbers
/// that no real run can produce.
/// </summary>
public static class ResultCheck
{
    public const int MinMsPerRoom = 1_000;
    public const int MaxTimeMs = 86_400_000; // 24 h
    public const int MaxCount = 10_000;

    public static bool IsPlausible(ResultValues values, int rooms) =>
        values.TimeMs >= MinMsPerRoom * rooms &&
        values.TimeMs <= MaxTimeMs &&
        InCountRange(values.Swaps) &&
        InCountRange(values.Deaths) &&
        InCountRange(values.RoomRestarts);

    private static bool InCountRange(int count) => count is >= 0 and <= MaxCount;
}
