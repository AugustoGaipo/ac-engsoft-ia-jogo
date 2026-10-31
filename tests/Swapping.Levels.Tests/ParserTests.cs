using System.Text.Json.Nodes;
using Swapping.Levels.Model;
using Swapping.Rules.Model;

namespace Swapping.Levels.Tests;

public class ParserTests
{
    private static LevelError SingleError(string json)
    {
        var (level, errors) = LevelParser.Parse(json);
        Assert.Null(level);
        return Assert.Single(errors);
    }

    // SC 2 (first half)
    [Fact]
    public void SpecExample_ParsesIntoTheExpectedValues()
    {
        var level = Fixtures.Example();

        Assert.Equal(1, level.SchemaVersion);
        Assert.Equal("level-3", level.Id);
        Assert.Equal(3, level.Number);
        Assert.Equal("res://levels/level_3.tscn", level.Scene);
        Assert.Equal(new[] { "mass" }, level.Rules.UnlockedProperties);
        Assert.Equal(8.0, level.Rules.RangeMeters["mass"]);
        Assert.Equal(1.0, level.Rules.CooldownSeconds);

        var room = Assert.Single(level.Rooms);
        Assert.Equal("3.1", room.Id);
        Assert.Equal(new Box(new Vec3(0, 0, 0), new Vec3(12, 6, 10)), room.Bounds);
        Assert.Equal(new Vec3(1, 0, 5), room.Spawn.Position);
        Assert.Equal(90, room.Spawn.YawDegrees);

        var crate = Assert.Single(room.Objects);
        Assert.Equal(ObjectKind.Box, crate.Kind);
        Assert.Equal(300, crate.MassKg);
        Assert.Null(crate.Accepts);
        Assert.Equal(0.1, crate.Bounce);

        var button = Assert.Single(room.Buttons);
        Assert.Equal(150, button.RequiredMassKg);
        Assert.Equal(ButtonDetects.Any, button.Detects);

        Assert.Equal(new[] { "heavy-button" }, Assert.Single(room.Doors).OpensWith);
        Assert.Equal(BarrierPreset.ExitGrid, Assert.Single(room.Barriers).Preset);
        Assert.Empty(room.KillZones);
    }

    // SC 4
    [Theory]
    [InlineData("{ not json")]
    [InlineData("")]
    [InlineData("[1, 2, 3]")]
    public void MalformedJson_IsAnErrorNotAnException(string json)
    {
        var error = SingleError(json);

        Assert.Equal(LevelErrorCode.InvalidJson, error.Code);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }

    // SC 4
    [Fact]
    public void MissingRequiredField_IsInvalidJsonWithItsPath()
    {
        var node = Fixtures.ExampleNode();
        node.Remove("rooms");

        var error = SingleError(node.ToJsonString());

        Assert.Equal(LevelErrorCode.InvalidJson, error.Code);
        Assert.Equal("rooms", error.Path);
    }

    // SC 4
    [Fact]
    public void VectorWithTwoNumbers_IsInvalidJsonWithItsPath()
    {
        var node = Fixtures.ExampleNode();
        Fixtures.Room(node)["spawn"]!["position"] = new JsonArray(1, 0);

        var error = SingleError(node.ToJsonString());

        Assert.Equal(LevelErrorCode.InvalidJson, error.Code);
        Assert.Equal("rooms[0].spawn.position", error.Path);
    }

    [Fact]
    public void WrongType_IsInvalidJsonWithItsPath()
    {
        var node = Fixtures.ExampleNode();
        Fixtures.Room(node)["objects"]![0]!["massKg"] = "heavy";

        var error = SingleError(node.ToJsonString());

        Assert.Equal("rooms[0].objects[0].massKg", error.Path);
    }

    [Theory]
    [InlineData("kind", "cube", "rooms[0].objects[0].kind")]
    public void UnknownObjectKind_IsInvalidJson(string field, string value, string path)
    {
        var node = Fixtures.ExampleNode();
        Fixtures.Room(node)["objects"]![0]![field] = value;

        Assert.Equal(path, SingleError(node.ToJsonString()).Path);
    }

    [Fact]
    public void UnknownButtonDetects_IsInvalidJson()
    {
        var node = Fixtures.ExampleNode();
        Fixtures.Room(node)["buttons"]![0]!["detects"] = "people";

        Assert.Equal("rooms[0].buttons[0].detects", SingleError(node.ToJsonString()).Path);
    }

