using Godot;
using Swapping.Levels.Model;

namespace Swapping.Game.Pieces;

/// <summary>Collision layers of project.godot, as bit values.</summary>
public static class Layers
{
    public const uint World = 1;
    public const uint Player = 2;
    public const uint Objects = 4;
    public const uint BarrierObjects = 8;
    public const uint BarrierPlayer = 16;
}

/// <summary>Level space (doubles, <c>Swapping.Rules.Model.Vec3</c>) ↔ Godot (floats).</summary>
public static class Space
{
    public static Vector3 ToGodot(this Swapping.Rules.Model.Vec3 v) => new((float)v.X, (float)v.Y, (float)v.Z);

    public static Swapping.Rules.Model.Vec3 ToLevel(this Vector3 v) => new(v.X, v.Y, v.Z);

    public static Vector3 Center(this Box box) => ((box.Min + box.Max) * 0.5).ToGodot();

    public static Vector3 Size(this Box box) => (box.Max - box.Min).ToGodot();
}

/// <summary>
/// A movable object of a room (box or ball), built from its <see cref="ObjectDef"/>. Its id is the
/// container id of the <c>rules</c> world. CCD is always on (fast light objects after an energy swap).
/// </summary>
public partial class Body : RigidBody3D, IHasWeight
{
    public const float Friction = 0.6f;

    public Swapping.GameLogic.WeightOnButton Weight => new(IsPlayer: false, MassKg: Mass);

    public string ContainerId { get; private set; } = "";

    public ObjectDef Def { get; private set; } = null!;

    public static Body Create(ObjectDef def)
    {
        var body = new Body
        {
            Name = def.Id,
            ContainerId = def.Id,
            Def = def,
            Mass = (float)def.MassKg,
            ContinuousCd = true,
            CollisionLayer = Layers.Objects,
            CollisionMask = Layers.World | Layers.Player | Layers.Objects | Layers.BarrierObjects,
            PhysicsMaterialOverride = new PhysicsMaterial { Bounce = (float)def.Bounce, Friction = Friction },
            Position = def.Position.ToGodot(),
            LinearVelocity = def.Velocity.ToGodot(),
        };

        var size = def.Size.ToGodot();
        var color = def.Locked ? new Color(0.45f, 0.45f, 0.48f) : def.Kind == ObjectKind.Ball ? new Color(0.95f, 0.8f, 0.2f) : new Color(0.8f, 0.5f, 0.25f);
        var material = new StandardMaterial3D { AlbedoColor = color };
        if (def.Kind == ObjectKind.Ball)
        {
            body.AddChild(new MeshInstance3D { Mesh = new SphereMesh { Radius = size.X / 2, Height = size.X, Material = material } });
            body.AddChild(new CollisionShape3D { Shape = new SphereShape3D { Radius = size.X / 2 } });
        }
        else
        {
            body.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size, Material = material } });
            body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        }
        return body;
    }
}
