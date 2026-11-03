using Swapping.Rules.Model;

namespace Swapping.GameLogic.Tests;

/// <summary>The color of a barrier says what it blocks: one for objects, another for the player.</summary>
public sealed class BarrierPaletteTests
{
    [Fact]
    public void Objects_player_both_and_variables_each_have_their_own_color()
    {
        var colors = new[]
        {
            BarrierPalette.For(new BarrierFilter(BlocksObjects: true)),
            BarrierPalette.For(new BarrierFilter(BlocksPlayer: true)),
            BarrierPalette.For(new BarrierFilter(BlocksObjects: true, BlocksPlayer: true)),
            BarrierPalette.For(new BarrierFilter(BlocksVariables: true)),
        };
        Assert.Equal(4, colors.Distinct().Count());
    }

    [Fact]
    public void The_exit_grid_looks_like_a_barrier_for_objects()
    {
        Assert.Equal(BarrierPalette.For(new BarrierFilter(BlocksObjects: true)), BarrierPalette.For(BarrierFilter.ExitGrid));
    }

    [Fact]
    public void Every_color_is_a_valid_light_enough_rgb()
    {
        foreach (var filter in new[] { new BarrierFilter(), new BarrierFilter(true), new BarrierFilter(false, true), new BarrierFilter(true, true), new BarrierFilter(false, false, true) })
        {
            var (r, g, b) = BarrierPalette.For(filter);
            Assert.InRange(r, 0.0, 1.0);
            Assert.InRange(g, 0.0, 1.0);
            Assert.InRange(b, 0.0, 1.0);
            Assert.True(Math.Max(r, Math.Max(g, b)) >= 0.6, "dark stripes need a bright base to stay colored");
        }
    }
}
