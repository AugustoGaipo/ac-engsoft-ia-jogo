using System.Text.Json.Nodes;

namespace Swapping.Levels.Tests;

public class ValidatorValueTests
{
    private static IReadOnlyList<LevelError> Validate(JsonObject node) =>
        LevelValidator.Validate(Fixtures.Parse(node));

    private static void AssertOnly(JsonObject node, LevelErrorCode code, string path)
    {
        var error = Assert.Single(Validate(node));
        Assert.Equal(code, error.Code);
        Assert.Equal(path, error.Path);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }

    private static JsonObject Crate(JsonObject node) => (JsonObject)Fixtures.Room(node)["objects"]![0]!;

    // SC 2 (second half)
    [Fact]
    public void SpecExample_IsValid() => Assert.Empty(LevelValidator.Validate(Fixtures.Example()));

    [Fact]
    public void ShiftedSecondRoom_KeepsTheLevelValid()
    {
        var node = Fixtures.ExampleNode();
        FixtureEdits.AddShiftedRoom(node);

        Assert.Empty(Validate(node));
    }

    [Fact]
    public void UnsupportedSchema()
    {
        var node = Fixtures.ExampleNode();
        node["schemaVersion"] = 2;

        AssertOnly(node, LevelErrorCode.UnsupportedSchema, "schemaVersion");
    }

    [Fact]
    public void BadId_LevelIdDoesNotMatchNumber()
    {
        var node = Fixtures.ExampleNode();
        node["id"] = "level-4";

        AssertOnly(node, LevelErrorCode.BadId, "id");
    }

    [Fact]
    public void BadId_RoomIdOfAnotherLevel()
    {
        var node = Fixtures.ExampleNode();
        Fixtures.Room(node)["id"] = "2.1";

        AssertOnly(node, LevelErrorCode.BadId, "rooms[0].id");
    }

    // SC 5
    [Fact]
    public void BadId_RoomIdsOutOfOrder()
    {
        var node = Fixtures.ExampleNode();
        FixtureEdits.AddShiftedRoom(node)["id"] = "3.3";

        AssertOnly(node, LevelErrorCode.BadId, "rooms[1].id");
    }

    // SC 5
    [Fact]
    public void DuplicateId_AcrossRooms()
    {
        var node = Fixtures.ExampleNode();
        FixtureEdits.AddShiftedRoom(node);
        ((JsonObject)Fixtures.Room(node, 1)["objects"]![0]!)["id"] = "crate";

        AssertOnly(node, LevelErrorCode.DuplicateId, "rooms[1].objects[0].id");
    }

    [Fact]
    public void DuplicateId_AcrossPieceKinds()
    {
        var node = Fixtures.ExampleNode();
        Fixtures.Room(node)["barriers"]![0]!["id"] = "door";

        AssertOnly(node, LevelErrorCode.DuplicateId, "rooms[0].barriers[0].id");
    }

