namespace Swapping.GameLogic.Tests;

/// <summary>Running (Shift): 1.4 × the walk, on top of the hold slowdown; pushing still caps it.</summary>
public sealed class SprintTests
{
    [Fact]
    public void Running_is_1_4_times_the_walk()
    {
        Assert.Equal(3.0, Sprint.Speed(3, sprinting: false), 6);
        Assert.Equal(4.2, Sprint.Speed(3, sprinting: true), 6);
    }

    [Fact]
    public void A_heavy_load_slows_the_run_too()
    {
        // Holding 2.5 × your mass leaves 5 % of the walk; running is 1.4 × that.
        Assert.Equal(3 * 0.05 * 1.4, Sprint.Speed(3 * HoldLoad.SpeedFactor(175, 70), sprinting: true), 6);
    }
}
