using Swapping.Rules.Model;

namespace Swapping.Rules.Operations;

/// <summary>What the game must apply to one physics body (m, kg, m/s).</summary>
public readonly record struct BodyUpdate(string ContainerId, Vec3 Position, double MassKg, Vec3 Velocity);
