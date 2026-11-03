using Swapping.Rules;
using Swapping.Rules.Model;
using Swapping.Rules.Operations;

namespace Swapping.GameLogic.Tests;

public sealed class RunTrackerTests
{
    private static readonly DateTimeOffset End = new(2026, 11, 3, 18, 0, 0, TimeSpan.Zero);

    /// <summary>A world with the player and one box, with <paramref name="swaps"/> position swaps already made.</summary>
    private static World WorldWithSwaps(int swaps)
    {
        var world = new World();
        world.Add(new Container("player", ContainerType.Player, 0.07, 0.36), new Vec3(0, 0.9, 0), 70, Vec3.Zero);
        world.Add(new Container("box", ContainerType.Object, 1, 1), new Vec3(2, 0.5, 0), 10, Vec3.Zero);
        var swap = new SwapOperation();
        for (var i = 0; i < swaps; i++)
        {
            var result = swap.Apply(world, new SwapRequest("player", "box", PropertyKind.Position), now: i * 10.0);
            Assert.True(result.IsApplied);
        }
        return world;
    }

    [Fact]
    public void A_new_run_is_all_zero()
    {
        var run = new RunTracker("level-1");
        run.EnterRoom(WorldWithSwaps(0));

        Assert.Equal((0, 0, 0, 0), (run.TimeMs, run.Swaps, run.Deaths, run.RoomRestarts));
        Assert.False(run.IsPaused);
        Assert.False(run.IsFinished);
    }

    [Fact]
    public void Time_adds_up_the_physics_steps()
    {
        var run = new RunTracker("level-1");
        for (var i = 0; i < 90; i++)
            run.Tick(1.0 / 60);

        Assert.Equal(1_500, run.TimeMs);
    }

    [Fact]
    public void The_pause_stops_the_time_and_resume_starts_it_again()
    {
        var run = new RunTracker("level-1");
        run.Tick(1.0);
        run.Pause();
        run.Tick(5.0);
        Assert.True(run.IsPaused);
        Assert.Equal(1_000, run.TimeMs);

        run.Resume();
        run.Tick(0.5);

        Assert.False(run.IsPaused);
        Assert.Equal(1_500, run.TimeMs);
    }

    [Fact]
    public void A_negative_step_is_a_programming_error()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RunTracker("level-1").Tick(-0.01));
    }

    [Fact]
    public void Swaps_are_the_live_count_of_the_current_room()
    {
        var run = new RunTracker("level-1");
        var room = WorldWithSwaps(0);
        run.EnterRoom(room);

        new SwapOperation().Apply(room, new SwapRequest("player", "box", PropertyKind.Position), now: 0);

        Assert.Equal(1, run.Swaps);
    }

    [Fact]
    public void Swaps_add_up_over_every_room_entered()
    {
        var run = new RunTracker("level-1");
        run.EnterRoom(WorldWithSwaps(2));
        run.EnterRoom(WorldWithSwaps(3));
        run.EnterRoom(WorldWithSwaps(1));

        Assert.Equal(6, run.Swaps);
    }

    [Fact]
    public void Restarting_the_room_keeps_time_and_swaps_and_counts_a_restart()
    {
        var run = new RunTracker("level-1");
        run.EnterRoom(WorldWithSwaps(2));
        run.Tick(10);

        run.RecordDeath();
        run.RestartRoom(WorldWithSwaps(0));

        Assert.Equal((10_000, 2, 1, 1), (run.TimeMs, run.Swaps, run.Deaths, run.RoomRestarts));
    }

    [Fact]
    public void A_restart_from_the_menu_is_not_a_death()
    {
        var run = new RunTracker("level-1");
        run.EnterRoom(WorldWithSwaps(1));

        run.RestartRoom(WorldWithSwaps(0));

        Assert.Equal((0, 1), (run.Deaths, run.RoomRestarts));
    }

    [Fact]
    public void Restarting_the_level_starts_a_new_run_from_zero()
    {
        var run = new RunTracker("level-1");
        run.EnterRoom(WorldWithSwaps(3));
        run.Tick(42);
        run.RecordDeath();
        run.RestartRoom(WorldWithSwaps(1));
        run.Pause();

        run.RestartLevel(WorldWithSwaps(0));

        Assert.Equal((0, 0, 0, 0), (run.TimeMs, run.Swaps, run.Deaths, run.RoomRestarts));
        Assert.False(run.IsPaused);
    }

    [Fact]
    public void Finish_gives_the_result_with_the_API_fields_and_stops_the_run()
    {
        var run = new RunTracker("level-1");
        run.EnterRoom(WorldWithSwaps(2));
        run.Tick(30.25);
        run.RecordDeath();
        run.RestartRoom(WorldWithSwaps(1));
        run.EnterRoom(WorldWithSwaps(4));

        var result = run.Finish(End);
        run.Tick(100);

        Assert.Equal(new RunResult("level-1", 30_250, 7, 1, 1, End), result);
        Assert.True(run.IsFinished);
        Assert.Equal(30_250, run.TimeMs);
        Assert.Equal(new[] { "LevelId", "TimeMs", "Swaps", "Deaths", "RoomRestarts", "FinishedAt" },
            typeof(RunResult).GetProperties().Select(p => p.Name));
    }

    [Fact]
    public void A_finished_run_cannot_be_finished_again()
    {
        var run = new RunTracker("level-1");
        run.Finish(End);

        Assert.Throws<InvalidOperationException>(() => run.Finish(End));
    }

    [Fact]
    public void A_very_long_run_does_not_overflow()
    {
        var run = new RunTracker("level-1");
        run.Tick(30.0 * 24 * 3600);

        Assert.Equal(int.MaxValue, run.TimeMs);
    }

    [Theory]
    [InlineData(0, "00:00.0")]
    [InlineData(99, "00:00.0")]
    [InlineData(100, "00:00.1")]
    [InlineData(59_999, "00:59.9")]
    [InlineData(61_234, "01:01.2")]
    [InlineData(3_600_000, "60:00.0")]
    [InlineData(6_000_000, "100:00.0")]
    public void The_clock_shows_minutes_seconds_and_tenths_without_rounding_up(int timeMs, string text)
    {
        Assert.Equal(text, RunClock.Format(timeMs));
    }
}
