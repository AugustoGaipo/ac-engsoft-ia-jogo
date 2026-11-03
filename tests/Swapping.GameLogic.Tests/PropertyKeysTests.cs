using Swapping.Rules.Model;

namespace Swapping.GameLogic.Tests;

/// <summary>SPEC-game slice 2, SC 15: keys 1/2/3 pick the property; a locked one is refused; the default is the first unlocked.</summary>
public sealed class PropertyKeysTests
{
    private static HashSet<IPropertyKind> Unlocked(params IPropertyKind[] kinds) => [.. kinds];

    [Theory]
    [InlineData(GameAction.ChoosePosition, "position")]
    [InlineData(GameAction.ChooseMass, "mass")]
    [InlineData(GameAction.ChooseEnergy, "energy")]
    public void Each_key_maps_to_its_property(GameAction action, string expected)
    {
        var all = Unlocked(PropertyKind.Position, PropertyKind.Mass, PropertyKind.KineticEnergy);
        var chosen = PropertyKeys.Choose(action, PropertyKind.Position, all);
        Assert.Equal(expected, chosen.Name);
    }

    [Fact]
    public void A_property_the_level_does_not_unlock_is_refused_and_the_choice_stays()
    {
        var onlyPosition = Unlocked(PropertyKind.Position);
        Assert.Same(PropertyKind.Position, PropertyKeys.Choose(GameAction.ChooseMass, PropertyKind.Position, onlyPosition));
        Assert.Same(PropertyKind.Position, PropertyKeys.Choose(GameAction.ChooseEnergy, PropertyKind.Position, onlyPosition));
    }

    [Fact]
    public void Other_actions_do_not_change_the_choice()
    {
        var all = Unlocked(PropertyKind.Position, PropertyKind.Mass);
        Assert.Same(PropertyKind.Mass, PropertyKeys.Choose(GameAction.Jump, PropertyKind.Mass, all));
    }

    [Fact]
    public void The_default_is_the_first_unlocked_property()
    {
        Assert.Same(PropertyKind.Position, PropertyKeys.Default(Unlocked(PropertyKind.Position, PropertyKind.Mass)));
        Assert.Same(PropertyKind.Mass, PropertyKeys.Default(Unlocked(PropertyKind.Mass, PropertyKind.KineticEnergy)));
    }

    [Fact]
    public void A_choice_that_became_locked_falls_back_to_the_default()
    {
        // A new room/level may unlock fewer properties than the one the player had picked.
        Assert.Same(PropertyKind.Position, PropertyKeys.Valid(PropertyKind.KineticEnergy, Unlocked(PropertyKind.Position)));
        Assert.Same(PropertyKind.Mass, PropertyKeys.Valid(PropertyKind.Mass, Unlocked(PropertyKind.Position, PropertyKind.Mass)));
    }

    [Theory]
    [InlineData("position", "Posição")]
    [InlineData("mass", "Massa")]
    [InlineData("energy", "Energia")]
    public void The_hud_names_the_property(string id, string label)
    {
        var kind = id switch { "position" => PropertyKind.Position, "mass" => PropertyKind.Mass, _ => (IPropertyKind)PropertyKind.KineticEnergy };
        Assert.Equal(label, PropertyKeys.Label(kind));
    }
}

public sealed class PropertyWheelTests
{
    private static HashSet<IPropertyKind> Unlocked(params IPropertyKind[] kinds) => [.. kinds];

    [Fact]
    public void The_wheel_goes_to_the_next_unlocked_property_and_wraps_around()
    {
        var all = Unlocked(PropertyKind.Position, PropertyKind.Mass, PropertyKind.KineticEnergy);
        Assert.Same(PropertyKind.Mass, PropertyKeys.Cycle(PropertyKind.Position, +1, all));
        Assert.Same(PropertyKind.KineticEnergy, PropertyKeys.Cycle(PropertyKind.Mass, +1, all));
        Assert.Same(PropertyKind.Position, PropertyKeys.Cycle(PropertyKind.KineticEnergy, +1, all));
    }

    [Fact]
    public void The_wheel_the_other_way_goes_back_and_wraps_around()
    {
        var all = Unlocked(PropertyKind.Position, PropertyKind.Mass, PropertyKind.KineticEnergy);
        Assert.Same(PropertyKind.KineticEnergy, PropertyKeys.Cycle(PropertyKind.Position, -1, all));
        Assert.Same(PropertyKind.Position, PropertyKeys.Cycle(PropertyKind.Mass, -1, all));
    }

    [Fact]
    public void The_wheel_skips_properties_the_level_does_not_unlock()
    {
        var positionAndEnergy = Unlocked(PropertyKind.Position, PropertyKind.KineticEnergy);
        Assert.Same(PropertyKind.KineticEnergy, PropertyKeys.Cycle(PropertyKind.Position, +1, positionAndEnergy));
        Assert.Same(PropertyKind.Position, PropertyKeys.Cycle(PropertyKind.KineticEnergy, +1, positionAndEnergy));
    }

    [Fact]
    public void With_a_single_unlocked_property_the_wheel_changes_nothing()
    {
        Assert.Same(PropertyKind.Position, PropertyKeys.Cycle(PropertyKind.Position, +1, Unlocked(PropertyKind.Position)));
    }

    [Fact]
    public void A_choice_that_is_not_unlocked_goes_to_the_default_first()
    {
        Assert.Same(PropertyKind.Position, PropertyKeys.Cycle(PropertyKind.KineticEnergy, +1, Unlocked(PropertyKind.Position)));
    }

    [Theory]
    [InlineData(GameAction.PropertyPrevious, -1)]
    [InlineData(GameAction.PropertyNext, +1)]
    [InlineData(GameAction.Swap, 0)]
    public void Wheel_actions_have_a_direction(GameAction action, int direction)
    {
        Assert.Equal(direction, PropertyKeys.WheelDirection(action));
    }
}

public sealed class HeldSpinTests
{
    [Fact]
    public void The_mouse_to_the_right_turns_the_object_the_same_way_the_view_turns()
    {
        // 0.0025 rad per pixel (the player's sensitivity): 100 px to the right = -0.25 rad.
        Assert.Equal(-0.25 * 180 / Math.PI, HeldSpin.Degrees(100, 0.0025), 6);
    }

    [Fact]
    public void The_mouse_to_the_left_turns_it_the_other_way()
    {
        Assert.True(HeldSpin.Degrees(-40, 0.0025) > 0);
    }

    [Fact]
    public void No_mouse_motion_no_turn()
    {
        Assert.Equal(0, HeldSpin.Degrees(0, 0.0025));
    }
}

public sealed class HeldTiltTests
{
    [Fact]
    public void The_mouse_up_tilts_the_object_the_way_the_view_looks_up()
    {
        Assert.Equal(0.25 * 180 / Math.PI, HeldSpin.TiltDegrees(-100, 0.0025), 6);
    }

    [Fact]
    public void The_mouse_down_tilts_it_the_other_way()
    {
        Assert.True(HeldSpin.TiltDegrees(40, 0.0025) < 0);
    }

    [Fact]
    public void No_vertical_motion_no_tilt()
    {
        Assert.Equal(0, HeldSpin.TiltDegrees(0, 0.0025));
    }
}
