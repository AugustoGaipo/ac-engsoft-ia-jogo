namespace Swapping.GameLogic.Tests;

/// <summary>SPEC-game slice 2, SC 17: a card is open, locked (previous level not finished) or under construction (no scene yet).</summary>
public sealed class LevelAccessTests
{
    private static readonly string[] Catalog = ["level-1", "level-2", "level-3"];
    private static readonly DateTimeOffset At = new(2026, 11, 3, 20, 0, 0, TimeSpan.Zero);

    private static void Finish(LevelProgress progress, string id) => progress.Record(new RunResult(id, 30_000, 5, 0, 0, At));

    [Fact]
    public void Level_without_a_scene_is_under_construction_even_when_open()
    {
        var progress = new LevelProgress(Catalog);
        Assert.Equal(LevelStatus.UnderConstruction, LevelAccess.StatusOf("level-1", progress, hasScene: false));
    }

    [Fact]
    public void Level_without_a_scene_is_under_construction_even_when_also_locked()
    {
        var progress = new LevelProgress(Catalog);
        Assert.Equal(LevelStatus.UnderConstruction, LevelAccess.StatusOf("level-3", progress, hasScene: false));
    }

    [Fact]
    public void Level_with_a_scene_but_unfinished_previous_level_is_locked()
    {
        var progress = new LevelProgress(Catalog);
        Assert.Equal(LevelStatus.Locked, LevelAccess.StatusOf("level-2", progress, hasScene: true));
    }

    [Fact]
    public void Level_with_a_scene_and_finished_previous_level_is_available()
    {
        var progress = new LevelProgress(Catalog);
        Finish(progress, "level-1");
        Assert.Equal(LevelStatus.Available, LevelAccess.StatusOf("level-2", progress, hasScene: true));
        Assert.Equal(LevelStatus.Available, LevelAccess.StatusOf("level-1", progress, hasScene: true));
    }

    [Fact]
    public void Unknown_level_is_locked()
    {
        Assert.Equal(LevelStatus.Locked, LevelAccess.StatusOf("level-9", new LevelProgress(Catalog), hasScene: true));
    }

    [Fact]
    public void Continue_is_the_first_available_unfinished_level()
    {
        var progress = new LevelProgress(Catalog);
        Finish(progress, "level-1");
        Assert.Equal("level-2", LevelAccess.ContinueLevel(progress, Catalog, id => true));
    }

    [Fact]
    public void Continue_skips_levels_under_construction_and_falls_back_to_the_last_playable_one()
    {
        var progress = new LevelProgress(Catalog);
        Finish(progress, "level-1");
        // Only level 1 has a scene: the next open level is not playable, so "continue" replays level 1.
        Assert.Equal("level-1", LevelAccess.ContinueLevel(progress, Catalog, id => id == "level-1"));
    }

    [Fact]
    public void Continue_is_null_when_no_level_has_a_scene()
    {
        Assert.Null(LevelAccess.ContinueLevel(new LevelProgress(Catalog), Catalog, _ => false));
    }

    [Fact]
    public void Next_playable_is_the_next_level_only_when_it_can_be_played()
    {
        var progress = new LevelProgress(Catalog);
        Finish(progress, "level-1");
        Assert.Equal("level-2", LevelAccess.NextPlayable("level-1", progress, id => true));
        Assert.Null(LevelAccess.NextPlayable("level-1", progress, id => id == "level-1"));
        Finish(progress, "level-2");
        Finish(progress, "level-3");
        Assert.Null(LevelAccess.NextPlayable("level-3", progress, id => true));
    }
}
