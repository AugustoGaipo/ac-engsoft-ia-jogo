using Godot;
using Swapping.Game.Pieces;
using Swapping.GameLogic;
using Swapping.Levels;
using Swapping.Levels.Model;
using Swapping.Rules.Physics;

namespace Swapping.Game;

/// <summary>
/// The player: a <see cref="CharacterBody3D"/> 0.6 × 1.8 × 0.6 m whose origin is at the feet, with a
/// simulated mass (70 kg) and velocity. Mouse look, walk 3 m/s, jump at <c>Formulas.JumpTakeoffSpeed</c>,
/// hold (G5, ≤ 2.5 m, spring), throw with a fixed energy (G4) and push movable bodies by mass. Controls
/// come from <see cref="GameLogic.Controls"/> (G1). It does not decide swaps.
/// </summary>
public partial class Player : CharacterBody3D, IHasWeight
{
    public const float WalkSpeed = 3f;
    public const float EyeHeight = 1.6f;
    public const float HoldReach = 2.5f;
    public const float HoldDistance = 1.5f;
    public const double ThrowEnergyJoules = 60;
    public const float MouseSensitivity = 0.0025f;

    private Camera3D _camera = null!;
    private float _yaw;
    private float _pitch;

    public double MassKg { get; set; } = LevelFile.PlayerMassKg;

    public WeightOnButton Weight => new(IsPlayer: true, MassKg: MassKg);

    public Camera3D Camera => _camera;

    /// <summary>The eye at the current physics state (not interpolated): aim, hold and throw use it.</summary>
    public Transform3D Eye => new(Basis.FromEuler(new Vector3(_pitch, _yaw, 0)), GlobalPosition + new Vector3(0, EyeHeight, 0));

    public Body? Held { get; private set; }

    /// <summary>Test hook: when set, replaces the keyboard (x = right, y = forward).</summary>
    public Vector2? ScriptedMove { get; set; }

    public static Player Create()
    {
        var player = new Player
        {
            Name = "Player",
            CollisionLayer = Layers.Player,
            CollisionMask = Layers.World | Layers.Objects | Layers.BarrierPlayer,
            FloorMaxAngle = Mathf.DegToRad(46),
        };
        var size = SpawnDef.PlayerSize.ToGodot();
        // A capsule, not a box: it does not change with the yaw (a turned box reached 0.4 m into a crate, user's play test).
        player.AddChild(new CollisionShape3D { Shape = new CapsuleShape3D { Radius = size.X / 2, Height = size.Y }, Position = new Vector3(0, size.Y / 2, 0) });
        // The camera is drawn every frame from the interpolated body (physics runs at 60 Hz, the screen may run faster).
        player._camera = new Camera3D { Name = "Eye", Fov = 75, Current = true, TopLevel = true, PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off };
        player.AddChild(player._camera);
        return player;
    }

    /// <summary>Puts the player standing at a spawn (feet), at rest, looking along the yaw.</summary>
    public void SpawnAt(SpawnDef spawn)
    {
        Drop();
        GlobalPosition = spawn.Position.ToGodot();
        Velocity = Vector3.Zero;
        Look((float)spawn.YawDegrees, 0);
        ResetPhysicsInterpolation();
    }

