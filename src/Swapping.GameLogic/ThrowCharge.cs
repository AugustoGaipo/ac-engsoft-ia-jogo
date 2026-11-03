namespace Swapping.GameLogic;

/// <summary>
/// The throw bar (ideas bank, user's numbers 2026-11-03): holding the throw button fills a bar linearly in
/// <see cref="FullSeconds"/>; letting go throws with that share of <see cref="MaxEnergyJoules"/>. A quick tap
/// (under <see cref="TapSeconds"/>) only drops the body, with no push.
/// </summary>
public static class ThrowCharge
{
    public const double FullSeconds = 3.0;
    public const double MaxEnergyJoules = 120.0;
    public const double TapSeconds = 0.15;

    /// <summary>0 (empty) to 1 (full) after holding for <paramref name="heldSeconds"/>.</summary>
    public static double Fraction(double heldSeconds) => Math.Clamp(heldSeconds / FullSeconds, 0, 1);

    public static double EnergyJoules(double heldSeconds) => Fraction(heldSeconds) * MaxEnergyJoules;

    public static bool IsTap(double heldSeconds) => heldSeconds < TapSeconds;
}
