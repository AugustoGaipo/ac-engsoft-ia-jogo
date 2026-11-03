using Godot;

namespace Swapping.Game;

/// <summary>
/// Entry scene. Reads the user arguments (after <c>--</c>):
/// <c>--smoke-test &lt;levelId&gt;</c> runs the scripted checks and quits with 0 or 1;
/// <c>--screenshot &lt;file.png&gt;</c> saves what the camera sees and quits.
/// Without arguments the game starts normally.
/// </summary>
public partial class Main : Node3D
{
    public override void _Ready()
    {
        var args = OS.GetCmdlineUserArgs();
        var smoke = System.Array.IndexOf(args, "--smoke-test");
        if (smoke >= 0)
        {
            var levelId = smoke + 1 < args.Length ? args[smoke + 1] : "level-1";
            var code = new SmokeTest(levelId).Run();
            GetTree().Quit(code);
            return;
        }

        var shot = System.Array.IndexOf(args, "--screenshot");
        if (shot >= 0 && shot + 1 < args.Length)
            _ = SaveScreenshot(args[shot + 1]);
    }

    private async System.Threading.Tasks.Task SaveScreenshot(string path)
    {
        // Wait a few frames so the scene is drawn.
        for (var i = 0; i < 5; i++)
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var error = GetViewport().GetTexture().GetImage().SavePng(path);
        GD.Print(error == Error.Ok ? $"[screenshot] saved {path}" : $"[screenshot] FAIL {error}");
        GetTree().Quit(error == Error.Ok ? 0 : 1);
    }
}
