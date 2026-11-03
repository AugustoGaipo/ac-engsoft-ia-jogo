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
        _root = new ColorRect { Color = new Color(0.07f, 0.08f, 0.11f), Visible = false };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);

        _main = NewScreen("Swapping");
        _play = AddButton(_main, "Jogar", Play);
        AddButton(_main, "Níveis", () => Show("levels"));
        AddButton(_main, "Mapa de teste", () => SandboxRequested?.Invoke());
        AddButton(_main, "Controles", () => Show("controls"));
        AddButton(_main, "Sair", () => GetTree().Quit());

        _levelsScreen = NewScreen("Níveis");
        _controlsScreen = NewScreen("Controles");
        var controls = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = "W A S D  andar        Espaço  pular\nMouse  olhar        E  segurar / soltar\n" +
                   "Segurar botão esquerdo  carregar e arremessar (toque solta)        Botão direito  trocar\n1 2 3  escolher a troca (posição, massa, energia)\nRoda do mouse  trocar a propriedade\nSegurar F + mover o mouse  girar o item segurado\nR  reiniciar a sala        Esc  pausa",
        };
        controls.AddThemeFontSizeOverride("font_size", 22);
        _controlsScreen.AddChild(controls);
        AddButton(_controlsScreen, "Voltar", () => Show("main"));
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
        _main.Visible = screen == "main";
        _levelsScreen.Visible = screen == "levels";
        _controlsScreen.Visible = screen == "controls";
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
        var label = new Label { Text = $"{level.Number} · {level.Name}\n{detail}", Position = new Vector2(16, 12), MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", 22);
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
        var box = new VBoxContainer { GrowHorizontal = Control.GrowDirection.Both, GrowVertical = Control.GrowDirection.Both, Visible = false };
        box.SetAnchorsPreset(Control.LayoutPreset.Center);
        box.AddThemeConstantOverride("separation", 14);
        var label = new Label { Text = title, HorizontalAlignment = HorizontalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", 56);
        box.AddChild(label);
        _root.AddChild(box);
        return box;
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
            var steel = new Color(0.72f, 0.78f, 0.9f);
            DrawArc(new Vector2(26, 20), 11, Mathf.Pi, Mathf.Tau, 16, steel, 4);
            DrawRect(new Rect2(10, 20, 32, 26), steel);
            DrawCircle(new Vector2(26, 33), 4, new Color(0.15f, 0.17f, 0.22f));
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
