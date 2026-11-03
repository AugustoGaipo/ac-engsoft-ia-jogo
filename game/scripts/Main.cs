using Godot;
using Swapping.Game.Pieces;
using Swapping.GameLogic;

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

    public Player Player { get; private set; } = null!;

    public SwapController Swaps { get; private set; } = null!;

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
        Hud = new Hud { Name = "Hud" };
        AddChild(Hud);
        Hud.Setup(Player, Swaps);

        var args = OS.GetCmdlineUserArgs();
        var smoke = Array.IndexOf(args, "--smoke-test");
        if (smoke >= 0)
        {
            _ = RunSmokeTest(smoke + 1 < args.Length ? args[smoke + 1] : "level-1");
            return;
        }

        // --sandbox: the test room instead of level 1.
        if (args.Contains("--sandbox"))
            _runner.LoadSandbox();
        else
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

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(Controls.Name(GameAction.RestartRoom)) && _runner.Room is not null)
            ShowRoom(_runner.Room.Id);
        else if (@event.IsActionPressed(Controls.Name(GameAction.Pause)))
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_runner.Room is null)
            return;
        // Provisional until Task 8 (deaths): falling into a pit restarts the room.
        if (Player.GlobalPosition.Y < -2.5f && !OS.GetCmdlineUserArgs().Contains("--smoke-test"))
        {
            ShowRoom(_runner.Room.Id);
            return;
        }
        // Crossing the room's exit builds the next room; the player keeps walking (part of Task 8).
        if (_runner.Room.Exit.Contains(Player.Center.ToLevel()))
            EnterNextRoom();
    }

    /// <summary>Raised when the exit of the last room is crossed.</summary>
    public event Action? LevelFinished;

    private void EnterNextRoom()
    {
        var rooms = _runner.Level.Rooms;
        var index = rooms.ToList().FindIndex(r => r.Id == _runner.Room.Id);
        if (index + 1 < rooms.Count)
        {
            _runner.BuildRoom(rooms[index + 1].Id);
            GD.Print($"[game] entered room {_runner.Room.Id}");
        }
        else if (!_finished)
        {
            _finished = true;
            GD.Print("[game] level finished");
            LevelFinished?.Invoke();
        }
    }

    private bool _finished;

    /// <summary>Builds (or rebuilds) a room and puts the player at its spawn.</summary>
    public void ShowRoom(string roomId)
    {
        _finished = false;
        _runner.BuildRoom(roomId);
        Player.SpawnAt(_runner.Room.Spawn);
    }

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
