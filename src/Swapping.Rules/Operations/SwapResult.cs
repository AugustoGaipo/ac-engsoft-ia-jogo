namespace Swapping.Rules.Operations;

/// <summary>Outcome of an operation: the body updates the game must apply.</summary>
public sealed class SwapResult
{
    private SwapResult(bool isApplied, IReadOnlyList<BodyUpdate> updates)
    {
        IsApplied = isApplied;
        Updates = updates;
    }

    public bool IsApplied { get; }
    public IReadOnlyList<BodyUpdate> Updates { get; }

    public static SwapResult Applied(IReadOnlyList<BodyUpdate> updates) => new(true, updates);
}
