using Swapping.Api.Progress;

namespace Swapping.Api.Tests.Progress;

public class ResultCheckTests
{
    private static bool Ok(int timeMs = 60_000, int swaps = 10, int deaths = 0, int roomRestarts = 0, int rooms = 3) =>
        ResultCheck.IsPlausible(new ResultValues(timeMs, swaps, deaths, roomRestarts), rooms);

    [Theory]
    [InlineData(2_999, false)]
    [InlineData(3_000, true)]
    [InlineData(86_400_000, true)]
    [InlineData(86_400_001, false)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void Time_needs_at_least_one_second_per_room_and_at_most_24_hours(int timeMs, bool expected)
    {
        Assert.Equal(expected, Ok(timeMs: timeMs, rooms: 3));
    }

    [Fact]
    public void The_time_floor_follows_the_room_count()
    {
        Assert.True(Ok(timeMs: 2_000, rooms: 2));
        Assert.False(Ok(timeMs: 3_999, rooms: 4));
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(10_000, true)]
    [InlineData(10_001, false)]
    public void Swaps_go_from_0_to_10_000(int swaps, bool expected)
    {
        Assert.Equal(expected, Ok(swaps: swaps));
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(10_000, true)]
    [InlineData(10_001, false)]
    public void Deaths_go_from_0_to_10_000(int deaths, bool expected)
    {
        Assert.Equal(expected, Ok(deaths: deaths));
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(10_000, true)]
    [InlineData(10_001, false)]
    public void Room_restarts_go_from_0_to_10_000(int roomRestarts, bool expected)
    {
        Assert.Equal(expected, Ok(roomRestarts: roomRestarts));
    }
}
