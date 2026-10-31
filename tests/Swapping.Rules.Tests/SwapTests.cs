using Swapping.Rules.Model;
using Swapping.Rules.Operations;
using Swapping.Rules.Policies;

namespace Swapping.Rules.Tests;

public class SwapTests
{
    private const double Tol = 0.001;

    // These tests are about what a swap does, not whether it is allowed (see LegalityTests).
    private static readonly LevelRules Permissive = LevelRules.Default with
    {
        AllowObjectToObject = true,
        AllowSimultaneousSwaps = true,
        CooldownSeconds = 0,
    };

    private static Container Player() => new("player", ContainerType.Player, VolumeM3: 0.07, ContactAreaM2: 0.1);
    private static Container Box(string id) => new(id, ContainerType.Object, VolumeM3: 0.001, ContactAreaM2: 0.01);

    private static SwapResult Swap(World world, string first, string second, params IPropertyKind[] kinds) =>
        new SwapOperation().Apply(world, new SwapRequest(first, second, kinds), now: 0);

    private static void AssertVec(Vec3 expected, Vec3 actual)
    {
        Assert.Equal(expected.X, actual.X, Tol);
        Assert.Equal(expected.Y, actual.Y, Tol);
        Assert.Equal(expected.Z, actual.Z, Tol);
    }

    // SC 2 (first half)
    [Fact]
    public void MassSwap_Chained_PlayerGetsBsMass()
    {
        var world = new World(Permissive);
        world.Add(Player(), Vec3.Zero, 10, Vec3.Zero);
        world.Add(Box("A"), new Vec3(1, 0, 0), 2, Vec3.Zero);
        world.Add(Box("B"), new Vec3(2, 0, 0), 3, Vec3.Zero);

        Swap(world, "player", "A", PropertyKind.Mass);
        Swap(world, "player", "B", PropertyKind.Mass);

        Assert.Equal(3.0, world.BodyUpdateFor("player").MassKg, Tol);
        Assert.Equal(10.0, world.BodyUpdateFor("A").MassKg, Tol);
        Assert.Equal(2.0, world.BodyUpdateFor("B").MassKg, Tol);
        Assert.True(world.CheckInvariant());
    }

    // SC 3 via a mass swap
    [Fact]
    public void MassSwap_KeepsEnergy_RecomputesSpeed()
    {
        var world = new World(Permissive);
        world.Add(Box("A"), Vec3.Zero, 2, new Vec3(3, 0, 0));
        world.Add(Box("B"), new Vec3(1, 0, 0), 8, Vec3.Zero);

        var result = Swap(world, "A", "B", PropertyKind.Mass);

        Assert.True(result.IsApplied);
        var a = result.Updates.Single(u => u.ContainerId == "A");
        Assert.Equal(8.0, a.MassKg, Tol);
        AssertVec(new Vec3(1.5, 0, 0), a.Velocity);
        var b = result.Updates.Single(u => u.ContainerId == "B");
        Assert.Equal(2.0, b.MassKg, Tol);
        AssertVec(Vec3.Zero, b.Velocity);
    }

    // SC 4
    [Fact]
    public void EnergySwap_ReceiverMovesTheWayTheGiverWasMoving()
    {
        var world = new World(Permissive);
        world.Add(Box("ball"), new Vec3(3, 0, 0), 1, new Vec3(4, 0, 0));
        world.Add(Player(), Vec3.Zero, 70, Vec3.Zero);

        var result = Swap(world, "ball", "player", PropertyKind.KineticEnergy);

        var player = result.Updates.Single(u => u.ContainerId == "player");
        AssertVec(new Vec3(0.478, 0, 0), player.Velocity);
        Assert.Equal(70.0, player.MassKg, Tol);
        var ball = result.Updates.Single(u => u.ContainerId == "ball");
        AssertVec(Vec3.Zero, ball.Velocity);
        Assert.True(world.CheckInvariant());
    }

