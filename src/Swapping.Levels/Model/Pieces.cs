using Swapping.Rules.Model;

namespace Swapping.Levels.Model;

public enum ObjectKind { Box, Ball }

public enum ButtonDetects { Any, Objects }

public enum BarrierPreset { ExitGrid }

/// <summary>Where the player (re)appears in a room. The position is the player's feet.</summary>
public sealed record SpawnDef(Vec3 Position, double YawDegrees)
{
    public static readonly Vec3 PlayerSize = new(0.6, 1.8, 0.6);

    /// <summary>The player-sized box standing on the spawn position.</summary>
    public Box Area => Box.StandingOn(Position, PlayerSize);
}

/// <summary>
/// A movable object. <see cref="Position"/> is its center. Volume and contact area are derived from
/// the size, so they can never disagree with it. <see cref="Accepts"/> null = every unlocked property.
/// </summary>
public sealed record ObjectDef(
    string Id,
    ObjectKind Kind,
    Vec3 Position,
    Vec3 Size,
    double MassKg,
    IReadOnlyList<string>? Accepts,
    bool Locked,
    Vec3 Velocity,
    double Bounce)
{
    /// <summary>Share of the cross-section a ball touches the floor with (it rolls on one point).</summary>
    public const double BallContactShare = 0.01;

    public double VolumeM3 => Kind == ObjectKind.Box
        ? Size.X * Size.Y * Size.Z
        : Math.PI * Math.Pow(Size.X, 3) / 6;

    public double ContactAreaM2 => Kind == ObjectKind.Box
        ? Size.X * Size.Z
        : BallContactShare * Math.PI * Size.X * Size.X / 4;

    public double DensityKgM3 => MassKg / VolumeM3;

    public Box Bounds => Box.Centered(Position, Size);
}

/// <summary>A floor plate. <see cref="Position"/> is the center of its bottom face.</summary>
public sealed record ButtonDef(string Id, Vec3 Position, Vec3 Size, double RequiredMassKg, ButtonDetects Detects)
{
    public Box Bounds => Box.StandingOn(Position, Size);
}

/// <summary>A door that is open while every button in <see cref="OpensWith"/> is pressed.</summary>
public sealed record DoorDef(string Id, Box Box, IReadOnlyList<string> OpensWith);

/// <summary>A barrier volume (wall or zone): a preset or the three basic, combinable filters.</summary>
public sealed record BarrierDef(
    string Id,
    BarrierPreset? Preset,
    bool BlocksObjects,
    bool BlocksPlayer,
    bool BlocksVariables,
    Box Box)
{
    public BarrierFilter Filter => Preset == BarrierPreset.ExitGrid
        ? BarrierFilter.ExitGrid
        : new BarrierFilter(BlocksObjects, BlocksPlayer, BlocksVariables);

    /// <summary>Barrier 1 + barrier 3 (blocks objects and variables, lets the player pass).</summary>
    public bool IsExitGrid => Filter == BarrierFilter.ExitGrid;
}

/// <summary>A volume that kills whatever falls into it (under a hole of the scenery).</summary>
public sealed record KillZoneDef(string Id, Box Box);
