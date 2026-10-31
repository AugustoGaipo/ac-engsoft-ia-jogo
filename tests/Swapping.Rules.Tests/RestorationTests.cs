using Swapping.Rules.Model;
using Swapping.Rules.Operations;
using Swapping.Rules.Policies;

namespace Swapping.Rules.Tests;

public class RestorationTests
{
    private const double Tol = 0.001;

    private static readonly LevelRules Permissive = LevelRules.Default with
    {
        AllowObjectToObject = true,
        AllowSimultaneousSwaps = true,
        CooldownSeconds = 0,
    };

    private static Container Player() => new("player", ContainerType.Player, VolumeM3: 0.07, ContactAreaM2: 0.1);
    private static Container Box(string id) => new(id, ContainerType.Object, VolumeM3: 0.001, ContactAreaM2: 0.01);

    private static void Swap(World world, string first, string second, params IPropertyKind[] kinds) =>
        Assert.True(new SwapOperation().Apply(world, new SwapRequest(first, second, kinds), now: 0).IsApplied);

    private static void AssertVec(Vec3 expected, Vec3 actual)
    {
        Assert.Equal(expected.X, actual.X, Tol);
        Assert.Equal(expected.Y, actual.Y, Tol);
        Assert.Equal(expected.Z, actual.Z, Tol);
    }

    // SC 2 (second half)
    [Fact]
    public void MassRestoration_AEntersBarrier_GetsItsOwnMassBack()
    {
        var world = new World(Permissive);
        world.Add(Player(), Vec3.Zero, 10, Vec3.Zero);
        world.Add(Box("A"), new Vec3(1, 0, 0), 2, Vec3.Zero);
        world.Add(Box("B"), new Vec3(2, 0, 0), 3, Vec3.Zero);
        Swap(world, "player", "A", PropertyKind.Mass);
        Swap(world, "player", "B", PropertyKind.Mass);

        var updates = Restoration.OnEnter(world, "A", BarrierFilter.BlocksVariablesOnly);

        Assert.Equal(3.0, world.BodyUpdateFor("player").MassKg, Tol);
        Assert.Equal(2.0, world.BodyUpdateFor("A").MassKg, Tol);
        Assert.Equal(10.0, world.BodyUpdateFor("B").MassKg, Tol);
        Assert.Equal(new[] { "A", "B" }, updates.Select(u => u.ContainerId));
        Assert.True(world.CheckInvariant());
    }

    // SC 6
    [Fact]
    public void PositionRestoration_MovingBoxSwapsPlacesWithRestingHolder()
    {
        var world = new World(Permissive);
        world.Add(Player(), new Vec3(0, 0, 0), 70, Vec3.Zero);
        world.Add(Box("box"), new Vec3(5, 0, 0), 2, new Vec3(2, 0, 0));
        Swap(world, "player", "box", PropertyKind.Position);
        world.SyncFromPhysics("box", new Vec3(1, 0, 0), new Vec3(2, 0, 0));

        Restoration.OnEnter(world, "box", BarrierFilter.ExitGrid);

        var box = world.BodyUpdateFor("box");
        var player = world.BodyUpdateFor("player");
        AssertVec(new Vec3(5, 0, 0), box.Position);
        AssertVec(new Vec3(1, 0, 0), player.Position);
        AssertVec(new Vec3(2, 0, 0), box.Velocity);
        AssertVec(Vec3.Zero, player.Velocity);
        Assert.Equal(0.0, world.VariableUsedBy("player", PropertyKind.KineticEnergy).Value.Scalar, Tol);
        Assert.True(world.CheckInvariant());
    }

    // SC 7
    [Fact]
    public void EnergyRestoration_ReversesDirection()
    {
        var world = new World(Permissive);
        world.Add(Box("A"), Vec3.Zero, 1, new Vec3(2, 0, 0));     // Xa = 2 J
        world.Add(Box("B"), new Vec3(3, 0, 0), 5, new Vec3(2, 0, 0)); // Xb = 10 J
        Swap(world, "A", "B", PropertyKind.KineticEnergy);
        Assert.Equal(10.0, world.VariableUsedBy("A", PropertyKind.KineticEnergy).Value.Scalar, Tol);
        // B, holding Xa = 2 J, is now moving left.
        world.SyncFromPhysics("B", new Vec3(3, 0, 0), new Vec3(-Math.Sqrt(2 * 2 / 5.0), 0, 0));

        Restoration.OnEnter(world, "A", BarrierFilter.BlocksVariablesOnly);

        AssertVec(new Vec3(-Math.Sqrt(2 * 2 / 1.0), 0, 0), world.BodyUpdateFor("A").Velocity);
        AssertVec(new Vec3(2, 0, 0), world.BodyUpdateFor("B").Velocity);
        Assert.True(world.CheckInvariant());
    }

