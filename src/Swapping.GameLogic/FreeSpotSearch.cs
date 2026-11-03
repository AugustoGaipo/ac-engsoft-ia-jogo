using Swapping.Rules.Model;

namespace Swapping.GameLogic;

public enum MakeRoomOutcome
{
    /// <summary>The body fits where the swap put it.</summary>
    AlreadyFree,

    /// <summary>It was moved to the nearest free point.</summary>
    Moved,

    /// <summary>No free point: the player dies.</summary>
    Death,

    /// <summary>No free point: the object pops (smoke) and respawns at its start.</summary>
    Pop,
}

public sealed record FreeSpot(MakeRoomOutcome Outcome, Vec3? Point);

/// <summary>
/// "Make room" after a position swap (SPEC-game, decision G2): a swap exchanges centers, so a body can end
/// up inside the floor, a ledge or a wall. The search tries the nearest free point of a 0.25 m grid
/// inside <see cref="Radius"/> (never below, straight up first on ties); never behind a wall.
/// </summary>
public static class FreeSpotSearch
{
    public const double Radius = 1.5;
    public const double Step = 0.25;

    /// <summary>
    /// Every point of a 0.25 m grid inside the radius, never below the target, nearest first. Ties go
    /// straight up first, then higher. Up and sideways together are included: near the floor, points at
    /// the same height are all inside it (the user's play test, a ball next to a crate).
    /// </summary>
    public static IEnumerable<Vec3> Candidates(Vec3 target) => Offsets.Select(o => target + o);

    private static readonly Vec3[] Offsets = BuildOffsets();

    private static Vec3[] BuildOffsets()
    {
        var steps = (int)Math.Round(Radius / Step);
        var offsets = new List<(Vec3 Offset, int Distance2, bool StraightUp, int Up)>();
        for (var x = -steps; x <= steps; x++)
        for (var y = 0; y <= steps; y++)
        for (var z = -steps; z <= steps; z++)
        {
            var distance2 = x * x + y * y + z * z;
            if (distance2 <= steps * steps)
                offsets.Add((new Vec3(x * Step, y * Step, z * Step), distance2, x == 0 && z == 0, y));
        }
        return offsets
            .OrderBy(o => o.Distance2)
            .ThenByDescending(o => o.StraightUp)
            .ThenByDescending(o => o.Up)
            .ThenBy(o => o.Offset.X).ThenBy(o => o.Offset.Z)
            .Select(o => o.Offset)
            .ToArray();
    }

    /// <param name="isFree">Whether the body fits with its center at that point.</param>
    /// <param name="reachable">Whether nothing solid lies between the target and that point (no wall crossed).</param>
    public static FreeSpot Find(Vec3 target, bool isPlayer, Func<Vec3, bool> isFree, Func<Vec3, Vec3, bool> reachable)
    {
        if (isFree(target))
            return new FreeSpot(MakeRoomOutcome.AlreadyFree, target);
        foreach (var candidate in Candidates(target).Skip(1))
        {
            if (reachable(target, candidate) && isFree(candidate))
                return new FreeSpot(MakeRoomOutcome.Moved, candidate);
        }
        return new FreeSpot(isPlayer ? MakeRoomOutcome.Death : MakeRoomOutcome.Pop, null);
    }
}
