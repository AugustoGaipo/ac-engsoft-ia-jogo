namespace Swapping.Rules.Operations;

/// <summary>Outcome of an operation: the body updates to apply, or the reason it was refused.</summary>
public sealed class SwapResult
{
    private SwapResult(RejectReason? reason, IReadOnlyList<BodyUpdate> updates)
    {
        Reason = reason;
        Updates = updates;
    }

    public bool IsApplied => Reason is null;
    public RejectReason? Reason { get; }
    public IReadOnlyList<BodyUpdate> Updates { get; }

    public static SwapResult Applied(IReadOnlyList<BodyUpdate> updates) => new(null, updates);

    public static SwapResult Rejected(RejectReason reason) => new(reason, Array.Empty<BodyUpdate>());
}
