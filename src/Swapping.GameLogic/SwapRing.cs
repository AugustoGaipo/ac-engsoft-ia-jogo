using Swapping.Rules.Model;
using Swapping.Rules.Operations;

namespace Swapping.GameLogic;

/// <summary>
/// What the crosshair tells about the aimed object (user, 2026-11-05): a ring split in equal slices, one per
/// swap the object accepts in this room, with the chosen property in the middle; an X when the swap is not
/// allowed. And the outline: thick when the object can be held right now, thin otherwise.
/// </summary>
public static class SwapRing
{
    /// <summary>The swaps the object accepts here, in the fixed order position, mass, energy. None when locked.</summary>
    public static IReadOnlyList<IPropertyKind> Segments(Container container, IReadOnlySet<IPropertyKind> unlocked) =>
        container.Locked ? [] : PropertyKind.All.Where(k => unlocked.Contains(k) && container.Accepts(k)).ToList();

    /// <summary>An X on the crosshair when the swap is refused; the cooldown is only waiting (the ring shows it).</summary>
    public static bool ShowsCross(RejectReason? reason) => reason is { } why && why != RejectReason.OnCooldown;

    /// <summary>Thick outline: within the hold reach, light enough to hold (<see cref="HoldLoad"/>) and nothing in between.</summary>
    public static bool ThickOutline(double distance, double reach, double bodyKg, double playerKg, bool blocked) =>
        distance <= reach && HoldLoad.CanHold(bodyKg, playerKg) && !blocked;
}
