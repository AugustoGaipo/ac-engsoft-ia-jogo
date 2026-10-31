namespace Swapping.Rules.Model;

/// <summary>
/// Value of a variable. Position: <see cref="Vector"/> is the point (m).
/// Mass: <see cref="Scalar"/> is kg. Kinetic energy: <see cref="Scalar"/> is J and
/// <see cref="Vector"/> is the unit direction of travel.
/// </summary>
public readonly record struct VariableValue(double Scalar, Vec3 Vector)
{
    public static VariableValue OfPosition(Vec3 position) => new(0, position);
    public static VariableValue OfMass(double massKg) => new(massKg, Vec3.Zero);
    public static VariableValue OfEnergy(double joules, Vec3 direction) => new(joules, direction.Normalized());
}

/// <summary>A variable born with (owned by) one container; whoever uses it may change.</summary>
public sealed class Variable
{
    internal Variable(IPropertyKind kind, string owner, VariableValue value)
    {
        Kind = kind;
        Owner = owner;
        Value = value;
    }

    /// <summary>"owner.property", e.g. "A.mass".</summary>
    public string Id => $"{Owner}.{Kind.Name}";
    public IPropertyKind Kind { get; }
    public string Owner { get; }
    public VariableValue Value { get; internal set; }

    public override string ToString() => Id;
}
