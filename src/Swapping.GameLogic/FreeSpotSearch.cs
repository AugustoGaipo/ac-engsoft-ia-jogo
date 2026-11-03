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
/// up inside the floor, a ledge or a wall. The search tries the target, then straight up, then rings
/// around at the same height, nearest first, step 0.25 m, inside <see cref="Radius"/>; never behind a wall.
/// </summary>
public static class FreeSpotSearch
{
    public const double Radius = 1.5;
    public const double Step = 0.25;

    public static IEnumerable<Vec3> Candidates(Vec3 target)
    {
        yield return target;
        var steps = (int)Math.Round(Radius / Step);
        for (var i = 1; i <= steps; i++)
            yield return target with { Y = target.Y + i * Step };
        for (var i = 1; i <= steps; i++)
        {
            var radius = i * Step;
            var points = 8 * i; // about one point every 0.2 m around the ring
            for (var k = 0; k < points; k++)
            {
                var angle = 2 * Math.PI * k / points;
                yield return new Vec3(target.X + radius * Math.Cos(angle), target.Y, target.Z + radius * Math.Sin(angle));
            }
        }
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
