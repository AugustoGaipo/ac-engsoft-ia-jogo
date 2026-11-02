using Swapping.Rules.Model;
using Swapping.Rules.Operations;
using Swapping.Rules.Policies;

namespace Swapping.Rules.Tests;

/// <summary>
/// Findings of the code review (autonomous item 7). Malformed requests and bodies are programming
/// errors, so they throw instead of silently counting a move or poisoning the variables.
/// </summary>
public class RequestValidationTests
{
    private static readonly LevelRules Permissive = LevelRules.Default with
    {
        AllowObjectToObject = true,
        AllowSimultaneousSwaps = true,
        CooldownSeconds = 0,
    };

    private static World TwoBodies()
    {
        var world = new World(Permissive);
        world.Add(new Container("player", ContainerType.Player, 0.07, 0.36), Vec3.Zero, 70, Vec3.Zero);
        world.Add(new Container("A", ContainerType.Object, 0.001, 0.01), new Vec3(1, 0, 0), 2, Vec3.Zero);
        return world;
    }

    // Before the fix an empty request was "applied": no variable moved, but it counted 1 move and
    // started the cooldown.
    [Fact]
    public void EmptyPropertyList_Throws()
    {
        Assert.Throws<ArgumentException>(() => new SwapRequest("player", "A", Array.Empty<IPropertyKind>()));
    }

    // Before the fix the same property twice was exchanged twice, so nothing changed, yet it counted a move.
    [Fact]
    public void SamePropertyTwice_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new SwapRequest("player", "A", new IPropertyKind[] { PropertyKind.Mass, PropertyKind.Mass }));
    }

    [Fact]
    public void WithExpression_IsValidatedToo()
    {
        var request = new SwapRequest("player", "A", PropertyKind.Mass);

        Assert.Throws<ArgumentException>(() => request with { Properties = Array.Empty<IPropertyKind>() });
    }

    [Fact]
    public void ValidSimultaneousRequest_StillWorks()
    {
        var world = TwoBodies();

        var result = new SwapOperation().Apply(world,
            new SwapRequest("player", "A", new IPropertyKind[] { PropertyKind.Mass, PropertyKind.Position }), now: 0);

        Assert.True(result.IsApplied);
        Assert.Equal(1, world.MoveCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    public void AddingABodyWithoutPositiveMass_Throws(double massKg)
    {
        var world = new World(Permissive);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            world.Add(new Container("A", ContainerType.Object, 0.001, 0.01), Vec3.Zero, massKg, Vec3.Zero));
        Assert.Empty(world.Containers);
    }

    [Fact]
    public void PhysicsWriteBackWithNaN_Throws_AndKeepsTheVariables()
    {
        var world = TwoBodies();
        var before = world.VariableUsedBy("A", PropertyKind.KineticEnergy).Value;

        Assert.Throws<ArgumentException>(() =>
            world.SyncFromPhysics("A", Vec3.Zero, new Vec3(double.NaN, 0, 0)));
        Assert.Equal(before, world.VariableUsedBy("A", PropertyKind.KineticEnergy).Value);
    }
}
