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

public sealed class HeldSpinTests
{
    [Fact]
    public void Each_wheel_notch_turns_fifteen_degrees()
    {
        Assert.Equal(15, HeldSpin.Degrees(GameAction.RotateHeldLeft));
        Assert.Equal(-15, HeldSpin.Degrees(GameAction.RotateHeldRight));
    }

    [Fact]
    public void Other_actions_do_not_turn_it()
    {
        Assert.Equal(0, HeldSpin.Degrees(GameAction.Swap));
    }
}
