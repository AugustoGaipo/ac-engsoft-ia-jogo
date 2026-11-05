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

    /// <summary>How long a swapped body ignores the player (the player's ghost at its old place).</summary>
    public const double GhostSeconds = 0.25;

    private PhysicsBody3D? _ignored;
    private double _ignoreLeft;

    /// <summary>Ignores collisions with <paramref name="other"/> for <see cref="GhostSeconds"/>.</summary>
    public void IgnoreForAMoment(PhysicsBody3D other)
    {
        AddCollisionExceptionWith(other);
        _ignored = other;
        _held = false;
        _ignoreLeft = GhostSeconds;
    }

    public bool IsIgnoring(PhysicsBody3D other) => _ignored == other && (_held || _ignoreLeft > 0);

    private bool _held;

    private AudioStreamPlayer3D _impactSound = null!;
    private float _speedBefore;
    private double _quietLeft;

    /// <summary>How many hits made a sound (headless checks).</summary>
    public int Impacts { get; private set; }

    private void OnHit(Node other)
    {
        if (_held || _quietLeft > 0 || Swapping.GameLogic.Feedback.ImpactVolumeDb(Mass, _speedBefore) is not { } volume)
            return;
        _impactSound.VolumeDb = (float)volume;
        _impactSound.PitchScale = (float)Swapping.GameLogic.Feedback.ImpactPitch(Mass);
        _impactSound.Play();
        Impacts++;
        _quietLeft = 0.1;                         // one knock per bounce, not one per contact point
    }

    /// <summary>While carried: no collision with the carrier at all.</summary>
    public void IgnoreWhileHeld(PhysicsBody3D carrier)
    {
        AddCollisionExceptionWith(carrier);
        _ignored = carrier;
        _held = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        _speedBefore = LinearVelocity.Length();   // before this step's contacts: the speed of a hit
        _quietLeft -= delta;
        if (_ignored is null || _held)
            return;
        _ignoreLeft -= delta;
        if (_ignoreLeft > 0)
            return;
        if (!IsInstanceValid(_ignored))
        {
            _ignored = null;
            return;
        }
        // Collide again only once the two no longer overlap; otherwise the solver would shove one of them away.
        if (Overlaps(_ignored))
            return;
        RemoveCollisionExceptionWith(_ignored);
        _ignored = null;
    }

    private bool Overlaps(PhysicsBody3D other)
    {
        var shape = GetChildren().OfType<CollisionShape3D>().First().Shape;
        var hits = GetWorld3D().DirectSpaceState.IntersectShape(new PhysicsShapeQueryParameters3D
        {
            Shape = shape,
            Transform = GlobalTransform,
            CollisionMask = other.CollisionLayer,
            Exclude = [GetRid()],
        });
        return hits.Any(h => h["rid"].AsRid() == other.GetRid());
    }

    /// <summary>Moves the body at once (a swap or a respawn), with a new velocity, and wakes it.</summary>
    public void Teleport(Vector3 center, Vector3 velocity)
    {
        var transform = new Transform3D(GlobalBasis, center);
        PhysicsServer3D.BodySetState(GetRid(), PhysicsServer3D.BodyState.Transform, transform);
        GlobalTransform = transform;
        LinearVelocity = velocity;
        AngularVelocity = Vector3.Zero;
        Sleeping = false;
        ResetPhysicsInterpolation();
    }

    /// <summary>
    /// Turns the body at once while held: <paramref name="yawDegrees"/> around the vertical axis and
    /// <paramref name="tiltDegrees"/> around the viewer's right axis (mouse left/right and up/down, any mix).
    /// </summary>
    public void Turn(double yawDegrees, double tiltDegrees, Vector3 viewRight)
    {
        var turn = new Basis(Vector3.Up, Mathf.DegToRad((float)yawDegrees)) * new Basis(viewRight.Normalized(), Mathf.DegToRad((float)tiltDegrees));
        var transform = new Transform3D((turn * GlobalBasis).Orthonormalized(), GlobalPosition);
        PhysicsServer3D.BodySetState(GetRid(), PhysicsServer3D.BodyState.Transform, transform);
        GlobalTransform = transform;
        AngularVelocity = Vector3.Zero;
        ResetPhysicsInterpolation();
    }

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
            // Not the player's layer: the player's kinematic body would shove a box as if with infinite mass.
            // The player still collides with boxes (stands on them, stops at them) and pushes through PushImpulse.
            CollisionMask = Layers.World | Layers.Objects | Layers.BarrierObjects,
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
        // The outline (Task 9): a slightly bigger copy drawn from the inside only, unshaded ("inverted hull").
        body._outlineMaterial = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Front,
        };
        PrimitiveMesh hull = def.Kind == ObjectKind.Ball
            ? new SphereMesh { Radius = size.X / 2 + 0.05f, Height = size.X + 0.1f }
            : new BoxMesh { Size = size + new Vector3(0.1f, 0.1f, 0.1f) };
        hull.Material = body._outlineMaterial;
        body._outline = new MeshInstance3D { Name = "Outline", Mesh = hull, Visible = false };
        body.AddChild(body._outline);
        // Impact sounds (user's request): the body reports new contacts; OnHit decides by momentum (Feedback).
        body.ContactMonitor = true;
        body.MaxContactsReported = 4;
        body._impactSound = Sfx.Player3D(body, Sfx.Impact, "ImpactSound");
        body.BodyEntered += body.OnHit;
        return body;
    }

    private MeshInstance3D _outline = null!;
    private StandardMaterial3D _outlineMaterial = null!;

    /// <summary>The outline shown now, or null when the body is neither aimed nor held.</summary>
    public Swapping.GameLogic.Outline? OutlineShown { get; private set; }

    public void ShowOutline(Swapping.GameLogic.Outline? outline)
    {
        OutlineShown = outline;
        _outline.Visible = outline is not null;
        if (outline is { } kind)
            _outlineMaterial.AlbedoColor = kind switch
            {
                Swapping.GameLogic.Outline.White => Colors.White,
                Swapping.GameLogic.Outline.Gray => new Color(0.5f, 0.5f, 0.5f),
                Swapping.GameLogic.Outline.Position => AbilityColors.Position,   // same colours as the HUD chips
                Swapping.GameLogic.Outline.Mass => AbilityColors.Mass,
                _ => AbilityColors.KineticEnergy,
            };
    }
}
