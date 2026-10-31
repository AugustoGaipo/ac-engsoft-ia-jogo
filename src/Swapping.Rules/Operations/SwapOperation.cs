namespace Swapping.Rules.Operations;

/// <summary>Exchanges the variables of the requested properties between two containers.</summary>
public sealed class SwapOperation : IOperation
{
    public SwapResult Apply(World world, SwapRequest request, double now)
    {
        foreach (var kind in request.Properties)
            world.ExchangeVariables(request.First, request.Second, kind);

        return SwapResult.Applied(world.BodyUpdatesFor(request.First, request.Second));
    }
}
