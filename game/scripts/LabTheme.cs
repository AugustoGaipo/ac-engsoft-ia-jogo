using Godot;

namespace Swapping.Game;

/// <summary>
/// The look of the menus (test-facility style): white wall panels, black-and-yellow hazard bands, signage
/// titles in dark strips and buttons with a cyan edge. One <see cref="Theme"/> shared by the main menu, the
/// pause menu and the end screen; <see cref="LabBackdrop"/> draws the wall behind the main menu.
/// </summary>
public static class LabTheme
{
    public static readonly Color Wall = new(0.86f, 0.87f, 0.88f);
    public static readonly Color Seam = new(0.70f, 0.72f, 0.75f);
    public static readonly Color Ink = new(0.11f, 0.13f, 0.16f);
    public static readonly Color Paper = new(0.97f, 0.97f, 0.96f);
    public static readonly Color Accent = new(0.05f, 0.62f, 0.88f);
    public static readonly Color Hazard = new(0.98f, 0.78f, 0.10f);
    public static readonly Color Muted = new(0.45f, 0.48f, 0.52f);

    private static Theme? _theme;

    public static Theme Get() => _theme ??= Build();

    private static Theme Build()
    {
        var theme = new Theme
        {
            DefaultFont = new SystemFont { FontNames = ["Bahnschrift", "DIN Alternate", "Segoe UI", "Arial"] },
            DefaultFontSize = 22,
        };

        theme.SetStylebox("normal", "Button", Box(Paper, Accent, left: 6, bottom: 0));
        theme.SetStylebox("hover", "Button", Box(Colors.White, Accent, left: 12, bottom: 2, all: 2));
        theme.SetStylebox("pressed", "Button", Box(Accent, Ink, left: 12, bottom: 0));
        theme.SetStylebox("disabled", "Button", Box(new Color(Paper, 0.55f), Seam, left: 6, bottom: 0));
        theme.SetStylebox("focus", "Button", new StyleBoxEmpty());
        theme.SetColor("font_color", "Button", Ink);
        theme.SetColor("font_hover_color", "Button", Ink);
        theme.SetColor("font_focus_color", "Button", Ink);
        theme.SetColor("font_pressed_color", "Button", Colors.White);
        theme.SetColor("font_disabled_color", "Button", Muted);

        theme.SetColor("font_color", "Label", Ink);

        // The card behind a screen's buttons: a white sheet with a cyan rule on top.
        var sheet = Box(new Color(Paper, 0.96f), Accent, left: 0, bottom: 0);
        sheet.BorderWidthTop = 8;
        sheet.BorderWidthRight = 0;
        sheet.SetContentMarginAll(28);
        sheet.ShadowColor = new Color(0, 0, 0, 0.25f);
        sheet.ShadowSize = 12;
        sheet.ShadowOffset = new Vector2(0, 6);
        theme.SetStylebox("panel", "PanelContainer", sheet);
        return theme;
    }

    private static StyleBoxFlat Box(Color bg, Color border, int left, int bottom, int all = 0)
    {
        var box = new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = border,
            BorderWidthLeft = left,
            BorderWidthBottom = bottom,
            BorderWidthTop = all,
            BorderWidthRight = all,
            CornerRadiusTopLeft = 2, CornerRadiusTopRight = 2, CornerRadiusBottomLeft = 2, CornerRadiusBottomRight = 2,
        };
        box.ContentMarginLeft = 22;
        box.ContentMarginRight = 22;
        box.ContentMarginTop = 8;
        box.ContentMarginBottom = 8;
        return box;
    }

    /// <summary>A title in a dark sign strip (white letters), like the room plates of a test facility.</summary>
    public static Label Sign(string text, int size)
    {
        var label = new Label { Text = text.ToUpperInvariant(), HorizontalAlignment = HorizontalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", Colors.White);
        var strip = new StyleBoxFlat { BgColor = Ink, ContentMarginLeft = 24, ContentMarginRight = 24, ContentMarginTop = 6, ContentMarginBottom = 6 };
        strip.BorderWidthBottom = 4;
        strip.BorderColor = Hazard;
        label.AddThemeStyleboxOverride("normal", strip);
        return label;
    }

    /// <summary>Centres a white sheet holding <paramref name="box"/>; returns the full-screen holder to show/hide.</summary>
    public static CenterContainer Sheet(Control box)
    {
        var center = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var sheet = new PanelContainer { Name = "Sheet" };
        sheet.AddChild(box);
        center.AddChild(sheet);
        return center;
    }
}

