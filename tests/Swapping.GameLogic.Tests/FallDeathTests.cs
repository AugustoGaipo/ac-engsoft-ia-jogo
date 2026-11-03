namespace Swapping.GameLogic.Tests;

public sealed class FallDeathTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(11.9, false)]
    [InlineData(12.0, false)]
    [InlineData(12.1, true)]
    [InlineData(30, true)]
    public void Landing_faster_than_12_m_s_kills(double landingSpeed, bool kills)
    {
        Assert.Equal(kills, FallDeath.Kills(landingSpeed));
    }

    [Fact]
    public void The_limit_is_about_a_7_m_drop()
    {
        // v² = 2 g h → h = 12² / (2 × 9.8) ≈ 7.35 m.
        Assert.InRange(FallDeath.HeightForLimit, 7.3, 7.4);
    }
}
