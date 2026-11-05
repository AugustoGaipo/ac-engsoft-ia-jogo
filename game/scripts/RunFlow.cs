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

    /// <summary>The death: 0.6 s falling over into dark red, 0.4 s held with the cause, then the room comes back.</summary>
    public const double DeathFallSeconds = 0.6;
    public const double DeathHoldSeconds = 0.4;

    private Label _deathLabel = null!;
    private AudioStreamPlayer _deathSound = null!;

    /// <summary>The text of the last death screen ("VOCÊ MORREU" + the cause).</summary>
    public string DeathText { get; private set; } = "";

    public bool DeathScreenVisible => _deathLabel.Visible;

    private LevelRunner _runner = null!;
    private Player _player = null!;
    private ColorRect _fade = null!;
    private Control _pauseMenu = null!;
    private Control _endScreen = null!;
    private Label _endText = null!;
    private Button _nextButton = null!;

    public RunTracker Run { get; private set; } = new("none");

    /// <summary>Set when the exit of the last room was crossed.</summary>
    public RunResult? Result { get; private set; }

    public bool IsDying { get; private set; }

    public bool IsPaused => _pauseMenu.Visible;

    public bool EndScreenVisible => _endScreen.Visible;

    public string EndText => _endText.Text;

    public Control PausePanel => _pauseMenu;

    public Control EndPanel => _endScreen;

    public bool NextButtonVisible => _nextButton.Visible;

    /// <summary>The player chose "Menu" (pause or end screen).</summary>
    public event Action? MenuRequested;

    /// <summary>The player chose "Próximo nível" on the end screen.</summary>
    public event Action? NextLevelRequested;

    /// <summary>What a finished run hands to <c>sync</c> (later).</summary>
    public event Action<RunResult>? LevelFinished;

    public void Setup(LevelRunner runner, Player player)
    {
        _runner = runner;
        _player = player;
        ProcessMode = ProcessModeEnum.Always; // the pause menu must work while the tree is paused
        BuildOverlay();
    }

    /// <summary>Shows or hides "Próximo nível" on the end screen (the caller knows the progress).</summary>
    public void SetNextLevelAvailable(bool available) => _nextButton.Visible = available;

    /// <summary>Back to a clean state before the menu: no pause, no end screen, no fade.</summary>
    public void ClearForMenu()
    {
        SetPaused(false);
        Result = null;
        CancelDeath();
        _endScreen.Visible = false;
        _nextButton.Visible = false;
        _fade.Color = _fade.Color with { A = 0 };
    }

    // ---- Rooms ----

    /// <summary>A new run from the first room.</summary>
    public void StartLevel()
    {
        CancelDeath();
        Result = null;
        _endScreen.Visible = false;
        Run = new RunTracker(_runner.Level.Id);
        _runner.BuildAll(_runner.Level.Rooms[0].Id);
        _player.SpawnAt(_runner.Room.Spawn);
        Run.EnterRoom(_runner.World);
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
        CancelDeath();
        SetPaused(false);
        Result = null;
        _endScreen.Visible = false;
        _runner.BuildAll(_runner.Level.Rooms[0].Id);
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
        var death = ++_death;
        GD.Print($"[game] death ({cause}) in room {_runner.Room.Id}");
        // So nobody thinks the room just reset (user's request): the view falls over, the screen turns dark red,
        // a falling tone plays and the cause is written in the middle, then the room comes back.
        Run.RecordDeath();
        DeathText = Feedback.DeathText(cause);
        _deathLabel.Text = DeathText;
        _deathLabel.Visible = true;
        _deathSound.Play();
        _fade.Color = new Color(0.35f, 0, 0, 0);
        _deathTween = CreateTween().SetParallel();
        _deathTween.TweenProperty(_fade, "color:a", 0.9f, DeathFallSeconds);
        _deathTween.TweenProperty(_player, "DeathFall", 1f, DeathFallSeconds).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
        await ToSignal(_deathTween, Tween.SignalName.Finished);
        await ToSignal(GetTree().CreateTimer(DeathHoldSeconds, processAlways: false), SceneTreeTimer.SignalName.Timeout);
        // Menu, new run or level restart during the death: that death is over (CancelDeath), do not touch the new room.
        if (death != _death || _runner.Room is null)
            return;
        RestartRoom(death: false);                         // the death was counted when it happened
        _deathLabel.Visible = false;
        _deathTween = CreateTween();
        _deathTween.TweenProperty(_fade, "color:a", 0f, FadeSeconds / 2);
        await ToSignal(_deathTween, Tween.SignalName.Finished);
        if (death != _death)
            return;
        _fade.Color = new Color(0, 0, 0, 0);
        IsDying = false;
    }

    private int _death;
    private Tween? _deathTween;

    /// <summary>Stops a death in progress and clears its looks (menu, new run, level restart).</summary>
    private void CancelDeath()
    {
        _death++;
        _deathTween?.Kill();
        _deathTween = null;
        IsDying = false;
        _deathLabel.Visible = false;
        _fade.Color = new Color(0, 0, 0, 0);
        _player.DeathFall = 0;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_runner?.Room is null || Result is not null || GetTree().Paused)
            return;
        Run.Tick(delta);
        if (IsDying)
            return;

        var center = _player.Center.ToLevel();
        if (_runner.AllKillZones.Any(z => z.Box.Contains(center)))
        {
            Die("zona de morte");
            return;
        }
        // Rooms have no ceiling: a ball thrown over a wall and swapped with took the player out of the map
        // (user's play test). Outside every room of the level = death.
        if (!_runner.Level.Rooms.Any(r => InsideMap(r.Bounds, center)))
        {
            Die("saiu do mapa");
            return;
        }
        if (_player.LandingSpeed is { } landing && FallDeath.Kills(landing))
        {
            Die($"queda a {landing:0.0} m/s");
            return;
        }
        foreach (var (body, room) in _runner.AllBodies)
        {
            // An object in a pit or out of its room (over a wall) pops back to its start (keeping its swapped mass variable).
            var at = body.GlobalPosition.ToLevel();
            if (room.KillZones.Any(z => z.Box.Contains(at)) || !InsideMap(room.Bounds, at))
                body.Teleport(body.Def.Position.ToGodot(), body.Def.Velocity.ToGodot());
        }
        if (_runner.Room.Exit.Contains(center))
            NextRoom();
    }

    /// <summary>Inside the room's bounds by the sides and from below; above is fine (no ceiling, gravity brings it back).</summary>
    private static bool InsideMap(Swapping.Levels.Model.Box bounds, Swapping.Rules.Model.Vec3 p) =>
        p.X >= bounds.Min.X && p.X <= bounds.Max.X && p.Z >= bounds.Min.Z && p.Z <= bounds.Max.Z && p.Y >= bounds.Min.Y;

    private void NextRoom()
    {
        var rooms = _runner.Level.Rooms;
        var index = rooms.ToList().FindIndex(r => r.Id == _runner.Room.Id);
        if (index + 1 < rooms.Count)
        {
            _runner.EnterRoom(rooms[index + 1].Id);   // the next room is already alive; only the checkpoint moves
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
            $"Mortes  {Result.Deaths}   ·   Reinícios  {Result.RoomRestarts}\n\nR: jogar de novo   ·   Esc: menu";
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
                MenuRequested?.Invoke();
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

        _deathLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _deathLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _deathLabel.AddThemeFontSizeOverride("font_size", 56);
        _deathLabel.AddThemeColorOverride("font_color", new Color(1, 0.85f, 0.85f));
        _deathLabel.AddThemeConstantOverride("outline_size", 10);
        _deathLabel.AddThemeColorOverride("font_outline_color", new Color(0.2f, 0, 0));
        layer.AddChild(_deathLabel);
        _deathSound = Sfx.Player(this, Sfx.Death, "DeathSound");

        _pauseMenu = Panel("Pausa");
        var buttons = BoxOf(_pauseMenu);
        AddButton(buttons, "Continuar", () => SetPaused(false));
        AddButton(buttons, "Reiniciar sala", () => { SetPaused(false); RestartRoom(death: false); });
        AddButton(buttons, "Reiniciar nível", RestartLevel);
        AddButton(buttons, "Menu", () => MenuRequested?.Invoke());
        AddButton(buttons, "Sair", () => GetTree().Quit());
        layer.AddChild(_pauseMenu);

        _endScreen = Panel("Relatório do teste");
        _endText = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _endText.AddThemeFontSizeOverride("font_size", 28);
        var endBox = BoxOf(_endScreen);
        endBox.AddChild(_endText);
        _nextButton = AddButton(endBox, "Próximo nível", () => NextLevelRequested?.Invoke());
        _nextButton.Visible = false;
        AddButton(endBox, "Menu", () => MenuRequested?.Invoke());
        layer.AddChild(_endScreen);
    }

    private static Control Panel(string title)
    {
        var dim = new ColorRect { Color = new Color(0.04f, 0.06f, 0.09f, 0.7f), Visible = false, Theme = LabTheme.Get() };
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var box = new VBoxContainer { Name = "Box" };
        box.AddThemeConstantOverride("separation", 12);
        box.AddChild(LabTheme.Sign(title, 36));
        var holder = LabTheme.Sheet(box);
        holder.Name = "Holder";
        dim.AddChild(holder);
        return dim;
    }

    private static VBoxContainer BoxOf(Control panel) => panel.GetNode<VBoxContainer>("Holder/Sheet/Box");

    private static Button AddButton(VBoxContainer box, string text, Action onPressed)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(260, 48) };
        button.AddThemeFontSizeOverride("font_size", 24);
        button.Pressed += onPressed;
        box.AddChild(button);
        return button;
    }
}
