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

        // The button counts the player too (play test 2026-11-03): standing on it opens the door, but
        // leaving it closes the door again, so the box is still the way. The ball alone is too light.
        var button = Assert.Single(room.Buttons);
        Assert.True(button.Position.Y > room.Spawn.Position.Y + 1, "the button is high (on a ledge)");
        Assert.All(room.Objects.Where(o => o.Kind == ObjectKind.Ball), ball => Assert.True(ball.MassKg < button.RequiredMassKg));
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

    [Fact]
    public void Room3_1_HeavyButton()
    {
        var room = Shipped.Room("3.1");

        Assert.Equal(new[] { PropertyNames.Mass }, Unlocked("3.1"));
        var button = Assert.Single(room.Buttons, b => b.RequiredMassKg > LevelFile.PlayerMassKg);
        Assert.Contains(room.Objects, o => o.MassKg >= button.RequiredMassKg && CanSwap(o, PropertyNames.Mass));
    }

    [Fact]
    public void Room3_2_JumpDependsOnMass()
    {
        var room = Shipped.Room("3.2");

        Assert.Contains(room.Objects, o => o.MassKg <= 10 && CanSwap(o, PropertyNames.Mass));
        Assert.True(room.Exit.Min.Y >= room.Spawn.Position.Y + 1.2, "the exit is at least 1.2 m above the spawn floor");
    }

    [Fact]
    public void Room5_1_ReceiveEnergyFromAMovingBall()
    {
        var room = Shipped.Room("5.1");

        Assert.Equal(new[] { PropertyNames.KineticEnergy }, Unlocked("5.1"));
        Assert.Contains(room.Objects, o =>
            o.Kind == ObjectKind.Ball && o.Velocity.Length > 0 && o.Bounce >= 0.9 && CanSwap(o, PropertyNames.KineticEnergy));
    }

    [Fact]
    public void Room5_2_GiveEnergyToABoxAtRest()
    {
        var room = Shipped.Room("5.2");

        Assert.Contains(room.Objects, o =>
            o.Kind == ObjectKind.Box && o.Velocity == Vec3.Zero && CanSwap(o, PropertyNames.KineticEnergy));
        Assert.Contains(room.Buttons, b => b.Detects == ButtonDetects.Objects);
    }
}
