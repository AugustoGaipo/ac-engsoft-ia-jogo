using System.Text.Json.Nodes;

namespace Swapping.Levels.Tests;

public class ValidatorSpatialTests
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

    private static JsonArray V(double x, double y, double z) => new(x, y, z);

    private static JsonObject BoxNode(JsonArray min, JsonArray max) => new() { ["min"] = min, ["max"] = max };

    private static JsonObject Crate(JsonObject node) => (JsonObject)Fixtures.Room(node)["objects"]![0]!;

    private static JsonArray List(JsonObject node, string name) => (JsonArray)Fixtures.Room(node)[name]!;

    [Fact]
    public void OutsideRoom_Object()
    {
        var node = Fixtures.ExampleNode();
        Crate(node)["position"] = V(11.8, 0.5, 2);

        AssertOnly(node, LevelErrorCode.OutsideRoom, "rooms[0].objects[0]");
    }

    [Fact]
    public void OutsideRoom_Spawn()
    {
        var node = Fixtures.ExampleNode();
        Fixtures.Room(node)["spawn"]!["position"] = V(0.1, 0, 5);

        AssertOnly(node, LevelErrorCode.OutsideRoom, "rooms[0].spawn");
    }

    [Fact]
    public void OutsideRoom_Exit()
    {
        var node = Fixtures.ExampleNode();
        Fixtures.Room(node)["exit"]!["max"] = V(12.5, 3, 6);

        AssertOnly(node, LevelErrorCode.OutsideRoom, "rooms[0].exit");
    }

    [Fact]
    public void OutsideRoom_PieceBox()
    {
        var node = Fixtures.ExampleNode();
        List(node, "doors")[0]!["box"] = BoxNode(V(10, 0, 4), V(10.3, 7, 6));

        AssertOnly(node, LevelErrorCode.OutsideRoom, "rooms[0].doors[0]");
    }

    [Fact]
    public void RoomsOverlap()
    {
        var node = Fixtures.ExampleNode();
        FixtureEdits.AddShiftedRoom(node, dx: 6);

        AssertOnly(node, LevelErrorCode.RoomsOverlap, "rooms[1].bounds");
    }

    [Fact]
    public void RoomsThatOnlyTouch_AreValid()
    {
        var node = Fixtures.ExampleNode();
        FixtureEdits.AddShiftedRoom(node, dx: 12);

        Assert.Empty(Validate(node));
    }

    [Fact]
    public void UnknownReference_DoorNamesAButtonThatIsNotInTheRoom()
    {
        var node = Fixtures.ExampleNode();
        List(node, "doors")[0]!["opensWith"] = new JsonArray("heavy-button", "ghost");

        AssertOnly(node, LevelErrorCode.UnknownReference, "rooms[0].doors[0].opensWith[1]");
    }

    [Fact]
    public void UnknownReference_ButtonOfAnotherRoom()
    {
        var node = Fixtures.ExampleNode();
        FixtureEdits.AddShiftedRoom(node);
        List(node, "doors")[0]!["opensWith"] = new JsonArray("heavy-button", "heavy-button-b");

        AssertOnly(node, LevelErrorCode.UnknownReference, "rooms[0].doors[0].opensWith[1]");
    }

    [Fact]
    public void DoorWithoutButton()
    {
        var node = Fixtures.ExampleNode();
        List(node, "doors").Add(new JsonObject
        {
            ["id"] = "door-2",
            ["box"] = BoxNode(V(3, 0, 8), V(3.3, 3, 9)),
            ["opensWith"] = new JsonArray(),
        });

        AssertOnly(node, LevelErrorCode.DoorWithoutButton, "rooms[0].doors[1].opensWith");
    }

    [Fact]
    public void UnusedButton()
    {
        var node = Fixtures.ExampleNode();
        List(node, "buttons").Add(new JsonObject
        {
            ["id"] = "lonely",
            ["position"] = V(3, 0, 8),
            ["size"] = V(1, 0.1, 1),
            ["requiredMassKg"] = 10,
            ["detects"] = "any",
        });

        AssertOnly(node, LevelErrorCode.UnusedButton, "rooms[0].buttons[1]");
    }

    // SC 7
    [Fact]
    public void MissingExitGrid_NoBarrierOnTheExit()
    {
        var node = Fixtures.ExampleNode();
        Fixtures.Room(node)["barriers"] = new JsonArray();

        AssertOnly(node, LevelErrorCode.MissingExitGrid, "rooms[0].exit");
    }

    // SC 7
    [Fact]
    public void MissingExitGrid_ABarrierThatAlsoBlocksThePlayerIsNotAnExitGrid()
    {
        var node = Fixtures.ExampleNode();
        List(node, "barriers")[0] = new JsonObject
        {
            ["id"] = "exit-grid",
            ["blocksObjects"] = true,
            ["blocksPlayer"] = true,
            ["blocksVariables"] = true,
            ["box"] = BoxNode(V(11.5, 0, 4), V(11.6, 3, 6)),
        };

        AssertOnly(node, LevelErrorCode.MissingExitGrid, "rooms[0].exit");
    }

    // SC 7
    [Fact]
    public void ExitGrid_MadeOfExplicitFlags1Plus3_Counts()
    {
        var node = Fixtures.ExampleNode();
        List(node, "barriers")[0] = new JsonObject
        {
            ["id"] = "exit-grid",
            ["blocksObjects"] = true,
            ["blocksVariables"] = true,
            ["box"] = BoxNode(V(11.5, 0, 4), V(11.6, 3, 6)),
        };

        Assert.Empty(Validate(node));
    }

    [Fact]
    public void MissingExitGrid_ExitGridElsewhereInTheRoom()
    {
        var node = Fixtures.ExampleNode();
        List(node, "barriers")[0]!["box"] = BoxNode(V(3, 0, 8), V(3.1, 3, 9));

        AssertOnly(node, LevelErrorCode.MissingExitGrid, "rooms[0].exit");
    }

    [Fact]
    public void Overlap_TwoObjects()
    {
        var node = Fixtures.ExampleNode();
        var twin = Crate(node).DeepClone();
        twin["id"] = "crate-2";
        twin["position"] = V(6.5, 0.5, 2);
        List(node, "objects").Add(twin);

        AssertOnly(node, LevelErrorCode.Overlap, "rooms[0].objects[1]");
    }

    [Fact]
    public void Overlap_ObjectOnAButton()
    {
        var node = Fixtures.ExampleNode();
        Crate(node)["position"] = V(6, 0.5, 7);

        AssertOnly(node, LevelErrorCode.Overlap, "rooms[0].objects[0]");
    }

    [Fact]
    public void Overlap_ObjectInsideADoor()
    {
        var node = Fixtures.ExampleNode();
        Crate(node)["position"] = V(10.15, 0.5, 5);

        AssertOnly(node, LevelErrorCode.Overlap, "rooms[0].objects[0]");
    }

    [Fact]
    public void Overlap_ObjectInsideAKillZone()
    {
        var node = Fixtures.ExampleNode();
        Fixtures.Room(node)["killZones"] = new JsonArray(new JsonObject
        {
            ["id"] = "hole",
            ["box"] = BoxNode(V(5.5, 0, 1.5), V(6.5, 0.2, 2.5)),
        });

        AssertOnly(node, LevelErrorCode.Overlap, "rooms[0].objects[0]");
    }

    [Fact]
    public void Overlap_ObjectOnTheSpawnArea()
    {
        var node = Fixtures.ExampleNode();
        Crate(node)["position"] = V(1.2, 0.5, 5);

        AssertOnly(node, LevelErrorCode.Overlap, "rooms[0].objects[0]");
    }

    [Fact]
    public void ObjectTouchingAButton_DoesNotOverlap()
    {
        var node = Fixtures.ExampleNode();
        Crate(node)["position"] = V(6, 0.5, 5.75); // crate z 5.25–6.25, button z 6.25–7.75

        Assert.Empty(Validate(node));
    }

    [Fact]
    public void SpawnUnsafe_InsideAKillZone()
    {
        var node = Fixtures.ExampleNode();
        Fixtures.Room(node)["killZones"] = new JsonArray(new JsonObject
        {
            ["id"] = "hole",
            ["box"] = BoxNode(V(0.5, 0, 4.5), V(1.5, 0.5, 5.5)),
        });

        AssertOnly(node, LevelErrorCode.SpawnUnsafe, "rooms[0].spawn");
    }

    [Fact]
    public void SpawnUnsafe_InsideABarrierThatBlocksThePlayer()
    {
        var node = Fixtures.ExampleNode();
        List(node, "barriers").Add(new JsonObject
        {
            ["id"] = "no-player",
            ["blocksPlayer"] = true,
            ["box"] = BoxNode(V(0.5, 0, 4), V(2, 3, 6)),
        });

        AssertOnly(node, LevelErrorCode.SpawnUnsafe, "rooms[0].spawn");
    }

    [Fact]
    public void SpawnInsideAVariableZone_IsAllowed()
    {
        var node = Fixtures.ExampleNode();
        List(node, "barriers").Add(new JsonObject
        {
            ["id"] = "zone",
            ["blocksVariables"] = true,
            ["box"] = BoxNode(V(0.5, 0, 4), V(2, 3, 6)),
        });

        Assert.Empty(Validate(node));
    }

    [Fact]
    public void EmptyRoomBounds_SkipsTheSpatialChecksOfThatRoom()
    {
        var node = Fixtures.ExampleNode();
        Fixtures.Room(node)["bounds"] = BoxNode(V(0, 0, 0), V(0, 0, 0));

        AssertOnly(node, LevelErrorCode.EmptyBox, "rooms[0].bounds");
    }

    // SC 3: every code of the spec table is produced by some test in this project.
    [Fact]
    public void EveryErrorCode_IsProducedByAtLeastOneTest()
    {
        var source = string.Join("\n", Directory.GetFiles(AppContext.BaseDirectory.Split("bin")[0], "*Tests.cs")
            .Select(File.ReadAllText));

        // CatalogOrder belongs to the catalog (Task 6); the exclusion is removed there.
        foreach (var code in Enum.GetNames<LevelErrorCode>().Where(c => c != nameof(LevelErrorCode.CatalogOrder)))
            Assert.Contains($"LevelErrorCode.{code}", source);
    }
}
