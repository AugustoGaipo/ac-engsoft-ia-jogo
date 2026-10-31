using System.Text.Json.Nodes;

namespace Swapping.Levels.Tests;

public sealed class CatalogTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "swapping-levels-" + Guid.NewGuid().ToString("N"));

    public CatalogTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private void WriteLevel(string file, int number)
    {
        var node = Fixtures.ExampleNode();
        node["number"] = number;
        node["id"] = $"level-{number}";
        Fixtures.Room(node)["id"] = $"{number}.1";
        File.WriteAllText(Path.Combine(_dir, file), node.ToJsonString());
    }

    private void WriteCatalog(params (string Id, string File)[] entries) =>
        File.WriteAllText(Path.Combine(_dir, LevelCatalog.FileName),
            new JsonArray(entries.Select(e => (JsonNode)new JsonObject { ["id"] = e.Id, ["file"] = e.File }).ToArray()).ToJsonString());

    [Fact]
    public void ValidCatalog_LoadsEveryLevelInOrder_GapsAllowed()
    {
        WriteLevel("level_1.json", 1);
        WriteLevel("level_3.json", 3);
        WriteCatalog(("level-1", "level_1.json"), ("level-3", "level_3.json"));

        var catalog = LevelCatalog.Load(_dir);

        Assert.Empty(catalog.Errors);
        Assert.Equal(new[] { "level-1", "level-3" }, catalog.Levels.Select(l => l.Id));
    }

    [Fact]
    public void CatalogOrder_NumbersNotIncreasing()
    {
        WriteLevel("level_1.json", 1);
        WriteLevel("level_3.json", 3);
        WriteCatalog(("level-3", "level_3.json"), ("level-1", "level_1.json"));

        var error = Assert.Single(LevelCatalog.Load(_dir).Errors);

        Assert.Equal(LevelErrorCode.CatalogOrder, error.Code);
        Assert.Equal("catalog[1]", error.Path);
    }

    [Fact]
    public void CatalogOrder_MissingFile()
    {
        WriteCatalog(("level-1", "level_1.json"));

        var error = Assert.Single(LevelCatalog.Load(_dir).Errors);

        Assert.Equal(LevelErrorCode.CatalogOrder, error.Code);
        Assert.Equal("catalog[0].file", error.Path);
    }

    [Fact]
    public void CatalogOrder_IdDoesNotMatchTheFile()
    {
        WriteLevel("level_1.json", 1);
        WriteCatalog(("level-2", "level_1.json"));

        var error = Assert.Single(LevelCatalog.Load(_dir).Errors);

        Assert.Equal(LevelErrorCode.CatalogOrder, error.Code);
        Assert.Equal("catalog[0].id", error.Path);
    }

    [Theory]
    [InlineData("../secret.json")]
    [InlineData("sub/level_1.json")]
    [InlineData("C:\\level_1.json")]
    public void CatalogOrder_FileMustBeAPlainNameInsideTheFolder(string file)
    {
        WriteCatalog(("level-1", file));

        var error = Assert.Single(LevelCatalog.Load(_dir).Errors);

        Assert.Equal(LevelErrorCode.CatalogOrder, error.Code);
        Assert.Equal("catalog[0].file", error.Path);
    }

    [Fact]
    public void InvalidLevelFile_ReportsItsErrorsWithTheFileName()
    {
        var node = Fixtures.ExampleNode();
        node["number"] = 1;
        node["id"] = "level-1";
        Fixtures.Room(node)["id"] = "1.1";
        Fixtures.Room(node)["objects"]![0]!["massKg"] = -1;
        File.WriteAllText(Path.Combine(_dir, "level_1.json"), node.ToJsonString());
        WriteCatalog(("level-1", "level_1.json"));

        var catalog = LevelCatalog.Load(_dir);

        var error = Assert.Single(catalog.Errors);
        Assert.Equal(LevelErrorCode.NotPositive, error.Code);
        Assert.Equal("level_1.json:rooms[0].objects[0].massKg", error.Path);
        Assert.Empty(catalog.Levels);
    }

    [Fact]
    public void MalformedCatalog_IsInvalidJson()
    {
        File.WriteAllText(Path.Combine(_dir, LevelCatalog.FileName), "{ \"levels\": ");

        var error = Assert.Single(LevelCatalog.Load(_dir).Errors);

        Assert.Equal(LevelErrorCode.InvalidJson, error.Code);
        Assert.Equal(LevelCatalog.FileName, error.Path);
    }

    [Fact]
    public void CatalogEntryWithoutFile_IsInvalidJson()
    {
        File.WriteAllText(Path.Combine(_dir, LevelCatalog.FileName), "[{ \"id\": \"level-1\" }]");

        var error = Assert.Single(LevelCatalog.Load(_dir).Errors);

        Assert.Equal(LevelErrorCode.InvalidJson, error.Code);
        Assert.Equal("catalog[0].file", error.Path);
    }

    [Fact]
    public void MissingCatalog_IsCatalogOrder()
    {
        var error = Assert.Single(LevelCatalog.Load(_dir).Errors);

        Assert.Equal(LevelErrorCode.CatalogOrder, error.Code);
        Assert.Equal(LevelCatalog.FileName, error.Path);
    }
}
