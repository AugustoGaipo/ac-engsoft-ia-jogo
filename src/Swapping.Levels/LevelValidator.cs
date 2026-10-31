using System.Globalization;
using Swapping.Levels.Model;
using Swapping.Rules.Model;

namespace Swapping.Levels;

/// <summary>
/// Checks the meaning of a parsed level and returns every problem found (never throws on bad data).
/// Value checks look at one field or list at a time; spatial checks compare pieces with each other.
/// </summary>
public static partial class LevelValidator
{
    /// <summary>Lightest and densest density allowed when an object is written (world principle).</summary>
    public const double MinDensityKgM3 = 1.0;
    public const double MaxDensityKgM3 = 22_600.0;

    /// <summary>The id the player always has in a <c>World</c>; no piece may use it.</summary>
    public const string PlayerId = "player";

    public static IReadOnlyList<LevelError> Validate(LevelFile level)
    {
        var errors = new List<LevelError>();
        CheckValues(level, errors);
        CheckSpatial(level, errors);
        return errors;
    }

    private static string Inv(int i) => i.ToString(CultureInfo.InvariantCulture);

    /// <summary>The unlocked properties in force in a room (its override, or the level's).</summary>
    internal static IReadOnlyList<string> UnlockedIn(LevelFile level, RoomDef room) =>
        room.RulesOverride?.UnlockedProperties ?? level.Rules.UnlockedProperties;

    private static void CheckValues(LevelFile level, List<LevelError> errors)
    {
        void Add(string path, LevelErrorCode code, string message) => errors.Add(new LevelError(path, code, message));

        if (level.SchemaVersion != LevelFile.CurrentSchemaVersion)
            Add("schemaVersion", LevelErrorCode.UnsupportedSchema,
                $"Schema version {level.SchemaVersion} is not supported (expected {LevelFile.CurrentSchemaVersion}).");

        if (level.Number is < 1 or > 11)
            Add("number", LevelErrorCode.NumberOutOfRange, $"Level number {level.Number} is not in 1–11.");

        if (level.Id != $"level-{Inv(level.Number)}")
            Add("id", LevelErrorCode.BadId, $"Level id '{level.Id}' must be 'level-{Inv(level.Number)}'.");

        CheckProperties(level.Rules.UnlockedProperties, "rules.unlockedProperties", Add);
        CheckRanges(level.Rules.RangeMeters, "rules.rangeMeters", Add);
        CheckCooldown(level.Rules.CooldownSeconds, "rules.cooldownSeconds", Add);

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        for (var r = 0; r < level.Rooms.Count; r++)
        {
            var room = level.Rooms[r];
            var path = $"rooms[{Inv(r)}]";

            var expectedId = $"{Inv(level.Number)}.{Inv(r + 1)}";
            if (room.Id != expectedId)
                Add($"{path}.id", LevelErrorCode.BadId, $"Room id '{room.Id}' must be '{expectedId}' (rooms are numbered in order).");

            CheckBox(room.Bounds, $"{path}.bounds", Add);
            CheckBox(room.Exit, $"{path}.exit", Add);

            if (room.RulesOverride is { } over)
            {
                if (over.UnlockedProperties is { } unlocked)
                    CheckProperties(unlocked, $"{path}.rulesOverride.unlockedProperties", Add);
                if (over.RangeMeters is { } ranges)
                    CheckRanges(ranges, $"{path}.rulesOverride.rangeMeters", Add);
                if (over.CooldownSeconds is { } cooldown)
                    CheckCooldown(cooldown, $"{path}.rulesOverride.cooldownSeconds", Add);
            }

            void CheckId(string id, string idPath)
            {
                if (id == PlayerId)
                    Add(idPath, LevelErrorCode.DuplicateId, $"'{PlayerId}' is reserved for the player.");
                else if (!seenIds.Add(id))
                    Add(idPath, LevelErrorCode.DuplicateId, $"Id '{id}' is already used in this level.");
            }

            var unlockedHere = UnlockedIn(level, room);
            for (var i = 0; i < room.Objects.Count; i++)
            {
                var obj = room.Objects[i];
                var p = $"{path}.objects[{Inv(i)}]";
                CheckId(obj.Id, $"{p}.id");
                CheckObject(obj, p, unlockedHere, Add);
            }

            for (var i = 0; i < room.Buttons.Count; i++)
            {
                var button = room.Buttons[i];
                var p = $"{path}.buttons[{Inv(i)}]";
                CheckId(button.Id, $"{p}.id");
                if (!IsPositive(button.Size))
                    Add($"{p}.size", LevelErrorCode.NotPositive, "Every edge of a button must be > 0 m.");
                if (button.RequiredMassKg <= 0)
                    Add($"{p}.requiredMassKg", LevelErrorCode.NotPositive, "The required mass must be > 0 kg.");
            }

            for (var i = 0; i < room.Doors.Count; i++)
            {
                var p = $"{path}.doors[{Inv(i)}]";
                CheckId(room.Doors[i].Id, $"{p}.id");
                CheckBox(room.Doors[i].Box, $"{p}.box", Add);
            }

            for (var i = 0; i < room.Barriers.Count; i++)
            {
                var barrier = room.Barriers[i];
                var p = $"{path}.barriers[{Inv(i)}]";
                CheckId(barrier.Id, $"{p}.id");
                CheckBox(barrier.Box, $"{p}.box", Add);
                if (barrier.Filter == default)
                    Add(p, LevelErrorCode.EmptyBarrier, "A barrier must block objects, the player or variables.");
            }

            for (var i = 0; i < room.KillZones.Count; i++)
            {
                var p = $"{path}.killZones[{Inv(i)}]";
                CheckId(room.KillZones[i].Id, $"{p}.id");
                CheckBox(room.KillZones[i].Box, $"{p}.box", Add);
            }
        }
    }

