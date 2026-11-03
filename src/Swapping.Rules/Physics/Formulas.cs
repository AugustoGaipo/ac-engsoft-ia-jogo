namespace Swapping.Rules.Physics;

public enum BuoyancyState { Sink, Swim, Float }

/// <summary>Pure SI formulas of the rules (m, kg, J, s, Pa).</summary>
public static class Formulas
{
    public const double Gravity = 9.8;
    public const double SinkFactor = 1.1;
    public const double FloatFactor = 0.1;
    public const double JumpEnergyJoules = 343.0;

    /// <summary>Take-off speed (m/s) above which only half of the extra counts (the old 6 m/s hard cap became this soft one).</summary>
    public const double SoftJumpSpeedCap = 12.0;

    public const double DefaultSwimSpeed = 3.0;

    private const double BandTolerance = 1e-9;

    public static double KineticEnergy(double massKg, double speed) => 0.5 * massKg * speed * speed;

    public static double SpeedFromEnergy(double energyJoules, double massKg) =>
        energyJoules <= 0 ? 0 : Math.Sqrt(2 * energyJoules / massKg);

    public static double Density(double massKg, double volumeM3) => massKg / volumeM3;

    /// <summary>(ρ_fluid − ρ_object)·V·g; positive means upward.</summary>
    public static double BuoyancyForce(double fluidDensity, double objectDensity, double volumeM3) =>
        (fluidDensity - objectDensity) * volumeM3 * Gravity;

    public static BuoyancyState BuoyancyState(double objectDensity, double fluidDensity)
    {
        if (objectDensity > fluidDensity * SinkFactor * (1 + BandTolerance)) return Physics.BuoyancyState.Sink;
        if (objectDensity < fluidDensity * FloatFactor * (1 - BandTolerance)) return Physics.BuoyancyState.Float;
        return Physics.BuoyancyState.Swim;
    }

    public static double StaticPressure(double massKg, double contactAreaM2) =>
        massKg * Gravity / contactAreaM2;

    public static bool Breaks(double pressurePa, double limitPa) => pressurePa > limitPa;

    /// <summary>Speed the thrower is pushed back; zero on the ground (friction) or when only dropping.</summary>
    public static double RecoilSpeed(double objectMassKg, double addedSpeed, double throwerMassKg, bool onGround) =>
        onGround ? 0 : objectMassKg * addedSpeed / throwerMassKg;

    /// <summary>
    /// Take-off speed of a jump: √(2·E_jump ÷ m) up to <see cref="SoftJumpSpeedCap"/> (12 m/s); above it only
    /// half of the extra speed counts: 12 + (raw − 12) ÷ 2. Continuous and still rising as the body gets lighter.
    /// </summary>
    public static double JumpTakeoffSpeed(double massKg)
    {
        var raw = SpeedFromEnergy(JumpEnergyJoules, massKg);
        return raw <= SoftJumpSpeedCap ? raw : SoftJumpSpeedCap + (raw - SoftJumpSpeedCap) / 2;
    }

    /// <summary>Jump height h = v²/(2·g_eff). Returns +∞ when the body is not denser than the medium.</summary>
    public static double JumpHeight(double massKg, double density, double fluidDensity)
    {
        var effectiveGravity = density > fluidDensity ? Gravity * (1 - fluidDensity / density) : 0;
        if (effectiveGravity <= 0) return double.PositiveInfinity;
        var v = JumpTakeoffSpeed(massKg);
        return v * v / (2 * effectiveGravity);
    }
}
