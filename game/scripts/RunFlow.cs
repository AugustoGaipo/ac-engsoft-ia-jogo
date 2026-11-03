using Godot;
using Swapping.Game.Pieces;
using Swapping.GameLogic;

namespace Swapping.Game;

/// <summary>
/// One run of a level (SPEC-game, "Rooms, deaths and the run"; Task 8): rooms in order through their
/// exits, deaths (kill zone, a fall over 12 m/s, no room after a swap) with a short fade and a room
/// restart that keeps time and swaps, the pause menu (Esc) and the end screen. The totals live in the
/// pure <see cref="RunTracker"/>.
/// </summary>
public partial class RunFlow : Node
{
    public const double FadeSeconds = 0.5;

    private LevelRunner _runner = null!;
    private Player _player = null!;
    private ColorRect _fade = null!;
    private Control _pauseMenu = null!;
    private Control _endScreen = null!;
    private Label _endText = null!;

    public RunTracker Run { get; private set; } = new("none");

    /// <summary>Set when the exit of the last room was crossed.</summary>
    public RunResult? Result { get; private set; }

    public bool IsDying { get; private set; }

    public bool IsPaused => _pauseMenu.Visible;

    public bool EndScreenVisible => _endScreen.Visible;

    public string EndText => _endText.Text;

    /// <summary>What a finished run hands to <c>sync</c> (later).</summary>
    public event Action<RunResult>? LevelFinished;

    public void Setup(LevelRunner runner, Player player)
    {
        _runner = runner;
        _player = player;
        ProcessMode = ProcessModeEnum.Always; // the pause menu must work while the tree is paused
        BuildOverlay();
    }

    // ---- Rooms ----

    /// <summary>A new run from the first room.</summary>
    public void StartLevel()
    {
        Result = null;
        _endScreen.Visible = false;
        Run = new RunTracker(_runner.Level.Id);
        GoToRoom(_runner.Level.Rooms[0].Id);
    }

    /// <summary>Builds a room, puts the player at its spawn and counts it in the run.</summary>
    public void GoToRoom(string roomId)
    {
        _runner.BuildRoom(roomId);
        _player.SpawnAt(_runner.Room.Spawn);
        Run.EnterRoom(_runner.World);
    }

    /// <summary>Death or menu: the room again from the file; time and swaps stay (SC 6).</summary>
    public void RestartRoom(bool death)
    {
        if (death)
            Run.RecordDeath();
        _runner.BuildRoom(_runner.Room.Id);
        _player.SpawnAt(_runner.Room.Spawn);
        Run.RestartRoom(_runner.World);
    }

    /// <summary>Back to the first room with everything at zero: a new run (SC 7).</summary>
    public void RestartLevel()
    {
        SetPaused(false);
        Result = null;
        _endScreen.Visible = false;
        _runner.BuildRoom(_runner.Level.Rooms[0].Id);
        _player.SpawnAt(_runner.Room.Spawn);
        Run.RestartLevel(_runner.World);
        Input.MouseMode = DisplayServer.GetName() == "headless" ? Input.MouseMode : Input.MouseModeEnum.Captured;
    }

    /// <summary>The player dies: a short fade to black, then the room restarts.</summary>
    public async void Die(string cause)
    {
        if (IsDying || Result is not null)
            return;
        IsDying = true;
        GD.Print($"[game] death ({cause}) in room {_runner.Room.Id}");
        var tween = CreateTween();
        tween.TweenProperty(_fade, "color:a", 1f, FadeSeconds / 2);
        await ToSignal(tween, Tween.SignalName.Finished);
        RestartRoom(death: true);
        tween = CreateTween();
        tween.TweenProperty(_fade, "color:a", 0f, FadeSeconds / 2);
        await ToSignal(tween, Tween.SignalName.Finished);
        IsDying = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_runner?.Room is null || Result is not null || GetTree().Paused)
            return;
        Run.Tick(delta);
        if (IsDying)
            return;

