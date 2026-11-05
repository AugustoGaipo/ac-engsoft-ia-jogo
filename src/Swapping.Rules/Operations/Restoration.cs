using Swapping.Rules.Model;

namespace Swapping.Rules.Operations;

/// <summary>
/// A container gets back each of its own variables from whoever holds it; that holder takes the
/// variable the container was using. Each property is restored independently. Restorations are not
/// swaps: they ignore legality and cooldown and do not count as moves.
/// </summary>
public static class Restoration
{
    /// <summary>
    /// The container entered the barrier: restores all its variables if the barrier blocks variables.
    /// Returns the body updates to apply (empty when nothing is restored).
    /// </summary>
    public static IReadOnlyList<BodyUpdate> OnEnter(World world, string containerId, BarrierFilter barrier) =>
        barrier.RestoresOnEntry ? RestoreAll(world, containerId) : Array.Empty<BodyUpdate>();

    /// <summary>Restores every property; returns updates for the container, then each holder affected.</summary>
    public static IReadOnlyList<BodyUpdate> RestoreAll(World world, string containerId)
    {
        world.Get(containerId);
        var affected = new List<string> { containerId };
        foreach (var kind in PropertyKind.All)
        {
            var holder = Restore(world, containerId, kind);
            if (holder is not null && !affected.Contains(holder))
                affected.Add(holder);
        }

        return affected.Select(world.BodyUpdateFor).ToArray();
    }

    /// <summary>
    /// Crossing a room's exit door (user, 2026-11-05): the container and every holder get their own variables
    /// back, but nobody moves — each position and velocity is set again from <paramref name="physics"/>, so
    /// only the masses (and the energies they mean at the same speed) change. Empty when nothing was borrowed.
    /// </summary>
    public static IReadOnlyList<BodyUpdate> ReturnAllInPlace(World world, string containerId, Func<string, (Vec3 Position, Vec3 Velocity)> physics)
    {
        var updates = RestoreAll(world, containerId);
        if (updates.Count < 2)
            return Array.Empty<BodyUpdate>();
        var affected = updates.Select(u => u.ContainerId).ToList();
        foreach (var id in affected)
        {
            var (position, velocity) = physics(id);
            world.SyncFromPhysics(id, position, velocity);
        }
        return affected.Select(world.BodyUpdateFor).ToArray();
    }

    /// <summary>Restores one property. Returns the former holder, or null if it already had its own.</summary>
    public static string? Restore(World world, string containerId, IPropertyKind kind)
    {
        var holder = world.UserOf(world.OwnVariable(containerId, kind));
        if (holder == containerId) return null;

        world.ExchangeVariables(containerId, holder, kind);
        return holder;
    }
}
