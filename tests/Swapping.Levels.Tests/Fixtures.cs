using System.Text.Json.Nodes;
using Swapping.Levels.Model;

namespace Swapping.Levels.Tests;

/// <summary>The example level of the spec: valid, used as the base that each test breaks in one place.</summary>
public static class Fixtures
{
    public const string ExampleJson = """
    {
      "schemaVersion": 1,
      "id": "level-3",
      "number": 3,
      "name": "Peso",
      "scene": "res://levels/level_3.tscn",
      "rules": {
        "unlockedProperties": ["mass"],
        "rangeMeters": { "mass": 8.0 },
        "cooldownSeconds": 1.0,
        "allowObjectToObject": false,
        "allowSimultaneousSwaps": false
      },
      "rooms": [
        {
          "id": "3.1",
          "teaches": "Botão que exige mais peso que o jogador",
          "bounds": { "min": [0, 0, 0], "max": [12, 6, 10] },
          "spawn": { "position": [1, 0, 5], "yawDegrees": 90 },
          "exit": { "min": [11.5, 0, 4], "max": [12, 3, 6] },
          "rulesOverride": null,
          "objects": [
            { "id": "crate", "kind": "box", "position": [6, 0.5, 2], "size": [1, 1, 1],
              "massKg": 300, "accepts": null, "locked": false,
              "velocity": [0, 0, 0], "bounce": 0.1 }
          ],
          "buttons": [
            { "id": "heavy-button", "position": [6, 0, 7], "size": [1.5, 0.1, 1.5],
              "requiredMassKg": 150, "detects": "any" }
          ],
          "doors": [
            { "id": "door", "box": { "min": [10, 0, 4], "max": [10.3, 3, 6] }, "opensWith": ["heavy-button"] }
          ],
          "barriers": [
            { "id": "exit-grid", "preset": "exitGrid", "box": { "min": [11.5, 0, 4], "max": [11.6, 3, 6] } }
          ],
          "killZones": []
        }
      ]
    }
    """;

    /// <summary>The example as an editable JSON tree.</summary>
    public static JsonObject ExampleNode() => (JsonObject)JsonNode.Parse(ExampleJson)!;

    /// <summary>The example's first room as an editable JSON tree inside <paramref name="level"/>.</summary>
    public static JsonObject Room(JsonObject level, int index = 0) => (JsonObject)level["rooms"]![index]!;

    /// <summary>Parses JSON that the test expects to be well formed.</summary>
    public static LevelFile Parse(JsonNode node)
    {
        var (level, errors) = LevelParser.Parse(node.ToJsonString());
        Assert.Empty(errors);
        return level!;
    }

    public static LevelFile Example() => Parse(ExampleNode());
}

/// <summary>Helpers to grow the example without creating spatial problems.</summary>
public static class FixtureEdits
{
    /// <summary>
    /// Appends a copy of room 0, moved <paramref name="dx"/> meters along X, with the next room id and
    /// every piece id suffixed by <paramref name="suffix"/>.
    /// </summary>
    public static JsonObject AddShiftedRoom(JsonObject level, double dx = 20, string suffix = "-b")
    {
        var rooms = (JsonArray)level["rooms"]!;
        var copy = (JsonObject)rooms[0]!.DeepClone();
        Shift(copy, dx);
        foreach (var list in new[] { "objects", "buttons", "doors", "barriers", "killZones" })
            foreach (var piece in (JsonArray?)copy[list] ?? new JsonArray())
                piece!["id"] = piece["id"]!.GetValue<string>() + suffix;
        foreach (var door in (JsonArray?)copy["doors"] ?? new JsonArray())
            door!["opensWith"] = new JsonArray(((JsonArray)door["opensWith"]!)
                .Select(b => (JsonNode)JsonValue.Create(b!.GetValue<string>() + suffix)!).ToArray());

        var number = level["number"]!.GetValue<int>();
        copy["id"] = $"{number}.{rooms.Count + 1}";
        rooms.Add(copy);
        return copy;
    }

    private static void Shift(JsonNode node, double dx)
    {
        if (node is JsonObject obj)
        {
            foreach (var (name, value) in obj.ToList())
            {
                if (name is "min" or "max" or "position" && value is JsonArray v)
                    v[0] = v[0]!.GetValue<double>() + dx;
                else if (value is not null)
                    Shift(value, dx);
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
                if (item is not null)
                    Shift(item, dx);
        }
    }
}
