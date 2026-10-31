using System.Globalization;
using System.Text.Json;
using Swapping.Levels.Model;

namespace Swapping.Levels;

/// <summary>One line of <c>catalog.json</c>: a level id and the file (a plain name in the same folder).</summary>
public sealed record CatalogEntry(string Id, string File);

/// <summary>The catalog and the valid levels it lists, in play order, plus every problem found.</summary>
public sealed record LoadedCatalog(
    IReadOnlyList<CatalogEntry> Entries,
    IReadOnlyList<LevelFile> Levels,
    IReadOnlyList<LevelError> Errors);

/// <summary>
/// Reads <c>levels/catalog.json</c> and the files it lists. This is the only class of the module that
/// touches the file system. Errors from a level file have the file name in front of the path.
/// </summary>
public static class LevelCatalog
{
    public const string FileName = "catalog.json";

    public static LoadedCatalog Load(string directory)
    {
        var errors = new List<LevelError>();
        var catalogPath = Path.Combine(directory, FileName);
        if (!System.IO.File.Exists(catalogPath))
        {
            errors.Add(new LevelError(FileName, LevelErrorCode.CatalogOrder, $"'{FileName}' was not found."));
            return new LoadedCatalog(Array.Empty<CatalogEntry>(), Array.Empty<LevelFile>(), errors);
        }

        var (entries, parseErrors) = ParseCatalog(System.IO.File.ReadAllText(catalogPath));
        errors.AddRange(parseErrors);

        var levels = new List<LevelFile>();
        var previousNumber = 0;
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var p = $"catalog[{i.ToString(CultureInfo.InvariantCulture)}]";

            if (!IsPlainFileName(entry.File))
            {
                errors.Add(new LevelError($"{p}.file", LevelErrorCode.CatalogOrder,
                    $"'{entry.File}' must be a plain file name in the levels folder."));
                continue;
            }

            var file = Path.Combine(directory, entry.File);
            if (!System.IO.File.Exists(file))
            {
                errors.Add(new LevelError($"{p}.file", LevelErrorCode.CatalogOrder, $"'{entry.File}' was not found."));
                continue;
            }

            var (level, levelErrors) = LevelParser.Parse(System.IO.File.ReadAllText(file));
            if (level is not null)
                levelErrors = LevelValidator.Validate(level);
            if (levelErrors.Count > 0)
            {
                errors.AddRange(levelErrors.Select(e => e with { Path = $"{entry.File}:{e.Path}" }));
                continue;
            }

            if (level!.Id != entry.Id)
            {
                errors.Add(new LevelError($"{p}.id", LevelErrorCode.CatalogOrder,
                    $"The catalog says '{entry.Id}' but '{entry.File}' is '{level.Id}'."));
                continue;
            }

            if (level.Number <= previousNumber)
            {
                errors.Add(new LevelError(p, LevelErrorCode.CatalogOrder,
                    $"Level {level.Number} comes after level {previousNumber}; numbers must increase."));
                continue;
            }

            previousNumber = level.Number;
            levels.Add(level);
        }

        return new LoadedCatalog(entries, levels, errors);
    }

    /// <summary>Parses the catalog text: an array of <c>{ "id", "file" }</c>.</summary>
    public static (IReadOnlyList<CatalogEntry> Entries, IReadOnlyList<LevelError> Errors) ParseCatalog(string json)
    {
        var errors = new List<LevelError>();
        var entries = new List<CatalogEntry>();
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                errors.Add(new LevelError(FileName, LevelErrorCode.InvalidJson, "The catalog must be an array of { id, file }."));
                return (entries, errors);
            }

            var i = 0;
            foreach (var item in document.RootElement.EnumerateArray())
            {
                var p = $"catalog[{i.ToString(CultureInfo.InvariantCulture)}]";
                var id = Text(item, "id", p, errors);
                var file = Text(item, "file", p, errors);
                if (id is not null && file is not null)
                    entries.Add(new CatalogEntry(id, file));
                i++;
            }
        }
        catch (JsonException e)
        {
            errors.Add(new LevelError(FileName, LevelErrorCode.InvalidJson, e.Message));
        }

        return (entries, errors);
    }

    private static string? Text(JsonElement item, string name, string path, List<LevelError> errors)
    {
        if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            return value.GetString();
        errors.Add(new LevelError($"{path}.{name}", LevelErrorCode.InvalidJson, $"'{name}' must be a string."));
        return null;
    }

    /// <summary>No folders, no "..", no drive: the catalog can only point at files next to it.</summary>
    private static bool IsPlainFileName(string file) =>
        file.Length > 0
        && file == Path.GetFileName(file)
        && file.IndexOfAny(new[] { '/', '\\', ':' }) < 0
        && file != "." && file != "..";
}
