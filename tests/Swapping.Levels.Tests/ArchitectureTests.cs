using System.Reflection;
using System.Xml.Linq;

namespace Swapping.Levels.Tests;

public class ArchitectureTests
{
    private static readonly Assembly Levels = typeof(LevelParser).Assembly;

    private static string ProjectFile() =>
        Path.Combine(Path.GetDirectoryName(Shipped.LevelsDirectory())!, "src", "Swapping.Levels", "Swapping.Levels.csproj");

    // SC 13
    [Fact]
    public void LevelsProject_ReferencesOnlyTheRulesProject()
    {
        var project = XDocument.Load(ProjectFile());
        var elements = project.Descendants().ToArray();

        Assert.DoesNotContain(elements, e => e.Name.LocalName is "PackageReference" or "Reference");
        var reference = Assert.Single(elements, e => e.Name.LocalName == "ProjectReference");
        Assert.EndsWith("Swapping.Rules.csproj", reference.Attribute("Include")!.Value);
        Assert.DoesNotContain("Godot", File.ReadAllText(ProjectFile()), StringComparison.OrdinalIgnoreCase);
    }

    // SC 13
    [Fact]
    public void LevelsAssembly_ReferencesNoGodotAssembly()
    {
        var names = Levels.GetReferencedAssemblies().Select(a => a.Name ?? "").ToArray();

        Assert.DoesNotContain(names, n => n.StartsWith("Godot", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("Swapping.Rules", names);
    }

    [Fact]
    public void LevelsAssembly_HasNoStaticMutableFields()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var mutable = Levels.GetTypes()
            // Coverage tools (coverlet) inject tracker types with static counters into the assembly.
            .Where(t => !(t.Namespace ?? "").StartsWith("Coverlet", StringComparison.Ordinal))
            // The C# compiler caches lambdas in static fields of generated types such as "<>c".
            .Where(t => !t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false))
            .SelectMany(t => t.GetFields(flags).Select(f => (Type: t, Field: f)))
            .Where(x => !x.Field.IsInitOnly && !x.Field.IsLiteral)
            .Select(x => $"{x.Type.FullName}.{x.Field.Name}")
            .ToArray();

        Assert.Empty(mutable);
    }
}
