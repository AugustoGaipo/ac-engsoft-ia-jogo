using Swapping.Rules.Model;
using Swapping.Rules.Operations;

namespace Swapping.Rules.Policies;

/// <summary>What a level allows: unlocked properties, range per property, cooldown and mode flags.</summary>
public sealed record LevelRules
{
    public const double DefaultRangeMeters = 8.0;
    public const double DefaultCooldownSeconds = 1.0;

    public static readonly LevelRules Default = new();

    public IReadOnlySet<IPropertyKind> UnlockedProperties { get; init; } = new HashSet<IPropertyKind>(PropertyKind.All);

    /// <summary>Range per property; a property not listed uses <see cref="DefaultRangeMeters"/>.</summary>
    public IReadOnlyDictionary<IPropertyKind, double> RangeMeters { get; init; } = new Dictionary<IPropertyKind, double>();

    public double CooldownSeconds { get; init; } = DefaultCooldownSeconds;

    /// <summary>Swap mode flag: object ↔ object (off means player ↔ object only).</summary>
    public bool AllowObjectToObject { get; init; }

    /// <summary>Multi-property flag: more than one property in one swap.</summary>
    public bool AllowSimultaneousSwaps { get; init; }

    public double RangeFor(IPropertyKind kind) =>
        RangeMeters.TryGetValue(kind, out var range) ? range : DefaultRangeMeters;

    /// <summary>The first reason (in the spec's table order) the request is illegal, or null.</summary>
    public RejectReason? Check(World world, SwapRequest request, double now)
    {
        var first = world.Get(request.First);
        var second = world.Get(request.Second);
        var context = request.Context;

        if (first.Id == second.Id) return RejectReason.SameContainer;
        if (!AllowObjectToObject && first.Type == ContainerType.Object && second.Type == ContainerType.Object)
            return RejectReason.ModeNotEnabled;
        if (request.Properties.Any(k => !UnlockedProperties.Contains(k))) return RejectReason.PropertyNotUnlocked;
        if (request.Properties.Any(k => !first.Accepts(k) || !second.Accepts(k))) return RejectReason.PropertyNotAccepted;
        if (first.Locked || second.Locked) return RejectReason.Locked;
        if (request.Properties.Any(k => context.DistanceMeters > RangeFor(k))) return RejectReason.OutOfRange;
        if (!context.HasLineOfSight) return RejectReason.NoLineOfSight;
        if (context.VariableBarrierBetween) return RejectReason.BlockedByBarrier;
        if (context.FirstInsideVariableBarrier || context.SecondInsideVariableBarrier)
            return RejectReason.InsideVariableBarrier;
        if (!world.Cooldown.IsReady(now, CooldownSeconds)) return RejectReason.OnCooldown;
        if (request.Properties.Count > 1 && !AllowSimultaneousSwaps) return RejectReason.TooManyProperties;
        return null;
    }
}
