using Swapping.Levels.Model;
using Swapping.Rules.Model;

namespace Swapping.Levels.Tests;

/// <summary>
/// One test per room of Friday's levels: the pieces its lesson needs exist
/// (table "The 4 levels for Friday" in SPEC-levels.md). Whether a room is fun or solvable is checked by playing.
/// </summary>
public class RoomDesignTests
{
    private static IReadOnlyList<string> Unlocked(string roomId) =>
        LevelValidator.UnlockedIn(Shipped.Level(int.Parse(roomId.Split('.')[0])), Shipped.Room(roomId));

    private static bool CanSwap(ObjectDef obj, string property) =>
        !obj.Locked && (obj.Accepts is null || obj.Accepts.Contains(property));

    private static Vec3 PlayerCenter(RoomDef room) => room.Spawn.Position + RulesMapping.PlayerCenterAboveFeet;

    private static double Distance(Vec3 a, Vec3 b) => (a - b).Length;

    [Fact]
    public void Room1_1_WalkHoldButtonDoorAndExitGrid_NoSwap()
    {
        var room = Shipped.Room("1.1");

        Assert.Empty(Unlocked("1.1"));
        Assert.Contains(room.Objects, o => o.Kind == ObjectKind.Box);
        Assert.NotEmpty(room.Buttons);
        Assert.NotEmpty(room.Doors);
    }

    [Fact]
    public void Room1_2_FirstPositionSwapAcrossAGap()
    {
        var room = Shipped.Room("1.2");
        var range = Shipped.Level(1).Rules.RangeMeters.GetValueOrDefault(PropertyNames.Position, 8.0);

        Assert.Contains(PropertyNames.Position, Unlocked("1.2"));
        var gap = Assert.Single(room.KillZones, z => z.Box.Min.X > room.Spawn.Position.X && z.Box.Max.X < room.Exit.Min.X);
        Assert.Contains(room.Objects, o =>
            o.Position.X > gap.Box.Max.X && CanSwap(o, PropertyNames.Position)
            && Distance(o.Position, PlayerCenter(room)) <= range);
    }

    [Fact]
    public void Room1_3_SwapToPutTheBoxOnAHighButton()
    {
        var room = Shipped.Room("1.3");

        var button = Assert.Single(room.Buttons, b => b.Detects == ButtonDetects.Objects);
        Assert.True(button.Position.Y > room.Spawn.Position.Y + 1, "the button is high (on a ledge)");
        Assert.Contains(room.Objects, o => o.Kind == ObjectKind.Box && CanSwap(o, PropertyNames.Position));
    }

    [Fact]
    public void Room1_4_OneBoxRefusesPositionAnotherAcceptsIt()
    {
        var room = Shipped.Room("1.4");

        Assert.Contains(room.Objects, o => !CanSwap(o, PropertyNames.Position));
        Assert.Contains(room.Objects, o => CanSwap(o, PropertyNames.Position));
    }

    [Fact]
    public void Room2_1_Barrier1_TheBoxCrossesBySwapping()
    {
        var room = Shipped.Room("2.1");

        var barrier = Assert.Single(room.Barriers, b => b.Filter == new BarrierFilter(BlocksObjects: true));
        Assert.True(room.Spawn.Position.X < barrier.Box.Min.X, "the player starts before the barrier");
        Assert.Contains(room.Buttons, b => b.Position.X > barrier.Box.Max.X && b.Detects == ButtonDetects.Objects);
        Assert.Contains(room.Objects, o => o.Position.X < barrier.Box.Min.X && CanSwap(o, PropertyNames.Position));
    }

    [Fact]
    public void Room2_2_Barrier2_ThePlayerCrossesBySwapping()
    {
        var room = Shipped.Room("2.2");

        var barrier = Assert.Single(room.Barriers, b => b.Filter == new BarrierFilter(BlocksPlayer: true));
        Assert.True(room.Spawn.Position.X < barrier.Box.Min.X, "the player starts before the barrier");
        Assert.Contains(room.Objects, o => o.Position.X > barrier.Box.Max.X && CanSwap(o, PropertyNames.Position));
    }

    [Fact]
    public void Room2_3_Barrier3_AZoneWithoutSwaps()
    {
        var room = Shipped.Room("2.3");

        Assert.Contains(room.Barriers, b =>
            b.Filter == new BarrierFilter(BlocksVariables: true)
            && b.Box.Max.X - b.Box.Min.X >= 2 && b.Box.Max.Z - b.Box.Min.Z >= 2);
    }
}
