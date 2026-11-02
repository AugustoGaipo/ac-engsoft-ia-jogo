using Swapping.Api.Progress;

namespace Swapping.Api.Tests.Progress;

public class RankingOrderTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);

    private static ResultScore Score(int timeMs, int swaps, int secondsAfterT0 = 0) =>
        new(timeMs, swaps, T0.AddSeconds(secondsAfterT0));

    [Fact]
    public void Time_mode_sorts_by_time_then_swaps()
    {
        var a = Score(50_000, 9);
        var b = Score(50_000, 7);
        var c = Score(40_000, 30);

        var sorted = new[] { a, b, c }.Order(RankingOrder.For(RankingMode.Time)).ToList();

        Assert.Equal(new[] { c, b, a }, sorted);
    }

    [Fact]
    public void Moves_mode_sorts_by_swaps_then_time()
    {
        var a = Score(50_000, 9);
        var b = Score(50_000, 7);
        var c = Score(40_000, 30);

        var sorted = new[] { a, b, c }.Order(RankingOrder.For(RankingMode.Moves)).ToList();

        Assert.Equal(new[] { b, a, c }, sorted);
    }

    [Theory]
    [InlineData(RankingMode.Time)]
    [InlineData(RankingMode.Moves)]
    public void Equal_results_are_ordered_by_who_got_it_first(RankingMode mode)
    {
        var first = Score(50_000, 7, secondsAfterT0: 0);
        var later = Score(50_000, 7, secondsAfterT0: 60);

        var sorted = new[] { later, first }.Order(RankingOrder.For(mode)).ToList();

        Assert.Equal(new[] { first, later }, sorted);
    }

    [Fact]
    public void Moves_mode_breaks_equal_swaps_by_time_before_date()
    {
        var slowEarly = Score(60_000, 7, secondsAfterT0: 0);
        var fastLate = Score(50_000, 7, secondsAfterT0: 60);

        var sorted = new[] { slowEarly, fastLate }.Order(RankingOrder.For(RankingMode.Moves)).ToList();

        Assert.Equal(new[] { fastLate, slowEarly }, sorted);
    }

    [Fact]
    public void Anything_is_better_than_no_result()
    {
        Assert.True(RankingOrder.IsBetter(RankingMode.Time, Score(99_000, 99), null));
        Assert.True(RankingOrder.IsBetter(RankingMode.Moves, Score(99_000, 99), null));
    }

    [Fact]
    public void Better_means_strictly_better_in_that_mode()
    {
        var current = Score(50_000, 10);

        Assert.True(RankingOrder.IsBetter(RankingMode.Time, Score(49_999, 50), current));
        Assert.False(RankingOrder.IsBetter(RankingMode.Time, Score(50_001, 0), current));
        Assert.True(RankingOrder.IsBetter(RankingMode.Time, Score(50_000, 9), current)); // tie on time, fewer swaps
        Assert.True(RankingOrder.IsBetter(RankingMode.Moves, Score(90_000, 9), current));
        Assert.False(RankingOrder.IsBetter(RankingMode.Moves, Score(1_000, 11), current));
        Assert.True(RankingOrder.IsBetter(RankingMode.Moves, Score(49_000, 10), current)); // tie on swaps, faster
    }

    [Theory]
    [InlineData(RankingMode.Time)]
    [InlineData(RankingMode.Moves)]
    public void The_same_result_arriving_later_never_replaces_the_old_one(RankingMode mode)
    {
        var current = Score(50_000, 10, secondsAfterT0: 0);
        var sameLater = Score(50_000, 10, secondsAfterT0: 3600);
        var sameEarlier = Score(50_000, 10, secondsAfterT0: -3600); // a client clock cannot help either

        Assert.False(RankingOrder.IsBetter(mode, sameLater, current));
        Assert.False(RankingOrder.IsBetter(mode, sameEarlier, current));
    }

    [Fact]
    public void An_unknown_mode_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RankingOrder.For((RankingMode)99));
    }
}
