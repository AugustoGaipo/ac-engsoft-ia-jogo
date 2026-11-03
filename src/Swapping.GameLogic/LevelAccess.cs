namespace Swapping.GameLogic;

/// <summary>What a level card in the menu shows.</summary>
public enum LevelStatus
{
    /// <summary>Can be played.</summary>
    Available,

    /// <summary>The previous level is not finished yet (normal padlock).</summary>
    Locked,

    /// <summary>The level has no scenery scene yet (<c>res://levels/level_N.tscn</c> missing): "Em construção".</summary>
    UnderConstruction,
}

/// <summary>
/// The menu rules of SPEC-game slice 2 that join <see cref="LevelProgress"/> with whether the level's
/// scene exists. "Under construction" wins over "locked": a level nobody can play yet is not waiting on progress.
/// </summary>
public static class LevelAccess
{
    public static LevelStatus StatusOf(string levelId, LevelProgress progress, bool hasScene) =>
        !hasScene ? LevelStatus.UnderConstruction
        : progress.IsUnlocked(levelId) ? LevelStatus.Available
        : LevelStatus.Locked;

    /// <summary>The first playable level not finished; if none, the last playable one; null when nothing is playable.</summary>
    public static string? ContinueLevel(LevelProgress progress, IReadOnlyList<string> catalog, Func<string, bool> hasScene)
    {
        var playable = catalog.Where(id => StatusOf(id, progress, hasScene(id)) == LevelStatus.Available).ToList();
        return playable.FirstOrDefault(id => !progress.IsFinished(id)) ?? playable.LastOrDefault();
    }

    /// <summary>The level after <paramref name="levelId"/> if it can be played now, else null (last level, locked or under construction).</summary>
    public static string? NextPlayable(string levelId, LevelProgress progress, Func<string, bool> hasScene) =>
        progress.NextLevel(levelId) is { } next && StatusOf(next, progress, hasScene(next)) == LevelStatus.Available ? next : null;
}
