namespace Swapping.GameLogic;

/// <summary>
/// The player launched by an energy swap (task 17): a horizontal speed above walking is kept instead of being
/// replaced by the walk, and slows down by friction — 4 m/s² on the floor, 0.3 m/s² in the air — until it is
/// back to walking speed. Without it the walk wiped the received energy on the next frame.
/// </summary>
public static class Launch
{
    public const double FloorBrake = 4.0;
    public const double AirBrake = 0.3;

    public static bool Starts(double horizontalSpeed, double walkSpeed) => horizontalSpeed > walkSpeed + 1e-6;

    /// <summary>
    /// After a swap: only an energy swap launches (user, 2026-11-05). A mass swap keeps the energy, so walking
    /// into a light mass would shoot the player forward with no energy received; walking stays walking.
    /// </summary>
    public static bool FromSwap(bool energySwap, double horizontalSpeed, double walkSpeed) =>
        energySwap && Starts(horizontalSpeed, walkSpeed);

    public static double Step(double speed, bool onFloor, double deltaSeconds) =>
        Math.Max(0, speed - (onFloor ? FloorBrake : AirBrake) * deltaSeconds);

    /// <summary>Distance (m) on the floor from <paramref name="fromSpeed"/> to <paramref name="toSpeed"/>: (v₀² − v²) / 2a.</summary>
    public static double FloorDistance(double fromSpeed, double toSpeed) => (fromSpeed * fromSpeed - toSpeed * toSpeed) / (2 * FloorBrake);
}
