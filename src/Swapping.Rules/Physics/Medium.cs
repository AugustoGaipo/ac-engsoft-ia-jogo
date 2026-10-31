namespace Swapping.Rules.Physics;

/// <summary>A fluid zone: density (kg/m³) and drag (1/s, how fast gliding fades). Values are tunable.</summary>
public sealed record Medium(string Name, double Density, double Drag)
{
    public static readonly Medium Air = new("Air", 1.2, 0.05);
    public static readonly Medium DenseGas = new("Dense gas", 6.1, 0.1);
    public static readonly Medium Water = new("Water", 1000.0, 3.0);
}
