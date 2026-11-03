using Godot;
using Swapping.Game.Pieces;
using Swapping.GameLogic;
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

    public double MassKg { get; set; } = LevelFile.PlayerMassKg;

    public WeightOnButton Weight => new(IsPlayer: true, MassKg: MassKg);

    public Camera3D Camera => _camera;

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
        player.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size }, Position = new Vector3(0, size.Y / 2, 0) });
        player._camera = new Camera3D { Name = "Eye", Position = new Vector3(0, EyeHeight, 0), Fov = 75, Current = true };
        player.AddChild(player._camera);
        return player;
    }

    /// <summary>Puts the player standing at a spawn (feet), at rest, looking along the yaw.</summary>
    public void SpawnAt(SpawnDef spawn)
    {
        Drop();
        GlobalPosition = spawn.Position.ToGodot();
        Rotation = new Vector3(0, Mathf.DegToRad((float)spawn.YawDegrees), 0);
        _camera.Rotation = Vector3.Zero;
        Velocity = Vector3.Zero;
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
            RotateY(-motion.Relative.X * MouseSensitivity);
            var pitch = Mathf.Clamp(_camera.Rotation.X - motion.Relative.Y * MouseSensitivity, -1.5f, 1.5f);
            _camera.Rotation = new Vector3(pitch, 0, 0);
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

    public override void _PhysicsProcess(double delta)
    {
        var input = ScriptedMove ?? Input.GetVector(
            Controls.Name(GameAction.MoveLeft), Controls.Name(GameAction.MoveRight),
            Controls.Name(GameAction.MoveBack), Controls.Name(GameAction.MoveForward));
        var direction = (GlobalBasis.X * input.X - GlobalBasis.Z * input.Y) with { Y = 0 };
        if (direction.LengthSquared() > 1)
            direction = direction.Normalized();

        var velocity = Velocity;
        velocity.X = direction.X * WalkSpeed;
        velocity.Z = direction.Z * WalkSpeed;
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
        body.LinearVelocity = -_camera.GlobalBasis.Z * speed;
    }

    public void Drop() => Held = null;

    /// <summary>The first object or wall hit by a ray from the eye, up to <paramref name="reach"/> meters.</summary>
    public Node3D? Aim(float reach)
    {
        var from = _camera.GlobalPosition;
        var query = PhysicsRayQueryParameters3D.Create(from, from - _camera.GlobalBasis.Z * reach,
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
        var target = _camera.GlobalPosition - _camera.GlobalBasis.Z * HoldDistance;
        var pull = (target - body.GlobalPosition) * 12f;
        body.LinearVelocity = pull.LimitLength(10f);
        body.AngularVelocity *= 0.5f;
        if (body.GlobalPosition.DistanceTo(target) > HoldReach + 0.5f)
            Drop();
    }

    private void PushBodies(Vector3 direction)
    {
        if (direction == Vector3.Zero)
            return;
        for (var i = 0; i < GetSlideCollisionCount(); i++)
        {
            if (GetSlideCollision(i).GetCollider() is not Body body || body == Held)
                continue;
            var along = body.LinearVelocity.Dot(direction);
            var impulse = PushImpulse.Impulse(MassKg, body.Mass, WalkSpeed, along);
            if (impulse > 0)
                body.ApplyCentralImpulse(direction * (float)impulse);
        }
    }
}
