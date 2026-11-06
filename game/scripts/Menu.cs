using Godot;
using Swapping.GameLogic;
using Swapping.Levels.Model;

namespace Swapping.Game;

/// <summary>
/// The main menu (SPEC-game slice 2): title, Jogar (continue), Níveis, Mapa de teste, Controles, Sair, and the
/// level-select screen with one card per catalog level. A card is open, shows a padlock (previous level not
/// finished) or "Em construção" (no scene yet) — the rule lives in <see cref="LevelAccess"/>.
/// </summary>
public partial class Menu : CanvasLayer
{
    private LevelProgress _progress = null!;
    private IReadOnlyList<LevelFile> _levels = null!;
    private Func<string, bool> _hasScene = null!;
    private Control _root = null!;
    private VBoxContainer _main = null!;
    private VBoxContainer _levelsScreen = null!;
    private VBoxContainer _controlsScreen = null!;
    private Button _play = null!;
    private readonly Dictionary<string, Button> _cards = [];

    public event Action<string>? LevelRequested;

    public event Action? SandboxRequested;

    public bool IsOpen => _root.Visible;

    public string Screen { get; private set; } = "";

    public IReadOnlyDictionary<string, Button> Cards => _cards;

    public void Setup(IReadOnlyList<LevelFile> levels, LevelProgress progress, Func<string, bool> hasScene)
    {
        _levels = levels;
        _progress = progress;
        _hasScene = hasScene;
        Layer = 20;
        _root = new ColorRect { Color = LabTheme.Wall, Visible = false, Theme = LabTheme.Get() };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);
        _root.AddChild(new LabBackdrop());

        _main = NewScreen("Swapping");
        _play = AddButton(_main, "Jogar", Play);
        AddButton(_main, "Níveis", () => Show("levels"));
        AddButton(_main, "Mapa de teste", () => SandboxRequested?.Invoke());
        AddButton(_main, "Controles", () => Show("controls"));
        AddButton(_main, "Conta", () => Show("account"));
        AddButton(_main, "Ranking", () => Show("ranking"));
        AddButton(_main, "Sair", () => GetTree().Quit());

