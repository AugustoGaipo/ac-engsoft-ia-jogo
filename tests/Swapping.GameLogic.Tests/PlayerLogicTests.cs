namespace Swapping.GameLogic.Tests;

public sealed class ControlsTests
{
    [Theory]
    [InlineData(GameAction.MoveForward, InputKind.Key, "W")]
    [InlineData(GameAction.MoveBack, InputKind.Key, "S")]
    [InlineData(GameAction.MoveLeft, InputKind.Key, "A")]
    [InlineData(GameAction.MoveRight, InputKind.Key, "D")]
    [InlineData(GameAction.Jump, InputKind.Key, "Space")]
    [InlineData(GameAction.Hold, InputKind.Key, "E")]
    [InlineData(GameAction.Throw, InputKind.MouseButton, "Left")]
    [InlineData(GameAction.Swap, InputKind.MouseButton, "Right")]
    [InlineData(GameAction.RestartRoom, InputKind.Key, "R")]
    [InlineData(GameAction.Pause, InputKind.Key, "Escape")]
    public void Every_control_of_G1_has_its_binding(GameAction action, InputKind kind, string code)
    {
        Assert.Contains(new InputBinding(action, kind, code), Controls.Bindings);
    }

    [Fact]
    public void Every_action_has_exactly_one_binding_and_no_input_is_used_twice()
    {
        Assert.Equal(Enum.GetValues<GameAction>().Order(), Controls.Bindings.Select(b => b.Action).Order());
        Assert.Equal(Controls.Bindings.Count, Controls.Bindings.Select(b => (b.Kind, b.Code)).Distinct().Count());
    }

    [Fact]
    public void Action_names_are_stable_for_the_engine()
    {
        Assert.Equal("swap", Controls.Name(GameAction.Swap));
        Assert.Equal("move_forward", Controls.Name(GameAction.MoveForward));
        Assert.Equal("restart_room", Controls.Name(GameAction.RestartRoom));
    }
}

public sealed class PushImpulseTests
{
    [Fact]
    public void A_light_box_is_pushed_almost_at_walking_speed()
    {
        // 70 kg player at 3 m/s against a 10 kg box at rest: the box gets 3 × 70 / 80 = 2.625 m/s.
        Assert.Equal(10 * 2.625, PushImpulse.Impulse(70, 10, 3, bodySpeedAlong: 0), precision: 6);
    }

    [Fact]
    public void A_heavy_crate_barely_moves()
    {
        var light = PushImpulse.Impulse(70, 10, 3, 0) / 10;
        var heavy = PushImpulse.Impulse(70, 300, 3, 0) / 300;

        Assert.True(heavy < light / 4, $"{heavy} m/s vs {light} m/s");
        Assert.Equal(3.0 * 70 / 370, heavy, precision: 6);
    }

    [Fact]
    public void A_body_already_moving_fast_enough_gets_no_impulse()
    {
        Assert.Equal(0, PushImpulse.Impulse(70, 10, 3, bodySpeedAlong: 2.625));
        Assert.Equal(0, PushImpulse.Impulse(70, 10, 3, bodySpeedAlong: 5));
    }

    [Fact]
    public void Standing_still_pushes_nothing()
    {
        Assert.Equal(0, PushImpulse.Impulse(70, 10, 0, 0));
    }
}