    [Fact]
    public void EnergySwap_ZeroJouleReceiverStops()
    {
        var world = new World(Permissive);
        world.Add(Box("A"), Vec3.Zero, 2, new Vec3(0, -5, 0));
        world.Add(Box("B"), new Vec3(1, 0, 0), 2, Vec3.Zero);

        Swap(world, "A", "B", PropertyKind.KineticEnergy);

        AssertVec(Vec3.Zero, world.BodyUpdateFor("A").Velocity);
        AssertVec(new Vec3(0, -5, 0), world.BodyUpdateFor("B").Velocity);
    }

    // SC 5
    [Fact]
    public void PositionSwap_ExchangesPlaces_KeepsVelocities()
    {
        var world = new World(Permissive);
        world.Add(Player(), new Vec3(0, 1, 0), 70, new Vec3(0, 0, 2));
        world.Add(Box("A"), new Vec3(5, 0, 0), 2, new Vec3(-1, 0, 0));

        var result = Swap(world, "player", "A", PropertyKind.Position);

        var player = result.Updates.Single(u => u.ContainerId == "player");
        var a = result.Updates.Single(u => u.ContainerId == "A");
        AssertVec(new Vec3(5, 0, 0), player.Position);
        AssertVec(new Vec3(0, 1, 0), a.Position);
        AssertVec(new Vec3(0, 0, 2), player.Velocity);
        AssertVec(new Vec3(-1, 0, 0), a.Velocity);
        Assert.Equal(70.0, player.MassKg, Tol);
        Assert.Equal(2.0, a.MassKg, Tol);
    }

    [Fact]
    public void Swap_ReturnsUpdatesForBothContainers_InRequestOrder()
    {
        var world = new World(Permissive);
        world.Add(Player(), Vec3.Zero, 70, Vec3.Zero);
        world.Add(Box("A"), new Vec3(1, 0, 0), 2, Vec3.Zero);

        var result = Swap(world, "player", "A", PropertyKind.Mass);

        Assert.Equal(new[] { "player", "A" }, result.Updates.Select(u => u.ContainerId));
    }

    [Fact]
    public void SyncFromPhysics_WritesPositionAndEnergyIntoTheUsedVariables()
    {
        var world = new World(Permissive);
        world.Add(Player(), Vec3.Zero, 70, Vec3.Zero);
        world.Add(Box("A"), new Vec3(1, 0, 0), 2, Vec3.Zero);
        Swap(world, "player", "A", PropertyKind.Position, PropertyKind.KineticEnergy);

        world.SyncFromPhysics("player", new Vec3(7, 0, 0), new Vec3(0, 3, 0));

        var position = world.VariableUsedBy("player", PropertyKind.Position);
        var energy = world.VariableUsedBy("player", PropertyKind.KineticEnergy);
        Assert.Equal("A", position.Owner);
        AssertVec(new Vec3(7, 0, 0), position.Value.Vector);
        Assert.Equal("A", energy.Owner);
        Assert.Equal(315.0, energy.Value.Scalar, Tol); // ½ · 70 · 3²
        AssertVec(new Vec3(0, 1, 0), energy.Value.Vector);
        AssertVec(new Vec3(0, 3, 0), world.BodyUpdateFor("player").Velocity);
    }

    [Fact]
    public void SyncFromPhysics_AtRest_KeepsLastDirection()
    {
        var world = new World(Permissive);
        world.Add(Box("A"), Vec3.Zero, 2, new Vec3(3, 0, 0));

        world.SyncFromPhysics("A", Vec3.Zero, Vec3.Zero);

        var energy = world.VariableUsedBy("A", PropertyKind.KineticEnergy).Value;
        Assert.Equal(0.0, energy.Scalar, Tol);
        AssertVec(new Vec3(1, 0, 0), energy.Vector);
        AssertVec(Vec3.Zero, world.BodyUpdateFor("A").Velocity);
    }

    [Fact]
    public void SyncFromPhysics_UnknownId_Throws()
    {
        var world = new World(Permissive);
        Assert.Throws<KeyNotFoundException>(() => world.SyncFromPhysics("ghost", Vec3.Zero, Vec3.Zero));
        Assert.Throws<KeyNotFoundException>(() => world.BodyUpdateFor("ghost"));
    }
}