        var center = _player.Center.ToLevel();
        if (_runner.Room.KillZones.Any(z => z.Box.Contains(center)) || _player.GlobalPosition.Y < -10)
        {
            Die("zona de morte");
            return;
        }
        if (_player.LandingSpeed is { } landing && FallDeath.Kills(landing))
        {
            Die($"queda a {landing:0.0} m/s");
            return;
        }
        foreach (var body in _runner.Bodies)
        {
            // An object in a pit pops back to its start (keeping its swapped mass variable).
            if (_runner.Room.KillZones.Any(z => z.Box.Contains(body.GlobalPosition.ToLevel())))
                body.Teleport(body.Def.Position.ToGodot(), body.Def.Velocity.ToGodot());
        }
        if (_runner.Room.Exit.Contains(center))
            NextRoom();
    }

    private void NextRoom()
    {
        var rooms = _runner.Level.Rooms;
        var index = rooms.ToList().FindIndex(r => r.Id == _runner.Room.Id);
        if (index + 1 < rooms.Count)
        {
            _runner.BuildRoom(rooms[index + 1].Id);
            Run.EnterRoom(_runner.World);
            GD.Print($"[game] entered room {_runner.Room.Id}");
            return;
        }
        Finish();
    }

    private void Finish()
    {
        Result = Run.Finish(DateTimeOffset.UtcNow);
        _endText.Text =
            $"{_runner.Level.Name}: concluído!\n\nTempo  {RunClock.Format(Result.TimeMs)}\nTrocas  {Result.Swaps}\n" +
            $"Mortes  {Result.Deaths}   ·   Reinícios  {Result.RoomRestarts}\n\nR: jogar de novo   ·   Esc: sair";
        _endScreen.Visible = true;
        GD.Print($"[game] level finished: {Result}");
        LevelFinished?.Invoke(Result);
    }

    // ---- Input, pause menu and end screen ----

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Result is not null)
        {
            if (@event.IsActionPressed(Controls.Name(GameAction.RestartRoom)))
                RestartLevel();
            else if (@event.IsActionPressed(Controls.Name(GameAction.Pause)))
                GetTree().Quit();
            return;
        }
        if (@event.IsActionPressed(Controls.Name(GameAction.Pause)))
            SetPaused(!IsPaused);
        else if (!IsPaused && !IsDying && @event.IsActionPressed(Controls.Name(GameAction.RestartRoom)))
            RestartRoom(death: false);
    }

    public void SetPaused(bool paused)
    {
        if (paused == IsPaused)
            return;
        _pauseMenu.Visible = paused;
        GetTree().Paused = paused;
        if (paused)
            Run.Pause();
        else
            Run.Resume();
        if (DisplayServer.GetName() != "headless")
            Input.MouseMode = paused ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
    }

    private void BuildOverlay()
    {
        var layer = new CanvasLayer { Layer = 10, ProcessMode = ProcessModeEnum.Always };
        AddChild(layer);

        _fade = new ColorRect { Color = new Color(0, 0, 0, 0), MouseFilter = Control.MouseFilterEnum.Ignore };
        _fade.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        layer.AddChild(_fade);

        _pauseMenu = Panel("Pausa");
        var buttons = _pauseMenu.GetNode<VBoxContainer>("Box");
        AddButton(buttons, "Continuar", () => SetPaused(false));
        AddButton(buttons, "Reiniciar sala", () => { SetPaused(false); RestartRoom(death: false); });
        AddButton(buttons, "Reiniciar nível", RestartLevel);
        AddButton(buttons, "Sair", () => GetTree().Quit());
        layer.AddChild(_pauseMenu);

        _endScreen = Panel("");
        _endText = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _endText.AddThemeFontSizeOverride("font_size", 28);
        _endScreen.GetNode<VBoxContainer>("Box").AddChild(_endText);
        layer.AddChild(_endScreen);
    }

    private static Control Panel(string title)
    {
        var dim = new ColorRect { Color = new Color(0, 0, 0, 0.6f), Visible = false };
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var box = new VBoxContainer
        {
            Name = "Box",
            GrowHorizontal = Control.GrowDirection.Both,
            GrowVertical = Control.GrowDirection.Both,
        };
        box.SetAnchorsPreset(Control.LayoutPreset.Center);
        box.AddThemeConstantOverride("separation", 12);
        dim.AddChild(box);
        if (title.Length > 0)
        {
            var label = new Label { Text = title, HorizontalAlignment = HorizontalAlignment.Center };
            label.AddThemeFontSizeOverride("font_size", 40);
            box.AddChild(label);
        }
        return dim;
    }

    private static void AddButton(VBoxContainer box, string text, Action onPressed)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(260, 48) };
        button.AddThemeFontSizeOverride("font_size", 24);
        button.Pressed += onPressed;
        box.AddChild(button);
    }
}
