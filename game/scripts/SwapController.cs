using Godot;
using Swapping.Game.Pieces;
using Swapping.GameLogic;
using Swapping.Levels;
using Swapping.Rules.Operations;

namespace Swapping.Game;

/// <summary>
/// The swap, end to end (SPEC-game, "How a swap is applied"). Every physics step it feeds the physics
/// back to the <c>rules</c> world, aims, measures the <see cref="SwapContext"/> and asks the rules for a
/// dry run (the crosshair). On the swap button it applies <see cref="SwapOperation"/> and writes each
/// <see cref="BodyUpdate"/> back to its body. It never decides legality itself.
/// </summary>
public partial class SwapController : Node
{
    public const float AimReach = 60f;

    private readonly SwapOperation _swap = new();
    private LevelRunner _runner = null!;
    private Player _player = null!;
    private double _now;
    private bool _swapRequested;

    public Body? Target { get; private set; }

    public Crosshair Crosshair { get; private set; } = new(CrosshairLook.Hidden, "");

    /// <summary>Seconds of play, the clock of the rules' cooldown.</summary>
    public double Now => _now;

    public void Setup(LevelRunner runner, Player player)
    {
        _runner = runner;
        _player = player;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // The click only asks; the swap runs inside the next physics step, so every body moves together.
        if (@event.IsActionPressed(Controls.Name(GameAction.Swap)))
            _swapRequested = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_runner?.Room is null)
            return;
        _now += delta;
        SyncWorld();
        UpdateAim();
        if (_swapRequested)
        {
            _swapRequested = false;
            TrySwap();
        }
    }

    /// <summary>Applies the swap with the aimed body, if the rules allow it. Returns null when nothing is aimed.</summary>
    public SwapResult? TrySwap()
    {
        if (Target is not { } target || !IsInstanceValid(target))
            return null;
        SyncWorld();
        var result = _swap.Apply(_runner.World, Request(target), _now);
        if (result.IsApplied)
            ApplyUpdates(result.Updates);
        UpdateAim();
        return result;
    }

    /// <summary>The physics changes the value of the variable in use (rules model): position and velocity of every body.</summary>
    private void SyncWorld()
    {
        var world = _runner.World;
        world.SyncFromPhysics(LevelValidator.PlayerId, _player.Center.ToLevel(), _player.Velocity.ToLevel());
        foreach (var body in _runner.Bodies)
            world.SyncFromPhysics(body.ContainerId, body.GlobalPosition.ToLevel(), body.LinearVelocity.ToLevel());
    }

    private void UpdateAim()
    {
        Target = _player.Aim(AimReach) as Body;
        var reason = Target is null ? null : _runner.World.Rules.Check(_runner.World, Request(Target), _now);
        Crosshair = CrosshairState.For(Target is not null, reason);
    }

    private SwapRequest Request(Body target) =>
        new(LevelValidator.PlayerId, target.ContainerId, SwapChoice.Property(_runner.World.Rules.UnlockedProperties))
        {
            Context = Measure(target),
        };

    /// <summary>Distance between centers, line of sight (ignoring both bodies) and the variable barriers.</summary>
    public SwapContext Measure(Body target)
    {
        var mine = _player.Center;
        var theirs = target.GlobalPosition;
        var eye = _player.Eye.Origin;
        var sight = PhysicsRayQueryParameters3D.Create(eye, theirs, Layers.World | Layers.Objects, [_player.GetRid(), target.GetRid()]);
        var blocked = _player.GetWorld3D().DirectSpaceState.IntersectRay(sight).Count > 0;

        var barriers = _runner.Barriers.Where(b => b.Def.Filter.BlocksVariables).Select(b => b.Def.Box).ToList();
        return new SwapContext(
            DistanceMeters: mine.DistanceTo(theirs),
            HasLineOfSight: !blocked,
            VariableBarrierBetween: barriers.Any(box => BarrierGeometry.Crosses(mine.ToLevel(), theirs.ToLevel(), box)),
            FirstInsideVariableBarrier: barriers.Any(box => BarrierGeometry.Inside(mine.ToLevel(), box)),
            SecondInsideVariableBarrier: barriers.Any(box => BarrierGeometry.Inside(theirs.ToLevel(), box)));
    }

    /// <summary>
    /// Writes the updates back. The physics server moves the player (a kinematic body) over the next step
    /// instead of at once, so for a moment a "ghost" of the player stays where it was. A box put there
    /// would start inside the ghost and be shot away (found in the user's play test), so the swapped
    /// bodies ignore the player for <see cref="Body.GhostSeconds"/>.
    /// </summary>
    private void ApplyUpdates(IReadOnlyList<BodyUpdate> updates)
    {
        var swapped = updates.Where(u => u.ContainerId != LevelValidator.PlayerId)
            .Select(u => _runner.Bodies.Single(b => b.ContainerId == u.ContainerId)).ToList();
        // Objects first, the player last, and a death only after the whole swap is written: a death rebuilds
        // the room, which frees the swapped bodies (user's play test: an error in the log).
        var died = false;
        foreach (var update in updates.OrderBy(u => u.ContainerId == LevelValidator.PlayerId))
        {
            var center = update.Position.ToGodot();
            if (update.ContainerId == LevelValidator.PlayerId)
            {
                // Task 7: a swap exchanges centers, so the feet may end up inside the floor or a ledge.
                var spot = MakeRoom.ForPlayer(_player, center, swapped);
                GD.Print($"[game] swap with {string.Join(",", swapped.Select(b => b.ContainerId))}: player from {_player.GlobalPosition} to center {center} → {spot.Outcome} {spot.Point}");
                LastMakeRoom = spot.Outcome;
                _player.PlaceAtCenter(spot.Point?.ToGodot() ?? center, update.Velocity.ToGodot(), update.MassKg);
                died = spot.Outcome == MakeRoomOutcome.Death;
                continue;
            }
            var body = swapped.Single(b => b.ContainerId == update.ContainerId);
            body.Mass = (float)update.MassKg;
            body.IgnoreForAMoment(_player);
            var room = MakeRoom.ForBody(body, _player, center);
            if (room.Outcome == MakeRoomOutcome.Pop)
                body.Teleport(body.Def.Position.ToGodot(), body.Def.Velocity.ToGodot()); // back to its start, keeping its mass variable
            else
                body.Teleport(room.Point!.Value.ToGodot(), update.Velocity.ToGodot());
        }
        if (died)
            PlayerDied?.Invoke();
    }

    /// <summary>What "make room" decided for the player in the last applied swap.</summary>
    public MakeRoomOutcome? LastMakeRoom { get; private set; }

    /// <summary>No free point after a swap: the player dies (the room restarts).</summary>
    public event Action? PlayerDied;
}
