using Swapping.Rules.Model;

namespace Swapping.GameLogic;

/// <summary>The base color (RGB, 0–1) of a barrier by what it blocks. The game draws the stripes in a darker shade of it.</summary>
public static class BarrierPalette
{
    private static readonly (double R, double G, double B) Objects = (0.25, 0.78, 1.0);   // blue: objects
    private static readonly (double R, double G, double B) Player = (1.0, 0.62, 0.2);     // orange: the player
    private static readonly (double R, double G, double B) Both = (0.72, 0.5, 1.0);       // violet: both
    private static readonly (double R, double G, double B) Variables = (0.4, 0.92, 0.62); // green: only variables
    private static readonly (double R, double G, double B) Neutral = (0.75, 0.78, 0.85);

    /// <summary>Objects and/or player decide the color (the exit grid blocks objects, so it is blue); variables alone are green.</summary>
    public static (double R, double G, double B) For(BarrierFilter filter) =>
        (filter.BlocksObjects, filter.BlocksPlayer) switch
        {
            (true, true) => Both,
            (true, false) => Objects,
            (false, true) => Player,
            _ => filter.BlocksVariables ? Variables : Neutral,
        };
}