    [Fact]
    public void DuplicateId_PlayerIsReserved()
    {
        var node = Fixtures.ExampleNode();
        Crate(node)["id"] = "player";

        AssertOnly(node, LevelErrorCode.DuplicateId, "rooms[0].objects[0].id");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    public void NumberOutOfRange(int number)
    {
        var node = Fixtures.ExampleNode();
        node["number"] = number;
        node["id"] = $"level-{number}";
        Fixtures.Room(node)["id"] = $"{number}.1";

        AssertOnly(node, LevelErrorCode.NumberOutOfRange, "number");
    }

    [Fact]
    public void UnknownProperty_InUnlockedProperties()
    {
        var node = Fixtures.ExampleNode();
        node["rules"]!["unlockedProperties"] = new JsonArray("mass", "time");

        AssertOnly(node, LevelErrorCode.UnknownProperty, "rules.unlockedProperties[1]");
    }

    [Fact]
    public void UnknownProperty_InRange()
    {
        var node = Fixtures.ExampleNode();
        node["rules"]!["rangeMeters"] = new JsonObject { ["weight"] = 8.0 };

        AssertOnly(node, LevelErrorCode.UnknownProperty, "rules.rangeMeters.weight");
    }

    [Fact]
    public void UnknownProperty_InAccepts()
    {
        var node = Fixtures.ExampleNode();
        Crate(node)["accepts"] = new JsonArray("colour");

        AssertOnly(node, LevelErrorCode.UnknownProperty, "rooms[0].objects[0].accepts[0]");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void NotPositive_ObjectMass(double massKg)
    {
        var node = Fixtures.ExampleNode();
        Crate(node)["massKg"] = massKg;

        AssertOnly(node, LevelErrorCode.NotPositive, "rooms[0].objects[0].massKg");
    }

    [Fact]
    public void NotPositive_ObjectSize()
    {
        var node = Fixtures.ExampleNode();
        Crate(node)["size"] = new JsonArray(1, 0, 1);

        AssertOnly(node, LevelErrorCode.NotPositive, "rooms[0].objects[0].size");
    }

    [Fact]
    public void NotPositive_ButtonRequiredMass()
    {
        var node = Fixtures.ExampleNode();
        Fixtures.Room(node)["buttons"]![0]!["requiredMassKg"] = 0;

        AssertOnly(node, LevelErrorCode.NotPositive, "rooms[0].buttons[0].requiredMassKg");
    }

    [Fact]
    public void NotPositive_ButtonSize()
    {
        var node = Fixtures.ExampleNode();
        Fixtures.Room(node)["buttons"]![0]!["size"] = new JsonArray(1.5, -0.1, 1.5);

        AssertOnly(node, LevelErrorCode.NotPositive, "rooms[0].buttons[0].size");
    }

    [Fact]
    public void NotPositive_Range()
    {
        var node = Fixtures.ExampleNode();
        node["rules"]!["rangeMeters"] = new JsonObject { ["mass"] = 0 };

        AssertOnly(node, LevelErrorCode.NotPositive, "rules.rangeMeters.mass");
    }

    [Fact]
    public void NotPositive_NegativeCooldown_ButZeroIsAllowed()
    {
        var node = Fixtures.ExampleNode();
        node["rules"]!["cooldownSeconds"] = 0;
        Assert.Empty(Validate(node));

        node["rules"]!["cooldownSeconds"] = -1;
        AssertOnly(node, LevelErrorCode.NotPositive, "rules.cooldownSeconds");
    }

    [Fact]
    public void NotPositive_InRulesOverride()
    {
        var node = Fixtures.ExampleNode();
        Fixtures.Room(node)["rulesOverride"] = new JsonObject { ["cooldownSeconds"] = -0.5 };

        AssertOnly(node, LevelErrorCode.NotPositive, "rooms[0].rulesOverride.cooldownSeconds");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void OutOfRange01_Bounce(double bounce)
    {
        var node = Fixtures.ExampleNode();
        Crate(node)["bounce"] = bounce;

        AssertOnly(node, LevelErrorCode.OutOfRange01, "rooms[0].objects[0].bounce");
    }

    // SC 6
    [Theory]
    [InlineData(300, false)]
    [InlineData(0.001, true)]
    [InlineData(30000, true)]
    [InlineData(1, false)]
    [InlineData(22600, false)]
    public void ImplausibleDensity_OutsideOneTo22600(double massKg, bool expectError)
    {
        var node = Fixtures.ExampleNode();
        Crate(node)["massKg"] = massKg;

        if (expectError)
            AssertOnly(node, LevelErrorCode.ImplausibleDensity, "rooms[0].objects[0].massKg");
        else
            Assert.Empty(Validate(node));
    }

    [Theory]
    [InlineData("bounds")]
    [InlineData("exit")]
    public void EmptyBox_RoomBoxes(string field)
    {
        var node = Fixtures.ExampleNode();
        var box = (JsonObject)Fixtures.Room(node)[field]!;
        box["max"] = box["min"]!.DeepClone();

        // An empty room box also makes later spatial checks meaningless, so they are skipped for that room.
        AssertOnly(node, LevelErrorCode.EmptyBox, $"rooms[0].{field}");
    }

    [Fact]
    public void EmptyBox_PieceBox()
    {
        var node = Fixtures.ExampleNode();
        var box = (JsonObject)Fixtures.Room(node)["doors"]![0]!["box"]!;
        box["max"] = new JsonArray(9, 3, 6);

        AssertOnly(node, LevelErrorCode.EmptyBox, "rooms[0].doors[0].box");
    }

    [Fact]
    public void NotAccepted_PropertyTheLevelDidNotUnlock()
    {
        var node = Fixtures.ExampleNode();
        Crate(node)["accepts"] = new JsonArray("position");

        AssertOnly(node, LevelErrorCode.NotAccepted, "rooms[0].objects[0].accepts[0]");
    }

    [Fact]
    public void NotAccepted_UsesTheRoomOverride()
    {
        var node = Fixtures.ExampleNode();
        Fixtures.Room(node)["rulesOverride"] = new JsonObject { ["unlockedProperties"] = new JsonArray("position") };
        Crate(node)["accepts"] = new JsonArray("position");

        Assert.Empty(Validate(node));
    }

    [Fact]
    public void NotAccepted_EmptyAcceptsList()
    {
        var node = Fixtures.ExampleNode();
        Crate(node)["accepts"] = new JsonArray();

        AssertOnly(node, LevelErrorCode.NotAccepted, "rooms[0].objects[0].accepts");
    }

    [Fact]
    public void EmptyBarrier_BlocksNothing()
    {
        var node = Fixtures.ExampleNode();
        ((JsonArray)Fixtures.Room(node)["barriers"]!).Add(new JsonObject
        {
            ["id"] = "nothing",
            ["box"] = new JsonObject { ["min"] = new JsonArray(3, 0, 8), ["max"] = new JsonArray(4, 3, 9) },
        });

        AssertOnly(node, LevelErrorCode.EmptyBarrier, "rooms[0].barriers[1]");
    }
}