    public override void _Ready()
    {
        if (DisplayServer.GetName() != "headless")
            Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _yaw -= motion.Relative.X * MouseSensitivity;
            _pitch = Mathf.Clamp(_pitch - motion.Relative.Y * MouseSensitivity, -1.5f, 1.5f);
            Rotation = new Vector3(0, _yaw, 0);
        }
        else if (@event.IsActionPressed(Controls.Name(GameAction.Hold)))
        {
            ToggleHold();
        }
        else if (@event.IsActionPressed(Controls.Name(GameAction.Throw)))
        {
            Throw();
        }
        else if (@event.IsActionPressed(Controls.Name(GameAction.Jump)))
        {
            Jump();
        }
    }

    /// <summary>The center of the body (feet + 0.9 m): the player's position in the rules world.</summary>
    public Vector3 Center => GlobalPosition + RulesMapping.PlayerCenterAboveFeet.ToGodot();

    /// <summary>Writes a swap's update back: the center becomes the new position, plus velocity and mass.</summary>
    public void PlaceAtCenter(Vector3 center, Vector3 velocity, double massKg)
    {
        Drop();
        GlobalPosition = center - RulesMapping.PlayerCenterAboveFeet.ToGodot();
        Velocity = velocity;
        MassKg = massKg;
        ResetPhysicsInterpolation();
    }

    /// <summary>Turns the view: yaw in degrees (0 = looking along -Z), pitch in radians (negative = down).</summary>
    public void Look(float yawDegrees, float pitch)
    {
        _yaw = Mathf.DegToRad(yawDegrees);
        _pitch = pitch;
        Rotation = new Vector3(0, _yaw, 0);
        _camera.GlobalTransform = Eye;
    }

    /// <summary>Every drawn frame: the interpolated body position and the latest mouse look.</summary>
    public override void _Process(double delta)
    {
        var feet = GetGlobalTransformInterpolated().Origin;
        _camera.GlobalTransform = new Transform3D(Basis.FromEuler(new Vector3(_pitch, _yaw, 0)), feet + new Vector3(0, EyeHeight, 0));
    }

    public override void _PhysicsProcess(double delta)
    {
        var input = ScriptedMove ?? Input.GetVector(
            Controls.Name(GameAction.MoveLeft), Controls.Name(GameAction.MoveRight),
            Controls.Name(GameAction.MoveBack), Controls.Name(GameAction.MoveForward));
        var direction = (GlobalBasis.X * input.X - GlobalBasis.Z * input.Y) with { Y = 0 };
        if (direction.LengthSquared() > 1)
            direction = direction.Normalized();

        // While pushing, the player moves only as fast as the pushed body can (PushImpulse).
        var speed = _pushSpeed is { } pushing ? Mathf.Min(WalkSpeed, pushing) : WalkSpeed;
        var velocity = Velocity;
        velocity.X = direction.X * speed;
        velocity.Z = direction.Z * speed;
        if (!IsOnFloor())
            velocity.Y -= (float)(Formulas.Gravity * delta);
        Velocity = velocity;
        MoveAndSlide();

        PushBodies(direction);
        FollowHeld();
    }

    /// <summary>Take-off at the speed the rules give for the current mass; only from the floor.</summary>
    public void Jump()
    {
        if (IsOnFloor())
            Velocity = Velocity with { Y = (float)Formulas.JumpTakeoffSpeed(MassKg) };
    }

    /// <summary><c>E</c>: hold the aimed movable body within reach, or drop the held one.</summary>
    public void ToggleHold()
    {
        if (Held is not null)
        {
            Drop();
            return;
        }
        if (Aim(HoldReach) is Body body)
            Held = body;
    }

    /// <summary>Left button: throw the held body forward with a fixed energy (G4).</summary>
    public void Throw()
    {
        if (Held is not { } body)
            return;
        Drop();
        var speed = (float)Formulas.SpeedFromEnergy(ThrowEnergyJoules, body.Mass);
        body.LinearVelocity = -Eye.Basis.Z * speed;
    }

    public void Drop() => Held = null;

    /// <summary>The first object or wall hit by a ray from the eye, up to <paramref name="reach"/> meters.</summary>
    public Node3D? Aim(float reach)
    {
        var eye = Eye;
        var query = PhysicsRayQueryParameters3D.Create(eye.Origin, eye.Origin - eye.Basis.Z * reach,
            Layers.World | Layers.Objects, [GetRid()]);
        var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
        return hit.Count > 0 ? hit["collider"].As<Node3D>() : null;
    }

    private void FollowHeld()
    {
        if (Held is not { } body)
            return;
        if (!IsInstanceValid(body) || !body.IsInsideTree())
        {
            Drop();
            return;
        }
        var target = Eye.Origin - Eye.Basis.Z * HoldDistance;
        var pull = (target - body.GlobalPosition) * 12f;
        body.LinearVelocity = pull.LimitLength(10f);
        body.AngularVelocity *= 0.5f;
        if (body.GlobalPosition.DistanceTo(target) > HoldReach + 0.5f)
            Drop();
    }

    private float? _pushSpeed;

    /// <summary>Whether the last step pushed (or tried to push) a body, and at what speed.</summary>
    public float? PushSpeed => _pushSpeed;

    private void PushBodies(Vector3 direction)
    {
        _pushSpeed = null;
        if (direction == Vector3.Zero)
            return;
        for (var i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision = GetSlideCollision(i);
            // Only a body in front, not one under the feet.
            if (collision.GetCollider() is not Body body || body == Held || collision.GetNormal().Dot(direction) > -0.5f)
                continue;
            var canPush = (float)PushImpulse.Speed(body.Mass, WalkSpeed);
            _pushSpeed = _pushSpeed is { } other ? Mathf.Min(other, canPush) : canPush;
            var impulse = PushImpulse.Impulse(body.Mass, WalkSpeed, body.LinearVelocity.Dot(direction));
            if (impulse > 0)
                body.ApplyCentralImpulse(direction * (float)impulse);
        }
    }
}
