using System.Net;
using System.Text.Json;

namespace Swapping.Api.Tests.Progress;

public class LevelsEndpointTests
{
    private static async Task<List<(string Id, int Number, string Name, int Rooms)>> Levels(HttpClient client)
    {
        var response = await client.GetAsync("/levels");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.EnumerateArray()
            .Select(l => (l.GetProperty("levelId").GetString()!, l.GetProperty("number").GetInt32(),
                l.GetProperty("name").GetString()!, l.GetProperty("rooms").GetInt32()))
            .ToList();
    }

    [Fact]
    public async Task With_the_shipped_levels_folder_the_API_lists_the_3_shipped_levels()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var levels = await Levels(client);

        Assert.Equal(new[] { 1, 2, 3 }, levels.Select(l => l.Number));
        Assert.Equal(new[] { 4, 4, 2 }, levels.Select(l => l.Rooms));
        Assert.Equal(new[] { "level-1", "level-2", "level-3" }, levels.Select(l => l.Id));
        Assert.All(levels, l => Assert.False(string.IsNullOrWhiteSpace(l.Name)));
    }

    [Fact]
    public async Task The_catalog_folder_comes_from_configuration()
    {
        using var catalog = new TestCatalog();
        using var factory = new ApiFactory { Settings = { ["Progress:LevelsDirectory"] = catalog.Directory } };
        var client = factory.CreateClient();

        var levels = await Levels(client);

        Assert.Equal(new[] { ("level-1", 1, "Test one", 4), ("level-2", 2, "Test two", 4), ("level-4", 4, "Test four", 2) }, levels);
    }

    [Fact]
    public void The_API_refuses_to_start_with_a_broken_catalog()
    {
        using var catalog = new TestCatalog();
        catalog.Break();
        using var factory = new ApiFactory { Settings = { ["Progress:LevelsDirectory"] = catalog.Directory } };

        var error = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("t_2.json", error.ToString());
    }

    [Fact]
    public void The_API_refuses_to_start_without_a_catalog()
    {
        var empty = Path.Combine(Path.GetTempPath(), $"swapping-empty-{Guid.NewGuid():N}");
        Directory.CreateDirectory(empty);
        using var factory = new ApiFactory { Settings = { ["Progress:LevelsDirectory"] = empty } };

        Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Directory.Delete(empty);
    }
}
