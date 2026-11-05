using Swapping.Rules.Model;
using Swapping.Rules.Operations;

namespace Swapping.GameLogic.Tests;

/// <summary>
/// The ring on the crosshair (user, 2026-11-05): one equal slice per swap the aimed object accepts here,
/// the chosen property in the middle, an X when the swap is not allowed (cooldown is only waiting, not an X).
/// The outline is thick when the object can be held right now, thin otherwise.
/// </summary>
public sealed class SwapRingTests
{
    private static readonly HashSet<IPropertyKind> All = [PropertyKind.Position, PropertyKind.Mass, PropertyKind.KineticEnergy];

    [Fact]
    public void An_object_that_accepts_everything_unlocked_gets_three_slices_in_a_fixed_order()
    {
        var box = new Container("box", ContainerType.Object, 1, 1);
        Assert.Equal([PropertyKind.Position, PropertyKind.Mass, PropertyKind.KineticEnergy], SwapRing.Segments(box, All));
    }

    [Fact]
    public void Only_what_it_accepts_and_the_room_unlocked()
    {
        var energyOnly = new Container("ball", ContainerType.Object, 1, 1, Locked: false, AcceptedProperties: new HashSet<IPropertyKind> { PropertyKind.KineticEnergy });
        Assert.Equal([PropertyKind.KineticEnergy], SwapRing.Segments(energyOnly, All));
        var box = new Container("box", ContainerType.Object, 1, 1);
        Assert.Equal([PropertyKind.Position], SwapRing.Segments(box, new HashSet<IPropertyKind> { PropertyKind.Position }));
    }

    [Fact]
    public void A_locked_object_has_no_slice()
    {
        var locked = new Container("box", ContainerType.Object, 1, 1, Locked: true);
        Assert.Empty(SwapRing.Segments(locked, All));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(RejectReason.OnCooldown, false)]        // just wait: the ring shows the reload
    [InlineData(RejectReason.PropertyNotAccepted, true)]
    [InlineData(RejectReason.OutOfRange, true)]
    [InlineData(RejectReason.BlockedByBarrier, true)]
    public void An_X_when_the_swap_is_not_allowed(RejectReason? reason, bool cross)
    {
        Assert.Equal(cross, SwapRing.ShowsCross(reason));
    }

    [Theory]
    [InlineData(2.0, 10, false, true)]     // close, light, nothing between: can be held → thick
    [InlineData(3.0, 10, false, false)]    // beyond the 2.5 m hold reach
    [InlineData(2.0, 200, false, false)]   // heavier than 2.5 × 70 kg
    [InlineData(2.0, 10, true, false)]     // an object barrier in between
    public void The_outline_is_thick_only_when_the_object_can_be_held_now(double distance, double massKg, bool blocked, bool thick)
    {
        Assert.Equal(thick, SwapRing.ThickOutline(distance, reach: 2.5, bodyKg: massKg, playerKg: 70, blocked));
    }
}
