using Swapping.Rules.Model;

namespace Swapping.GameLogic;

/// <summary>The outline of the aimed or held object (SPEC-game "What the player sees"). Colors are drawn by the game.</summary>
public enum Outline { White, Gray, Position, Mass, KineticEnergy }

public static class OutlineColor
{
    /// <summary>
    /// White = accepts what the room unlocked (or several of it); gray = locked or accepts none of it;
    /// a property's color = it accepts exactly one of several unlocked properties.
    /// </summary>
    public static Outline For(Container container, IReadOnlySet<IPropertyKind> unlocked)
    {
        if (container.Locked)
            return Outline.Gray;
        var accepted = PropertyKind.All.Where(k => unlocked.Contains(k) && container.Accepts(k)).ToList();
        if (accepted.Count == 0)
            return Outline.Gray;
        if (accepted.Count == 1 && unlocked.Count > 1)
            return accepted[0] == PropertyKind.Position ? Outline.Position
                : accepted[0] == PropertyKind.Mass ? Outline.Mass
                : Outline.KineticEnergy;
        return Outline.White;
    }
}

/// <summary>The cooldown ring around the crosshair.</summary>
public static class CooldownRing
{
    /// <summary>0 right after a swap, 1 when the next swap is allowed.</summary>
    public static double Progress(double now, double? lastSwapAt, double cooldownSeconds) =>
        lastSwapAt is not { } last || cooldownSeconds <= 0 ? 1 : Math.Clamp((now - last) / cooldownSeconds, 0, 1);
}
