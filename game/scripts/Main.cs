using Godot;
using Swapping.Game.Pieces;
using Swapping.GameLogic;

namespace Swapping.Game;

/// <summary>
/// Entry scene: builds the player, the swap controller, the run flow and the HUD, then reads the user
/// arguments (after <c>--</c>):
/// <c>--smoke-test &lt;levelId&gt;</c> runs the scripted checks and quits with 0 or 1;
/// <c>--screenshot &lt;room&gt; &lt;file.png&gt;</c> saves what a room's spawn sees and quits;
/// <c>--preview-shot &lt;file.png&gt;</c> saves the editor preview seen from above;
/// <c>--sandbox</c> plays the test room instead of level 1.
/// </summary>
public partial class Main : Node3D
{
    private LevelRunner _runner = null!;

    public Player Player { get; private set; } = null!;

    public SwapController Swaps { get; private set; } = null!;

    public RunFlow Flow { get; private set; } = null!;

    public Hud Hud { get; private set; } = null!;

    public LevelRunner Runner => _runner;

    public override void _Ready()
    {
        RegisterControls();
        _runner = GetNode<LevelRunner>("LevelRunner");
        Player = Player.Create();
        AddChild(Player);
        Swaps = new SwapController { Name = "Swaps" };
        AddChild(Swaps);
        Swaps.Setup(_runner, Player);
        Flow = new RunFlow { Name = "Flow" };
        AddChild(Flow);
        Flow.Setup(_runner, Player);
        Swaps.PlayerDied += () => Flow.Die("sem espaço depois da troca");
        Hud = new Hud { Name = "Hud" };
        AddChild(Hud);
        Hud.Setup(Player, Swaps, Flow);

        var args = OS.GetCmdlineUserArgs();
        var smoke = Array.IndexOf(args, "--smoke-test");
        if (smoke >= 0)
        {
            _ = RunSmokeTest(smoke + 1 < args.Length ? args[smoke + 1] : "level-1");
            return;
        }

        var preview = Array.IndexOf(args, "--preview-shot");
        if (preview >= 0 && preview + 1 < args.Length)
        {
            LevelPreview.ShowInGame = true;
            LoadChosenLevel(args);
            Hud.Visible = false;
            var top = new Camera3D { Current = true, Fov = 70 };
            AddChild(top);
            var wide = args.Contains("--sandbox") ? new Vector3(15, 26, 22) : new Vector3(24, 34, 18);
            top.LookAtFromPosition(wide, wide with { Y = 0, Z = args.Contains("--sandbox") ? 10 : 5 }, Vector3.Up);
            _ = SaveScreenshot(args[preview + 1]);
            return;
        }

        LoadChosenLevel(args);
        Flow.StartLevel();
        var shot = Array.IndexOf(args, "--screenshot");
        if (shot >= 0 && shot + 2 < args.Length)
        {
            ShowRoom(args[shot + 1]);
            // --aim x y z: look at that point (to check outlines and the crosshair in a picture).
            var aim = Array.IndexOf(args, "--aim");
            if (aim >= 0 && aim + 3 < args.Length)
            {
                var point = new Vector3(float.Parse(args[aim + 1], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(args[aim + 2], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(args[aim + 3], System.Globalization.CultureInfo.InvariantCulture));
                var to = point - Player.Eye.Origin;
                Player.Look(Mathf.RadToDeg(Mathf.Atan2(-to.X, -to.Z)), Mathf.Atan2(to.Y, new Vector2(to.X, to.Z).Length()));
            }
            _ = SaveScreenshot(args[shot + 2]);
        }
    }

    private void LoadChosenLevel(string[] args)
    {
        if (args.Contains("--sandbox"))
            _runner.LoadSandbox();
        else
            _runner.LoadLevel("level-1");
    }

    /// <summary>Builds a room and puts the player at its spawn (tests and screenshots).</summary>
    public void ShowRoom(string roomId) => Flow.GoToRoom(roomId);

    /// <summary>Registers the G1 controls of <see cref="GameLogic.Controls"/> in the engine.</summary>
    private static void RegisterControls()
    {
        foreach (var binding in GameLogic.Controls.Bindings)
        {
            var name = GameLogic.Controls.Name(binding.Action);
            if (!Godot.InputMap.HasAction(name))
                Godot.InputMap.AddAction(name);
            InputEvent input = binding.Kind == InputKind.Key
                ? new InputEventKey { PhysicalKeycode = Enum.Parse<Key>(binding.Code) }
                : new InputEventMouseButton { ButtonIndex = Enum.Parse<MouseButton>(binding.Code) };
            Godot.InputMap.ActionAddEvent(name, input);
        }
    }

    private async Task RunSmokeTest(string levelId)
    {
        var smoke = new SmokeTest();
        AddChild(smoke);
        var code = await smoke.Run(levelId, _runner, this);
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
