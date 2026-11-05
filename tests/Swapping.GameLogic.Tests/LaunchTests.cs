namespace Swapping.GameLogic.Tests;

/// <summary>
/// Task 17: an energy swap can give the player more speed than walking. That speed is kept (launched) and
/// slows down by friction on the floor (fast) and by the air (slowly), until walking takes over again.
/// </summary>
public sealed class LaunchTests
{
    [Theory]
    [InlineData(3.0, false)]    // the walking speed is not a launch
    [InlineData(3.2, true)]
    [InlineData(16, true)]
    public void A_launch_is_any_horizontal_speed_above_walking(double speed, bool launched)
    {
        Assert.Equal(launched, Launch.Starts(speed, walkSpeed: 3));
    }

    [Fact]
    public void On_the_floor_friction_brakes_4_m_s2_in_the_air_only_0_3()
    {
        Assert.Equal(16 - 4 * 0.5, Launch.Step(16, onFloor: true, deltaSeconds: 0.5), 6);
        Assert.Equal(16 - 0.3 * 0.5, Launch.Step(16, onFloor: false, deltaSeconds: 0.5), 6);
        Assert.Equal(0, Launch.Step(1, onFloor: true, deltaSeconds: 1), 6);   // never backwards
    }

    [Fact]
    public void An_8_m_s_slide_on_the_floor_stops_within_8_m()
    {
        // v² = 2 a d → d = 8² / (2 × 4) = 8 m to a stop.
        Assert.Equal(8, Launch.FloorDistance(8, 0), 6);
    }
}

/// <summary>
/// User play test 2026-11-05 (room 3.3): walking (≈ 315 J) and then taking a 5 kg mass kept the energy, so the
/// player shot forward at ≈ 11 m/s with no ball. Only an energy swap launches; after a mass swap walking stays walking.
/// </summary>
public sealed class LaunchFromSwapTests
{
    [Fact]
    public void A_mass_swap_never_launches_even_when_the_kept_energy_means_a_high_speed()
    {
        Assert.False(Launch.FromSwap(energySwap: false, horizontalSpeed: 11.2, walkSpeed: 3));
    }

    [Fact]
    public void An_energy_swap_launches_above_walking_speed()
    {
        Assert.True(Launch.FromSwap(energySwap: true, horizontalSpeed: 11.2, walkSpeed: 3));
        Assert.False(Launch.FromSwap(energySwap: true, horizontalSpeed: 2, walkSpeed: 3));
    }
}
