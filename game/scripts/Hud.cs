using Godot;
using Swapping.Game.Pieces;
using Swapping.GameLogic;
using Swapping.Rules.Model;

namespace Swapping.Game;

/// <summary>
/// The colour of each swappable property, the same everywhere: object outlines, HUD chips, crosshair.
/// </summary>
public static class AbilityColors
{
    public static readonly Color Position = new(0.3f, 0.85f, 1f);
    public static readonly Color Mass = new(1f, 0.6f, 0.2f);
    public static readonly Color KineticEnergy = new(1f, 0.35f, 0.8f);

    public static Color Of(IPropertyKind kind) =>
        kind == PropertyKind.Position ? Position : kind == PropertyKind.Mass ? Mass : KineticEnergy;
}

/// <summary>
/// The HUD (redesigned with the user, 2026-11-05): the player's numbers in a panel (top left), each property
/// with its colour; time and swaps (top right); the swap choices as coloured chips (bottom centre, the chosen
/// one lit); the crosshair and cooldown ring in the chosen property's colour (red when refused); the aimed
/// body's numbers under the crosshair. Text only, in plain <see cref="Label"/>s (never BBCode).
/// </summary>
public partial class Hud : CanvasLayer
{
    private static readonly Color RefusedColor = new(1f, 0.3f, 0.25f);
    private static readonly Color PanelColor = new(0.05f, 0.07f, 0.1f, 0.62f);
    private static readonly Color SpeedColor = new(0.85f, 0.88f, 0.92f);

    private Player _player = null!;
    private SwapController _swaps = null!;
    private Label _crosshair = null!;
    private Label _reason = null!;
    private Label _target = null!;
    private Label _time = null!;
    private Label _swapCount = null!;
    private CooldownRingControl _ring = null!;
    private ThrowBarControl _charge = null!;
    private RunFlow? _flow;

    // The player's panel: one row per property.
    private Label _massRow = null!;
    private Label _speedRow = null!;
    private Label _energyRow = null!;
    private Control _positionLine = null!;
    private Label _positionRow = null!;

    // The swap choices.
    private HBoxContainer _chips = null!;
    private readonly Dictionary<IPropertyKind, PanelContainer> _chipOf = [];

    /// <summary>The throw bar shown (0–1), or null when hidden.</summary>
    public double? ChargeShown => _charge.Fraction;

    public double RingProgress => _ring.Progress;

    public string CrosshairText => _crosshair.Text;

    public string ReasonText => _reason.Text;

    /// <summary>The player's panel as text, one line per row (the smoke test reads it).</summary>
    public string PlayerText { get; private set; } = "";

    public string TargetText => _target.Text;

    /// <summary>Time, swaps and the chosen property as text (the smoke test reads it).</summary>
    public string RunText { get; private set; } = "";

    /// <summary>The chip lit as the chosen property (headless checks).</summary>
    public IPropertyKind? LitChip { get; private set; }

    public void Setup(Player player, SwapController swaps, RunFlow flow)
    {
        _player = player;
        _swaps = swaps;
        _flow = flow;
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, Theme = LabTheme.Get() };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        _ring = new CooldownRingControl { MouseFilter = Control.MouseFilterEnum.Ignore };
        _ring.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(_ring);
        _charge = new ThrowBarControl { MouseFilter = Control.MouseFilterEnum.Ignore };
        _charge.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(_charge);
        _crosshair = Centered(root, "+", 40, 0);
        _reason = Centered(root, "", 20, 34);
        _target = Centered(root, "", 18, 62);

