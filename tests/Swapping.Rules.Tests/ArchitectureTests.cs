using System.Reflection;
using System.Xml.Linq;
using Swapping.Rules.Model;
using Swapping.Rules.Operations;
using Swapping.Rules.Policies;

namespace Swapping.Rules.Tests;

public class ArchitectureTests
{
    private static readonly Assembly Rules = typeof(World).Assembly;

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Swapping.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Swapping.sln not found.");
    }

    [Fact]
    public void RulesAssembly_HasTheExpectedName() => Assert.Equal(RulesAssembly.Name, Rules.GetName().Name);

    // SC 14
    [Fact]
    public void RulesAssembly_ReferencesNoGodotAssembly()
    {
        var references = Rules.GetReferencedAssemblies().Select(a => a.Name ?? "");
        Assert.DoesNotContain(references, n => n.StartsWith("Godot", StringComparison.OrdinalIgnoreCase));
    }

    // SC 14
    [Fact]
    public void RulesProject_HasNoPackageOrProjectReferences()
    {
        var path = Path.Combine(RepoRoot(), "src", "Swapping.Rules", "Swapping.Rules.csproj");
        var project = XDocument.Load(path);
        var names = project.Descendants().Select(e => e.Name.LocalName).ToArray();

        Assert.DoesNotContain("PackageReference", names);
        Assert.DoesNotContain("ProjectReference", names);
        Assert.DoesNotContain("Reference", names);
        Assert.DoesNotContain("Godot", File.ReadAllText(path), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RulesAssembly_HasNoStaticMutableFields()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var mutable = Rules.GetTypes()
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

    [Theory]
    [InlineData(typeof(IPropertyKind), typeof(PropertyKind))]
    [InlineData(typeof(IOperation), typeof(SwapOperation))]
    [InlineData(typeof(ISwapDurationPolicy), typeof(PermanentSwapDuration))]
    [InlineData(typeof(ICooldownModifier), typeof(NoCooldownModifier))]
    public void ExtensionSeam_HasItsSingleImplementation(Type seam, Type implementation)
    {
        Assert.True(seam.IsInterface);
        var implementations = Rules.GetTypes().Where(t => !t.IsInterface && seam.IsAssignableFrom(t)).ToArray();
        Assert.Equal(new[] { implementation }, implementations);
    }

    [Fact]
    public void DefaultLevel_UsesPermanentSwapsNoCooldownModifierAndFlagsOff()
    {
        var rules = LevelRules.Default;

        Assert.IsType<PermanentSwapDuration>(rules.SwapDuration);
        Assert.IsType<NoCooldownModifier>(rules.CooldownModifier);
        Assert.False(rules.AllowObjectToObject);
        Assert.False(rules.AllowSimultaneousSwaps);
        Assert.False(rules.EnableEntangledProperties);
    }

    [Fact]
    public void PermanentSwapDuration_NeverReverts()
    {
        var request = new SwapRequest("player", "A", PropertyKind.Mass);
        Assert.Null(new PermanentSwapDuration().RevertAfterSeconds(request));
    }

    [Fact]
    public void NoCooldownModifier_KeepsTheLevelCooldown()
    {
        Assert.Equal(2.0, new NoCooldownModifier().Modify(2.0), 1e-9);
        Assert.Equal(2.0, (LevelRules.Default with { CooldownSeconds = 2.0 }).EffectiveCooldownSeconds, 1e-9);
    }
}
