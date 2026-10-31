namespace Swapping.Rules.Operations;

/// <summary>Extension seam: something done between two containers (swap now; transfer later).</summary>
public interface IOperation
{
    SwapResult Apply(World world, SwapRequest request, double now);
}
