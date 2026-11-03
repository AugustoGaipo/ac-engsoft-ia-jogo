using Godot;
using Swapping.Levels;

namespace Swapping.Game;

/// <summary>
/// Scripted checks on the real game, run headless with <c>--smoke-test &lt;levelId&gt;</c> (SC 10).
/// Prints one line per check and returns the exit code: 0 when every check passed, 1 at the first failure.
/// Each Godot task adds its checks here.
/// </summary>
public sealed class SmokeTest(string levelId)
{
    private int _passed;

    public int Run()
    {
        try
        {
            var directory = ProjectSettings.GlobalizePath("res://levels");
            var catalog = LevelCatalog.Load(directory);
            Check("catalog loads without errors",
                catalog.Errors.Count == 0,
                string.Join("; ", catalog.Errors.Select(e => $"{e.Path}: {e.Code} {e.Message}")));

            var level = catalog.Levels.FirstOrDefault(l => l.Id == levelId);
            Check($"level {levelId} is in the catalog", level is not null, $"ids: {string.Join(", ", catalog.Levels.Select(l => l.Id))}");
            Check($"level {levelId} has rooms", level!.Rooms.Count > 0, "no rooms");
            GD.Print($"[smoke] info {levelId}: {level.Rooms.Count} rooms ({string.Join(", ", level.Rooms.Select(r => r.Id))})");

            GD.Print($"[smoke] PASSED {_passed} checks");
            return 0;
        }
        catch (SmokeFailure failure)
        {
            GD.PrintErr($"[smoke] FAIL {failure.Message}");
            return 1;
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[smoke] FAIL unexpected {exception.GetType().Name}: {exception.Message}");
            return 1;
        }
    }

    private void Check(string name, bool ok, string detail)
    {
        if (!ok)
            throw new SmokeFailure($"{name} — {detail}");
        _passed++;
        GD.Print($"[smoke] ok {name}");
    }

    private sealed class SmokeFailure(string message) : Exception(message);
}
