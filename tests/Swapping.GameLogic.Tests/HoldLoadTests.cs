namespace Swapping.GameLogic.Tests;

public sealed class HoldLoadTests
{
    [Theory]
    [InlineData(175, 70, true)]    // exactly 2.5 × your mass
    [InlineData(176, 70, false)]
    [InlineData(300, 70, false)]   // the heavy crate of room 2.3
    [InlineData(70, 300, true)]    // after taking the crate's mass
    public void You_hold_up_to_two_and_a_half_times_your_mass(double heldKg, double playerKg, bool can)
    {
        Assert.Equal(can, HoldLoad.CanHold(heldKg, playerKg));
    }

    [Theory]
    [InlineData(0, 1.0)]
    [InlineData(35, 1.0)]       // half your mass: still free
    [InlineData(35.001, 0.9)]   // just over half: 90 %
    [InlineData(105, 0.475)]    // halfway between 0.5 and 2.5: halfway between 90 % and 5 %
    [InlineData(175, 0.05)]     // 2.5 × your mass: 5 %
    public void Walking_slows_from_90_to_5_percent_between_half_and_two_and_a_half_times(double heldKg, double factor)
    {
        Assert.Equal(factor, HoldLoad.SpeedFactor(heldKg, 70), 3);
    }

    [Fact]
    public void The_jump_carries_the_held_mass_too()
    {
        // Same 343 J for 70 + 35 kg: h = E / (m g) = 343 / (105 × 9.8) ≈ 0.33 m instead of 0.5 m.
        var v = HoldLoad.JumpTakeoffSpeed(heldKg: 35, playerKg: 70);
        Assert.Equal(0.333, v * v / (2 * Rules.Physics.Formulas.Gravity), 3);
        Assert.Equal(Rules.Physics.Formulas.JumpTakeoffSpeed(70), HoldLoad.JumpTakeoffSpeed(0, 70));
    }
}
