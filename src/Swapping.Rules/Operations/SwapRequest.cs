using Swapping.Rules.Model;

namespace Swapping.Rules.Operations;

/// <summary>Ask to swap one or more properties between two containers.</summary>
public sealed record SwapRequest(string First, string Second, IReadOnlyList<IPropertyKind> Properties)
{
    public SwapRequest(string first, string second, IPropertyKind property)
        : this(first, second, new[] { property })
    {
    }

    /// <summary>What the game measured for this request.</summary>
    public SwapContext Context { get; init; } = SwapContext.Clear;
}
