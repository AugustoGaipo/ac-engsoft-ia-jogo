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

    /// <summary>Seconds the throw button has been held with a body in hand; null when not charging.</summary>
    public double? ChargeSeconds { get; private set; }

    /// <summary>0–1, the throw bar under the crosshair (null when not charging).</summary>
    public double? ChargeFraction => ChargeSeconds is { } s ? ThrowCharge.Fraction(s) : null;
    public const float MouseSensitivity = 0.0025f;

    private Camera3D _camera = null!;
    private AudioStreamPlayer _jumpSound = null!;
    private AudioStreamPlayer _landSound = null!;
    private AudioStreamPlayer _throwSound = null!;
    private AudioStreamPlayer _tickSound = null!;
    private AudioStreamPlayer _fullSound = null!;
    private AudioStreamPlayer _grabSound = null!;
    private AudioStreamPlayer _releaseSound = null!;
    private AudioStreamPlayer _tooHeavySound = null!;
    private readonly RandomNumberGenerator _shakeRandom = new();

    /// <summary>Camera shake 0–1 from the last heavy landing (Feedback.LandingShake); fades in 0.4 s.</summary>
    public double Shake { get; set; }

    /// <summary>0–1 while dying: the view rolls to the side and drops to the floor (RunFlow.Die).</summary>
    [Export] public float DeathFall { get; set; }
    private float _yaw;
    private float _pitch;

    public double MassKg { get; set; } = LevelFile.PlayerMassKg;

    public WeightOnButton Weight => new(IsPlayer: true, MassKg: MassKg);

    public Camera3D Camera => _camera;

    /// <summary>The eye at the current physics state (not interpolated): aim, hold and throw use it.</summary>
    public Transform3D Eye => new(Basis.FromEuler(new Vector3(_pitch, _yaw, 0)), GlobalPosition + new Vector3(0, EyeHeight, 0));

    public Body? Held { get; private set; }

    /// <summary>The downward speed of the step that touched the floor (null on any other step): the fall death check.</summary>
    public float? LandingSpeed { get; private set; }

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
        player._jumpSound = Sfx.Player(player, Sfx.Jump, "JumpSound");
        player._landSound = Sfx.Player(player, Sfx.Land, "LandSound");
        player._throwSound = Sfx.Player(player, Sfx.Throw, "ThrowSound");
        player._tickSound = Sfx.Player(player, Sfx.Tick, "TickSound");
        player._fullSound = Sfx.Player(player, Sfx.Full, "FullSound");
        player._grabSound = Sfx.Player(player, Sfx.Grab, "GrabSound");
        player._releaseSound = Sfx.Player(player, Sfx.Release, "ReleaseSound");
        player._tooHeavySound = Sfx.Player(player, Sfx.Refused, "TooHeavySound");
        return player;
    }

    /// <summary>
    /// Puts the player standing at a spawn (feet), at rest, looking along the yaw, with its own mass: the room is
    /// rebuilt with a new rules world where the player is 70 kg (user's bug: after a mass swap, dying kept the swapped mass).
    /// </summary>
    public void SpawnAt(SpawnDef spawn)
    {
        Drop();
        GlobalPosition = spawn.Position.ToGodot();
        Velocity = Vector3.Zero;
        MassKg = LevelFile.PlayerMassKg;
        Shake = 0;
        DeathFall = 0;
        Look((float)spawn.YawDegrees, 0);
        ResetPhysicsInterpolation();
    }

    public override void _Ready()
    {
        if (DisplayServer.GetName() != "headless")
            Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    /// <summary>True while the rotate button is down with an object in hand: the mouse turns the object, not the view.</summary>
    public bool IsRotatingHeld => Held is not null && Input.IsActionPressed(Controls.Name(GameAction.HoldToRotate));

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion spin && IsRotatingHeld)
        {
            SpinHeld(HeldSpin.Degrees(spin.Relative.X, MouseSensitivity), HeldSpin.TiltDegrees(spin.Relative.Y, MouseSensitivity));
        }
        else if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
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
            if (Held is not null)
                ChargeSeconds = 0;   // the bar starts filling
        }
        else if (@event.IsActionReleased(Controls.Name(GameAction.Throw)))
        {
            ReleaseCharge();
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
        // Dying: roll 75° and drop the eye to 0.35 m. Shaking: a random offset of up to 0.15 m and 2°.
        var roll = Mathf.DegToRad(75) * DeathFall;
        var eye = new Vector3(0, Mathf.Lerp(EyeHeight, 0.35f, DeathFall), 0);
        if (Shake > 0)
        {
            var s = (float)Shake;
            eye += new Vector3(_shakeRandom.RandfRange(-1, 1), _shakeRandom.RandfRange(-1, 1), _shakeRandom.RandfRange(-1, 1)) * 0.15f * s;
            roll += Mathf.DegToRad(2) * s * _shakeRandom.RandfRange(-1, 1);
            Shake = Math.Max(0, Shake - delta / 0.4);
        }
        _camera.GlobalTransform = new Transform3D(Basis.FromEuler(new Vector3(_pitch, _yaw, roll)), feet + eye);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (ChargeSeconds is { } charging)
        {
            ChargeSeconds = Held is null ? null : charging + delta;   // letting go of the body (E, a barrier) cancels the bar
            if (ChargeSeconds is { } now)
                PlayCharge(Feedback.ChargeStep(ThrowCharge.Fraction(charging), ThrowCharge.Fraction(now)), ThrowCharge.Fraction(now));
        }

        var input = ScriptedMove ?? Input.GetVector(
            Controls.Name(GameAction.MoveLeft), Controls.Name(GameAction.MoveRight),
            Controls.Name(GameAction.MoveBack), Controls.Name(GameAction.MoveForward));
        var direction = (GlobalBasis.X * input.X - GlobalBasis.Z * input.Y) with { Y = 0 };
        if (direction.LengthSquared() > 1)
            direction = direction.Normalized();

        // While pushing, the player moves only as fast as the pushed body can (PushImpulse).
        var walk = WalkSpeed * (float)HoldLoad.SpeedFactor(HeldMassKg, MassKg);
        var speed = _pushSpeed is { } pushing ? Mathf.Min(walk, pushing) : walk;
        var velocity = Velocity;
        velocity.X = direction.X * speed;
        velocity.Z = direction.Z * speed;
        if (!IsOnFloor())
            velocity.Y -= (float)(Formulas.Gravity * delta);
        Velocity = velocity;
        var wasOnFloor = IsOnFloor();
        MoveAndSlide();
        LandingSpeed = !wasOnFloor && IsOnFloor() ? -velocity.Y : null;
        if (LandingSpeed is > 2f and var landing)
        {
            // Heavy landings shake the view and thud louder (Feedback); a light body lands softly.
            var mass = MassKg + HeldMassKg;
            var shake = Feedback.LandingShake(mass, landing);
            Shake = Math.Max(Shake, shake);
            _landSound.VolumeDb = Mathf.Lerp(-20f, 2f, (float)shake);
            _landSound.PitchScale = (float)Feedback.JumpPitch(mass);
            _landSound.Play();
        }

        PushBodies(direction);
        FollowHeld();
    }

    /// <summary>Take-off at the speed the rules give for the current mass; only from the floor.</summary>
    public void Jump()
    {
        if (IsOnFloor())
        {
            Velocity = Velocity with { Y = (float)HoldLoad.JumpTakeoffSpeed(HeldMassKg, MassKg) };
            _jumpSound.PitchScale = (float)Feedback.JumpPitch(MassKg + HeldMassKg);   // heavy: low, light: high
            _jumpSound.Play();
        }
    }

    /// <summary>The mass of the held body (0 when nothing is held): it slows the walk and the jump (HoldLoad).</summary>
    public double HeldMassKg => Held is { } body && IsInstanceValid(body) ? body.Mass : 0;

    /// <summary><c>E</c>: hold the aimed movable body within reach, or drop the held one.</summary>
    public void ToggleHold()
    {
        if (Held is { } held)
        {
            PlayHoldSound(_releaseSound, held.Mass);
            Drop();
            return;
        }
        if (Aim(HoldReach) is not Body body)
            return;
        if (!HoldLoad.CanHold(body.Mass, MassKg))
        {
            _tooHeavySound.Play();                  // too heavy (> 2.5 × your mass): the failure sound
            TooHeavySounds++;
            return;
        }
        Held = body;
        PlayHoldSound(_grabSound, body.Mass);
        // A carried box never collides with its carrier: looking down, the hold point is inside the
        // player, and the box squeezed the player through a wall (user's play test).
        body.IgnoreWhileHeld(this);
    }

    /// <summary>Grab/release and "too heavy" sounds played so far (headless checks).</summary>
    public int HoldSounds { get; private set; }
    public int TooHeavySounds { get; private set; }

    private void PlayHoldSound(AudioStreamPlayer sound, float massKg)
    {
        sound.PitchScale = (float)Feedback.ImpactPitch(massKg);   // heavier: lower
        sound.Play();
        HoldSounds++;
    }

    /// <summary>Holding the rotate button: the mouse turns the held body (left/right around the vertical, up/down around the view's right axis).</summary>
    public void SpinHeld(double yawDegrees, double tiltDegrees)
    {
        if (Held is { } body && IsInstanceValid(body))
            body.Turn(yawDegrees, tiltDegrees, Eye.Basis.X);
    }

    /// <summary>Left button released: a quick tap only drops; otherwise throws with the charged energy.</summary>
    public void ReleaseCharge()
    {
        if (ChargeSeconds is not { } held)
            return;
        ChargeSeconds = null;
        if (ThrowCharge.IsTap(held))
            Drop();
        else
            Throw(ThrowCharge.EnergyJoules(held));
    }

    /// <summary>Throws the held body forward with this energy; speed √(2E/m), soft-capped at 15 m/s like the jump.</summary>
    public void Throw(double energyJoules = ThrowCharge.MaxEnergyJoules / 2)
    {
        ChargeSeconds = null;
        if (Held is not { } body)
            return;
        Drop();
        var speed = (float)Formulas.ThrowSpeed(energyJoules, body.Mass);
        body.LinearVelocity = -Eye.Basis.Z * speed;
        _throwSound.PitchScale = (float)Feedback.ThrowPitch(speed);
        _throwSound.Play();
        Throws++;
    }

    /// <summary>Throws and charge-bar sounds played so far (headless checks).</summary>
    public int Throws { get; private set; }
    public int ChargeTicks { get; private set; }
    public int ChargeRings { get; private set; }

    private void PlayCharge(ChargeSound sound, double fraction)
    {
        if (sound == ChargeSound.Tick)
        {
            _tickSound.PitchScale = (float)Feedback.TickPitch(fraction);
            _tickSound.Play();
            ChargeTicks++;
        }
        else if (sound == ChargeSound.Full)
        {
            _fullSound.Play();
            ChargeRings++;
        }
    }

    /// <summary>Lets go of the held body. It keeps ignoring the player until they no longer overlap.</summary>
    public void Drop()
    {
        if (Held is { } body && IsInstanceValid(body))
            body.IgnoreForAMoment(this);
        Held = null;
    }

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
        // A mass swap can make the held body too heavy for you: it slips out of your hands.
        if (!IsInstanceValid(body) || !body.IsInsideTree() || !HoldLoad.CanHold(body.Mass, MassKg))
        {
            Drop();
            return;
        }
        // In front of the body, never inside it: at least the player's radius + the object's half size + 0.1 m
        // away horizontally, and not below the feet.
        var target = Eye.Origin - Eye.Basis.Z * HoldDistance;
        var forward = (-Eye.Basis.Z with { Y = 0 }).Normalized();
        var half = body.Def.Size.ToGodot() / 2;
        var clearance = 0.3f + Mathf.Sqrt(half.X * half.X + half.Z * half.Z) + 0.1f;
        var flat = (target - GlobalPosition) with { Y = 0 };
        if (flat.Dot(forward) < clearance)
            target += forward * (clearance - flat.Dot(forward));
        target.Y = Mathf.Max(target.Y, GlobalPosition.Y + half.Y);
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
