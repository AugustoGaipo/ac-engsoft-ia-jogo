using System.Text.Json;
using System.Text.Json.Nodes;

namespace Swapping.Api.Tests.Progress;

/// <summary>
/// A small catalog for the progress tests, built in a temporary folder from the shipped level files
/// renumbered and renamed: <c>level-1</c> (4 rooms), <c>level-2</c> (3 rooms) and <c>level-4</c> (2 rooms, made
/// from level 3).
/// The gap 2 → 4 checks that unlocking follows the catalog, not the numbers. Changing the shipped
/// levels' content does not break these tests (only the room counts are used).
/// </summary>
public sealed class TestCatalog : IDisposable
{
    public string Directory { get; } = Path.Combine(Path.GetTempPath(), $"swapping-catalog-{Guid.NewGuid():N}");

    public TestCatalog()
    {
        System.IO.Directory.CreateDirectory(Directory);
        var source = Path.Combine(RepoPaths.Root, "levels");
        Write(source, "level_1.json", 1, "Test one");
        Write(source, "level_2.json", 2, "Test two");
        Write(source, "level_3.json", 4, "Test four");
        File.WriteAllText(Path.Combine(Directory, "catalog.json"), """
            [
              { "id": "level-1", "file": "t_1.json" },
              { "id": "level-2", "file": "t_2.json" },
              { "id": "level-4", "file": "t_4.json" }
            ]
            """);
    }

    public void Break() => File.WriteAllText(Path.Combine(Directory, "t_2.json"), "{ not json");

    private void Write(string source, string file, int number, string name)
    {
        var level = JsonNode.Parse(File.ReadAllText(Path.Combine(source, file)), documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true })!.AsObject();
        level["id"] = $"level-{number}";
        level["number"] = number;
        level["name"] = name;
        var rooms = level["rooms"]!.AsArray();
        for (var i = 0; i < rooms.Count; i++)
            rooms[i]!["id"] = $"{number}.{i + 1}";
        File.WriteAllText(Path.Combine(Directory, $"t_{number}.json"), level.ToJsonString());
    }

    public void Dispose()
    {
        try
        {
            System.IO.Directory.Delete(Directory, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temp folder is harmless.
        }
    }
}
