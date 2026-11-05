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
        // User play test 2026-11-05: a 10 kg box was just thrown onto the button, no swap needed. Too heavy to hold
        // (HoldLoad: > 2.5 × 70 kg), it goes up only by a position swap; mass swaps are locked in level 1.
        Assert.All(room.Objects.Where(o => o.Kind == ObjectKind.Box), box => Assert.True(box.MassKg > 2.5 * LevelFile.PlayerMassKg, "too heavy to hold and throw"));
        Assert.DoesNotContain(PropertyNames.Mass, Unlocked("1.3"));
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
    public void Room2_3_BorrowedMass_TheCrateIsCarriedLightAndPaidBackOnTheCagedButton()
    {
        // Design reviewed with the user (2026-11-03): no shortcut by holding the heavy crate (HoldLoad: > 2.5 × 70 kg),
        // by teleporting it onto the button (it accepts only mass, and a cage keeps the player off the button),
        // or by making the ball heavy (it accepts only position).
        var room = Shipped.Room("2.3");

        Assert.Contains(PropertyNames.Mass, Unlocked("2.3"));
        var button = Assert.Single(room.Buttons);
        Assert.True(button.RequiredMassKg > LevelFile.PlayerMassKg && button.Detects == ButtonDetects.Objects);
        var crate = Assert.Single(room.Objects, o => o.MassKg >= button.RequiredMassKg);
        Assert.True(crate.MassKg > 2.5 * LevelFile.PlayerMassKg, "too heavy to hold before the mass swap");
        Assert.True(CanSwap(crate, PropertyNames.Mass) && !CanSwap(crate, PropertyNames.Position));
        Assert.Contains(room.Barriers, b => b.BlocksPlayer && !b.BlocksObjects && !b.BlocksVariables && b.Box.Contains(button.Position));
        var grid = Assert.Single(room.Barriers, b => b.BlocksPlayer && b.Box.Min.Z <= room.Bounds.Min.Z && b.Box.Max.Z >= room.Bounds.Max.Z);
        Assert.True(crate.Position.X < grid.Box.Min.X && button.Position.X > grid.Box.Max.X);
        Assert.Contains(room.Objects, o => o.Position.X > grid.Box.Max.X && CanSwap(o, PropertyNames.Position) && !CanSwap(o, PropertyNames.Mass));
    }

    [Fact]
    public void Room2_4_HighJump_ThenSwapPlacesBeforeTheFallKills()
    {
        // User's design (2026-11-03): a 2 kg box lends its mass for a ~12 m jump over a wall; a block beyond
        // it takes the player down by a position swap. Closed ceiling at 15 m, so the rest of the map stays hidden.
        var room = Shipped.Room("2.4");

        var light = Assert.Single(room.Objects, o => CanSwap(o, PropertyNames.Mass));
        Assert.True(light.MassKg <= 2 && !CanSwap(light, PropertyNames.Position));
        var block = Assert.Single(room.Objects, o => CanSwap(o, PropertyNames.Position));
        Assert.True(!CanSwap(block, PropertyNames.Mass) && block.Position.X > light.Position.X);
        Assert.True(room.Bounds.Max.Y >= 15, "room tall enough for the jump under the ceiling");
        Assert.True(room.RulesOverride?.RangeMeters?[PropertyNames.Position] >= 13, "the block is reachable from the top of the jump");
    }

    [Fact]
    public void Room3_1_ReceiveEnergyFromAMovingBall()
    {
        var room = Shipped.Room("3.1");

        Assert.Contains(PropertyNames.KineticEnergy, Unlocked("3.1"));
        Assert.All(room.Objects, o => Assert.False(CanSwap(o, PropertyNames.Position) || CanSwap(o, PropertyNames.Mass), $"{o.Id} would skip the lesson"));
        Assert.Contains(room.Objects, o =>
            o.Kind == ObjectKind.Ball && o.Velocity.Length > 0 && o.Bounce >= 0.9 && CanSwap(o, PropertyNames.KineticEnergy));
    }

    [Fact]
    public void Room3_2_GiveEnergyToABoxAtRest()
    {
        var room = Shipped.Room("3.2");

        Assert.Contains(room.Objects, o =>
            o.Kind == ObjectKind.Box && o.Velocity == Vec3.Zero && CanSwap(o, PropertyNames.KineticEnergy) && !CanSwap(o, PropertyNames.Position));
        Assert.Contains(room.Buttons, b => b.Detects == ButtonDetects.Objects);
    }
}
