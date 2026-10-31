using Swapping.Rules.Model;
using Swapping.Rules.Operations;
using Swapping.Rules.Policies;

namespace Swapping.Rules.Tests;

public class LegalityTests
{
    private static readonly IPropertyKind Mass = PropertyKind.Mass;

    private static World NewWorld(LevelRules? rules = null, Container? a = null)
    {
        var world = new World(rules ?? LevelRules.Default);
        world.Add(new Container("player", ContainerType.Player, 0.07, 0.1), Vec3.Zero, 70, Vec3.Zero);
        world.Add(a ?? new Container("A", ContainerType.Object, 0.001, 0.01), new Vec3(2, 0, 0), 2, Vec3.Zero);
        world.Add(new Container("B", ContainerType.Object, 0.001, 0.01), new Vec3(4, 0, 0), 3, Vec3.Zero);
        return world;
    }

    private static SwapResult Swap(World world, SwapRequest request, double now = 0) =>
        new SwapOperation().Apply(world, request, now);

    private static void AssertRejected(RejectReason expected, World world, SwapRequest request, double now = 0)
    {
        var massBefore = world.VariableUsedBy(request.First, Mass);
        var result = Swap(world, request, now);
        Assert.False(result.IsApplied);
        Assert.Equal(expected, result.Reason);
        Assert.Empty(result.Updates);
        Assert.Same(massBefore, world.VariableUsedBy(request.First, Mass)); // nothing changed
        Assert.True(world.CheckInvariant());
    }

    [Fact]
    public void Defaults_Range8m_Cooldown1s_AllPropertiesUnlocked_PlayerObjectOnly()
    {
        var rules = LevelRules.Default;
        Assert.All(PropertyKind.All, k => Assert.Equal(8.0, rules.RangeFor(k), 1e-9));
        Assert.Equal(1.0, rules.CooldownSeconds, 1e-9);
        Assert.All(PropertyKind.All, k => Assert.Contains(k, rules.UnlockedProperties));
        Assert.False(rules.AllowObjectToObject);
        Assert.False(rules.AllowSimultaneousSwaps);
    }

