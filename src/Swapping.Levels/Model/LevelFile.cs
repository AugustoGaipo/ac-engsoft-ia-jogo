namespace Swapping.Levels.Model;

/// <summary>Property names used in level files.</summary>
public static class PropertyNames
{
    public const string Position = "position";
    public const string Mass = "mass";
    public const string KineticEnergy = "kineticEnergy";

    public static readonly IReadOnlyList<string> All = new[] { Position, Mass, KineticEnergy };
}

/// <summary>Swap rules of a level, as written in the file.</summary>
public sealed record LevelRulesDef(
    IReadOnlyList<string> UnlockedProperties,
    IReadOnlyDictionary<string, double> RangeMeters,
    double CooldownSeconds,
    bool AllowObjectToObject,
    bool AllowSimultaneousSwaps);

/// <summary>Room-only replacement of some level rules; a null field keeps the level's value.</summary>
public sealed record RulesOverrideDef(
    IReadOnlyList<string>? UnlockedProperties = null,
    IReadOnlyDictionary<string, double>? RangeMeters = null,
    double? CooldownSeconds = null,
    bool? AllowObjectToObject = null,
    bool? AllowSimultaneousSwaps = null);

public sealed record RoomDef(
    string Id,
    string Teaches,
    Box Bounds,
    SpawnDef Spawn,
    Box Exit,
    RulesOverrideDef? RulesOverride,
    IReadOnlyList<ObjectDef> Objects,
    IReadOnlyList<ButtonDef> Buttons,
    IReadOnlyList<DoorDef> Doors,
    IReadOnlyList<BarrierDef> Barriers,
    IReadOnlyList<KillZoneDef> KillZones);

/// <summary>One level file (schema version 1).</summary>
public sealed record LevelFile(
    int SchemaVersion,
    string Id,
    int Number,
    string Name,
    string Scene,
    LevelRulesDef Rules,
    IReadOnlyList<RoomDef> Rooms)
{
    public const int CurrentSchemaVersion = 1;

    /// <summary>The player of every level (rules spec).</summary>
    public const double PlayerMassKg = 70.0;
    public const double PlayerVolumeM3 = 0.07;
}
