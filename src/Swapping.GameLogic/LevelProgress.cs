using System.Text.Json;
using System.Text.Json.Nodes;

namespace Swapping.GameLogic;

/// <summary>The best time and the fewest swaps of one level, kept apart (each may come from a different run).</summary>
public sealed record BestRun(int TimeMs, int Swaps);

/// <summary>
/// The local progress of the menu (SPEC-game slice 2): which levels are finished and their two bests.
/// The first catalog level is always open; finishing a level opens the next one in catalog order.
/// Results of unknown or still locked levels are ignored. Saved as JSON by the game in <c>user://</c>.
/// </summary>
public sealed class LevelProgress
{
    public const int SchemaVersion = 1;

    private readonly IReadOnlyList<string> _catalog;
    private readonly Dictionary<string, BestRun> _best = [];

    /// <param name="catalog">Level ids in play order.</param>
    public LevelProgress(IReadOnlyList<string> catalog) => _catalog = catalog;

    /// <summary>True when the text could not be read and this progress started fresh.</summary>
    public bool IsCorrupt { get; private init; }

    public bool IsFinished(string levelId) => _best.ContainsKey(levelId);

    public bool IsUnlocked(string levelId)
    {
        var index = IndexOf(levelId);
        return index == 0 || (index > 0 && IsFinished(_catalog[index - 1]));
    }

    public BestRun? Best(string levelId) => _best.GetValueOrDefault(levelId);

    /// <summary>The level after <paramref name="levelId"/> in the catalog, or null after the last one.</summary>
    public string? NextLevel(string levelId)
    {
        var index = IndexOf(levelId);
        return index >= 0 && index + 1 < _catalog.Count ? _catalog[index + 1] : null;
    }

    /// <summary>The first open level not finished; with every level finished, the last one.</summary>
    public string ContinueLevel() =>
        _catalog.FirstOrDefault(id => IsUnlocked(id) && !IsFinished(id)) ?? _catalog[^1];

    /// <summary>Records a finished run: keeps the lower time and the lower swap count separately.</summary>
    public void Record(RunResult result)
    {
        if (!IsUnlocked(result.LevelId))
            return;
        _best[result.LevelId] = _best.TryGetValue(result.LevelId, out var old)
            ? new BestRun(Math.Min(old.TimeMs, result.TimeMs), Math.Min(old.Swaps, result.Swaps))
            : new BestRun(result.TimeMs, result.Swaps);
    }

    public string ToJson()
    {
        var levels = new JsonObject();
        foreach (var id in _catalog.Where(_best.ContainsKey))
            levels[id] = new JsonObject { ["timeMs"] = _best[id].TimeMs, ["swaps"] = _best[id].Swaps };
        return new JsonObject { ["schemaVersion"] = SchemaVersion, ["levels"] = levels }
            .ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>Reads a saved progress. Any problem gives fresh progress with <see cref="IsCorrupt"/> set.</summary>
    public static LevelProgress FromJson(string text, IReadOnlyList<string> catalog)
    {
        try
        {
            var root = JsonNode.Parse(text)!.AsObject();
            if (root["schemaVersion"]!.GetValue<int>() != SchemaVersion)
                return Corrupt(catalog);
            var progress = new LevelProgress(catalog);
            foreach (var (id, node) in root["levels"]!.AsObject())
            {
                if (!catalog.Contains(id))
                    continue;
                var time = node!["timeMs"]!.GetValue<int>();
                var swaps = node["swaps"]!.GetValue<int>();
                if (time < 0 || swaps < 0)
                    return Corrupt(catalog);
                progress._best[id] = new BestRun(time, swaps);
            }
            return progress;
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or NullReferenceException or FormatException)
        {
            return Corrupt(catalog);
        }
    }

    private static LevelProgress Corrupt(IReadOnlyList<string> catalog) => new(catalog) { IsCorrupt = true };

    private int IndexOf(string levelId)
    {
        for (var i = 0; i < _catalog.Count; i++)
            if (_catalog[i] == levelId)
                return i;
        return -1;
    }
}
