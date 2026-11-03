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
    private RunFlow? _flow;

    public string CrosshairText => _crosshair.Visible ? _crosshair.Text : "";

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

        var crosshair = _swaps.Crosshair;
        _crosshair.Visible = crosshair.Look != CrosshairLook.Hidden;
        var color = crosshair.Look == CrosshairLook.Refused ? RefusedColor : ReadyColor;
        _crosshair.AddThemeColorOverride("font_color", color);
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
