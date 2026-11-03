using Swapping.Levels.Model;
using Swapping.Rules.Model;
using Swapping.Rules.Operations;

namespace Swapping.GameLogic.Tests;

public sealed class CrosshairStateTests
{
    [Fact]
    public void Nothing_aimed_shows_the_idle_dot()
    {
        Assert.Equal(new Crosshair(CrosshairLook.Idle, ""), CrosshairState.For(aimingAtContainer: false, reason: null));
    }

    [Fact]
    public void A_swap_that_would_be_applied_is_white_without_text()
    {
        Assert.Equal(new Crosshair(CrosshairLook.Ready, ""), CrosshairState.For(aimingAtContainer: true, reason: null));
    }

    [Theory]
    [InlineData(RejectReason.SameContainer, "você mesmo")]
    [InlineData(RejectReason.ModeNotEnabled, "objeto com objeto: ainda não")]
    [InlineData(RejectReason.PropertyNotUnlocked, "trocas bloqueadas aqui")]
    [InlineData(RejectReason.PropertyNotAccepted, "não aceita esta propriedade")]
    [InlineData(RejectReason.Locked, "travado")]
    [InlineData(RejectReason.OutOfRange, "fora de alcance")]
    [InlineData(RejectReason.NoLineOfSight, "sem linha de visão")]
    [InlineData(RejectReason.BlockedByBarrier, "barreira no caminho")]
    [InlineData(RejectReason.InsideVariableBarrier, "dentro de uma barreira")]
    [InlineData(RejectReason.OnCooldown, "recarregando")]
    [InlineData(RejectReason.TooManyProperties, "uma propriedade por vez")]
    public void A_refused_swap_is_red_with_a_short_reason(RejectReason reason, string text)
    {
        Assert.Equal(new Crosshair(CrosshairLook.Refused, text), CrosshairState.For(aimingAtContainer: true, reason));
    }

    [Fact]
    public void Every_reject_reason_has_its_own_text()
    {
        var texts = Enum.GetValues<RejectReason>().Select(r => CrosshairState.For(true, r).Reason).ToList();

        Assert.All(texts, t => Assert.False(string.IsNullOrWhiteSpace(t)));
        Assert.Equal(texts.Count, texts.Distinct().Count());
    }
}

public sealed class SwapChoiceTests
{
    [Fact]
    public void The_right_button_swaps_the_first_unlocked_property_in_the_fixed_order()
    {
        Assert.Same(PropertyKind.Position, SwapChoice.Property(new HashSet<IPropertyKind> { PropertyKind.Position }));
        Assert.Same(PropertyKind.Mass, SwapChoice.Property(new HashSet<IPropertyKind> { PropertyKind.KineticEnergy, PropertyKind.Mass }));
        Assert.Same(PropertyKind.KineticEnergy, SwapChoice.Property(new HashSet<IPropertyKind> { PropertyKind.KineticEnergy }));
    }

    [Fact]
    public void A_room_with_nothing_unlocked_still_asks_for_position_so_the_rules_explain_the_refusal()
    {
        Assert.Same(PropertyKind.Position, SwapChoice.Property(new HashSet<IPropertyKind>()));
    }
}

public sealed class ReadoutTests
{
    [Fact]
    public void The_numbers_of_a_body()
    {
        Assert.Equal(new Readout("70 kg", "0,0 m/s", "0 J"), Readout.Of(70, Vec3.Zero));
        Assert.Equal(new Readout("10 kg", "3,0 m/s", "45 J"), Readout.Of(10, new Vec3(0, 0, 3)));
        Assert.Equal(new Readout("2,5 kg", "5,0 m/s", "31 J"), Readout.Of(2.5, new Vec3(3, 4, 0)));
    }
}

public sealed class BarrierGeometryTests
{
    private static readonly Box Grid = new(new Vec3(11.5, 0, 4), new Vec3(11.6, 3, 6));

    [Fact]
    public void A_segment_through_the_box_crosses_it()
    {
        Assert.True(BarrierGeometry.Crosses(new Vec3(10, 1, 5), new Vec3(13, 1, 5), Grid));
    }

    [Fact]
    public void A_segment_that_stops_before_the_box_or_passes_beside_it_does_not()
    {
        Assert.False(BarrierGeometry.Crosses(new Vec3(10, 1, 5), new Vec3(11.4, 1, 5), Grid));
        Assert.False(BarrierGeometry.Crosses(new Vec3(10, 1, 7), new Vec3(13, 1, 7), Grid));
        Assert.False(BarrierGeometry.Crosses(new Vec3(10, 4, 5), new Vec3(13, 4, 5), Grid));
    }

    [Fact]
    public void A_segment_parallel_to_a_face_inside_the_slab_crosses_it()
    {
        Assert.True(BarrierGeometry.Crosses(new Vec3(11.55, 1, 0), new Vec3(11.55, 1, 10), Grid));
        Assert.False(BarrierGeometry.Crosses(new Vec3(11.7, 1, 0), new Vec3(11.7, 1, 10), Grid));
    }

    [Fact]
    public void Inside_counts_the_faces()
    {
        Assert.True(BarrierGeometry.Inside(new Vec3(11.55, 1, 5), Grid));
        Assert.True(BarrierGeometry.Inside(new Vec3(11.5, 0, 4), Grid));
        Assert.False(BarrierGeometry.Inside(new Vec3(11.4, 1, 5), Grid));
    }
}
