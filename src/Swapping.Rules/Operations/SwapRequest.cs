using Swapping.Rules.Model;

namespace Swapping.Rules.Operations;

/// <summary>
/// Ask to swap one or more properties between two containers. The property list must be non-empty and
/// without repeats (a programming error otherwise: an empty or repeated list would count a move while
/// changing nothing).
/// </summary>
public sealed record SwapRequest(string First, string Second, IReadOnlyList<IPropertyKind> Properties)
{
    private readonly IReadOnlyList<IPropertyKind> _properties = Validated(Properties);

    public SwapRequest(string first, string second, IPropertyKind property)
        : this(first, second, new[] { property })
    {
    }

    public IReadOnlyList<IPropertyKind> Properties
    {
        get => _properties;
        init => _properties = Validated(value);
    }

    /// <summary>What the game measured for this request.</summary>
    public SwapContext Context { get; init; } = SwapContext.Clear;

    private static IReadOnlyList<IPropertyKind> Validated(IReadOnlyList<IPropertyKind> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        if (properties.Count == 0)
            throw new ArgumentException("A swap needs at least one property.", nameof(properties));
        if (properties.Distinct().Count() != properties.Count)
            throw new ArgumentException("A property can appear only once in a swap.", nameof(properties));
        return properties;
    }
}