    private static void CheckObject(ObjectDef obj, string p, IReadOnlyList<string> unlocked, Action<string, LevelErrorCode, string> add)
    {
        var sizeOk = IsPositive(obj.Size);
        if (!sizeOk)
            add($"{p}.size", LevelErrorCode.NotPositive, "Every edge of an object must be > 0 m.");
        if (obj.MassKg <= 0)
            add($"{p}.massKg", LevelErrorCode.NotPositive, "Mass must be > 0 kg.");
        else if (sizeOk && obj.DensityKgM3 is < MinDensityKgM3 or > MaxDensityKgM3)
            add($"{p}.massKg", LevelErrorCode.ImplausibleDensity,
                $"Density {obj.DensityKgM3:0.###} kg/m³ is outside {MinDensityKgM3}–{MaxDensityKgM3} kg/m³.");

        if (obj.Bounce is < 0 or > 1)
            add($"{p}.bounce", LevelErrorCode.OutOfRange01, "Bounce must be between 0 and 1.");

        if (obj.Accepts is not { } accepts)
            return;
        if (accepts.Count == 0)
        {
            add($"{p}.accepts", LevelErrorCode.NotAccepted,
                "An empty list accepts nothing; use null (every unlocked property) or lock the object.");
            return;
        }

        for (var j = 0; j < accepts.Count; j++)
        {
            var name = accepts[j];
            var path = $"{p}.accepts[{Inv(j)}]";
            if (!PropertyNames.All.Contains(name))
                add(path, LevelErrorCode.UnknownProperty, $"'{name}' is not a property ({string.Join(", ", PropertyNames.All)}).");
            else if (!unlocked.Contains(name))
                add(path, LevelErrorCode.NotAccepted, $"'{name}' is not unlocked in this room.");
        }
    }

    private static void CheckProperties(IReadOnlyList<string> names, string path, Action<string, LevelErrorCode, string> add)
    {
        for (var i = 0; i < names.Count; i++)
            if (!PropertyNames.All.Contains(names[i]))
                add($"{path}[{Inv(i)}]", LevelErrorCode.UnknownProperty,
                    $"'{names[i]}' is not a property ({string.Join(", ", PropertyNames.All)}).");
    }

    private static void CheckRanges(IReadOnlyDictionary<string, double> ranges, string path, Action<string, LevelErrorCode, string> add)
    {
        foreach (var (name, meters) in ranges)
        {
            if (!PropertyNames.All.Contains(name))
                add($"{path}.{name}", LevelErrorCode.UnknownProperty, $"'{name}' is not a property.");
            else if (meters <= 0)
                add($"{path}.{name}", LevelErrorCode.NotPositive, "A range must be > 0 m.");
        }
    }

    private static void CheckCooldown(double seconds, string path, Action<string, LevelErrorCode, string> add)
    {
        if (seconds < 0)
            add(path, LevelErrorCode.NotPositive, "The cooldown must be ≥ 0 s.");
    }

    private static void CheckBox(Box box, string path, Action<string, LevelErrorCode, string> add)
    {
        if (box.IsEmpty)
            add(path, LevelErrorCode.EmptyBox, "Min must be below max on every axis.");
    }

    private static bool IsPositive(Vec3 v) => v.X > 0 && v.Y > 0 && v.Z > 0;
}
