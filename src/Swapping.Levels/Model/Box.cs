using Swapping.Rules.Model;

namespace Swapping.Levels.Model;

/// <summary>Axis-aligned box in level space (m, Y up). Touching faces do not count as overlap.</summary>
public readonly record struct Box(Vec3 Min, Vec3 Max)
{
    /// <summary>A box of the given size whose center is <paramref name="center"/>.</summary>
    public static Box Centered(Vec3 center, Vec3 size) => new(center - size * 0.5, center + size * 0.5);

    /// <summary>A box of the given size standing on <paramref name="bottomCenter"/> (center of its bottom face).</summary>
    public static Box StandingOn(Vec3 bottomCenter, Vec3 size) =>
        new(new Vec3(bottomCenter.X - size.X / 2, bottomCenter.Y, bottomCenter.Z - size.Z / 2),
            new Vec3(bottomCenter.X + size.X / 2, bottomCenter.Y + size.Y, bottomCenter.Z + size.Z / 2));

    public bool IsEmpty => !(Min.X < Max.X && Min.Y < Max.Y && Min.Z < Max.Z);

    public bool Contains(Vec3 p) =>
        p.X >= Min.X && p.X <= Max.X && p.Y >= Min.Y && p.Y <= Max.Y && p.Z >= Min.Z && p.Z <= Max.Z;

    public bool Contains(Box other) => Contains(other.Min) && Contains(other.Max);

    public bool Overlaps(Box other) =>
        Min.X < other.Max.X && other.Min.X < Max.X &&
        Min.Y < other.Max.Y && other.Min.Y < Max.Y &&
        Min.Z < other.Max.Z && other.Min.Z < Max.Z;
}
