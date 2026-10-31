using System.Text.Json.Nodes;
using Swapping.Rules;
using Swapping.Rules.Model;
using Swapping.Rules.Operations;

namespace Swapping.Levels.Tests;

public class MappingTests
{
    private const double Tolerance = 1e-9;

    [Fact]
    public void ToLevelRules_CopiesTheLevelRules()
    {
        var rules = RulesMapping.ToLevelRules(Fixtures.Example(), "3.1");

        Assert.Equal(new IPropertyKind[] { PropertyKind.Mass }, rules.UnlockedProperties);
        Assert.Equal(8.0, rules.RangeMeters[PropertyKind.Mass]);
        Assert.Equal(1.0, rules.CooldownSeconds);
        Assert.False(rules.AllowObjectToObject);
        Assert.False(rules.AllowSimultaneousSwaps);
    }

    [Fact]
    public void ToLevelRules_AppliesTheRoomOverrideOnlyForTheFieldsItSets()
    {
        var node = Fixtures.ExampleNode();
        Fixtures.Room(node)["rulesOverride"] = new JsonObject
        {
            ["unlockedProperties"] = new JsonArray("position", "kineticEnergy"),
            ["cooldownSeconds"] = 3.0,
            ["allowSimultaneousSwaps"] = true,
        };

        var rules = RulesMapping.ToLevelRules(Fixtures.Parse(node), "3.1");

        Assert.Equal(new IPropertyKind[] { PropertyKind.Position, PropertyKind.KineticEnergy }, rules.UnlockedProperties.OrderBy(k => k.Name == "position" ? 0 : 1));
        Assert.Equal(3.0, rules.CooldownSeconds);
        Assert.True(rules.AllowSimultaneousSwaps);
        Assert.Equal(8.0, rules.RangeMeters[PropertyKind.Mass]);
        Assert.False(rules.AllowObjectToObject);
    }

    // SC 8
    [Fact]
    public void EmptyUnlockedOverride_RejectsEverySwapWithPropertyNotUnlocked()
    {
        var node = Fixtures.ExampleNode();
        Fixtures.Room(node)["rulesOverride"] = new JsonObject { ["unlockedProperties"] = new JsonArray() };
        var world = RulesMapping.BuildWorld(Fixtures.Parse(node), "3.1");

        foreach (var kind in PropertyKind.All)
        {
            var result = new SwapOperation().Apply(world, new SwapRequest("player", "crate", kind), now: 10);
            Assert.Equal(RejectReason.PropertyNotUnlocked, result.Reason);
        }
    }

    [Fact]
    public void BuildWorld_HasThePlayerAtTheSpawnAndEveryObject()
    {
        var world = RulesMapping.BuildWorld(Fixtures.Example(), "3.1");

        var player = world.Get("player");
        Assert.Equal(ContainerType.Player, player.Type);
        Assert.Equal(0.07, player.VolumeM3, Tolerance);
        Assert.Equal(70.0, world.VariableUsedBy("player", PropertyKind.Mass).Value.Scalar, Tolerance);
        Assert.Equal(new Vec3(1, 0.9, 5), world.VariableUsedBy("player", PropertyKind.Position).Value.Vector);

        var crate = world.Get("crate");
        Assert.Equal(ContainerType.Object, crate.Type);
        Assert.Equal(1.0, crate.VolumeM3, Tolerance);
        Assert.Equal(1.0, crate.ContactAreaM2, Tolerance);
        Assert.False(crate.Locked);
        Assert.Null(crate.AcceptedProperties);
        Assert.Equal(new Vec3(6, 0.5, 2), world.VariableUsedBy("crate", PropertyKind.Position).Value.Vector);
        Assert.Equal(2, world.Containers.Count);
    }

    [Fact]
    public void BuildWorld_MapsAcceptsLockedAndStartVelocity()
    {
        var node = Fixtures.ExampleNode();
        var crate = (JsonObject)Fixtures.Room(node)["objects"]![0]!;
        crate["accepts"] = new JsonArray("mass");
        crate["locked"] = true;
        crate["velocity"] = new JsonArray(2, 0, 0);

        var world = RulesMapping.BuildWorld(Fixtures.Parse(node), "3.1");

        var container = world.Get("crate");
        Assert.True(container.Locked);
        Assert.Equal(new IPropertyKind[] { PropertyKind.Mass }, container.AcceptedProperties);
        var energy = world.VariableUsedBy("crate", PropertyKind.KineticEnergy).Value;
        Assert.Equal(0.5 * 300 * 2 * 2, energy.Scalar, Tolerance);
        Assert.Equal(new Vec3(1, 0, 0), energy.Vector);
    }

    // SC 9
    [Fact]
    public void Room31_PlayerCrateMassSwap_IsAppliedAndThePlayerWeighs300()
    {
        var world = RulesMapping.BuildWorld(Fixtures.Example(), "3.1");

        var result = new SwapOperation().Apply(world, new SwapRequest("player", "crate", PropertyKind.Mass)
        {
            Context = new SwapContext(DistanceMeters: 5),
        }, now: 0);

        Assert.True(result.IsApplied);
        Assert.Equal(300.0, world.VariableUsedBy("player", PropertyKind.Mass).Value.Scalar, Tolerance);
        Assert.Equal(70.0, world.VariableUsedBy("crate", PropertyKind.Mass).Value.Scalar, Tolerance);
        Assert.True(world.CheckInvariant());
    }

    // SC 10
    [Fact]
    public void BuildWorld_TwiceGivesIndependentWorlds()
    {
        var level = Fixtures.Example();
        var first = RulesMapping.BuildWorld(level, "3.1");
        new SwapOperation().Apply(first, new SwapRequest("player", "crate", PropertyKind.Mass), now: 0);

        var restarted = RulesMapping.BuildWorld(level, "3.1");

        Assert.NotSame(first, restarted);
        Assert.Equal(70.0, restarted.VariableUsedBy("player", PropertyKind.Mass).Value.Scalar, Tolerance);
        Assert.Equal(0, restarted.MoveCount);
        Assert.Equal(300.0, first.VariableUsedBy("player", PropertyKind.Mass).Value.Scalar, Tolerance);
    }

    [Fact]
    public void UnknownRoomId_Throws()
    {
        Assert.Throws<KeyNotFoundException>(() => RulesMapping.BuildWorld(Fixtures.Example(), "9.9"));
        Assert.Throws<KeyNotFoundException>(() => RulesMapping.ToLevelRules(Fixtures.Example(), "9.9"));
    }
}
