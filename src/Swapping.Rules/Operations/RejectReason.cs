namespace Swapping.Rules.Operations;

/// <summary>Why a swap request was refused (see the legality table of the spec).</summary>
public enum RejectReason
{
    SameContainer,
    ModeNotEnabled,
    PropertyNotUnlocked,
    PropertyNotAccepted,
    Locked,
    OutOfRange,
    NoLineOfSight,
    BlockedByBarrier,
    InsideVariableBarrier,
    OnCooldown,
    TooManyProperties,
}
