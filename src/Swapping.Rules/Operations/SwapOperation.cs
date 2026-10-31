namespace Swapping.Rules.Operations;

/// <summary>Exchanges the variables of the requested properties between two containers.</summary>
public sealed class SwapOperation : IOperation
{
    public SwapResult Apply(World world, SwapRequest request, double now)
    {
        var rejection = world.Rules.Check(world, request, now);
        if (rejection is not null)
            return SwapResult.Rejected(rejection.Value);

        foreach (var kind in request.Properties)
            world.ExchangeVariables(request.First, request.Second, kind);

        world.Cooldown.Record(now);
        world.Log.Record(request, now);
        return SwapResult.Applied(world.BodyUpdatesFor(request.First, request.Second));
    }
}
