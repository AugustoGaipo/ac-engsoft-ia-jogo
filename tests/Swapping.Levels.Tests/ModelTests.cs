using Swapping.Levels.Model;
using Swapping.Rules.Model;

namespace Swapping.Levels.Tests;

public class ModelTests
{
    private const double Tolerance = 1e-9;

    private static ObjectDef Obj(ObjectKind kind, Vec3 size, double massKg = 1) =>
        new("o", kind, new Vec3(0, 0, 0), size, massKg, Accepts: null, Locked: false, Vec3.Zero, Bounce: 0);

    [Fact]
    public void BoxObject_VolumeIsProductOfEdges_ContactIsBottomFace()
    {
        var box = Obj(ObjectKind.Box, new Vec3(1, 2, 3));

        Assert.Equal(6.0, box.VolumeM3, Tolerance);
        Assert.Equal(3.0, box.ContactAreaM2, Tolerance);
    }

    [Fact]
    public void BallObject_VolumeIsPiDCubedOverSix_ContactIsOnePercentOfCrossSection()
    {
        var ball = Obj(ObjectKind.Ball, new Vec3(1, 1, 1));

        Assert.Equal(Math.PI / 6, ball.VolumeM3, Tolerance);
        Assert.Equal(0.01 * Math.PI / 4, ball.ContactAreaM2, Tolerance);
    }

    [Fact]
    public void Density_IsMassOverDerivedVolume()
    {
        var crate = Obj(ObjectKind.Box, new Vec3(1, 1, 1), massKg: 300);

        Assert.Equal(300.0, crate.DensityKgM3, Tolerance);
    }

    [Fact]
    public void ObjectBounds_AreCenteredOnThePosition()
    {
        var crate = Obj(ObjectKind.Box, new Vec3(2, 1, 2)) with { Position = new Vec3(5, 0.5, 5) };

        Assert.Equal(new Box(new Vec3(4, 0, 4), new Vec3(6, 1, 6)), crate.Bounds);
    }

    [Fact]
    public void ButtonBounds_StandOnThePosition()
    {
        var button = new ButtonDef("b", new Vec3(5, 0, 5), new Vec3(2, 0.1, 2), RequiredMassKg: 1, ButtonDetects.Any);

        Assert.Equal(new Box(new Vec3(4, 0, 4), new Vec3(6, 0.1, 6)), button.Bounds);
    }

    [Fact]
    public void SpawnArea_IsPlayerSizedBoxStandingOnTheSpawn()
    {
        var spawn = new SpawnDef(new Vec3(1, 0, 1), YawDegrees: 0);

        Assert.Equal(new Box(new Vec3(0.7, 0, 0.7), new Vec3(1.3, 1.8, 1.3)), spawn.Area);
    }

    [Fact]
    public void BoxesThatOnlyTouch_DoNotOverlap()
    {
        var a = new Box(new Vec3(0, 0, 0), new Vec3(1, 1, 1));
        var b = new Box(new Vec3(1, 0, 0), new Vec3(2, 1, 1));

        Assert.False(a.Overlaps(b));
        Assert.False(b.Overlaps(a));
    }

    [Fact]
    public void BoxesThatShareVolume_Overlap()
    {
        var a = new Box(new Vec3(0, 0, 0), new Vec3(1, 1, 1));
        var b = new Box(new Vec3(0.5, 0.5, 0.5), new Vec3(2, 2, 2));

        Assert.True(a.Overlaps(b));
    }

    [Fact]
    public void BoxInsideAnother_IsContained_EvenWhenTouchingTheFaces()
    {
        var room = new Box(new Vec3(0, 0, 0), new Vec3(10, 5, 10));

        Assert.True(room.Contains(new Box(new Vec3(0, 0, 0), new Vec3(1, 1, 1))));
        Assert.False(room.Contains(new Box(new Vec3(9.5, 0, 0), new Vec3(10.5, 1, 1))));
        Assert.True(room.Contains(new Vec3(10, 0, 10)));
        Assert.False(room.Contains(new Vec3(10.01, 0, 10)));
    }

    [Fact]
    public void Box_IsEmptyWhenMinIsNotBelowMaxOnSomeAxis()
    {
        Assert.True(new Box(new Vec3(0, 0, 0), new Vec3(1, 0, 1)).IsEmpty);
        Assert.False(new Box(new Vec3(0, 0, 0), new Vec3(1, 1, 1)).IsEmpty);
    }

    [Fact]
    public void BarrierPreset_ExitGrid_IsBarrier1Plus3()
    {
        var grid = new BarrierDef("g", BarrierPreset.ExitGrid, false, false, false, new Box(Vec3.Zero, new Vec3(1, 1, 1)));
        var flags = new BarrierDef("f", null, BlocksObjects: true, BlocksPlayer: false, BlocksVariables: true, grid.Box);

        Assert.Equal(BarrierFilter.ExitGrid, grid.Filter);
        Assert.True(flags.IsExitGrid);
        Assert.True(grid.IsExitGrid);
    }

    [Fact]
    public void LevelErrorCode_HasEveryCodeOfTheSpec()
    {
        var names = Enum.GetNames<LevelErrorCode>();

        Assert.Equal(21, names.Length);
    }
}
