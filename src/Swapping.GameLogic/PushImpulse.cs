namespace Swapping.GameLogic;

/// <summary>
/// The player walking into a movable body. After the user's play test ("the box has no weight"), the
/// push is a force: the player pushes with at most <see cref="MaxPushForceN"/>, friction with the floor
/// takes μ·m·g of it, and what is left sets the speed: walk speed × (1 − μ·m·g / F). A 10 kg box moves at
/// about 2.1 m/s, a 30 kg box crawls, and from about 34 kg on nothing moves. The player slows down to
/// the same speed while pushing. Draft numbers, tuned by playing.
/// </summary>
public static class PushImpulse
{
    public const double MaxPushForceN = 200;
    public const double FloorFriction = 0.6;

    /// <summary>The speed (m/s) at which the player can push a body of this mass.</summary>
    public static double Speed(double bodyMassKg, double walkSpeed)
    {
        var friction = FloorFriction * bodyMassKg * Rules.Physics.Formulas.Gravity;
        return walkSpeed * Math.Max(0, 1 - friction / MaxPushForceN);
    }

    /// <summary>The impulse (N·s) that brings the body up to the push speed along the walking direction; never negative.</summary>
    public static double Impulse(double bodyMassKg, double walkSpeed, double bodySpeedAlong) =>
        Math.Max(0, Speed(bodyMassKg, walkSpeed) - bodySpeedAlong) * bodyMassKg;
}
