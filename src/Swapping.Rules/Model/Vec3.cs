namespace Swapping.Rules.Model;

/// <summary>Plain 3D vector (no engine types). Units are decided by the caller (m, m/s).</summary>
public readonly record struct Vec3(double X, double Y, double Z)
{
    public static readonly Vec3 Zero = new(0, 0, 0);

    public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

    public Vec3 Normalized()
    {
        var length = Length;
        return length == 0 ? Zero : this * (1.0 / length);
    }

    public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vec3 operator *(Vec3 a, double k) => new(a.X * k, a.Y * k, a.Z * k);
}
