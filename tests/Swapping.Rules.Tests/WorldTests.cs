using Swapping.Rules.Model;

namespace Swapping.Rules.Tests;

public class WorldTests
{
    private const double Tol = 0.001;

    private static World NewWorld()
    {
        var world = new World();
        world.Add(new Container("player", ContainerType.Player, VolumeM3: 0.07, ContactAreaM2: 0.1),
            position: new Vec3(0, 0, 0), massKg: 70, velocity: Vec3.Zero);
        world.Add(new Container("A", ContainerType.Object, VolumeM3: 0.001, ContactAreaM2: 0.01),
            position: new Vec3(2, 0, 0), massKg: 2, velocity: new Vec3(3, 0, 0));
        world.Add(new Container("B", ContainerType.Object, VolumeM3: 0.001, ContactAreaM2: 0.01),
            position: new Vec3(4, 0, 0), massKg: 3, velocity: Vec3.Zero);
        return world;
    }

    [Fact]
    public void NewContainer_UsesItsOwnVariables_AndInvariantHolds()
    {
        var world = NewWorld();

        foreach (var id in new[] { "player", "A", "B" })
            foreach (var kind in PropertyKind.All)
            {
                var variable = world.VariableUsedBy(id, kind);
                Assert.Equal(id, variable.Owner);
                Assert.Same(kind, variable.Kind);
                Assert.Equal(id, world.UserOf(variable));
            }

        Assert.True(world.CheckInvariant());
    }

    [Fact]
    public void NewContainer_VariablesHoldInitialValues()
    {
        var world = NewWorld();

        Assert.Equal(new Vec3(2, 0, 0), world.VariableUsedBy("A", PropertyKind.Position).Value.Vector);
        Assert.Equal(2.0, world.VariableUsedBy("A", PropertyKind.Mass).Value.Scalar, Tol);
        var energy = world.VariableUsedBy("A", PropertyKind.KineticEnergy).Value;
        Assert.Equal(9.0, energy.Scalar, Tol);           // ½ · 2 · 3²
        Assert.Equal(new Vec3(1, 0, 0), energy.Vector); // direction of travel
        Assert.Equal(0.0, world.VariableUsedBy("B", PropertyKind.KineticEnergy).Value.Scalar, Tol);
    }

    [Fact]
    public void VariableIds_AreOwnerDotProperty()
    {
        var world = NewWorld();
        Assert.Equal("A.mass", world.VariableUsedBy("A", PropertyKind.Mass).Id);
        Assert.Equal("player.position", world.VariableUsedBy("player", PropertyKind.Position).Id);
        Assert.Equal("B.energy", world.VariableUsedBy("B", PropertyKind.KineticEnergy).Id);
    }

    [Fact]
    public void ExchangeVariables_SwapsUsersOfThatKindOnly()
    {
        var world = NewWorld();

        world.ExchangeVariables("player", "A", PropertyKind.Mass);

        Assert.Equal("A", world.VariableUsedBy("player", PropertyKind.Mass).Owner);
        Assert.Equal("player", world.VariableUsedBy("A", PropertyKind.Mass).Owner);
        Assert.Equal("player", world.UserOf(world.VariableUsedBy("player", PropertyKind.Mass)));
        Assert.Equal("player", world.VariableUsedBy("player", PropertyKind.Position).Owner);
        Assert.Equal("player", world.VariableUsedBy("player", PropertyKind.KineticEnergy).Owner);
        Assert.Equal("A", world.VariableUsedBy("A", PropertyKind.Position).Owner);
        Assert.Equal("A", world.VariableUsedBy("A", PropertyKind.KineticEnergy).Owner);
        Assert.True(world.CheckInvariant());
    }

    [Fact]
    public void ExchangeVariables_Twice_ChainsVariables()
    {
        var world = NewWorld();

        world.ExchangeVariables("player", "A", PropertyKind.Mass);
        world.ExchangeVariables("player", "B", PropertyKind.Mass);

        Assert.Equal(3.0, world.VariableUsedBy("player", PropertyKind.Mass).Value.Scalar, Tol);
        Assert.Equal(70.0, world.VariableUsedBy("A", PropertyKind.Mass).Value.Scalar, Tol);
        Assert.Equal(2.0, world.VariableUsedBy("B", PropertyKind.Mass).Value.Scalar, Tol);
        Assert.True(world.CheckInvariant());
    }

    [Fact]
    public void UnknownContainerId_Throws()
    {
        var world = NewWorld();
        Assert.Throws<KeyNotFoundException>(() => world.VariableUsedBy("ghost", PropertyKind.Mass));
        Assert.Throws<KeyNotFoundException>(() => world.ExchangeVariables("player", "ghost", PropertyKind.Mass));
        Assert.Throws<KeyNotFoundException>(() => world.Get("ghost"));
    }

    [Fact]
    public void DuplicateContainerId_Throws()
    {
        var world = NewWorld();
        Assert.Throws<ArgumentException>(() =>
            world.Add(new Container("A", ContainerType.Object, 0.001, 0.01), Vec3.Zero, 1, Vec3.Zero));
    }

    [Fact]
    public void Container_AcceptsAllProperties_ByDefault_OrOnlyTheGivenOnes()
    {
        var any = new Container("A", ContainerType.Object, 0.001, 0.01);
        var massOnly = new Container("B", ContainerType.Object, 0.001, 0.01,
            AcceptedProperties: new HashSet<IPropertyKind> { PropertyKind.Mass });

        Assert.All(PropertyKind.All, k => Assert.True(any.Accepts(k)));
        Assert.True(massOnly.Accepts(PropertyKind.Mass));
        Assert.False(massOnly.Accepts(PropertyKind.Position));
        Assert.False(massOnly.Accepts(PropertyKind.KineticEnergy));
        Assert.False(any.Locked);
    }

    [Fact]
    public void Containers_ListsRegisteredContainers()
    {
        var world = NewWorld();
        Assert.Equal(new[] { "player", "A", "B" }, world.Containers.Select(c => c.Id));
        Assert.Equal(ContainerType.Player, world.Get("player").Type);
    }
}
