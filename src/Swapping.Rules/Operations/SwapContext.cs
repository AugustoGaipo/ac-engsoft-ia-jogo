namespace Swapping.Rules.Operations;

/// <summary>
/// Facts measured by the game (raycasts) when a swap is requested. The library only decides.
/// The defaults describe a clear swap: touching, in sight, no variable barrier involved.
/// </summary>
public sealed record SwapContext(
    double DistanceMeters = 0,
    bool HasLineOfSight = true,
    bool VariableBarrierBetween = false,
    bool FirstInsideVariableBarrier = false,
    bool SecondInsideVariableBarrier = false)
{
    public static readonly SwapContext Clear = new();
}
