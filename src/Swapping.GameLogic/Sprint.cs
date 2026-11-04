namespace Swapping.GameLogic;

/// <summary>
/// Running (Shift, user's request 2026-11-04): 1.4 × the current walk speed (after the hold slowdown of
/// <see cref="HoldLoad"/>). Kept low on purpose: a running jump must still fall short of the pits.
/// </summary>
public static class Sprint
{
    public const double Factor = 1.4;

    public static double Speed(double walkSpeed, bool sprinting) => sprinting ? walkSpeed * Factor : walkSpeed;
}
