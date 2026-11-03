using Godot;
using Swapping.Game.Pieces;
using Swapping.GameLogic;

namespace Swapping.Game;

/// <summary>
/// First HUD (pulled forward from Task 9 after the user's play test): the crosshair with its color and
/// reason, the player's numbers (top left) and the aimed body's numbers (under the crosshair). Text only,
/// in plain <see cref="Label"/>s (never BBCode).
/// </summary>
public partial class Hud : CanvasLayer
{
    private static readonly Color ReadyColor = Colors.White;
    private static readonly Color RefusedColor = new(1f, 0.3f, 0.25f);

    private Player _player = null!;
    private SwapController _swaps = null!;
    private Label _crosshair = null!;
    private Label _reason = null!;
    private Label _mine = null!;
    private Label _target = null!;
    private Label _run = null!;
    private CooldownRingControl _ring = null!;

    public double RingProgress => _ring.Progress;
    private RunFlow? _flow;

    public string CrosshairText => _crosshair.Text;

    public string ReasonText => _reason.Text;

    public string PlayerText => _mine.Text;

    public string TargetText => _target.Text;

    public string RunText => _run.Text;

    public void Setup(Player player, SwapController swaps, RunFlow flow)
    {
        _player = player;
        _swaps = swaps;
        _flow = flow;
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        _ring = new CooldownRingControl { MouseFilter = Control.MouseFilterEnum.Ignore };
        _ring.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(_ring);
        _crosshair = Centered(root, "+", 40, 0);
        _reason = Centered(root, "", 20, 34);
        _target = Centered(root, "", 18, 62);
        _mine = new Label { Position = new Vector2(16, 12), MouseFilter = Control.MouseFilterEnum.Ignore };
        _mine.AddThemeFontSizeOverride("font_size", 20);
        Outline(_mine);
        root.AddChild(_mine);

        // Time and swaps, always (top right).
        _run = new Label { HorizontalAlignment = HorizontalAlignment.Right, MouseFilter = Control.MouseFilterEnum.Ignore };
        _run.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _run.Size = new Vector2(320, 70);
        _run.Position = new Vector2(-336, 12);
        _run.AddThemeFontSizeOverride("font_size", 26);
        Outline(_run);
        root.AddChild(_run);
    }

    public override void _Process(double delta)
    {
        if (_player is null)
            return;
        var me = Readout.Of(_player.MassKg, _player.Velocity.ToLevel());
        _mine.Text = $"Você\n{me.Mass}\n{me.Speed}\n{me.Energy}";
        if (_flow is not null)
            _run.Text = $"{RunClock.Format(_flow.Run.TimeMs)}\nTrocas {_flow.Run.Swaps}";

        _ring.Progress = _swaps.CooldownProgress;
        var crosshair = _swaps.Crosshair;
        // Always a crosshair (user's request): a small faint dot at rest, a bigger "+" on an object.
        var idle = crosshair.Look == CrosshairLook.Idle;
        _crosshair.Text = idle ? "•" : "+";
        _crosshair.AddThemeFontSizeOverride("font_size", idle ? 18 : 40);
        var color = crosshair.Look == CrosshairLook.Refused ? RefusedColor : ReadyColor;
        _crosshair.AddThemeColorOverride("font_color", idle ? new Color(1, 1, 1, 0.6f) : color);
        _reason.AddThemeColorOverride("font_color", color);
        _reason.Text = crosshair.Reason;

        if (_swaps.Target is { } target && IsInstanceValid(target))
        {
            var it = Readout.Of(target.Mass, target.LinearVelocity.ToLevel());
            _target.Text = $"{it.Mass} · {it.Speed} · {it.Energy}";
        }
        else
        {
            _target.Text = "";
        }
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
        Outline(label);
        root.AddChild(label);
        return label;
    }

    private static void Outline(Label label)
    {
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 6);
    }
}

/// <summary>The cooldown ring around the crosshair: an arc that fills up until the next swap is allowed.</summary>
public partial class CooldownRingControl : Control
{
    private double _progress = 1;

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

    public override void _Draw()
    {
        if (_progress >= 1)
            return;
        var center = Size / 2;
        DrawArc(center, 22, -Mathf.Pi / 2, -Mathf.Pi / 2 + Mathf.Tau * (float)_progress, 48, new Color(1, 1, 1, 0.8f), 3, antialiased: true);
    }
}
