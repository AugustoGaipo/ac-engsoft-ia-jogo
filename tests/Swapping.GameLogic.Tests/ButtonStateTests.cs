using Swapping.Levels.Model;

namespace Swapping.GameLogic.Tests;

public sealed class ButtonStateTests
{
    private static readonly WeightOnButton Box10 = new(IsPlayer: false, MassKg: 10);
    private static readonly WeightOnButton Ball2 = new(IsPlayer: false, MassKg: 2);
    private static readonly WeightOnButton Player = new(IsPlayer: true, MassKg: 70);

    [Theory]
    [InlineData(9.99, true)]
    [InlineData(10.0, true)]
    [InlineData(10.01, false)]
    public void The_button_is_pressed_from_the_required_mass_up(double required, bool pressed)
    {
        Assert.Equal(pressed, ButtonState.IsPressed(required, ButtonDetects.Objects, [Box10]));
    }

    [Fact]
    public void Nothing_on_it_is_not_pressed()
    {
        Assert.False(ButtonState.IsPressed(5, ButtonDetects.Any, []));
    }

    [Fact]
    public void The_masses_on_it_add_up()
    {
        Assert.True(ButtonState.IsPressed(4, ButtonDetects.Objects, [Ball2, Ball2]));
        Assert.False(ButtonState.IsPressed(4.5, ButtonDetects.Objects, [Ball2, Ball2]));
    }

    [Fact]
    public void An_objects_button_ignores_the_player()
    {
        Assert.False(ButtonState.IsPressed(5, ButtonDetects.Objects, [Player]));
        Assert.False(ButtonState.IsPressed(5, ButtonDetects.Objects, [Player, Ball2]));
    }

    [Fact]
    public void An_any_button_counts_the_player()
    {
        Assert.True(ButtonState.IsPressed(5, ButtonDetects.Any, [Player]));
    }

    [Fact]
    public void A_door_is_open_only_while_every_button_is_pressed()
    {
        var pressed = new HashSet<string> { "b1" };

        Assert.True(ButtonState.IsDoorOpen(["b1"], pressed));
        Assert.False(ButtonState.IsDoorOpen(["b1", "b2"], pressed));
        Assert.False(ButtonState.IsDoorOpen(["b1"], new HashSet<string>()));
    }

    [Fact]
    public void A_door_with_no_button_never_opens()
    {
        Assert.False(ButtonState.IsDoorOpen([], new HashSet<string> { "b1" }));
    }
}