        BuildPlayerPanel(root);
        BuildRunPanel(root);
        BuildChips(root);
    }

    private void BuildPlayerPanel(Control root)
    {
        var panel = Panel(new Vector2(16, 14));
        var rows = new VBoxContainer();
        rows.AddThemeConstantOverride("separation", 2);
        panel.AddChild(rows);
        var title = HudLabel("VOCÊ", 14, new Color(1, 1, 1, 0.6f));
        rows.AddChild(title);
        _massRow = Row(rows, AbilityColors.Mass);
        _speedRow = Row(rows, SpeedColor);
        _energyRow = Row(rows, AbilityColors.KineticEnergy);
        _positionRow = Row(rows, AbilityColors.Position);
        _positionLine = (Control)_positionRow.GetParent();
        root.AddChild(panel);
    }

    private void BuildRunPanel(Control root)
    {
        var panel = Panel(Vector2.Zero);
        panel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        panel.GrowHorizontal = Control.GrowDirection.Begin;
        panel.Position = new Vector2(-16, 14);
        var rows = new VBoxContainer();
        rows.AddThemeConstantOverride("separation", 0);
        panel.AddChild(rows);
        _time = HudLabel("00:00.0", 28, Colors.White);
        _time.HorizontalAlignment = HorizontalAlignment.Right;
        _swapCount = HudLabel("Trocas 0", 16, new Color(1, 1, 1, 0.75f));
        _swapCount.HorizontalAlignment = HorizontalAlignment.Right;
        rows.AddChild(_time);
        rows.AddChild(_swapCount);
        root.AddChild(panel);
    }

    private void BuildChips(Control root)
    {
        _chips = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
        _chips.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        _chips.GrowHorizontal = Control.GrowDirection.Both;
        _chips.GrowVertical = Control.GrowDirection.Begin;
        _chips.Position += new Vector2(0, -22);
        _chips.AddThemeConstantOverride("separation", 10);
        var key = 1;
        foreach (var kind in PropertyKind.All)
        {
            var chip = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            chip.AddChild(HudLabel($"{key++}  {PropertyKeys.Label(kind)}", 18, Colors.White));
            _chips.AddChild(chip);
            _chipOf[kind] = chip;
        }
        root.AddChild(_chips);
    }

    public override void _Process(double delta)
    {
        if (_player is null)
            return;
        var me = Readout.Of(_player.MassKg, _player.Velocity.ToLevel());
        var massMark = Mark(PlayerId, PropertyKind.Mass);
        var energyMark = Mark(PlayerId, PropertyKind.KineticEnergy);
        var where = Mark(PlayerId, PropertyKind.Position);
        _massRow.Text = me.Mass + massMark;
        _speedRow.Text = me.Speed;
        _energyRow.Text = me.Energy + energyMark;
        _positionLine.Visible = where.Length > 0;
        _positionRow.Text = $"Posição{where}";
        PlayerText = $"Você\n{me.Mass}{massMark}\n{me.Speed}\n{me.Energy}{energyMark}" + (where.Length > 0 ? $"\nPosição{where}" : "");

        var choice = _swaps.Choice;
        if (_flow is not null)
        {
            _time.Text = RunClock.Format(_flow.Run.TimeMs);
            _swapCount.Text = $"Trocas {_flow.Run.Swaps}";
            RunText = $"{_time.Text}\n{_swapCount.Text}\nTroca por: {PropertyKeys.Label(choice)}";
        }
        UpdateChips(choice);

        _ring.Progress = _swaps.CooldownProgress;
        _ring.Tint = AbilityColors.Of(choice);
        _charge.Fraction = _player.ChargeFraction;
        var crosshair = _swaps.Crosshair;
        // Always a crosshair (user's request): a small faint dot at rest, a bigger "+" on an object, in the
        // chosen property's colour, red when the swap would be refused.
        var idle = crosshair.Look == CrosshairLook.Idle;
        _crosshair.Text = idle ? "•" : "+";
        _crosshair.AddThemeFontSizeOverride("font_size", idle ? 18 : 40);
        var color = crosshair.Look == CrosshairLook.Refused ? RefusedColor : AbilityColors.Of(choice);
        _crosshair.AddThemeColorOverride("font_color", idle ? new Color(1, 1, 1, 0.6f) : color);
        _reason.AddThemeColorOverride("font_color", color);
        _reason.Text = crosshair.Reason;

        if (_swaps.Target is { } target && IsInstanceValid(target))
        {
            var it = Readout.Of(target.Mass, target.LinearVelocity.ToLevel());
            var id = target.ContainerId;
            _target.Text = $"{it.Mass}{Mark(id, PropertyKind.Mass)} · {it.Speed} · {it.Energy}{Mark(id, PropertyKind.KineticEnergy)}"
                + (Mark(id, PropertyKind.Position) is { Length: > 0 } itsPlace ? $"\nPosição{itsPlace}" : "");
        }
        else
        {
            _target.Text = "";
        }
    }

    /// <summary>Only the room's unlocked properties get a chip; the chosen one is filled with its colour.</summary>
    private void UpdateChips(IPropertyKind choice)
    {
        var unlocked = _swaps.Runner?.World?.Rules.UnlockedProperties;
        LitChip = null;
        foreach (var (kind, chip) in _chipOf)
        {
            chip.Visible = unlocked?.Contains(kind) ?? false;
            var lit = kind == choice;
            if (lit && chip.Visible)
                LitChip = kind;
            var color = AbilityColors.Of(kind);
            var style = new StyleBoxFlat
            {
                BgColor = lit ? new Color(color, 0.92f) : PanelColor,
                BorderColor = color,
                ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 5, ContentMarginBottom = 5,
            };
            style.SetBorderWidthAll(lit ? 0 : 2);
            style.SetCornerRadiusAll(6);
            chip.AddThemeStyleboxOverride("panel", style);
            var label = (Label)chip.GetChild(0);
            label.AddThemeColorOverride("font_color", lit ? LabTheme.Ink : color);
            label.AddThemeConstantOverride("outline_size", lit ? 0 : 4);
        }
    }

    private const string PlayerId = Swapping.Levels.LevelValidator.PlayerId;

    /// <summary>" ⇄ da caixa" / " ⇄ seu" when the body uses someone else's property (Borrowed), "" otherwise.</summary>
    private string Mark(string containerId, IPropertyKind kind)
    {
        var runner = _swaps.Runner;
        if (runner?.World is not { } world)
            return "";
        try
        {
            return Borrowed.Mark(world, containerId, kind, PlayerId, owner =>
                runner.Bodies.FirstOrDefault(b => b.ContainerId == owner)?.Def.Kind == Swapping.Levels.Model.ObjectKind.Ball ? "bola" : "caixa");
        }
        catch (Exception)
        {
            return "";                              // a body of a room being rebuilt: no mark this frame
        }
    }

    private static PanelContainer Panel(Vector2 at)
    {
        var panel = new PanelContainer { Position = at, MouseFilter = Control.MouseFilterEnum.Ignore };
        var style = new StyleBoxFlat
        {
            BgColor = PanelColor,
            ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 8, ContentMarginBottom = 10,
        };
        style.SetCornerRadiusAll(8);
        style.BorderWidthLeft = 3;
        style.BorderColor = new Color(LabTheme.Accent, 0.9f);
        panel.AddThemeStyleboxOverride("panel", style);
        return panel;
    }

    /// <summary>A row of the player's panel: a small square in the property's colour, then the text.</summary>
    private static Label Row(VBoxContainer rows, Color color)
    {
        var line = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        line.AddThemeConstantOverride("separation", 8);
        var swatch = new ColorRect { Color = color, CustomMinimumSize = new Vector2(10, 10), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, MouseFilter = Control.MouseFilterEnum.Ignore };
        var label = HudLabel("", 20, Colors.White);
        line.AddChild(swatch);
        line.AddChild(label);
        rows.AddChild(line);
        return label;
    }

    private static Label HudLabel(string text, int size, Color color)
    {
        var label = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        Outline(label);
        return label;
    }

    private static Label Centered(Control root, string text, int size, float below)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.SetAnchorsPreset(Control.LayoutPreset.Center);
        label.Size = new Vector2(400, 40);
        label.Position = new Vector2(-200, -20 + below);
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", Colors.White);
        Outline(label);
        root.AddChild(label);
        return label;
    }

    private static void Outline(Label label)
    {
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 4);
    }
}

