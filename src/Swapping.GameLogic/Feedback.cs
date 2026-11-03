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
}
