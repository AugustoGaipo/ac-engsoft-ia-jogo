using Swapping.Rules.Physics;

namespace Swapping.GameLogic;

/// <summary>
/// What holding a body costs (user's request, 2026-11-03): up to half your mass you walk freely; above it the
/// walk falls from 90 % to 5 % at 2.5 × your mass; above 2.5 × you cannot hold it. The jump lifts both masses.
/// </summary>
public static class HoldLoad
{
    public const double FreeRatio = 0.5;
    public const double MaxRatio = 2.5;
    public const double SlowestFactor = 0.05;
    public const double FirstSlowFactor = 0.9;

    public static bool CanHold(double heldKg, double playerKg) => heldKg <= MaxRatio * playerKg;

    public static double SpeedFactor(double heldKg, double playerKg)
    {
        var ratio = heldKg / playerKg;
        if (ratio <= FreeRatio) return 1.0;
        var t = Math.Min(1.0, (ratio - FreeRatio) / (MaxRatio - FreeRatio));
        return FirstSlowFactor + (SlowestFactor - FirstSlowFactor) * t;
    }

    public static double JumpTakeoffSpeed(double heldKg, double playerKg) => Formulas.JumpTakeoffSpeed(playerKg + heldKg);
}
