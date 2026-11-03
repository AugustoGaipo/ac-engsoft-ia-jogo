using Swapping.Rules.Model;

namespace Swapping.GameLogic;

/// <summary>
/// The property keys of SPEC-game slice 2: <c>1</c> position, <c>2</c> mass, <c>3</c> kinetic energy.
/// A property the level does not unlock is refused (the choice stays); the default is the first unlocked one.
/// </summary>
public static class PropertyKeys
{
    /// <summary>The first unlocked property in key order (position when nothing is unlocked: the rules then refuse it).</summary>
    public static IPropertyKind Default(IReadOnlySet<IPropertyKind> unlocked) => SwapChoice.Property(unlocked);

    /// <summary>The key's property if the level unlocks it; otherwise the current choice. Other actions change nothing.</summary>
    public static IPropertyKind Choose(GameAction action, IPropertyKind current, IReadOnlySet<IPropertyKind> unlocked) =>
        KeyProperty(action) is { } wanted && unlocked.Contains(wanted) ? wanted : current;

    /// <summary>The choice itself if still unlocked (a new room or level may unlock fewer), else the default.</summary>
    public static IPropertyKind Valid(IPropertyKind current, IReadOnlySet<IPropertyKind> unlocked) =>
        unlocked.Contains(current) ? current : Default(unlocked);

    /// <summary>The mouse wheel: the next (+1) or previous (-1) unlocked property in key order, wrapping around.</summary>
    public static IPropertyKind Cycle(IPropertyKind current, int direction, IReadOnlySet<IPropertyKind> unlocked)
    {
        var options = PropertyKind.All.Where(unlocked.Contains).ToList();
        var at = options.IndexOf(Valid(current, unlocked));
        if (options.Count == 0 || at < 0)
            return Default(unlocked);
        return options[(at + direction % options.Count + options.Count) % options.Count];
    }

    /// <summary>+1 for the "next" wheel action, -1 for "previous", 0 for anything else.</summary>
    public static int WheelDirection(GameAction action) => action switch
    {
        GameAction.PropertyNext => 1,
        GameAction.PropertyPrevious => -1,
        _ => 0,
    };

    /// <summary>The name shown on the HUD.</summary>
    public static string Label(IPropertyKind kind) => kind.Name switch
    {
        "position" => "Posição",
        "mass" => "Massa",
        "energy" => "Energia",
        _ => kind.Name,
    };

    private static IPropertyKind? KeyProperty(GameAction action) => action switch
    {
        GameAction.ChoosePosition => PropertyKind.Position,
        GameAction.ChooseMass => PropertyKind.Mass,
        GameAction.ChooseEnergy => PropertyKind.KineticEnergy,
        _ => null,
    };
}

/// <summary>Holding the rotate button turns the held object with the mouse (the view stays still), like Superliminal.</summary>
public static class HeldSpin
{
    private const double RadiansToDegrees = 180 / Math.PI;

    /// <summary>Degrees around the vertical axis for a horizontal mouse motion; same sense as the view's own turn.</summary>
    public static double Degrees(double mouseRelativeX, double radiansPerPixel) =>
        -mouseRelativeX * radiansPerPixel * RadiansToDegrees;
}
