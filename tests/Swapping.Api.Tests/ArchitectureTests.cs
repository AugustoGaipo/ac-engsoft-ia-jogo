using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Swapping.Api.Profiles;
using Swapping.Api.Progress;

namespace Swapping.Api.Tests;

/// <summary>
/// The module boundaries of the API (P-SC 17, 24, G-SC 17): <c>Profiles/</c> knows nothing of
/// <c>Progress/</c>, no Godot anywhere, and the database is reached only through EF Core LINQ.
/// </summary>
public sealed class ArchitectureTests
{
    private static readonly Assembly Api = typeof(ProfileService).Assembly;

    private static string SourceDirectory => Path.Combine(RepoPaths.Root, "src", "Swapping.Api");

    private static IEnumerable<string> SourceFiles(string folder = "") =>
        Directory.EnumerateFiles(Path.Combine(SourceDirectory, folder), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    /// <summary>The types written by hand in one namespace (no compiler or coverage-tool types).</summary>
    private static Type[] TypesIn(string ns) => Api.GetTypes()
        .Where(t => t.Namespace == ns)
        .Where(t => !t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false))
        .Where(t => !t.Name.Contains('<', StringComparison.Ordinal))
        .ToArray();

    // ---- P-SC 17: Profiles does not use Progress ----

    [Fact]
    public void No_Profiles_source_file_names_the_Progress_namespace_or_any_of_its_types()
    {
        var progressTypes = TypesIn(typeof(ProgressService).Namespace!).Select(t => t.Name.Split('`')[0]).Distinct().ToArray();
        Assert.Contains("ProgressService", progressTypes);
        var pattern = new Regex(@"\b(Swapping\.Api\.Progress|" + string.Join("|", progressTypes) + @")\b");

        var hits = SourceFiles("Profiles")
            .SelectMany(file => File.ReadAllLines(file).Select((line, i) => (file, line, number: i + 1)))
            .Where(x => !x.line.TrimStart().StartsWith("//", StringComparison.Ordinal) && pattern.IsMatch(x.line))
            .Select(x => $"{Path.GetFileName(x.file)}:{x.number}: {x.line.Trim()}")
            .ToArray();

        Assert.Empty(hits);
    }

    [Fact]
    public void No_Profiles_type_has_a_Progress_type_in_its_fields_properties_or_signatures()
    {
        var progressNamespace = typeof(ProgressService).Namespace!;
        const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        static IEnumerable<Type> Expand(Type type) =>
            type.IsGenericType ? type.GetGenericArguments().SelectMany(Expand).Prepend(type)
            : type.HasElementType ? Expand(type.GetElementType()!).Prepend(type)
            : [type];

        var hits = TypesIn(typeof(ProfileService).Namespace!)
            .SelectMany(t => t.GetFields(all).Select(f => (t, f.FieldType))
                .Concat(t.GetProperties(all).Select(p => (t, p.PropertyType)))
                .Concat(t.GetMethods(all).SelectMany(m => m.GetParameters().Select(p => p.ParameterType).Append(m.ReturnType)).Select(x => (t, x)))
                .Concat(t.GetConstructors(all).SelectMany(c => c.GetParameters()).Select(p => (t, p.ParameterType))))
            .SelectMany(x => Expand(x.Item2).Select(used => (owner: x.t, used)))
            .Where(x => x.used.Namespace == progressNamespace)
            .Select(x => $"{x.owner.Name} uses {x.used.Name}")
            .Distinct()
            .ToArray();

        Assert.Empty(hits);
    }

    // ---- P-SC 17, G-SC 17: no Godot ----

    [Fact]
    public void The_API_references_no_Godot_assembly_or_package()
    {
        var names = Api.GetReferencedAssemblies().Select(a => a.Name ?? "").ToArray();
        var project = XDocument.Load(Path.Combine(SourceDirectory, "Swapping.Api.csproj"));
        var packages = project.Descendants().Where(e => e.Name.LocalName == "PackageReference")
            .Select(e => e.Attribute("Include")!.Value).ToArray();

        Assert.DoesNotContain(names, n => n.StartsWith("Godot", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(new[] { "Microsoft.EntityFrameworkCore.Sqlite" }, packages);
        Assert.Contains("Swapping.Levels", names);
    }

    // ---- P-SC 24 (source part): EF Core LINQ only ----

    [Fact]
    public void No_source_file_runs_raw_SQL()
    {
        var raw = new Regex(@"\b(FromSqlRaw|ExecuteSqlRaw|ExecuteSqlRawAsync|SqlQueryRaw|FromSql|ExecuteSql|ExecuteSqlAsync|SqlQuery|ExecuteSqlInterpolated|FromSqlInterpolated|CreateCommand|SqliteCommand)\b");
        var files = SourceFiles().ToArray();

        var hits = files.Where(f => raw.IsMatch(File.ReadAllText(f))).Select(Path.GetFileName).ToArray();

        Assert.True(files.Length > 10, "the source folder was not found");
        Assert.Empty(hits);
    }
}