    [Fact]
    public void UnknownBarrierPreset_IsInvalidJson()
    {
        var node = Fixtures.ExampleNode();
        Fixtures.Room(node)["barriers"]![0]!["preset"] = "lava";

        Assert.Equal("rooms[0].barriers[0].preset", SingleError(node.ToJsonString()).Path);
    }

    [Fact]
    public void SeveralProblems_AreAllReported()
    {
        var node = Fixtures.ExampleNode();
        node.Remove("name");
        Fixtures.Room(node).Remove("teaches");

        var (level, errors) = LevelParser.Parse(node.ToJsonString());

        Assert.Null(level);
        Assert.Equal(new[] { "name", "rooms[0].teaches" }, errors.Select(e => e.Path).Order());
    }

    [Fact]
    public void OptionalFields_TakeTheirDefaults()
    {
        var node = Fixtures.ExampleNode();
        var rules = (JsonObject)node["rules"]!;
        rules.Remove("rangeMeters");
        rules.Remove("allowObjectToObject");
        rules.Remove("allowSimultaneousSwaps");
        var room = Fixtures.Room(node);
        room.Remove("rulesOverride");
        room.Remove("killZones");
        room.Remove("doors");
        var crate = (JsonObject)room["objects"]![0]!;
        crate.Remove("accepts");
        crate.Remove("locked");
        crate.Remove("velocity");
        crate.Remove("bounce");

        var level = Fixtures.Parse(node);

        Assert.Empty(level.Rules.RangeMeters);
        Assert.False(level.Rules.AllowObjectToObject);
        Assert.False(level.Rules.AllowSimultaneousSwaps);
        var parsedRoom = level.Rooms[0];
        Assert.Null(parsedRoom.RulesOverride);
        Assert.Empty(parsedRoom.KillZones);
        Assert.Empty(parsedRoom.Doors);
        var parsedCrate = parsedRoom.Objects[0];
        Assert.Null(parsedCrate.Accepts);
        Assert.False(parsedCrate.Locked);
        Assert.Equal(Vec3.Zero, parsedCrate.Velocity);
        Assert.Equal(0, parsedCrate.Bounce);
    }

    [Fact]
    public void RulesOverride_WithEmptyUnlockedProperties_Parses()
    {
        var node = Fixtures.ExampleNode();
        Fixtures.Room(node)["rulesOverride"] = new JsonObject
        {
            ["unlockedProperties"] = new JsonArray(),
            ["cooldownSeconds"] = 2.5,
        };

        var over = Fixtures.Parse(node).Rooms[0].RulesOverride!;

        Assert.Empty(over.UnlockedProperties!);
        Assert.Equal(2.5, over.CooldownSeconds);
        Assert.Null(over.RangeMeters);
        Assert.Null(over.AllowObjectToObject);
    }

    [Fact]
    public void ExplicitBarrierFlags_AndBallKind_Parse()
    {
        var node = Fixtures.ExampleNode();
        var room = Fixtures.Room(node);
        room["barriers"]![0] = new JsonObject
        {
            ["id"] = "zone",
            ["blocksVariables"] = true,
            ["box"] = new JsonObject { ["min"] = new JsonArray(2, 0, 2), ["max"] = new JsonArray(5, 3, 5) },
        };
        room["objects"]![0]!["kind"] = "ball";
        room["objects"]![0]!["accepts"] = new JsonArray("mass");

        var parsed = Fixtures.Parse(node).Rooms[0];

        var barrier = parsed.Barriers[0];
        Assert.Null(barrier.Preset);
        Assert.True(barrier.BlocksVariables);
        Assert.False(barrier.BlocksObjects);
        Assert.Equal(ObjectKind.Ball, parsed.Objects[0].Kind);
        Assert.Equal(new[] { "mass" }, parsed.Objects[0].Accepts);
    }

    [Fact]
    public void CommentsAndTrailingCommas_AreAllowedForHandWrittenFiles()
    {
        var json = Fixtures.ExampleJson.Replace("\"schemaVersion\": 1,", "// level file\n\"schemaVersion\": 1,")
            .Replace("\"killZones\": []", "\"killZones\": [],");

        var (level, errors) = LevelParser.Parse(json);

        Assert.Empty(errors);
        Assert.NotNull(level);
    }
}
