using Swapping.Rules.Model;

namespace Swapping.GameLogic.Tests;

public sealed class FreeSpotSearchTests
{
    private static readonly Vec3 Target = new(10, 1, 5);

    private static readonly Func<Vec3, Vec3, bool> NoWalls = (_, _) => true;

    [Fact]
    public void The_target_itself_comes_first_then_straight_up_in_quarter_meters()
    {
        var candidates = FreeSpotSearch.Candidates(Target).Take(7).ToList();

        Assert.Equal(Target, candidates[0]);
        Assert.Equal(new[] { 0.25, 0.5, 0.75, 1.0, 1.25, 1.5 }, candidates.Skip(1).Select(c => Math.Round(c.Y - Target.Y, 6)));
        Assert.All(candidates.Skip(1), c => Assert.Equal((Target.X, Target.Z), (c.X, c.Z)));
    }

    [Fact]
    public void After_up_come_rings_around_at_the_same_height_nearest_first()
    {
        var rings = FreeSpotSearch.Candidates(Target).Skip(7).ToList();

        Assert.NotEmpty(rings);
        Assert.All(rings, c => Assert.Equal(Target.Y, c.Y, precision: 9));
        var distances = rings.Select(c => Math.Round(Distance(c, Target), 6)).ToList();
        Assert.Equal(distances.Order(), distances);
        Assert.Equal(0.25, distances[0], precision: 6);
    }

    [Fact]
    public void Every_candidate_is_within_the_radius_and_none_repeats()
    {
        var candidates = FreeSpotSearch.Candidates(Target).ToList();

        Assert.All(candidates, c => Assert.True(Distance(c, Target) <= FreeSpotSearch.Radius + 1e-9));
        Assert.Equal(candidates.Count, candidates.Select(c => (Math.Round(c.X, 6), Math.Round(c.Y, 6), Math.Round(c.Z, 6))).Distinct().Count());
    }

    [Fact]
    public void A_free_target_needs_nothing()
    {
        var spot = FreeSpotSearch.Find(Target, isPlayer: true, isFree: _ => true, NoWalls);

        Assert.Equal(new FreeSpot(MakeRoomOutcome.AlreadyFree, Target), spot);
    }

    [Fact]
    public void Feet_in_the_floor_go_up_to_the_first_free_height()
    {
        // Free only from 0.5 m up (the body was half inside a ledge).
        var spot = FreeSpotSearch.Find(Target, isPlayer: true, isFree: p => p.Y >= Target.Y + 0.5 - 1e-9, NoWalls);

        Assert.Equal(MakeRoomOutcome.Moved, spot.Outcome);
        Assert.Equal(Target with { Y = Target.Y + 0.5 }, spot.Point);
    }

    [Fact]
    public void When_up_is_blocked_the_nearest_free_point_around_wins()
    {
        // A low ceiling: nothing above is free; only points at least 0.5 m to the side are.
        var spot = FreeSpotSearch.Find(Target, isPlayer: false,
            isFree: p => p.Y == Target.Y && Distance(p, Target) >= 0.5 - 1e-9, NoWalls);

        Assert.Equal(MakeRoomOutcome.Moved, spot.Outcome);
        Assert.Equal(0.5, Distance(spot.Point!.Value, Target), precision: 6);
    }

    [Fact]
    public void A_point_behind_a_wall_is_never_used()
    {
        // Free only on the far side of a wall at x = 10.3.
        var spot = FreeSpotSearch.Find(Target, isPlayer: true,
            isFree: p => p.X > 10.3, reachable: (from, to) => to.X <= 10.3);

        Assert.Equal(MakeRoomOutcome.Death, spot.Outcome);
    }

    [Fact]
    public void No_free_point_kills_the_player_and_pops_an_object()
    {
        Assert.Equal(new FreeSpot(MakeRoomOutcome.Death, null), FreeSpotSearch.Find(Target, isPlayer: true, _ => false, NoWalls));
        Assert.Equal(new FreeSpot(MakeRoomOutcome.Pop, null), FreeSpotSearch.Find(Target, isPlayer: false, _ => false, NoWalls));
    }

    private static double Distance(Vec3 a, Vec3 b) => (a - b).Length;
}
