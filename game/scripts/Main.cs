using Godot;

namespace Swapping.Game;

/// <summary>
/// Entry scene. Reads the user arguments (after <c>--</c>):
/// <c>--smoke-test &lt;levelId&gt;</c> runs the scripted checks and quits with 0 or 1;
/// <c>--screenshot &lt;room&gt; &lt;file.png&gt;</c> builds a room of level 1, saves what the spawn sees and quits.
/// Without arguments the game starts in room 1.1.
/// </summary>
public partial class Main : Node3D
{
    private LevelRunner _runner = null!;

    public override void _Ready()
    {
        _runner = GetNode<LevelRunner>("LevelRunner");
        var args = OS.GetCmdlineUserArgs();

        var smoke = Array.IndexOf(args, "--smoke-test");
        if (smoke >= 0)
        {
            _ = RunSmokeTest(smoke + 1 < args.Length ? args[smoke + 1] : "level-1");
            return;
        }

        _runner.LoadLevel("level-1");
        var shot = Array.IndexOf(args, "--screenshot");
        if (shot >= 0 && shot + 2 < args.Length)
        {
            ShowRoom(args[shot + 1]);
            _ = SaveScreenshot(args[shot + 2]);
            return;
        }

        ShowRoom(_runner.Level.Rooms[0].Id);
    }

    private void ShowRoom(string roomId)
    {
        _runner.BuildRoom(roomId);
        GetNode<Camera3D>("Camera").GlobalTransform = _runner.SpawnEye();
    }

    private async Task RunSmokeTest(string levelId)
    {
        var smoke = new SmokeTest();
        AddChild(smoke);
        var code = await smoke.Run(levelId, _runner);
        GetTree().Quit(code);
    }

    private async Task SaveScreenshot(string path)
    {
        // Let physics settle and the scene draw.
        for (var i = 0; i < 30; i++)
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var error = GetViewport().GetTexture().GetImage().SavePng(path);
        GD.Print(error == Error.Ok ? $"[screenshot] saved {path}" : $"[screenshot] FAIL {error}");
        GetTree().Quit(error == Error.Ok ? 0 : 1);
    }
}
