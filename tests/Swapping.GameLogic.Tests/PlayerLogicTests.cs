namespace Swapping.GameLogic.Tests;

public sealed class ControlsTests
{
    [Theory]
    [InlineData(GameAction.MoveForward, InputKind.Key, "W")]
    [InlineData(GameAction.MoveBack, InputKind.Key, "S")]
    [InlineData(GameAction.MoveLeft, InputKind.Key, "A")]
    [InlineData(GameAction.MoveRight, InputKind.Key, "D")]
    [InlineData(GameAction.Jump, InputKind.Key, "Space")]
    [InlineData(GameAction.Throw, InputKind.MouseButton, "Left")]
    [InlineData(GameAction.Swap, InputKind.MouseButton, "Right")]
    [InlineData(GameAction.RestartRoom, InputKind.Key, "R")]
    [InlineData(GameAction.Pause, InputKind.Key, "Escape")]
    [InlineData(GameAction.ChoosePosition, InputKind.Key, "Key1")]
    [InlineData(GameAction.ChooseMass, InputKind.Key, "Key2")]
    [InlineData(GameAction.ChooseEnergy, InputKind.Key, "Key3")]
    [InlineData(GameAction.PropertyPrevious, InputKind.MouseButton, "WheelUp")]
    [InlineData(GameAction.PropertyNext, InputKind.MouseButton, "WheelDown")]
    [InlineData(GameAction.HoldToRotate, InputKind.Key, "F")]
    [InlineData(GameAction.Sprint, InputKind.Key, "Shift")]
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
    // Draft numbers (play test 2026-11-03): 200 N of push, friction 0.6, g = 9.8.

    [Fact]
    public void A_light_box_moves_a_bit_slower_than_walking()
    {
        // 10 kg: friction 58.8 N → 3 × (1 − 58.8 / 200) = 2.118 m/s.
        Assert.Equal(3 * (1 - 58.8 / 200), PushImpulse.Speed(10, 3), precision: 6);
    }

    [Fact]
    public void A_30_kg_box_crawls()
    {
        Assert.InRange(PushImpulse.Speed(30, 3), 0.3, 0.4);
    }

    [Theory]
    [InlineData(34.1)]
    [InlineData(300)]
    public void A_box_heavier_than_the_push_can_beat_does_not_move(double massKg)
    {
        Assert.Equal(0, PushImpulse.Speed(massKg, 3));
        Assert.Equal(0, PushImpulse.Impulse(massKg, 3, bodySpeedAlong: 0));
    }

    [Fact]
    public void The_impulse_brings_the_box_up_to_the_push_speed()
    {
        Assert.Equal(10 * PushImpulse.Speed(10, 3), PushImpulse.Impulse(10, 3, bodySpeedAlong: 0), precision: 6);
        Assert.Equal(10 * (PushImpulse.Speed(10, 3) - 1), PushImpulse.Impulse(10, 3, bodySpeedAlong: 1), precision: 6);
    }

    [Fact]
    public void A_box_already_moving_fast_enough_gets_no_impulse()
    {
        Assert.Equal(0, PushImpulse.Impulse(10, 3, bodySpeedAlong: 2.5));
    }

    [Fact]
    public void Standing_still_pushes_nothing()
    {
        Assert.Equal(0, PushImpulse.Speed(10, 0));
        Assert.Equal(0, PushImpulse.Impulse(10, 0, 0));
    }
}
