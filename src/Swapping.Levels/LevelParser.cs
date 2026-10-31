using System.Globalization;
using System.Text.Json;
using Swapping.Levels.Model;
using Swapping.Rules.Model;

namespace Swapping.Levels;

/// <summary>
/// Turns level-file text into the typed model. Only the <b>shape</b> is checked here (required fields,
/// types, 3-number vectors, known enum words); every meaning check is in <see cref="LevelValidator"/>.
/// Bad data never throws: every problem becomes an <see cref="LevelErrorCode.InvalidJson"/> error.
/// </summary>
public static class LevelParser
{
    private static readonly JsonDocumentOptions Options = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static (LevelFile? Level, IReadOnlyList<LevelError> Errors) Parse(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, Options);
        }
        catch (JsonException e)
        {
            return (null, new[] { new LevelError("", LevelErrorCode.InvalidJson, e.Message) });
        }

        using (document)
        {
            var reader = new Reader();
            var level = reader.Level(document.RootElement);
            return reader.Errors.Count == 0 ? (level, reader.Errors) : (null, reader.Errors);
        }
    }

    /// <summary>Reads elements and collects shape errors with the JSON path of each one.</summary>
    private sealed class Reader
    {
        public List<LevelError> Errors { get; } = new();

        private void Fail(string path, string message) =>
            Errors.Add(new LevelError(path, LevelErrorCode.InvalidJson, message));

        private static string Join(string path, string name) => path.Length == 0 ? name : $"{path}.{name}";

        public LevelFile? Level(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Object)
            {
                Fail("", "A level file must be a JSON object.");
                return null;
            }

            return new LevelFile(
                Int(root, "schemaVersion", ""),
                Text(root, "id", ""),
                Int(root, "number", ""),
                Text(root, "name", ""),
                Text(root, "scene", ""),
                Rules(Required(root, "rules", "", JsonValueKind.Object), "rules"),
                Items(root, "rooms", "", Room, required: true));
        }

        private LevelRulesDef Rules(JsonElement? e, string path)
        {
            if (e is not { } rules)
                return new LevelRulesDef(Array.Empty<string>(), new Dictionary<string, double>(), 0, false, false);

            return new LevelRulesDef(
                Strings(rules, "unlockedProperties", path, required: true) ?? Array.Empty<string>(),
                Ranges(rules, path) ?? new Dictionary<string, double>(),
                Number(rules, "cooldownSeconds", path),
                Bool(rules, "allowObjectToObject", path) ?? false,
                Bool(rules, "allowSimultaneousSwaps", path) ?? false);
        }

        private RulesOverrideDef? Override(JsonElement room, string path)
        {
            if (Optional(room, "rulesOverride", path, JsonValueKind.Object) is not { } o)
                return null;

            var p = Join(path, "rulesOverride");
            return new RulesOverrideDef(
                Strings(o, "unlockedProperties", p, required: false),
                Ranges(o, p),
                OptionalNumber(o, "cooldownSeconds", p),
                Bool(o, "allowObjectToObject", p),
                Bool(o, "allowSimultaneousSwaps", p));
        }

        private RoomDef Room(JsonElement room, string path)
        {
            var spawnPath = Join(path, "spawn");
            var spawn = Required(room, "spawn", path, JsonValueKind.Object);
            return new RoomDef(
                Text(room, "id", path),
                Text(room, "teaches", path),
                Box(room, "bounds", path),
                spawn is { } s
                    ? new SpawnDef(Vector(s, "position", spawnPath), Number(s, "yawDegrees", spawnPath))
                    : new SpawnDef(Vec3.Zero, 0),
                Box(room, "exit", path),
                Override(room, path),
                Items(room, "objects", path, Obj, required: false),
                Items(room, "buttons", path, Button, required: false),
                Items(room, "doors", path, Door, required: false),
                Items(room, "barriers", path, Barrier, required: false),
                Items(room, "killZones", path, KillZone, required: false));
        }

        private ObjectDef Obj(JsonElement o, string path) => new(
            Text(o, "id", path),
            Word(o, "kind", path, new Dictionary<string, ObjectKind> { ["box"] = ObjectKind.Box, ["ball"] = ObjectKind.Ball }),
            Vector(o, "position", path),
            Vector(o, "size", path),
            Number(o, "massKg", path),
            Strings(o, "accepts", path, required: false),
            Bool(o, "locked", path) ?? false,
            OptionalVector(o, "velocity", path) ?? Vec3.Zero,
            OptionalNumber(o, "bounce", path) ?? 0);

        private ButtonDef Button(JsonElement b, string path) => new(
            Text(b, "id", path),
            Vector(b, "position", path),
            Vector(b, "size", path),
            Number(b, "requiredMassKg", path),
            Word(b, "detects", path, new Dictionary<string, ButtonDetects> { ["any"] = ButtonDetects.Any, ["objects"] = ButtonDetects.Objects }));

        private DoorDef Door(JsonElement d, string path) => new(
            Text(d, "id", path),
            Box(d, "box", path),
            Strings(d, "opensWith", path, required: true) ?? Array.Empty<string>());

        private BarrierDef Barrier(JsonElement b, string path)
        {
            BarrierPreset? preset = null;
            if (Optional(b, "preset", path, JsonValueKind.String) is not null)
                preset = Word(b, "preset", path, new Dictionary<string, BarrierPreset> { ["exitGrid"] = BarrierPreset.ExitGrid });

            return new BarrierDef(
                Text(b, "id", path),
                preset,
                Bool(b, "blocksObjects", path) ?? false,
                Bool(b, "blocksPlayer", path) ?? false,
                Bool(b, "blocksVariables", path) ?? false,
                Box(b, "box", path));
        }

        private KillZoneDef KillZone(JsonElement k, string path) => new(Text(k, "id", path), Box(k, "box", path));

        // ---- primitives ----

        private JsonElement? Required(JsonElement obj, string name, string path, JsonValueKind kind)
        {
            var p = Join(path, name);
            if (!obj.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
            {
                Fail(p, $"Required field '{name}' is missing.");
                return null;
            }

            if (value.ValueKind != kind)
            {
                Fail(p, $"'{name}' must be {Describe(kind)}.");
                return null;
            }

            return value;
        }

        private JsonElement? Optional(JsonElement obj, string name, string path, JsonValueKind kind)
        {
            if (!obj.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
                return null;
            return Required(obj, name, path, kind);
        }

        private static string Describe(JsonValueKind kind) => kind switch
        {
            JsonValueKind.Object => "an object",
            JsonValueKind.Array => "an array",
            JsonValueKind.String => "a string",
            JsonValueKind.Number => "a number",
            _ => kind.ToString(),
        };

        private string Text(JsonElement obj, string name, string path) =>
            Required(obj, name, path, JsonValueKind.String)?.GetString() ?? "";

        private double Number(JsonElement obj, string name, string path) =>
            Required(obj, name, path, JsonValueKind.Number)?.GetDouble() ?? 0;

        private double? OptionalNumber(JsonElement obj, string name, string path) =>
            Optional(obj, name, path, JsonValueKind.Number)?.GetDouble();

        private int Int(JsonElement obj, string name, string path)
        {
            if (Required(obj, name, path, JsonValueKind.Number) is not { } value)
                return 0;
            if (value.TryGetInt32(out var i))
                return i;
            Fail(Join(path, name), $"'{name}' must be a whole number.");
            return 0;
        }

        private bool? Bool(JsonElement obj, string name, string path)
        {
            if (!obj.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
                return null;
            if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                return value.GetBoolean();
            Fail(Join(path, name), $"'{name}' must be true or false.");
            return null;
        }

        private T Word<T>(JsonElement obj, string name, string path, IReadOnlyDictionary<string, T> words)
            where T : struct
        {
            if (Required(obj, name, path, JsonValueKind.String) is not { } value)
                return default;
            var word = value.GetString()!;
            if (words.TryGetValue(word, out var result))
                return result;
            Fail(Join(path, name), $"'{word}' is not one of: {string.Join(", ", words.Keys)}.");
            return default;
        }

        private Vec3 Vector(JsonElement obj, string name, string path) =>
            Required(obj, name, path, JsonValueKind.Array) is { } value ? ToVector(value, Join(path, name)) : Vec3.Zero;

        private Vec3? OptionalVector(JsonElement obj, string name, string path) =>
            Optional(obj, name, path, JsonValueKind.Array) is { } value ? ToVector(value, Join(path, name)) : null;

        private Vec3 ToVector(JsonElement array, string path)
        {
            if (array.GetArrayLength() != 3 || array.EnumerateArray().Any(n => n.ValueKind != JsonValueKind.Number))
            {
                Fail(path, "A vector must be an array of 3 numbers [x, y, z].");
                return Vec3.Zero;
            }

            var n = array.EnumerateArray().Select(e => e.GetDouble()).ToArray();
            return new Vec3(n[0], n[1], n[2]);
        }

        private Box Box(JsonElement obj, string name, string path)
        {
            if (Required(obj, name, path, JsonValueKind.Object) is not { } value)
                return default;
            var p = Join(path, name);
            return new Box(Vector(value, "min", p), Vector(value, "max", p));
        }

        private IReadOnlyList<string>? Strings(JsonElement obj, string name, string path, bool required)
        {
            var value = required
                ? Required(obj, name, path, JsonValueKind.Array)
                : Optional(obj, name, path, JsonValueKind.Array);
            if (value is not { } array)
                return null;

            var result = new List<string>();
            var i = 0;
            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                    result.Add(item.GetString()!);
                else
                    Fail($"{Join(path, name)}[{i}]", "Must be a string.");
                i++;
            }

            return result;
        }

        private IReadOnlyDictionary<string, double>? Ranges(JsonElement obj, string path)
        {
            if (Optional(obj, "rangeMeters", path, JsonValueKind.Object) is not { } value)
                return null;

            var result = new Dictionary<string, double>();
            foreach (var property in value.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Number)
                    result[property.Name] = property.Value.GetDouble();
                else
                    Fail($"{Join(path, "rangeMeters")}.{property.Name}", "A range must be a number of meters.");
            }

            return result;
        }

        private IReadOnlyList<T> Items<T>(JsonElement obj, string name, string path, Func<JsonElement, string, T> read, bool required)
        {
            var value = required
                ? Required(obj, name, path, JsonValueKind.Array)
                : Optional(obj, name, path, JsonValueKind.Array);
            if (value is not { } array)
                return Array.Empty<T>();

            var result = new List<T>();
            var i = 0;
            foreach (var item in array.EnumerateArray())
            {
                var itemPath = $"{Join(path, name)}[{i.ToString(CultureInfo.InvariantCulture)}]";
                if (item.ValueKind == JsonValueKind.Object)
                    result.Add(read(item, itemPath));
                else
                    Fail(itemPath, "Must be an object.");
                i++;
            }

            return result;
        }
    }
}
