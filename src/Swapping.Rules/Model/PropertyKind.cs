namespace Swapping.Rules.Model;

/// <summary>Extension seam: a kind of swappable property (future: momentum, temperature...).</summary>
public interface IPropertyKind
{
    /// <summary>Short lowercase name, used in variable ids (e.g. "mass").</summary>
    string Name { get; }
}

/// <summary>The three properties of this version. Instances are immutable singletons.</summary>
public sealed class PropertyKind : IPropertyKind
{
    public static readonly PropertyKind Position = new("position");
    public static readonly PropertyKind Mass = new("mass");
    public static readonly PropertyKind KineticEnergy = new("energy");

    /// <summary>Every property a container is born with, in a fixed order.</summary>
    public static readonly IReadOnlyList<IPropertyKind> All = new IPropertyKind[] { Position, Mass, KineticEnergy };

    private PropertyKind(string name) => Name = name;

    public string Name { get; }

    public override string ToString() => Name;
}
