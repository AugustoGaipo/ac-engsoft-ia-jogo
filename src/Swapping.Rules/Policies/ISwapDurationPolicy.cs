using Swapping.Rules.Operations;

namespace Swapping.Rules.Policies;

/// <summary>
/// Extension seam: how long a swap lasts (future: unstable objects that revert after X s).
/// Null means the swap is permanent.
/// </summary>
public interface ISwapDurationPolicy
{
    double? RevertAfterSeconds(SwapRequest request);
}

/// <summary>The only policy in this version: every swap is permanent.</summary>
public sealed class PermanentSwapDuration : ISwapDurationPolicy
{
    public double? RevertAfterSeconds(SwapRequest request) => null;
}
