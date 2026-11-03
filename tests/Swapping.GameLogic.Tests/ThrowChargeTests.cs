namespace Swapping.GameLogic.Tests;

/// <summary>The throw bar (user's request, 2026-11-03): hold to charge for 3 s, linear; a quick tap only drops.</summary>
public sealed class ThrowChargeTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1.5, 0.5)]
    [InlineData(3, 1)]
    [InlineData(10, 1)]
    [InlineData(-1, 0)]
    public void The_bar_fills_linearly_in_three_seconds(double held, double fraction) =>
        Assert.Equal(fraction, ThrowCharge.Fraction(held), 1e-9);

    [Fact]
    public void A_full_bar_gives_400_joules_and_half_gives_200()
    {
        Assert.Equal(400, ThrowCharge.EnergyJoules(3), 1e-9);
        Assert.Equal(200, ThrowCharge.EnergyJoules(1.5), 1e-9);
    }

    [Theory]
    [InlineData(0.05, true)]
    [InlineData(0.149, true)]
    [InlineData(0.15, false)]
    [InlineData(2, false)]
    public void A_quick_tap_only_drops(double held, bool tap) =>
        Assert.Equal(tap, ThrowCharge.IsTap(held));
}
