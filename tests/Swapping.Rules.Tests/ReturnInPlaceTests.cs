using Swapping.Rules.Model;
using Swapping.Rules.Operations;
using Swapping.Rules.Policies;

namespace Swapping.Rules.Tests;

/// <summary>
/// Crossing a room's exit door (user, 2026-11-05): everyone gets its own variables back, but nobody moves —
/// positions and velocities stay what the physics says. Plain restoration would teleport the player to the
/// object holding its position variable.
/// </summary>
public class ReturnInPlaceTests
{
    private static readonly LevelRules Rules = LevelRules.Default with { CooldownSeconds = 0, AllowSimultaneousSwaps = true };

    private static World PlayerAndBox()
    {
        var world = new World(Rules);
        world.Add(new Container("player", ContainerType.Player, 0.07, 0.36), new Vec3(0, 0.9, 0), 70, Vec3.Zero);
        world.Add(new Container("box", ContainerType.Object, 1, 1), new Vec3(3, 0.5, 0), 10, Vec3.Zero);
        return world;
    }

    [Fact]
    public void Every_variable_goes_back_to_its_owner_and_nobody_moves()
    {
        var world = PlayerAndBox();
        var swap = new SwapOperation();
        Assert.True(swap.Apply(world, new SwapRequest("player", "box", PropertyKind.Mass), 0).IsApplied);
        Assert.True(swap.Apply(world, new SwapRequest("player", "box", PropertyKind.Position), 1).IsApplied);
        // Physics after the swaps: the player walked to x = 5, the box rests at x = 0.
        var physics = new Dictionary<string, (Vec3 Position, Vec3 Velocity)>
        {
            ["player"] = (new Vec3(5, 0.9, 0), new Vec3(3, 0, 0)),
            ["box"] = (new Vec3(0, 0.5, 0), Vec3.Zero),
        };

        var updates = Restoration.ReturnAllInPlace(world, "player", id => physics[id]);

        foreach (var kind in PropertyKind.All)
        {
            Assert.Equal("player", world.VariableUsedBy("player", kind).Owner);
            Assert.Equal("box", world.VariableUsedBy("box", kind).Owner);
        }
        var player = updates.Single(u => u.ContainerId == "player");
        var box = updates.Single(u => u.ContainerId == "box");
        Assert.Equal((new Vec3(5, 0.9, 0), 70.0), (player.Position, player.MassKg));
        Assert.Equal(3, player.Velocity.Length, 6);                    // still walking at 3 m/s
        Assert.Equal((new Vec3(0, 0.5, 0), 10.0), (box.Position, box.MassKg));
    }

    [Fact]
    public void With_nothing_borrowed_there_is_nothing_to_return()
    {
        var world = PlayerAndBox();
        Assert.Empty(Restoration.ReturnAllInPlace(world, "player", _ => (Vec3.Zero, Vec3.Zero)));
    }
}
