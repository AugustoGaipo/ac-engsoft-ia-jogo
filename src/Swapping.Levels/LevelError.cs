namespace Swapping.Levels;

/// <summary>Every problem the parser, the validator or the catalog can report (spec table).</summary>
public enum LevelErrorCode
{
    InvalidJson,
    UnsupportedSchema,
    BadId,
    DuplicateId,
    NumberOutOfRange,
    UnknownProperty,
    NotPositive,
    OutOfRange01,
    ImplausibleDensity,
    EmptyBox,
    OutsideRoom,
    RoomsOverlap,
    UnknownReference,
    DoorWithoutButton,
    UnusedButton,
    EmptyBarrier,
    MissingExitGrid,
    Overlap,
    SpawnUnsafe,
    NotAccepted,
    CatalogOrder,
}

/// <summary>A problem in a level file. <see cref="Path"/> points at it, e.g. <c>rooms[1].objects[0].massKg</c>.</summary>
public sealed record LevelError(string Path, LevelErrorCode Code, string Message)
{
    public override string ToString() => $"{Path}: {Code} — {Message}";
}
