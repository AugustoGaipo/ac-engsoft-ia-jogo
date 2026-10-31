namespace Swapping.Rules.Policies;

/// <summary>Remembers when the last swap (any property) was applied. Time is passed in (s).</summary>
public sealed class Cooldown
{
    private const double Tolerance = 1e-9;

    public double? LastSwapAt { get; private set; }

    public bool IsReady(double now, double cooldownSeconds) =>
        LastSwapAt is not { } last || now - last >= cooldownSeconds - Tolerance;

    public void Record(double now) => LastSwapAt = now;
}
