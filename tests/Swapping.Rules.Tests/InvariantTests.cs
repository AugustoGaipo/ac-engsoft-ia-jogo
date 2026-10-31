using Swapping.Rules.Model;
using Swapping.Rules.Operations;
using Swapping.Rules.Policies;

namespace Swapping.Rules.Tests;

public class InvariantTests
{
    private static readonly LevelRules Permissive = LevelRules.Default with
    {
        AllowObjectToObject = true,
        AllowSimultaneousSwaps = true,
        CooldownSeconds = 0,
    };

    private static Container Player() => new("player", ContainerType.Player, VolumeM3: 0.07, ContactAreaM2: 0.1);
    private static Container Box(string id) => new(id, ContainerType.Object, VolumeM3: 0.001, ContactAreaM2: 0.01);

    private static World ThreeContainers(LevelRules rules)
    {
        var world = new World(rules);
        world.Add(Player(), Vec3.Zero, 70, Vec3.Zero);
        world.Add(Box("A"), new Vec3(1, 0, 0), 2, new Vec3(1, 0, 0));
        world.Add(Box("B"), new Vec3(2, 0, 0), 3, Vec3.Zero);
        return world;
    }

    [Fact]
    public void NewWorld_HasEmptyLogAndZeroMoves()
    {
        var world = ThreeContainers(Permissive);
        Assert.Empty(world.Log.Entries);
        Assert.Equal(0, world.MoveCount);
    }

    [Fact]
    public void AppliedSwap_IsRecordedAndCountsOneMove()
    {
        var world = ThreeContainers(Permissive);
        var request = new SwapRequest("player", "A", PropertyKind.Mass);

        Assert.True(new SwapOperation().Apply(world, request, now: 2.5).IsApplied);

        var entry = Assert.Single(world.Log.Entries);
        Assert.Equal("player", entry.First);
        Assert.Equal("A", entry.Second);
        Assert.Equal(new IPropertyKind[] { PropertyKind.Mass }, entry.Properties);
        Assert.Equal(2.5, entry.AtSeconds, 1e-9);
        Assert.Equal(1, world.MoveCount);
    }

    [Fact]
    public void SimultaneousSwap_CountsAsOneMove()
    {
        var world = ThreeContainers(Permissive);
        var request = new SwapRequest("player", "A", new IPropertyKind[] { PropertyKind.Mass, PropertyKind.Position });

        Assert.True(new SwapOperation().Apply(world, request, now: 0).IsApplied);

        Assert.Equal(1, world.MoveCount);
        Assert.Equal(2, world.Log.Entries[0].Properties.Count);
    }

    [Fact]
    public void RejectedSwap_IsNotRecorded()
    {
        var world = ThreeContainers(Permissive);
        var result = new SwapOperation().Apply(world, new SwapRequest("A", "A", PropertyKind.Mass), now: 0);

        Assert.False(result.IsApplied);
        Assert.Empty(world.Log.Entries);
        Assert.Equal(0, world.MoveCount);
    }

    [Fact]
    public void Restorations_DoNotChangeMoveCount()
    {
        var world = ThreeContainers(Permissive);
        var swap = new SwapOperation();
        Assert.True(swap.Apply(world, new SwapRequest("player", "A", PropertyKind.Mass), now: 0).IsApplied);
        Assert.True(swap.Apply(world, new SwapRequest("player", "B", PropertyKind.Mass), now: 1).IsApplied);

        Restoration.OnEnter(world, "A", BarrierFilter.ExitGrid);
        Restoration.RestoreAll(world, "player");
        Restoration.Restore(world, "B", PropertyKind.Mass);

        Assert.Equal(2, world.MoveCount);
        Assert.Equal(2, world.Log.Entries.Count);
    }

    [Fact]
    public void Log_KeepsSwapsInOrder()
    {
        var world = ThreeContainers(Permissive);
        var swap = new SwapOperation();
        swap.Apply(world, new SwapRequest("player", "A", PropertyKind.Mass), now: 0);
        swap.Apply(world, new SwapRequest("player", "B", PropertyKind.KineticEnergy), now: 1);

        Assert.Equal(new[] { "A", "B" }, world.Log.Entries.Select(e => e.Second));
        Assert.Equal(new[] { 0.0, 1.0 }, world.Log.Entries.Select(e => e.AtSeconds));
    }

    // SC 13
    [Fact]
    public void Invariant_HoldsAfterEachOf1000RandomSteps()
    {
        var random = new Random(20261031);
        var world = new World(Permissive);
        world.Add(Player(), Vec3.Zero, 70, Vec3.Zero);
        var ids = new List<string> { "player" };
        for (var i = 0; i < 5; i++)
        {
            var id = $"box{i}";
            world.Add(Box(id), new Vec3(i + 1, 0, 0), i + 1, new Vec3(0, 0, i));
            ids.Add(id);
        }

        var swap = new SwapOperation();
        var applied = 0;
        Assert.True(world.CheckInvariant());

        for (var step = 0; step < 1000; step++)
        {
            var now = (double)step;
            if (random.NextDouble() < 0.7)
            {
                var first = ids[random.Next(ids.Count)];
                var second = ids[random.Next(ids.Count)];
                var kinds = PropertyKind.All.Where(_ => random.NextDouble() < 0.5).ToArray();
                if (kinds.Length == 0) kinds = new[] { PropertyKind.All[random.Next(PropertyKind.All.Count)] };

                var result = swap.Apply(world, new SwapRequest(first, second, kinds), now);
                if (result.IsApplied) applied++;
                else Assert.Equal(RejectReason.SameContainer, result.Reason);
            }
            else if (random.NextDouble() < 0.5)
            {
                Restoration.RestoreAll(world, ids[random.Next(ids.Count)]);
            }
            else
            {
                var kind = PropertyKind.All[random.Next(PropertyKind.All.Count)];
                Restoration.Restore(world, ids[random.Next(ids.Count)], kind);
            }

            Assert.True(world.CheckInvariant(), $"Invariant broken at step {step}.");
        }

        Assert.True(applied > 0);
        Assert.Equal(applied, world.MoveCount);
    }
}