/// <summary>The cooldown ring around the crosshair: an arc that fills up until the next swap is allowed.</summary>
public partial class CooldownRingControl : Control
{
    private double _progress = 1;
    private Color _tint = new(1, 1, 1, 0.8f);

    public double Progress
    {
        get => _progress;
        set
        {
            if (Mathf.IsEqualApprox(value, _progress))
                return;
            _progress = value;
            QueueRedraw();
        }
    }

    /// <summary>The ring takes the chosen property's colour.</summary>
    public Color Tint
    {
        get => _tint;
        set
        {
            if (value == _tint)
                return;
            _tint = value;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_progress >= 1)
            return;
        var center = Size / 2;
        DrawArc(center, 22, 0, Mathf.Tau, 48, new Color(0, 0, 0, 0.35f), 5, antialiased: true);
        DrawArc(center, 22, -Mathf.Pi / 2, -Mathf.Pi / 2 + Mathf.Tau * (float)_progress, 48, new Color(_tint, 0.9f), 3, antialiased: true);
    }
}

/// <summary>The throw bar just above the crosshair: fills left to right while the throw button is held.</summary>
public partial class ThrowBarControl : Control
{
    private const float Width = 120, Height = 8, AboveCenter = 44;
    private double? _fraction;

    public double? Fraction
    {
        get => _fraction;
        set
        {
            if (value == _fraction)
                return;
            _fraction = value;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_fraction is not { } f)
            return;
        var corner = Size / 2 + new Vector2(-Width / 2, -AboveCenter);
        DrawRect(new Rect2(corner, new Vector2(Width, Height)), new Color(0, 0, 0, 0.5f));
        DrawRect(new Rect2(corner, new Vector2(Width * (float)f, Height)), f >= 1 ? new Color(1, 0.55f, 0.2f) : new Color(1, 1, 1, 0.9f));
        DrawRect(new Rect2(corner, new Vector2(Width, Height)), Colors.Black, filled: false, width: 1);
    }
}
