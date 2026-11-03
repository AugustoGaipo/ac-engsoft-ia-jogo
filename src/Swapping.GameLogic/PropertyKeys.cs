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

/// <summary>The mouse wheel turns the held object around the vertical axis, 15° per notch.</summary>
public static class HeldSpin
{
    public const double DegreesPerNotch = 15;

    public static double Degrees(GameAction action) => action switch
    {
        GameAction.RotateHeldLeft => DegreesPerNotch,
        GameAction.RotateHeldRight => -DegreesPerNotch,
        _ => 0,
    };
}
