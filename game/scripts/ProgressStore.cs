using Godot;
using Swapping.GameLogic;

namespace Swapping.Game;

/// <summary>
/// Reads and writes the local progress file (<c>user://progress.json</c>) around the pure
/// <see cref="LevelProgress"/>. An unreadable file is kept as <c>progress.corrupt.json</c> and the
/// player starts fresh (SPEC-game slice 2).
/// </summary>
public static class ProgressStore
{
    public const string DefaultPath = "user://progress.json";

    public static LevelProgress Load(string path, IReadOnlyList<string> catalog)
    {
        if (!Godot.FileAccess.FileExists(path))
            return new LevelProgress(catalog);
        var progress = LevelProgress.FromJson(Godot.FileAccess.GetFileAsString(path), catalog);
        if (progress.IsCorrupt)
        {
            var corrupt = path.Replace(".json", ".corrupt.json");
            DirAccess.RenameAbsolute(ProjectSettings.GlobalizePath(path), ProjectSettings.GlobalizePath(corrupt));
            GD.PrintErr($"[game] {path} unreadable; kept as {corrupt} and started fresh");
        }
        return progress;
    }

    public static void Save(string path, LevelProgress progress)
    {
        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
        if (file is null)
        {
            GD.PrintErr($"[game] could not write {path}: {Godot.FileAccess.GetOpenError()}");
            return;
        }
        file.StoreString(progress.ToJson());
    }
}
