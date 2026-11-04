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
