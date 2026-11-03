using Swapping.Rules.Model;

namespace Swapping.GameLogic.Tests;

public sealed class OutlineColorTests
{
    private static readonly IReadOnlySet<IPropertyKind> All = new HashSet<IPropertyKind>(PropertyKind.All);
    private static readonly IReadOnlySet<IPropertyKind> PositionOnly = new HashSet<IPropertyKind> { PropertyKind.Position };

    private static Container Box(bool locked = false, params IPropertyKind[] accepts) =>
        new("box", ContainerType.Object, 1, 1, locked, accepts.Length == 0 ? null : new HashSet<IPropertyKind>(accepts));

    [Fact]
    public void A_box_that_accepts_everything_the_room_unlocked_is_white()
    {
        Assert.Equal(Outline.White, OutlineColor.For(Box(), PositionOnly));
        Assert.Equal(Outline.White, OutlineColor.For(Box(), All));
        Assert.Equal(Outline.White, OutlineColor.For(Box(false, PropertyKind.Position, PropertyKind.Mass), PositionOnly));
    }

    [Fact]
    public void A_locked_box_is_gray()
    {
        Assert.Equal(Outline.Gray, OutlineColor.For(Box(locked: true), PositionOnly));
    }

    [Fact]
    public void A_box_that_accepts_nothing_the_room_unlocked_is_gray()
    {
        Assert.Equal(Outline.Gray, OutlineColor.For(Box(false, PropertyKind.Mass), PositionOnly));
        Assert.Equal(Outline.Gray, OutlineColor.For(Box(), new HashSet<IPropertyKind>()));
    }

    [Theory]
    [InlineData("position", Outline.Position)]
    [InlineData("mass", Outline.Mass)]
    [InlineData("energy", Outline.KineticEnergy)]
    public void Accepting_only_one_of_several_unlocked_properties_shows_that_property(string name, Outline outline)
    {
        var only = PropertyKind.All.Single(k => ((PropertyKind)k).Name == name);

        Assert.Equal(outline, OutlineColor.For(Box(false, only), All));
    }

    [Fact]
    public void Accepting_two_of_three_is_white()
    {
        Assert.Equal(Outline.White, OutlineColor.For(Box(false, PropertyKind.Position, PropertyKind.Mass), All));
    }
}

public sealed class CooldownRingTests
{
    [Fact]
    public void Ready_before_any_swap()
    {
        Assert.Equal(1, CooldownRing.Progress(now: 5, lastSwapAt: null, cooldownSeconds: 1));
    }

    [Theory]
    [InlineData(10.0, 0.0)]
    [InlineData(10.25, 0.25)]
    [InlineData(10.5, 0.5)]
    [InlineData(11.0, 1.0)]
    [InlineData(20.0, 1.0)]
    public void The_ring_fills_up_during_the_cooldown(double now, double progress)
    {
        Assert.Equal(progress, CooldownRing.Progress(now, lastSwapAt: 10, cooldownSeconds: 1), precision: 9);
    }

    [Fact]
    public void No_cooldown_is_always_ready()
    {
        Assert.Equal(1, CooldownRing.Progress(now: 10, lastSwapAt: 10, cooldownSeconds: 0));
    }
}
