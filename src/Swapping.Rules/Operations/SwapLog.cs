using Swapping.Rules.Model;

namespace Swapping.Rules.Operations;

/// <summary>One applied swap: who, which properties and when (s).</summary>
public sealed record SwapLogEntry(string First, string Second, IReadOnlyList<IPropertyKind> Properties, double AtSeconds);

/// <summary>
/// Records applied swaps in order (ranking move count; future ghost/replay). Restorations are not
/// swaps and are never recorded.
/// </summary>
public sealed class SwapLog
{
    private readonly List<SwapLogEntry> _entries = new();

    public IReadOnlyList<SwapLogEntry> Entries => _entries;

    /// <summary>Moves for the ranking: one per applied swap, a simultaneous swap included.</summary>
    public int MoveCount => _entries.Count;

    public void Record(SwapRequest request, double now) =>
        _entries.Add(new SwapLogEntry(request.First, request.Second, request.Properties.ToArray(), now));
}
