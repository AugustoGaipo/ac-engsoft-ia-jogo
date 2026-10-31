using Swapping.Levels.Model;

namespace Swapping.Levels.Tests;

/// <summary>The real files in <c>levels/</c>. A broken level fails <c>dotnet test</c>.</summary>
public static class Shipped
{
    private static readonly Lazy<LoadedCatalog> Catalog = new(() => LevelCatalog.Load(LevelsDirectory()));

    public static LoadedCatalog Load() => Catalog.Value;

    public static string LevelsDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Swapping.sln")))
            dir = dir.Parent;
        return Path.Combine(dir?.FullName ?? throw new InvalidOperationException("Swapping.sln not found."), "levels");
    }

    public static LevelFile Level(int number) => Load().Levels.Single(l => l.Number == number);

    public static RoomDef Room(string roomId)
    {
        var level = Level(int.Parse(roomId.Split('.')[0]));
        return level.Rooms.Single(r => r.Id == roomId);
    }
}

public class ShippedLevelsTests
{
    [Fact]
    public void CatalogAndEveryLevelFile_AreValid()
    {
        var catalog = Shipped.Load();

        Assert.True(catalog.Errors.Count == 0, string.Join("\n", catalog.Errors));
        Assert.Equal(catalog.Entries.Count, catalog.Levels.Count);
    }

    [Fact]
    public void Catalog_StartsWithLevels1And2()
    {
        var ids = Shipped.Load().Levels.Select(l => l.Id).ToArray();

        Assert.Equal(new[] { "level-1", "level-2" }, ids.Take(2));
    }

    [Fact]
    public void EveryShippedRoom_BuildsAWorldThatKeepsTheInvariant()
    {
        foreach (var level in Shipped.Load().Levels)
            foreach (var room in level.Rooms)
                Assert.True(RulesMapping.BuildWorld(level, room.Id).CheckInvariant(), $"room {room.Id}");
    }

    [Fact]
    public void EveryShippedRoom_SaysWhatItTeaches()
    {
        foreach (var room in Shipped.Load().Levels.SelectMany(l => l.Rooms))
            Assert.False(string.IsNullOrWhiteSpace(room.Teaches), $"room {room.Id}");
    }
}
