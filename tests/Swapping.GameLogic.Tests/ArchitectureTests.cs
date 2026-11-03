using System.Xml.Linq;

namespace Swapping.GameLogic.Tests;

/// <summary>SC 11: the game's decisions live in a library with no Godot reference.</summary>
public sealed class ArchitectureTests
{
    private static string ProjectFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Swapping.sln")))
            dir = dir.Parent;
        return Path.Combine(dir!.FullName, "src", "Swapping.GameLogic", "Swapping.GameLogic.csproj");
    }

    [Fact]
    public void The_project_references_only_rules_and_levels_and_no_package()
    {
        var project = XDocument.Load(ProjectFile());
        var elements = project.Descendants().ToArray();

        Assert.Equal("Microsoft.NET.Sdk", project.Root!.Attribute("Sdk")!.Value);
        Assert.DoesNotContain(elements, e => e.Name.LocalName is "PackageReference" or "Reference");
        Assert.Equal(new[] { "Swapping.Levels.csproj", "Swapping.Rules.csproj" },
            elements.Where(e => e.Name.LocalName == "ProjectReference")
                .Select(e => Path.GetFileName(e.Attribute("Include")!.Value)).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void The_assembly_references_no_Godot_assembly()
    {
        var names = typeof(RunTracker).Assembly.GetReferencedAssemblies().Select(a => a.Name ?? "").ToArray();

        Assert.DoesNotContain(names, n => n.StartsWith("Godot", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("Swapping.Rules", names);
    }
}
