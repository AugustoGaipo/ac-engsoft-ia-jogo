namespace Swapping.Rules.Policies;

/// <summary>
/// Extension seam: changes the level's cooldown (future: power-up, "glorious weight",
/// time-limited downgrade).
/// </summary>
public interface ICooldownModifier
{
    double Modify(double cooldownSeconds);
}

/// <summary>The only modifier in this version: keeps the level's cooldown.</summary>
public sealed class NoCooldownModifier : ICooldownModifier
{
    public double Modify(double cooldownSeconds) => cooldownSeconds;
}
