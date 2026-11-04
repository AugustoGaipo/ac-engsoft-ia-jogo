using Swapping.Rules;
using Swapping.Rules.Model;

namespace Swapping.GameLogic.Tests;

/// <summary>The HUD marks the properties a body uses that belong to someone else (user's request, 2026-11-03).</summary>
public sealed class BorrowedTests
{
    private static World NewWorld()
    {
        var world = new World();
        world.Add(new Container("player", ContainerType.Player, VolumeM3: 0.07, ContactAreaM2: 0.1),
            position: new Vec3(0, 0, 0), massKg: 70, velocity: Vec3.Zero);
        world.Add(new Container("crate-2-3", ContainerType.Object, VolumeM3: 1, ContactAreaM2: 1),
            position: new Vec3(2, 0, 0), massKg: 300, velocity: Vec3.Zero);
        return world;
    }

    private static string Name(string id) => "caixa";

    [Fact]
    public void Nothing_is_marked_before_a_swap()
    {
        var world = NewWorld();
        Assert.Equal("", Borrowed.Mark(world, "player", PropertyKind.Mass, "player", Name));
        Assert.Empty(Borrowed.Of(world, "player"));
    }

    [Fact]
    public void After_a_mass_swap_the_player_sees_whose_mass_it_uses_and_the_crate_shows_yours()
    {
        var world = NewWorld();
        world.ExchangeVariables("player", "crate-2-3", PropertyKind.Mass);

        Assert.Equal(" ⇄ da caixa", Borrowed.Mark(world, "player", PropertyKind.Mass, "player", Name));
        Assert.Equal(" ⇄ seu", Borrowed.Mark(world, "crate-2-3", PropertyKind.Mass, "player", Name));
        Assert.Equal("", Borrowed.Mark(world, "player", PropertyKind.Position, "player", Name));
        var only = Assert.Single(Borrowed.Of(world, "player"));
        Assert.Equal((PropertyKind.Mass.Name, "crate-2-3"), (only.Kind.Name, only.Owner));
    }

    [Fact]
    public void Swapping_back_makes_it_yours_again()
    {
        var world = NewWorld();
        world.ExchangeVariables("player", "crate-2-3", PropertyKind.Mass);
        world.ExchangeVariables("player", "crate-2-3", PropertyKind.Mass);

        Assert.Equal("", Borrowed.Mark(world, "player", PropertyKind.Mass, "player", Name));
    }
}