        _levelsScreen = NewScreen("Níveis");
        _controlsScreen = NewScreen("Controles");
        _controlsScreen.AddChild(ControlsGrid());
        AddButton(_controlsScreen, "Voltar", () => Show("main"));
        BuildOnline();
    }

    /// <summary>The key list (main menu "Controles" and the pause menu).</summary>
    public static GridContainer ControlsGrid()
    {
        var controls = new GridContainer { Columns = 2 };
        controls.AddThemeConstantOverride("h_separation", 18);
        controls.AddThemeConstantOverride("v_separation", 8);
        (string Key, string Action)[] keys =
        [
            ("W A S D", "andar"), ("Shift", "correr"), ("Espaço", "pular"), ("Mouse", "olhar"),
            ("Botão esquerdo", "pegar · com item: toque solta, segurar arremessa"), ("Botão direito", "trocar"),
            ("1  2  3", "escolher a troca (posição, massa, energia)"), ("Roda do mouse", "trocar a propriedade"),
            ("F + mouse", "girar o item segurado"), ("R", "reiniciar a sala"), ("Esc", "pausa"),
        ];
        foreach (var (key, action) in keys)
        {
            controls.AddChild(KeyChip(key));
            controls.AddChild(new Label { Text = action, VerticalAlignment = VerticalAlignment.Center });
        }
        return controls;
    }

    /// <summary>Opens the menu on its first screen (progress may have changed since the last time).</summary>
    public void Open()
    {
        _root.Visible = true;
        Show("main");
        if (DisplayServer.GetName() != "headless")
            Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    public void Close() => _root.Visible = false;

    public void Show(string screen)
    {
        Screen = screen;
        if (screen == "levels")
            RebuildCards();
        _play.Disabled = LevelAccess.ContinueLevel(_progress, Ids(), _hasScene) is null;
        Holder(_main).Visible = screen == "main";
        Holder(_levelsScreen).Visible = screen == "levels";
        Holder(_controlsScreen).Visible = screen == "controls";
        ShowOnline(screen);
    }

    public LevelStatus StatusOf(string levelId) => LevelAccess.StatusOf(levelId, _progress, _hasScene(levelId));

    public static Button? FindButton(Node root, string text) =>
        root.FindChildren("*", "Button", recursive: true, owned: false).OfType<Button>().FirstOrDefault(b => b.Text == text);

    private List<string> Ids() => _levels.Select(l => l.Id).ToList();

    private void Play()
    {
        if (LevelAccess.ContinueLevel(_progress, Ids(), _hasScene) is { } id)
            LevelRequested?.Invoke(id);
    }

    private void RebuildCards()
    {
        foreach (var child in _levelsScreen.GetChildren().Skip(1)) // keep the title
        {
            _levelsScreen.RemoveChild(child);
            child.QueueFree();
        }
        _cards.Clear();
        foreach (var level in _levels)
            _levelsScreen.AddChild(MakeCard(level));
        AddButton(_levelsScreen, "Voltar", () => Show("main"));
    }

    private Button MakeCard(LevelFile level)
    {
        var status = StatusOf(level.Id);
        var best = _progress.Best(level.Id);
        var detail = status switch
        {
            LevelStatus.UnderConstruction => "Em construção",
            LevelStatus.Locked => "Termine o nível anterior",
            _ when best is not null => $"Melhor tempo {RunClock.Format(best.TimeMs)}   ·   menos trocas {best.Swaps}",
            _ => "Ainda não concluído",
        };
        var card = new Button { Name = $"Card_{level.Id}", CustomMinimumSize = new Vector2(520, 84), Disabled = status != LevelStatus.Available };
        card.Pressed += () => OnCard(level.Id);
        var label = new Label { Text = $"{level.Number} · {level.Name}\n{detail}", Position = new Vector2(26, 12), MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", 22);
        if (status != LevelStatus.Available)
            label.Modulate = new Color(1, 1, 1, 0.55f);
        card.AddChild(label);
        if (status != LevelStatus.Available)
            card.AddChild(new CardIcon { Kind = status, Position = new Vector2(450, 16), MouseFilter = Control.MouseFilterEnum.Ignore });
        _cards[level.Id] = card;
        return card;
    }

    private void OnCard(string levelId)
    {
        if (StatusOf(levelId) == LevelStatus.Available)
            LevelRequested?.Invoke(levelId);
    }

    private VBoxContainer NewScreen(string title)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 14);
        box.AddChild(LabTheme.Sign(title, title == "Swapping" ? 60 : 40));
        var holder = LabTheme.Sheet(box);
        holder.Visible = false;
        _root.AddChild(holder);
        return box;
    }

    /// <summary>The full-screen holder of a screen's sheet (what is shown and hidden).</summary>
    private static Control Holder(VBoxContainer box) => (Control)box.GetParent().GetParent();

    private static Label KeyChip(string key)
    {
        var chip = new Label { Text = key, HorizontalAlignment = HorizontalAlignment.Center };
        chip.AddThemeColorOverride("font_color", Colors.White);
        chip.AddThemeFontSizeOverride("font_size", 18);
        var style = new StyleBoxFlat { BgColor = LabTheme.Ink, ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 4, ContentMarginBottom = 4 };
        style.SetCornerRadiusAll(4);
        style.BorderWidthBottom = 3;
        style.BorderColor = LabTheme.Accent;
        chip.AddThemeStyleboxOverride("normal", style);
        return chip;
    }

    private static Button AddButton(VBoxContainer box, string text, Action onPressed)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(280, 52) };
        button.AddThemeFontSizeOverride("font_size", 26);
        button.Pressed += onPressed;
        box.AddChild(button);
        return button;
    }
}

/// <summary>The two icons of a level card: a padlock (locked) and a warning triangle (under construction).</summary>
public partial class CardIcon : Control
{
    public LevelStatus Kind { get; set; }

    public override void _Ready() => CustomMinimumSize = new Vector2(52, 52);

    public override void _Draw()
    {
        if (Kind == LevelStatus.Locked)
        {
            var steel = LabTheme.Muted;
            DrawArc(new Vector2(26, 20), 11, Mathf.Pi, Mathf.Tau, 16, steel, 4);
            DrawRect(new Rect2(10, 20, 32, 26), steel);
            DrawCircle(new Vector2(26, 33), 4, LabTheme.Paper);
        }
        else
        {
            var amber = new Color(1f, 0.75f, 0.15f);
            var dark = new Color(0.15f, 0.12f, 0.05f);
            DrawColoredPolygon([new Vector2(26, 4), new Vector2(50, 46), new Vector2(2, 46)], amber);
            DrawRect(new Rect2(24, 18, 4, 16), dark);
            DrawRect(new Rect2(24, 38, 4, 4), dark);
        }
    }
}
