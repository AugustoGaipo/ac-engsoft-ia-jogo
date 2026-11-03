using Swapping.Rules.Model;
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;

/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
namespace Swapping.GameLogic;
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;

/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
/// <summary>
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
/// The property keys of SPEC-game slice 2: <c>1</c> position, <c>2</c> mass, <c>3</c> kinetic energy.
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
/// A property the level does not unlock is refused (the choice stays); the default is the first unlocked one.
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
/// </summary>
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
public static class PropertyKeys
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
{
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    /// <summary>The first unlocked property in key order (position when nothing is unlocked: the rules then refuse it).</summary>
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    public static IPropertyKind Default(IReadOnlySet<IPropertyKind> unlocked) => SwapChoice.Property(unlocked);
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;

/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    /// <summary>The key's property if the level unlocks it; otherwise the current choice. Other actions change nothing.</summary>
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    public static IPropertyKind Choose(GameAction action, IPropertyKind current, IReadOnlySet<IPropertyKind> unlocked) =>
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
        KeyProperty(action) is { } wanted && unlocked.Contains(wanted) ? wanted : current;
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;

/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    /// <summary>The choice itself if still unlocked (a new room or level may unlock fewer), else the default.</summary>
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    public static IPropertyKind Valid(IPropertyKind current, IReadOnlySet<IPropertyKind> unlocked) =>
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
        unlocked.Contains(current) ? current : Default(unlocked);
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;

/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    /// <summary>The mouse wheel: the next (+1) or previous (-1) unlocked property in key order, wrapping around.</summary>
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    public static IPropertyKind Cycle(IPropertyKind current, int direction, IReadOnlySet<IPropertyKind> unlocked)
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    {
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
        var options = PropertyKind.All.Where(unlocked.Contains).ToList();
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
        var at = options.IndexOf(Valid(current, unlocked));
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
        if (options.Count == 0 || at < 0)
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
            return Default(unlocked);
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
        return options[(at + direction % options.Count + options.Count) % options.Count];
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    }
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;

/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    /// <summary>+1 for the "next" wheel action, -1 for "previous", 0 for anything else.</summary>
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    public static int WheelDirection(GameAction action) => action switch
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    {
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
        GameAction.PropertyNext => 1,
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
        GameAction.PropertyPrevious => -1,
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
        _ => 0,
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    };
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;

/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    /// <summary>The name shown on the HUD.</summary>
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    public static string Label(IPropertyKind kind) => kind.Name switch
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    {
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
        "position" => "Posição",
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
        "mass" => "Massa",
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
        "energy" => "Energia",
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
        _ => kind.Name,
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    };
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;

/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    private static IPropertyKind? KeyProperty(GameAction action) => action switch
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    {
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
        GameAction.ChoosePosition => PropertyKind.Position,
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
        GameAction.ChooseMass => PropertyKind.Mass,
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
        GameAction.ChooseEnergy => PropertyKind.KineticEnergy,
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
        _ => null,
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    };
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
}
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;

/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
/// <summary>Holding the rotate button turns the held object with the mouse (the view stays still), like Superliminal.</summary>
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
public static class HeldSpin
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
{
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    private const double RadiansToDegrees = 180 / Math.PI;
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;

/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    /// <summary>Degrees around the vertical axis for a horizontal mouse motion; same sense as the view's own turn.</summary>
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
    public static double Degrees(double mouseRelativeX, double radiansPerPixel) =>
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
        -mouseRelativeX * radiansPerPixel * RadiansToDegrees;

    /// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>
    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>
        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
}
/// <summary>Degrees around the view's right axis for a vertical mouse motion; mouse up = looking up.</summary>    public static double TiltDegrees(double mouseRelativeY, double radiansPerPixel) =>        -mouseRelativeY * radiansPerPixel * RadiansToDegrees;