    [Fact]
    public void LegalSwap_IsApplied()
    {
        var world = NewWorld();
        var result = Swap(world, new SwapRequest("player", "A", Mass));
        Assert.True(result.IsApplied);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void SameContainer() =>
        AssertRejected(RejectReason.SameContainer, NewWorld(), new SwapRequest("player", "player", Mass));

    [Fact]
    public void ModeNotEnabled_ObjectToObject()
    {
        AssertRejected(RejectReason.ModeNotEnabled, NewWorld(), new SwapRequest("A", "B", Mass));
        var enabled = NewWorld(LevelRules.Default with { AllowObjectToObject = true });
        Assert.True(Swap(enabled, new SwapRequest("A", "B", Mass)).IsApplied);
    }

    [Fact]
    public void PropertyNotUnlocked()
    {
        var rules = LevelRules.Default with { UnlockedProperties = new HashSet<IPropertyKind> { Mass } };
        AssertRejected(RejectReason.PropertyNotUnlocked, NewWorld(rules),
            new SwapRequest("player", "A", PropertyKind.Position));
    }

    [Fact]
    public void PropertyNotAccepted()
    {
        var massOnly = new Container("A", ContainerType.Object, 0.001, 0.01,
            AcceptedProperties: new HashSet<IPropertyKind> { Mass });
        var world = NewWorld(a: massOnly);
        AssertRejected(RejectReason.PropertyNotAccepted, world, new SwapRequest("player", "A", PropertyKind.KineticEnergy));
        Assert.True(Swap(world, new SwapRequest("player", "A", Mass)).IsApplied);
    }

    [Fact]
    public void Locked()
    {
        var locked = new Container("A", ContainerType.Object, 0.001, 0.01, Locked: true);
        AssertRejected(RejectReason.Locked, NewWorld(a: locked), new SwapRequest("player", "A", Mass));
    }

    [Fact]
    public void OutOfRange()
    {
        var request = new SwapRequest("player", "A", Mass) { Context = new SwapContext(DistanceMeters: 8.01) };
        AssertRejected(RejectReason.OutOfRange, NewWorld(), request);
    }

    [Fact]
    public void DistanceEqualToRange_IsAllowed()
    {
        var request = new SwapRequest("player", "A", Mass) { Context = new SwapContext(DistanceMeters: 8.0) };
        Assert.True(Swap(NewWorld(), request).IsApplied);
    }

    [Fact]
    public void OutOfRange_UsesThePropertysOwnRange()
    {
        var rules = LevelRules.Default with
        {
            RangeMeters = new Dictionary<IPropertyKind, double> { [PropertyKind.Position] = 3 },
        };
        var far = new SwapContext(DistanceMeters: 5);
        AssertRejected(RejectReason.OutOfRange, NewWorld(rules),
            new SwapRequest("player", "A", PropertyKind.Position) { Context = far });
        Assert.True(Swap(NewWorld(rules), new SwapRequest("player", "A", Mass) { Context = far }).IsApplied);
    }

    [Fact]
    public void NoLineOfSight()
    {
        var request = new SwapRequest("player", "A", Mass) { Context = new SwapContext(HasLineOfSight: false) };
        AssertRejected(RejectReason.NoLineOfSight, NewWorld(), request);
    }

    [Fact]
    public void BlockedByBarrier()
    {
        var request = new SwapRequest("player", "A", Mass) { Context = new SwapContext(VariableBarrierBetween: true) };
        AssertRejected(RejectReason.BlockedByBarrier, NewWorld(), request);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void InsideVariableBarrier(bool firstInside, bool secondInside)
    {
        var request = new SwapRequest("player", "A", Mass)
        {
            Context = new SwapContext(FirstInsideVariableBarrier: firstInside, SecondInsideVariableBarrier: secondInside),
        };
        AssertRejected(RejectReason.InsideVariableBarrier, NewWorld(), request);
    }

    [Fact]
    public void TooManyProperties()
    {
        var both = new SwapRequest("player", "A", new IPropertyKind[] { Mass, PropertyKind.Position });
        AssertRejected(RejectReason.TooManyProperties, NewWorld(), both);
        var simultaneous = NewWorld(LevelRules.Default with { AllowSimultaneousSwaps = true });
        Assert.True(Swap(simultaneous, both).IsApplied);
    }

    [Fact]
    public void OnCooldown_AfterAnyProperty()
    {
        var world = NewWorld();
        Assert.True(Swap(world, new SwapRequest("player", "A", Mass), now: 0).IsApplied);
        AssertRejected(RejectReason.OnCooldown, world, new SwapRequest("player", "B", PropertyKind.Position), now: 0.5);
    }

    // SC 9
    [Fact]
    public void Cooldown2s_At1_5IsRejected_At2_0IsApplied()
    {
        var world = NewWorld(LevelRules.Default with { CooldownSeconds = 2 });
        Assert.True(Swap(world, new SwapRequest("player", "A", Mass), now: 0).IsApplied);
        AssertRejected(RejectReason.OnCooldown, world, new SwapRequest("player", "B", Mass), now: 1.5);
        Assert.True(Swap(world, new SwapRequest("player", "B", Mass), now: 2.0).IsApplied);
    }

    [Fact]
    public void RejectedSwap_DoesNotStartTheCooldown()
    {
        var world = NewWorld();
        Swap(world, new SwapRequest("player", "player", Mass), now: 0);
        Assert.True(Swap(world, new SwapRequest("player", "A", Mass), now: 0.1).IsApplied);
    }

    [Fact]
    public void Cooldown_IsReadyUntilTheFirstSwap()
    {
        var cooldown = new Cooldown();
        Assert.True(cooldown.IsReady(now: 0, cooldownSeconds: 1));
        cooldown.Record(now: 3);
        Assert.False(cooldown.IsReady(now: 3.9, cooldownSeconds: 1));
        Assert.True(cooldown.IsReady(now: 4, cooldownSeconds: 1));
    }

    [Fact]
    public void UnknownContainer_Throws()
    {
        var world = NewWorld();
        Assert.Throws<KeyNotFoundException>(() => Swap(world, new SwapRequest("player", "ghost", Mass)));
    }
}
