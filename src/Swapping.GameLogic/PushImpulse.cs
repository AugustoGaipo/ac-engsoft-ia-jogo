namespace Swapping.GameLogic;

/// <summary>
/// The player walking into a movable body (SPEC-game, physics settings). The body is brought up to the
/// speed a perfectly inelastic push would give, walk speed × player / (player + body), so a 300 kg crate
/// barely moves and a 10 kg box almost keeps up.
/// </summary>
public static class PushImpulse
{
    /// <summary>The impulse (N·s) along the walking direction; never negative.</summary>
    public static double Impulse(double playerMassKg, double bodyMassKg, double walkSpeed, double bodySpeedAlong)
    {
        var target = walkSpeed * playerMassKg / (playerMassKg + bodyMassKg);
        return Math.Max(0, target - bodySpeedAlong) * bodyMassKg;
    }
}