/// <summary>
/// The wall behind the main menu: a grid of white panels with seams, black-and-yellow hazard bands top and
/// bottom, stencilled plates in the corners and the emblem of the three properties (position, mass, energy).
/// </summary>
public partial class LabBackdrop : Control
{
    private const float Tile = 150;
    private const float Band = 26;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        Resized += QueueRedraw;
    }

    public override void _Draw()
    {
        var size = Size;
        DrawRect(new Rect2(Vector2.Zero, size), LabTheme.Wall);

        // Wall panels: a slightly different white per tile, with dark seams.
        for (var x = 0f; x < size.X; x += Tile)
            for (var y = Band; y < size.Y - Band; y += Tile)
            {
                var shade = 0.84f + 0.05f * Hash((int)(x / Tile), (int)(y / Tile));
                DrawRect(new Rect2(x + 3, y + 3, Tile - 6, Tile - 6), new Color(shade, shade + 0.01f, shade + 0.02f));
            }
        for (var x = 0f; x < size.X; x += Tile)
            DrawLine(new Vector2(x, Band), new Vector2(x, size.Y - Band), LabTheme.Seam, 3);
        for (var y = Band; y < size.Y - Band; y += Tile)
            DrawLine(new Vector2(0, y), new Vector2(size.X, y), LabTheme.Seam, 3);

        HazardBand(0, size.X);
        HazardBand(size.Y - Band, size.X);

        var font = GetThemeDefaultFont();
        DrawString(font, new Vector2(24, Band + 40), "INSTALAÇÃO DE TESTES  ·  CÂMARA DE TROCA", fontSize: 20, modulate: LabTheme.Muted);
        DrawString(font, new Vector2(24, Band + 66), "PROTOCOLO: POSIÇÃO / MASSA / ENERGIA CINÉTICA", fontSize: 16, modulate: LabTheme.Muted);
        DrawString(font, new Vector2(size.X - 330, size.Y - Band - 24), "ÁREA RESTRITA · SOMENTE SUJEITOS DE TESTE", fontSize: 14, modulate: LabTheme.Muted);

        Emblem(new Vector2(size.X - 150, Band + 130), 70);
    }

    private void HazardBand(float top, float width)
    {
        DrawRect(new Rect2(0, top, width, Band), LabTheme.Ink);
        for (var x = -Band; x < width + Band; x += Band * 2)
            DrawColoredPolygon(
                [new Vector2(x, top + Band), new Vector2(x + Band, top), new Vector2(x + Band * 2, top), new Vector2(x + Band, top + Band)],
                LabTheme.Hazard);
    }

    /// <summary>A ring with the three swappable properties on it, linked by the swap arrows.</summary>
    private void Emblem(Vector2 c, float r)
    {
        DrawArc(c, r, 0, Mathf.Tau, 64, LabTheme.Ink, 6, antialiased: true);
        DrawArc(c, r - 14, 0, Mathf.Tau, 64, LabTheme.Seam, 2, antialiased: true);
        Color[] colors = [LabTheme.Accent, new Color(0.95f, 0.45f, 0.15f), new Color(0.3f, 0.75f, 0.35f)];
        for (var i = 0; i < 3; i++)
        {
            var a = -Mathf.Pi / 2 + i * Mathf.Tau / 3;
            var p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            DrawCircle(p, 14, LabTheme.Ink);
            DrawCircle(p, 10, colors[i]);
        }
        DrawString(GetThemeDefaultFont(), c + new Vector2(-22, 14), "⇄", fontSize: 40, modulate: LabTheme.Ink);
    }

    private static float Hash(int a, int b) => (Mathf.Abs((a * 73856093) ^ (b * 19349663)) % 100) / 100f;
}
