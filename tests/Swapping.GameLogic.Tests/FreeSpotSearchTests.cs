using Swapping.Rules.Model;

namespace Swapping.GameLogic.Tests;

public sealed class FreeSpotSearchTests
{
    private static readonly Vec3 Target = new(10, 1, 5);

    private static readonly Func<Vec3, Vec3, bool> NoWalls = (_, _) => true;

    [Fact]
    public void The_target_comes_first_then_straight_up_wins_every_tie()
    {
        var candidates = FreeSpotSearch.Candidates(Target).Take(2).ToList();

        Assert.Equal(Target, candidates[0]);
        Assert.Equal(Target with { Y = Target.Y + 0.25 }, candidates[1]);
    }

    [Fact]
    public void Candidates_go_from_the_nearest_to_the_farthest()
    {
        var distances = FreeSpotSearch.Candidates(Target).Select(c => Math.Round(Distance(c, Target), 6)).ToList();

        Assert.Equal(distances.Order(), distances);
    }

    [Fact]
    public void The_search_covers_up_and_sideways_together_never_down()
    {
        var candidates = FreeSpotSearch.Candidates(Target).ToList();

        Assert.All(candidates, c => Assert.True(c.Y >= Target.Y - 1e-9, "never below the target"));
        Assert.Contains(new Vec3(Target.X + 0.5, Target.Y + 0.75, Target.Z), candidates);   // diagonal (the user's case)
        Assert.Contains(new Vec3(Target.X, Target.Y + 1.5, Target.Z), candidates);          // straight up to the radius
        Assert.Contains(new Vec3(Target.X, Target.Y, Target.Z - 1.5), candidates);          // sideways to the radius
    }

    [Fact]
    public void Every_candidate_is_within_the_radius_and_none_repeats()
    {
        var candidates = FreeSpotSearch.Candidates(Target).ToList();

        Assert.All(candidates, c => Assert.True(Distance(c, Target) <= FreeSpotSearch.Radius + 1e-9));
        Assert.Equal(candidates.Count, candidates.Distinct().Count());
    }

    [Fact]
    public void A_free_target_needs_nothing()
    {
        var spot = FreeSpotSearch.Find(Target, isPlayer: true, isFree: _ => true, NoWalls);

        Assert.Equal(new FreeSpot(MakeRoomOutcome.AlreadyFree, Target), spot);
    }

    [Fact]
    public void Feet_in_the_floor_go_straight_up_to_the_first_free_height()
    {
        var spot = FreeSpotSearch.Find(Target, isPlayer: true, isFree: p => p.Y >= Target.Y + 0.5 - 1e-9, NoWalls);

        Assert.Equal(new FreeSpot(MakeRoomOutcome.Moved, Target with { Y = Target.Y + 0.5 }), spot);
    }

    [Fact]
    public void The_users_case_floor_below_and_a_crate_on_one_side_moves_up_and_away_from_the_crate()
    {
        // Free only 0.75 m up or more (floor) and not within 0.3 m of the crate side at z + 0.3.
        var spot = FreeSpotSearch.Find(Target, isPlayer: true,
            isFree: p => p.Y >= Target.Y + 0.75 - 1e-9 && p.Z <= Target.Z - 0.25 + 1e-9, NoWalls);

        Assert.Equal(MakeRoomOutcome.Moved, spot.Outcome);
        Assert.Equal(Target.Y + 0.75, spot.Point!.Value.Y, precision: 9);
        Assert.Equal(Target.Z - 0.25, spot.Point.Value.Z, precision: 9);
    }

    [Fact]
    public void When_up_is_blocked_the_nearest_free_point_around_wins()
    {
        var spot = FreeSpotSearch.Find(Target, isPlayer: false,
            isFree: p => p.Y == Target.Y && Distance(p, Target) >= 0.5 - 1e-9, NoWalls);

        Assert.Equal(MakeRoomOutcome.Moved, spot.Outcome);
        Assert.Equal(0.5, Distance(spot.Point!.Value, Target), precision: 6);
    }

    [Fact]
    public void A_point_behind_a_wall_is_never_used()
    {
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
