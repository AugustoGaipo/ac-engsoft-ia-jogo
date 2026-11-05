using Godot;
using Swapping.Game.Pieces;
using Swapping.GameLogic;
using Swapping.Levels;
using Swapping.Levels.Model;

namespace Swapping.Game;

/// <summary>
/// Entry scene: builds the player, the swap controller, the run flow and the HUD, then reads the user
/// arguments (after <c>--</c>):
/// <c>--smoke-test &lt;levelId&gt;</c> runs the scripted checks and quits with 0 or 1;
/// <c>--screenshot &lt;room&gt; &lt;file.png&gt;</c> saves what a room's spawn sees and quits;
/// <c>--preview-shot &lt;file.png&gt;</c> saves the editor preview seen from above;
/// <c>--sandbox</c> plays the test room; <c>--level &lt;id&gt;</c> plays that level directly;
/// <c>--smoke-menu</c> runs the headless menu checks; with no arguments the menu opens.
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
        Flow.ReturnAttributes = Swaps.ReturnAttributes;
        Hud = new Hud { Name = "Hud" };
        AddChild(Hud);
        Hud.Setup(Player, Swaps, Flow);

        var args = OS.GetCmdlineUserArgs();
        // Scripted runs must never touch the player's real progress file.
        if (args.Contains("--smoke-test") || args.Contains("--smoke-menu"))
            ProgressPath = SmokeProgressPath;
        SetupMenu();

        var smoke = Array.IndexOf(args, "--smoke-test");
        if (smoke >= 0)
        {
            _ = RunSmokeTest(smoke + 1 < args.Length ? args[smoke + 1] : "level-1");
            return;
        }

        if (args.Contains("--smoke-menu"))
        {
            ShowMenu();
            _ = RunSmokeMenu();
            return;
        }

        // --menu-shot <file.png>: saves the main menu (with --resolution WxH to check other window sizes).
        var menuShot = Array.IndexOf(args, "--menu-shot");
        if (menuShot >= 0 && menuShot + 1 < args.Length)
        {
            ShowMenu();
            _ = SaveScreenshot(args[menuShot + 1]);
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
            var wide = args.Contains("--sandbox") ? new Vector3(18, 46, 76) : new Vector3(24, 34, 18);
            top.LookAtFromPosition(wide, wide with { Y = 0, Z = args.Contains("--sandbox") ? 30 : 5 }, Vector3.Up);
            _ = SaveScreenshot(args[preview + 1]);
            return;
        }

        var direct = args.Contains("--sandbox") || Array.IndexOf(args, "--level") >= 0 || Array.IndexOf(args, "--screenshot") >= 0;
        if (!direct)
        {
            ShowMenu();
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
            // --pause-keys: the pause menu with the key list open.
            if (args.Contains("--pause-keys"))
                Flow.ShowPauseKeys();
            _ = SaveScreenshot(args[shot + 2]);
        }
    }

    private void LoadChosenLevel(string[] args)
    {
        var level = Array.IndexOf(args, "--level");
        if (args.Contains("--sandbox"))
            _runner.LoadSandbox();
        else
            _runner.LoadLevel(level >= 0 && level + 1 < args.Length ? args[level + 1] : "level-1");
    }

    // ---- Menu, progress and moving between levels ----

    public const string SmokeProgressPath = "user://progress.smoke.json";

    public Menu Menu { get; private set; } = null!;

    public LevelProgress Progress { get; private set; } = null!;

    /// <summary>Where the progress is saved; scripted runs use a separate file.</summary>
    public string ProgressPath { get; private set; } = ProgressStore.DefaultPath;

    /// <summary>Whether a level has its scenery scene. Replaceable so tests can pretend a scene exists.</summary>
    public Func<LevelFile, bool> HasScene { get; set; } = level => ResourceLoader.Exists(level.Scene);

    private IReadOnlyList<LevelFile> _catalogLevels = [];

    private void SetupMenu()
    {
        var catalog = LevelCatalog.Load(GameFiles.LevelsDirectory());
        if (catalog.Errors.Count > 0)
            throw new InvalidOperationException(string.Join("; ", catalog.Errors.Select(e => $"{e.Path}: {e.Code} {e.Message}")));
        _catalogLevels = catalog.Levels;
        if (ProgressPath == SmokeProgressPath)
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(ProgressPath));
        Progress = ProgressStore.Load(ProgressPath, _catalogLevels.Select(l => l.Id).ToList());

        Menu = new Menu { Name = "Menu" };
        AddChild(Menu);
        Menu.Setup(_catalogLevels, Progress, HasSceneOf);
        // Online (user, 2026-11-05): the smoke test uses its own account file, started empty.
        var accountPath = ProgressPath == SmokeProgressPath ? "user://account.smoke.json" : "user://account.json";
        if (ProgressPath == SmokeProgressPath)
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(accountPath));
        Online = new Online(accountPath);
        Menu.Online = Online;
        Menu.LevelRequested += StartLevel;
        Menu.SandboxRequested += StartSandbox;
        Flow.MenuRequested += ShowMenu;
        Flow.NextLevelRequested += () =>
        {
            if (Flow.Result is { } result && NextPlayable(result.LevelId) is { } next)
                StartLevel(next);
        };
        Flow.LevelFinished += OnLevelFinished;
    }

    private bool HasSceneOf(string levelId) => _catalogLevels.FirstOrDefault(l => l.Id == levelId) is { } level && HasScene(level);

    /// <summary>The level after this one, if it can be played now.</summary>
    public string? NextPlayable(string levelId) => LevelAccess.NextPlayable(levelId, Progress, HasSceneOf);

    private void OnLevelFinished(RunResult result)
    {
        Progress.Record(result);
        ProgressStore.Save(ProgressPath, Progress);
        Flow.SetNextLevelAvailable(NextPlayable(result.LevelId) is not null);
        _ = SendResult(result);
    }

    public Online Online { get; private set; } = null!;

    /// <summary>The finished level goes to the ranking when logged in; the end screen says how it went.</summary>
    private async Task SendResult(RunResult result)
    {
        Flow.SetOnlineLine("Enviando ao ranking...");
        var line = await Online.SendAsync(result);
        if (Flow.Result == result)
            Flow.SetOnlineLine(line);
    }

    /// <summary>Back to the menu: the level is unloaded and nothing of the game keeps running.</summary>
    public void ShowMenu()
    {
        Flow.ClearForMenu();
        _runner.Unload();
        SetPlaying(false);
        Menu.Open();
    }

    public void StartLevel(string levelId)
    {
        Menu.Close();
        _runner.LoadLevel(levelId);
        BeginPlaying();
    }

    public void StartSandbox()
    {
        Menu.Close();
        _runner.LoadSandbox();
        BeginPlaying();
    }

    private void BeginPlaying()
    {
        SetPlaying(true);
        Flow.StartLevel();
        if (DisplayServer.GetName() != "headless")
            Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    /// <summary>The player, swaps, run flow and HUD only work while a level is on screen.</summary>
    private void SetPlaying(bool playing)
    {
        var mode = playing ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
        Player.ProcessMode = mode;
        Swaps.ProcessMode = mode;
        Hud.ProcessMode = mode;
        Hud.Visible = playing;
        Flow.ProcessMode = playing ? ProcessModeEnum.Always : ProcessModeEnum.Disabled;
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

    private async Task RunSmokeMenu()
    {
        var smoke = new SmokeTest();
        AddChild(smoke);
        var code = await smoke.RunMenu(this);
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
