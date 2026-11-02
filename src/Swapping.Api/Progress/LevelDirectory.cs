using Swapping.Levels;

namespace Swapping.Api.Progress;

/// <summary>Configuration section <c>Progress</c>.</summary>
public sealed class ProgressOptions
{
    public const string Section = "Progress";

    /// <summary>The <c>levels/</c> folder, absolute or relative to the API's content root.</summary>
    public string LevelsDirectory { get; set; } = Path.Combine("..", "..", "levels");
}

/// <summary>One level as the API knows it.</summary>
public sealed record LevelInfo(string LevelId, int Number, string Name, int Rooms);

/// <summary>
/// The catalog, loaded once at startup with <see cref="LevelCatalog.Load"/> (the same file the game
/// reads). It is the only source of level order and ids (allow-list for routes, P4).
/// </summary>
public sealed class LevelDirectory
{
    private readonly Dictionary<string, int> indexById;

    public LevelDirectory(IReadOnlyList<LevelInfo> levels)
    {
        Levels = levels;
        indexById = levels.Select((level, index) => (level.LevelId, index)).ToDictionary(x => x.LevelId, x => x.index, StringComparer.Ordinal);
    }

    /// <summary>The levels in play order.</summary>
    public IReadOnlyList<LevelInfo> Levels { get; }

    /// <summary>Loads the catalog; any error stops the API from starting.</summary>
    public static LevelDirectory Load(string directory)
    {
        var catalog = LevelCatalog.Load(directory);
        if (catalog.Errors.Count > 0)
            throw new InvalidOperationException(
                $"The level catalog in '{directory}' has errors:{Environment.NewLine}" +
                string.Join(Environment.NewLine, catalog.Errors.Select(e => $"  {e.Path}: {e.Code} {e.Message}")));
        if (catalog.Levels.Count == 0)
            throw new InvalidOperationException($"The level catalog in '{directory}' is empty.");

        return new LevelDirectory(catalog.Levels.Select(l => new LevelInfo(l.Id, l.Number, l.Name, l.Rooms.Count)).ToList());
    }

    public LevelInfo? Find(string levelId) => indexById.TryGetValue(levelId, out var i) ? Levels[i] : null;

    /// <summary>The level before this one in the catalog, or null for the first level.</summary>
    public LevelInfo? Previous(string levelId) => indexById.TryGetValue(levelId, out var i) && i > 0 ? Levels[i - 1] : null;

    /// <summary>The level after this one in the catalog, or null for the last level.</summary>
    public LevelInfo? Next(string levelId) =>
        indexById.TryGetValue(levelId, out var i) && i + 1 < Levels.Count ? Levels[i + 1] : null;
}
