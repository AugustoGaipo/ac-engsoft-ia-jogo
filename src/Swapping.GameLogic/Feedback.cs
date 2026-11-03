using Swapping.Levels.Model;

namespace Swapping.GameLogic;

/// <summary>
/// Sound and screen feedback (user's request, 2026-11-03): the jump sounds lower when heavy and higher when
/// light; a landing shakes the camera by its momentum (m·v), so heavy landings shake and light ones do not;
/// the death screen says why the player died.
/// </summary>
public static class Feedback
{
    public const double MinPitch = 0.5;
    public const double MaxPitch = 2.5;

    /// <summary>Momentum (kg·m/s) below which a landing does not shake: a 70 kg player's own jump lands at ≈ 217.</summary>
    public const double ShakeFromMomentum = 250;

    /// <summary>Momentum span from no shake to full shake.</summary>
    public const double ShakeSpan = 750;

    /// <summary>Pitch scale of the jump sound: 1 at the player's own 70 kg, (70 / m)^¼ otherwise, clamped.</summary>
    public static double JumpPitch(double massKg) =>
        Math.Clamp(Math.Pow(LevelFile.PlayerMassKg / Math.Max(massKg, 1e-6), 0.25), MinPitch, MaxPitch);

    /// <summary>Camera shake strength 0–1 for a landing at <paramref name="speed"/> m/s with <paramref name="massKg"/>.</summary>
    public static double LandingShake(double massKg, double speed) =>
        Math.Clamp((massKg * speed - ShakeFromMomentum) / ShakeSpan, 0, 1);

    public static string DeathText(string cause) =>
        string.IsNullOrWhiteSpace(cause) ? "VOCÊ MORREU" : $"VOCÊ MORREU\n{char.ToUpperInvariant(cause[0])}{cause[1..]}";

    /// <summary>Below this speed (m/s) a body touching something makes no sound (resting, sliding, rolling).</summary>
    public const double SilentImpactSpeed = 1.5;

    /// <summary>
    /// Volume (dB) of a body hitting something, from its momentum m·v on a log scale: a 2 kg ball at 3 m/s ≈ −21 dB,
    /// a 10 kg box at 5 m/s ≈ −9 dB, capped at 0 dB; null (no sound) below <see cref="SilentImpactSpeed"/>.
    /// </summary>
    public static double? ImpactVolumeDb(double massKg, double speed) =>
        speed < SilentImpactSpeed ? null : Math.Clamp(-31.1 + 13 * Math.Log10(Math.Max(massKg * speed, 1e-6)), -30, 0);

    /// <summary>Pitch of an impact: 1 for a 10 kg box, lower when heavier, higher when lighter.</summary>
    public static double ImpactPitch(double massKg) => Math.Clamp(Math.Pow(10 / Math.Max(massKg, 1e-6), 0.25), MinPitch, MaxPitch);

    /// <summary>What the throw bar sounds between two steps: a tick at each 10 %, a ring once when it fills.</summary>
    public static ChargeSound ChargeStep(double before, double after)
    {
        if (before < 1 && after >= 1) return ChargeSound.Full;
        return Math.Floor(after * 10) > Math.Floor(before * 10) ? ChargeSound.Tick : ChargeSound.None;
    }

    /// <summary>The tick rises an octave from the empty bar (1) to the full one (2).</summary>
    public static double TickPitch(double fraction) => 1 + Math.Clamp(fraction, 0, 1);

    /// <summary>The throw whoosh: higher the faster the body leaves the hand.</summary>
    public static double ThrowPitch(double speed) => Math.Clamp(0.6 + speed / 15 * 0.8, MinPitch, MaxPitch);
}

public enum ChargeSound { None, Tick, Full }
