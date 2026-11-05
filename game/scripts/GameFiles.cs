using Godot;

namespace Swapping.Game;

/// <summary>
/// Reading the game's JSON files in the editor and in an exported game. In an export the files live inside
/// the .pck, where <c>System.IO</c> cannot see them: they are read with Godot's <see cref="Godot.FileAccess"/>, and the
/// level folder is copied to <c>user://levels</c> (every start, so a new version of the game brings its levels).
/// </summary>
public static class GameFiles
{
    public const string LevelsFolder = "res://levels";

    /// <summary>A real folder with <c>catalog.json</c> and the level files, for <c>LevelCatalog.Load</c>.</summary>
    public static string LevelsDirectory()
    {
        if (!OS.HasFeature("template"))
            return ProjectSettings.GlobalizePath(LevelsFolder);
        const string target = "user://levels";
        DirAccess.MakeDirRecursiveAbsolute(target);
        foreach (var file in DirAccess.GetFilesAt(LevelsFolder))
            if (file.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase))
                System.IO.File.WriteAllText(ProjectSettings.GlobalizePath($"{target}/{file}"), ReadText($"{LevelsFolder}/{file}"));
        return ProjectSettings.GlobalizePath(target);
    }

    /// <summary>The text of a <c>res://</c> file (works inside the .pck too); empty if it is missing.</summary>
    public static string ReadText(string path) => Godot.FileAccess.FileExists(path) ? Godot.FileAccess.GetFileAsString(path) : "";
}
