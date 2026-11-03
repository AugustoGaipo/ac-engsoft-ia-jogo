namespace Swapping.GameLogic.Tests;

/// <summary>SPEC-game slice 2, SC 13 and 14: local progress, unlocking in catalog order, two bests.</summary>
public sealed class LevelProgressTests
{
    private static readonly string[] Catalog = ["level-1", "level-2", "level-3", "level-5"];
    private static readonly DateTimeOffset At = new(2026, 11, 3, 20, 0, 0, TimeSpan.Zero);

    private static RunResult Run(string level, int timeMs, int swaps) => new(level, timeMs, swaps, 0, 0, At);

    [Fact]
    public void Fresh_progress_unlocks_only_the_first_level()
    {
        var progress = new LevelProgress(Catalog);
        Assert.True(progress.IsUnlocked("level-1"));
        Assert.False(progress.IsUnlocked("level-2"));
        Assert.False(progress.IsUnlocked("level-5"));
        Assert.Null(progress.Best("level-1"));
    }

    [Fact]
    public void Finishing_a_level_unlocks_the_next_one_in_catalog_order_even_with_a_gap()
    {
        var progress = new LevelProgress(Catalog);
        progress.Record(Run("level-1", 30_000, 5));
        Assert.True(progress.IsUnlocked("level-2"));
        Assert.False(progress.IsUnlocked("level-3"));

        progress.Record(Run("level-2", 30_000, 5));
        progress.Record(Run("level-3", 30_000, 5));
        Assert.True(progress.IsUnlocked("level-5"));
    }

    [Fact]
    public void Time_and_swaps_are_kept_as_two_independent_bests()
    {
        var progress = new LevelProgress(Catalog);
        progress.Record(Run("level-1", 40_000, 3));
        progress.Record(Run("level-1", 20_000, 9));
        progress.Record(Run("level-1", 50_000, 8));

        var best = progress.Best("level-1")!;
        Assert.Equal(20_000, best.TimeMs);
        Assert.Equal(3, best.Swaps);
    }

    [Fact]
    public void A_result_of_an_unknown_or_locked_level_is_ignored()
    {
        var progress = new LevelProgress(Catalog);
        progress.Record(Run("level-9", 1_000, 1));
        progress.Record(Run("level-3", 1_000, 1));
        Assert.Null(progress.Best("level-9"));
        Assert.Null(progress.Best("level-3"));
        Assert.False(progress.IsUnlocked("level-5"));
    }

    [Fact]
    public void Continue_is_the_first_unlocked_level_not_finished_or_the_last_when_all_are_done()
    {
        var progress = new LevelProgress(Catalog);
        Assert.Equal("level-1", progress.ContinueLevel());

        progress.Record(Run("level-1", 30_000, 5));
        Assert.Equal("level-2", progress.ContinueLevel());

        foreach (var level in Catalog)
            progress.Record(Run(level, 30_000, 5));
        Assert.Equal("level-5", progress.ContinueLevel());
    }

    [Fact]
    public void Next_level_follows_the_catalog_and_is_null_after_the_last()
    {
        var progress = new LevelProgress(Catalog);
        Assert.Equal("level-2", progress.NextLevel("level-1"));
        Assert.Equal("level-5", progress.NextLevel("level-3"));
        Assert.Null(progress.NextLevel("level-5"));
        Assert.Null(progress.NextLevel("level-9"));
    }

    [Fact]
    public void Progress_round_trips_through_json()
    {
        var progress = new LevelProgress(Catalog);
        progress.Record(Run("level-1", 30_000, 5));
        progress.Record(Run("level-2", 45_000, 7));

        var loaded = LevelProgress.FromJson(progress.ToJson(), Catalog);

        Assert.True(loaded.IsUnlocked("level-3"));
        Assert.Equal(new BestRun(45_000, 7), loaded.Best("level-2"));
        Assert.False(loaded.IsCorrupt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"schemaVersion\": 99, \"levels\": {}}")]
    [InlineData("{\"schemaVersion\": 1, \"levels\": {\"level-1\": {\"timeMs\": -5, \"swaps\": 1}}}")]
    public void Unreadable_or_unknown_text_gives_fresh_progress_marked_corrupt(string text)
    {
        var progress = LevelProgress.FromJson(text, Catalog);
        Assert.True(progress.IsCorrupt);
        Assert.False(progress.IsUnlocked("level-2"));
        Assert.Null(progress.Best("level-1"));
    }

    [Fact]
    public void Unknown_level_ids_in_the_file_are_ignored()
    {
        var text = "{\"schemaVersion\": 1, \"levels\": {\"level-1\": {\"timeMs\": 30000, \"swaps\": 5}, \"level-7\": {\"timeMs\": 1, \"swaps\": 1}}}";
        var progress = LevelProgress.FromJson(text, Catalog);
        Assert.False(progress.IsCorrupt);
        Assert.True(progress.IsUnlocked("level-2"));
        Assert.Null(progress.Best("level-7"));
        Assert.DoesNotContain("level-7", progress.ToJson());
    }
}
