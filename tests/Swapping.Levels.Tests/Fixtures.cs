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