    [Fact]
    public void Restoration_IsPerProperty_FromDifferentHolders()
    {
        var world = new World(Permissive);
        world.Add(Box("A"), Vec3.Zero, 1, Vec3.Zero);
        world.Add(Box("B"), new Vec3(1, 0, 0), 2, Vec3.Zero);
        world.Add(Box("C"), new Vec3(2, 0, 0), 3, Vec3.Zero);
        Swap(world, "A", "B", PropertyKind.Position);
        Swap(world, "A", "C", PropertyKind.Mass);

        var updates = Restoration.RestoreAll(world, "A");

        foreach (var id in new[] { "A", "B", "C" })
            foreach (var kind in PropertyKind.All)
                Assert.Equal(id, world.VariableUsedBy(id, kind).Owner);
        Assert.Equal(new[] { "A", "B", "C" }, updates.Select(u => u.ContainerId));
    }

    [Fact]
    public void Restore_SingleProperty_LeavesOthersAlone()
    {
        var world = new World(Permissive);
        world.Add(Box("A"), Vec3.Zero, 1, Vec3.Zero);
        world.Add(Box("B"), new Vec3(1, 0, 0), 2, Vec3.Zero);
        Swap(world, "A", "B", PropertyKind.Position, PropertyKind.Mass);

        Restoration.Restore(world, "A", PropertyKind.Mass);

        Assert.Equal("A", world.VariableUsedBy("A", PropertyKind.Mass).Owner);
        Assert.Equal("B", world.VariableUsedBy("A", PropertyKind.Position).Owner);
    }

    [Fact]
    public void Restoration_OfContainerUsingOwnVariables_ChangesNothing()
    {
        var world = new World(Permissive);
        world.Add(Box("A"), new Vec3(1, 2, 3), 4, new Vec3(0, 1, 0));
        world.Add(Box("B"), Vec3.Zero, 2, Vec3.Zero);
        var before = world.BodyUpdateFor("A");

        var updates = Restoration.RestoreAll(world, "A");

        Assert.Equal(new[] { "A" }, updates.Select(u => u.ContainerId));
        Assert.Equal(before, world.BodyUpdateFor("A"));
        foreach (var kind in PropertyKind.All)
            Assert.Equal("A", world.VariableUsedBy("A", kind).Owner);
        Assert.True(world.CheckInvariant());
    }

    [Fact]
    public void Restoration_DoesNotStartCooldown()
    {
        var rules = Permissive with { CooldownSeconds = 1 };
        var world = new World(rules);
        world.Add(Player(), Vec3.Zero, 70, Vec3.Zero);
        world.Add(Box("A"), new Vec3(1, 0, 0), 2, Vec3.Zero);
        Swap(world, "player", "A", PropertyKind.Mass);

        Restoration.RestoreAll(world, "A");

        Assert.True(world.Cooldown.IsReady(1.0, 1.0));
    }

    [Fact]
    public void EnteringBarrierThatDoesNotBlockVariables_RestoresNothing()
    {
        var world = new World(Permissive);
        world.Add(Player(), Vec3.Zero, 70, Vec3.Zero);
        world.Add(Box("A"), new Vec3(1, 0, 0), 2, Vec3.Zero);
        Swap(world, "player", "A", PropertyKind.Mass);

        var updates = Restoration.OnEnter(world, "A", new BarrierFilter(BlocksObjects: true, BlocksPlayer: true));

        Assert.Empty(updates);
        Assert.Equal("player", world.VariableUsedBy("A", PropertyKind.Mass).Owner);
    }

    [Fact]
    public void Restoration_UnknownId_Throws()
    {
        var world = new World(Permissive);
        Assert.Throws<KeyNotFoundException>(() => Restoration.RestoreAll(world, "ghost"));
    }

    [Fact]
    public void ExitGrid_BlocksObjectsAndVariables_LetsPlayerPass()
    {
        var grid = BarrierFilter.ExitGrid;

        Assert.True(grid.BlocksObjects);
        Assert.True(grid.BlocksVariables);
        Assert.False(grid.BlocksPlayer);
        Assert.True(grid.BlocksPassageOf(Box("A")));
        Assert.False(grid.BlocksPassageOf(Player()));
        Assert.True(grid.RestoresOnEntry);
        Assert.True(grid.BlocksSwaps);
    }

    [Fact]
    public void BarrierFilters_Combine()
    {
        var playerWall = new BarrierFilter(BlocksPlayer: true);
        Assert.True(playerWall.BlocksPassageOf(Player()));
        Assert.False(playerWall.BlocksPassageOf(Box("A")));
        Assert.False(playerWall.RestoresOnEntry);
        Assert.False(playerWall.BlocksSwaps);

        var all = playerWall.Combine(BarrierFilter.ExitGrid);
        Assert.True(all.BlocksPlayer && all.BlocksObjects && all.BlocksVariables);
    }
}
