namespace Swapping.GameLogic;

/// <summary>Death by falling (decision G3): landing faster than 12 m/s, about a 7 m drop. Draft, tuned by playing.</summary>
public static class FallDeath
{
    public const double LimitSpeed = 12.0;

    public static double HeightForLimit => LimitSpeed * LimitSpeed / (2 * Rules.Physics.Formulas.Gravity);

    public static bool Kills(double landingSpeed) => landingSpeed > LimitSpeed;
}
