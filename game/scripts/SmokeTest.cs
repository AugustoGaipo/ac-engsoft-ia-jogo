using Godot;
using Swapping.Game.Pieces;
using Swapping.GameLogic;
using Swapping.Levels;

namespace Swapping.Game;

/// <summary>
/// Scripted checks on the real game, run headless with <c>--smoke-test &lt;levelId&gt;</c> (SC 10).
/// Prints one line per check and returns the exit code: 0 when every check passed, 1 at the first failure.
/// Each Godot task adds its checks here.
/// </summary>
public partial class SmokeTest : Node
{
    private const float Tolerance = 0.01f;
    private int _passed;

    public async Task<int> Run(string levelId, LevelRunner runner, Main main)
    {
        try
        {
            var catalog = LevelCatalog.Load(ProjectSettings.GlobalizePath("res://levels"));
            Check("catalog loads without errors", catalog.Errors.Count == 0,
                string.Join("; ", catalog.Errors.Select(e => $"{e.Path}: {e.Code} {e.Message}")));
            Check($"level {levelId} is in the catalog", catalog.Levels.Any(l => l.Id == levelId),
                $"ids: {string.Join(", ", catalog.Levels.Select(l => l.Id))}");

            runner.LoadLevel(levelId);
            Check("the scenery scene is loaded", runner.GetChildren().Any(c => c.Name == "Level1" || c.SceneFilePath == runner.Level.Scene),
                runner.Level.Scene);

            await PhysicsFrames(2);
            Check("the editor-only preview removes itself in the game (no doubled pieces)",
                runner.FindChildren("Preview", owned: false).Count == 0, "a Preview node is still in the scene");

            foreach (var room in runner.Level.Rooms)
                await CheckRoomPieces(runner, room.Id);

            await CheckRebuild(runner);
            await CheckButtonDoorAndExitGrid(runner);
            await CheckPlayer(main);
            await CheckSwap(main);
            await CheckRunFlow(main);
            await CheckFullRun(main);
            await CheckSandbox(main);
            await CheckBarrierBays(main);
            await CheckFullRunLevel2(main);

            GD.Print($"[smoke] PASSED {_passed} checks");
            return 0;
        }
        catch (SmokeFailure failure)
        {
            GD.PrintErr($"[smoke] FAIL {failure.Message}");
            return 1;
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[smoke] FAIL unexpected {exception}");
            return 1;
        }
    }

    // ---- Task 3 (SC 2): every object and kill zone at the file's position, and resting on the scenery ----

    private async Task CheckRoomPieces(LevelRunner runner, string roomId)
    {
        runner.BuildRoom(roomId);
        var room = runner.Room;

        var bodies = runner.Bodies.ToDictionary(b => b.ContainerId);
        Check($"room {roomId}: one body per object", bodies.Count == room.Objects.Count && room.Objects.All(o => bodies.ContainsKey(o.Id)),
            $"{bodies.Count} bodies for {room.Objects.Count} objects");
        foreach (var obj in room.Objects)
        {
            var body = bodies[obj.Id];
            Check($"room {roomId}: {obj.Id} at the file position", body.Position.DistanceTo(obj.Position.ToGodot()) < Tolerance,
                $"{body.Position} vs {obj.Position}");
            Check($"room {roomId}: {obj.Id} mass, bounce and CCD",
                Mathf.IsEqualApprox(body.Mass, (float)obj.MassKg) && Mathf.IsEqualApprox(body.PhysicsMaterialOverride.Bounce, (float)obj.Bounce) && body.ContinuousCd,
                $"mass {body.Mass}, bounce {body.PhysicsMaterialOverride.Bounce}, ccd {body.ContinuousCd}");
            Check($"room {roomId}: {obj.Id} is in the rules world", runner.World.Containers.Any(c => c.Id == obj.Id), "missing container");
        }

        var zones = runner.KillZones.ToDictionary(z => z.Def.Id);
        Check($"room {roomId}: one kill zone per file entry", zones.Count == room.KillZones.Count, $"{zones.Count} for {room.KillZones.Count}");
        foreach (var def in room.KillZones)
        {
            var shape = (BoxShape3D)zones[def.Id].GetChild<CollisionShape3D>(0).Shape;
            Check($"room {roomId}: {def.Id} at the file box",
                zones[def.Id].Position.DistanceTo(def.Box.Center()) < Tolerance && shape.Size.DistanceTo(def.Box.Size()) < Tolerance,
                $"{zones[def.Id].Position} size {shape.Size}");
        }

        foreach (var def in room.Buttons)
        {
            var button = runner.Buttons.Single(b => b.Def.Id == def.Id);
            Check($"room {roomId}: {def.Id} at the file position", button.Position.DistanceTo(def.Position.ToGodot()) < Tolerance, $"{button.Position}");
        }
        foreach (var def in room.Doors)
        {
            var door = runner.Doors.Single(d => d.Def.Id == def.Id);
            Check($"room {roomId}: {def.Id} at the file box and closed", door.Position.DistanceTo(def.Box.Center()) < Tolerance && !door.IsOpen, $"{door.Position}");
        }
        foreach (var def in room.Barriers)
        {
            var barrier = runner.Barriers.Single(b => b.Def.Id == def.Id);
            Check($"room {roomId}: {def.Id} at the file box", barrier.Position.DistanceTo(def.Box.Center()) < Tolerance, $"{barrier.Position}");
            var look = (ShaderMaterial)((MeshInstance3D)barrier.GetChild(0)).Mesh.SurfaceGetMaterial(0);
            var baseColor = (Vector3)look.GetShaderParameter("base_color");
            var stripeColor = (Vector3)look.GetShaderParameter("stripe_color");
            Check($"room {roomId}: {def.Id} has moving stripes darker than its color (not black), one side only",
                stripeColor.X < baseColor.X && stripeColor.Y < baseColor.Y && stripeColor.Z < baseColor.Z && stripeColor.Length() > 0.05f && look.Shader.Code.Contains("cull_back"),
                $"base {baseColor}, stripes {stripeColor}");
            if (def.IsExitGrid)
                Check($"room {roomId}: {def.Id} blocks objects only", barrier.CollisionLayer == Layers.BarrierObjects, $"layer {barrier.CollisionLayer}");
        }
        Check($"room {roomId}: exit at the file box", runner.Exit.Position.DistanceTo(room.Exit.Center()) < Tolerance, $"{runner.Exit.Position}");

        // One second of physics: objects that start at rest stay where the file put them (they stand on
        // the floor or the ledge, so the scenery and the file agree).
        await PhysicsFrames(60);
        foreach (var obj in room.Objects.Where(o => o.Velocity == Swapping.Rules.Model.Vec3.Zero))
        {
            var body = bodies[obj.Id];
            Check($"room {roomId}: {obj.Id} rests on the scenery", body.Position.DistanceTo(obj.Position.ToGodot()) < 0.05f,
                $"moved to {body.Position}");
        }
    }

    private async Task CheckRebuild(LevelRunner runner)
    {
        runner.BuildRoom("1.3");
        var firstWorld = runner.World;
        runner.BuildRoom("1.3");
        await PhysicsFrames(1);
        Check("building a room twice leaves one set of pieces", runner.PieceCount == runner.ExpectedPieceCount,
            $"{runner.PieceCount} pieces, expected {runner.ExpectedPieceCount}");
        Check("building a room again makes a new world", !ReferenceEquals(firstWorld, runner.World), "same world");
    }

    // ---- Task 4: room 1.1 — the box on the button opens the door; the exit grid stops the box ----

    private async Task CheckButtonDoorAndExitGrid(LevelRunner runner)
    {
        runner.BuildRoom("1.1");
        await PhysicsFrames(5);
        var box = runner.Bodies.Single(b => b.ContainerId == "box-1-1");
        var button = runner.Buttons.Single();
        var door = runner.Doors.Single();
        Check("room 1.1: the door starts closed", !door.IsOpen && !button.IsPressed, "open at start");

        var onButton = button.Position + new Vector3(0, (float)button.Def.Size.Y + 0.5f, 0);
        Teleport(box, onButton);
        await PhysicsFrames(30);
        Check("room 1.1: the box on the button presses it and opens the door", button.IsPressed && door.IsOpen,
            $"pressed {button.IsPressed}, open {door.IsOpen}, box at {box.Position}");

        Teleport(box, new Vector3(4, 0.5f, 3));
        await PhysicsFrames(30);
        Check("room 1.1: taking the box away closes the door", !button.IsPressed && !door.IsOpen, $"pressed {button.IsPressed}");

        // Pushed towards the exit grid (x = 11.5): a 1 m box stops with its center at x ≈ 11.0.
        Teleport(box, new Vector3(10.8f, 0.5f, 5), new Vector3(4, 0, 0));
        await PhysicsFrames(60);
        Check("room 1.1: the exit grid stops the box", box.Position.X is > 10.95f and < 11.05f, $"box at {box.Position}");
    }

    // ---- Task 5: the player spawns standing, jumps at the rules' speed, holds a box, crosses the grid ----

    private async Task CheckPlayer(Main main)
    {
        var player = main.Player;
        main.ShowRoom("1.1");
        await PhysicsFrames(30);
        var spawn = main.GetNode<LevelRunner>("LevelRunner").Room.Spawn;
        Check("player: spawns standing at the room spawn", player.IsOnFloor() && player.GlobalPosition.DistanceTo(spawn.Position.ToGodot()) < 0.05f,
            $"at {player.GlobalPosition}, on floor {player.IsOnFloor()}");
        Check("player: faces the spawn yaw", Mathf.IsEqualApprox(Mathf.RadToDeg(player.Rotation.Y), (float)spawn.YawDegrees, 0.5f),
            $"yaw {Mathf.RadToDeg(player.Rotation.Y)}");

        player.Jump();
        var expected = (float)Swapping.Rules.Physics.Formulas.JumpTakeoffSpeed(player.MassKg);
        Check("player: take-off speed = JumpTakeoffSpeed(70)", Mathf.Abs(player.Velocity.Y - expected) <= expected * 0.05f,
            $"{player.Velocity.Y} vs {expected}");
        await PhysicsFrames(90);
        Check("player: lands again after the jump", player.IsOnFloor() && Mathf.Abs(player.GlobalPosition.Y) < 0.05f, $"y {player.GlobalPosition.Y}");

        // Stand 1.8 m in front of the box of room 1.1 (box at x 4, z 3), looking at it.
        var box = main.GetNode<LevelRunner>("LevelRunner").Bodies.Single();
        player.GlobalPosition = new Vector3(4, 0, 4.8f);
        player.Look(0, -0.6f);                       // looking along -Z and down, towards the box
        await PhysicsFrames(5);
        player.ToggleHold();
        Check("player: E holds the aimed box", player.Held == box, $"held {player.Held?.Name ?? "nothing"}");
        await PhysicsFrames(40);
        var holdPoint = player.Eye.Origin - player.Eye.Basis.Z * Player.HoldDistance;
        Check("player: the held box follows the hold point", box.GlobalPosition.DistanceTo(holdPoint) < 0.3f,
            $"box {box.GlobalPosition}, point {holdPoint}");
        player.ToggleHold();
        await PhysicsFrames(60);
        Check("player: E again drops it to the floor", player.Held is null && Mathf.Abs(box.GlobalPosition.Y - 0.5f) < 0.05f, $"box {box.GlobalPosition}");

        // The exit grid of room 1.1 lets the player through (it blocks objects only).
        player.GlobalPosition = new Vector3(11, 0, 5);
        player.Look(-90, 0);                         // facing +X
        player.ScriptedMove = new Vector2(0, 1);
        await PhysicsFrames(60);
        player.ScriptedMove = null;
        Check("player: walks through the exit grid", player.GlobalPosition.X > 12.3f, $"x {player.GlobalPosition.X}");
        Check("exit: crossing the exit of 1.1 builds room 1.2 with its box",
            main.Runner.Room.Id == "1.2" && main.Runner.Bodies.Any(b => b.ContainerId == "box-1-2"),
            $"room {main.Runner.Room.Id}, bodies {string.Join(",", main.Runner.Bodies.Select(b => b.ContainerId))}");

        // Play test 2026-11-03: the player's weight counts on the buttons of level 1 ("detects": "any").
        main.ShowRoom("1.1");
        var runner = main.GetNode<LevelRunner>("LevelRunner");
        player.GlobalPosition = runner.Buttons.Single().Position + new Vector3(0, 0.2f, 0);
        player.ResetPhysicsInterpolation();
        await PhysicsFrames(30);
        Check("player: standing on the button presses it and opens the door", runner.Buttons.Single().IsPressed && runner.Doors.Single().IsOpen,
            $"pressed {runner.Buttons.Single().IsPressed}");
        Check("player: the camera follows the eye", player.Camera.GlobalPosition.DistanceTo(player.Eye.Origin) < 0.1f,
            $"camera {player.Camera.GlobalPosition}, eye {player.Eye.Origin}");

        // Play test 2026-11-03: pushing has weight (PushImpulse: 200 N against friction).
        foreach (var (mass, minMove, maxMove) in new[] { (10f, 1.0f, 2.4f), (300f, -1f, 0.05f) })
        {
            main.ShowRoom("1.1");
            var pushed = main.Runner.Bodies.Single();
            pushed.Mass = mass;
            player.GlobalPosition = new Vector3(2.6f, 0, 3);   // 0.6 m before the box at x = 4 (faces touch at x 3.2/3.5)
            player.ResetPhysicsInterpolation();
            player.Look(-90, 0);                                // facing +X
            await PhysicsFrames(5);
            var start = pushed.GlobalPosition.X;
            player.ScriptedMove = new Vector2(0, 1);
            await PhysicsFrames(60);
            player.ScriptedMove = null;
            var moved = pushed.GlobalPosition.X - start;
            Check($"player: walking 1 s into a {mass} kg box moves it {minMove}–{maxMove} m", moved > minMove && moved < maxMove,
                $"moved {moved:0.00} m, player push speed {player.PushSpeed}");
        }
    }

    // ---- Task 6 (SC 3, 5): crosshair from the rules' dry run, the swap applied and written back ----

    private async Task CheckSwap(Main main)
    {
        var player = main.Player;
        var swaps = main.Swaps;

        main.ShowRoom("1.2");
        await PhysicsFrames(10);
        var box = main.Runner.Bodies.Single();
        await LookAt(player, box.GlobalPosition);
        Check("swap: room 1.2, the box within 8 m → white crosshair",
            swaps.Target == box && swaps.Crosshair.Look == Swapping.GameLogic.CrosshairLook.Ready,
            $"target {swaps.Target?.Name ?? "none"}, {swaps.Crosshair}, distance {swaps.Measure(box).DistanceMeters:0.00}");
        Check("swap: the HUD shows the aimed box's numbers", main.Hud.TargetText.StartsWith("10 kg"), main.Hud.TargetText);
        Check("swap: the HUD shows the player's numbers", main.Hud.PlayerText.Contains("70 kg"), main.Hud.PlayerText);
        await PhysicsFrames(1);
        Check("crosshair: on an object it is the \"+\"", main.Hud.CrosshairText == "+", main.Hud.CrosshairText);
        player.Look(-90, 1.2f);                                 // up at the sky
        await PhysicsFrames(3);
        Check("crosshair (user's request): with nothing aimed there is always a small dot",
            swaps.Crosshair.Look == Swapping.GameLogic.CrosshairLook.Idle && main.Hud.CrosshairText == "•", main.Hud.CrosshairText);

        player.GlobalPosition = new Vector3(12.6f, 0, 5);
        player.ResetPhysicsInterpolation();
        await PhysicsFrames(5);
        await LookAt(player, box.GlobalPosition);
        Check("swap: beyond 8 m → red \"fora de alcance\"",
            swaps.Crosshair == new Swapping.GameLogic.Crosshair(Swapping.GameLogic.CrosshairLook.Refused, "fora de alcance"),
            $"{swaps.Crosshair}, distance {swaps.Measure(box).DistanceMeters:0.00}");

        main.ShowRoom("1.2");
        await PhysicsFrames(10);
        box = main.Runner.Bodies.Single();
        var playerCenter = player.Center;
        await LookAt(player, box.GlobalPosition);
        var result = swaps.TrySwap();
        Check("swap: the position swap is applied", result is { IsApplied: true }, $"{result?.Reason}");
        Check("cooldown ring: empty right after the swap", swaps.CooldownProgress < 0.1, $"{swaps.CooldownProgress:0.00}");
        Check("make room (SC 4): after the swap of room 1.2 the feet are on the floor, not inside it",
            swaps.LastMakeRoom == Swapping.GameLogic.MakeRoomOutcome.Moved && player.GlobalPosition.Y is >= -0.01f and < 0.26f && player.GlobalPosition.X > 20,
            $"{swaps.LastMakeRoom}, feet {player.GlobalPosition}");
        Check("swap: one swap counted and the invariant holds", main.Runner.World.MoveCount == 1 && main.Runner.World.CheckInvariant(),
            $"moves {main.Runner.World.MoveCount}");
        Check("swap: the box is where the player was", box.GlobalPosition.DistanceTo(playerCenter) < 0.05f, $"box {box.GlobalPosition}, was {playerCenter}");
        var probe = new PhysicsShapeQueryParameters3D
        {
            Shape = new BoxShape3D { Size = new Vector3(1, 1, 1) },
            Transform = box.GlobalTransform,
            CollisionMask = uint.MaxValue,
            Exclude = [box.GetRid()],
        };
        var overlaps = player.GetWorld3D().DirectSpaceState.IntersectShape(probe);
        GD.Print($"[smoke] info overlaps right after the swap: {string.Join(", ", overlaps.Select(o => o["collider"].As<Node>()?.Name.ToString() ?? "?"))}; box basis {box.GlobalBasis}");
        for (var i = 0; i < 6; i++)
        {
            await PhysicsFrames(10);
            GD.Print($"[smoke] info +{(i + 1) * 10} frames: box {box.GlobalPosition} v {box.LinearVelocity}, player {player.GlobalPosition}");
        }
        Check("make room (SC 4): the player then stands on the floor across the pit", player.IsOnFloor() && Mathf.Abs(player.GlobalPosition.Y) < 0.02f && player.GlobalPosition.X > 20, $"feet {player.GlobalPosition}");
        Check("cooldown ring: full again one second later", swaps.CooldownProgress >= 1 && main.Hud.RingProgress >= 1, $"{swaps.CooldownProgress:0.00}");
        Check("swap: the box is not shot away (it settles under where the player was)",
            new Vector2(box.GlobalPosition.X - playerCenter.X, box.GlobalPosition.Z - playerCenter.Z).Length() < 0.05f && box.LinearVelocity.Length() < 0.1f,
            $"box {box.GlobalPosition} v {box.LinearVelocity}, player was {playerCenter}");

        // SC 5: the locked box of 1.4 refuses the swap but can be held and thrown.
        main.ShowRoom("1.4");
        await PhysicsFrames(10);
        var locked = main.Runner.Bodies.Single(b => b.Def.Locked);
        player.GlobalPosition = new Vector3(46, 0, 6.3f);
        player.ResetPhysicsInterpolation();
        await PhysicsFrames(5);
        await LookAt(player, locked.GlobalPosition);
        Check("swap: the locked box → red \"travado\"",
            swaps.Target == locked && swaps.Crosshair.Reason == "travado", $"target {swaps.Target?.Name ?? "none"}, {swaps.Crosshair}");
        Check("swap: the locked box refuses the swap", swaps.TrySwap() is { IsApplied: false, Reason: RejectReasonLocked }, "applied");
        var other = main.Runner.Bodies.Single(b => !b.Def.Locked);
        Check("outline: the aimed locked box is gray and the others show nothing",
            locked.OutlineShown == Swapping.GameLogic.Outline.Gray && other.OutlineShown is null, $"{locked.OutlineShown} / {other.OutlineShown}");
        await LookAt(player, other.GlobalPosition);
        Check("outline: the aimed box that accepts position is white and the locked one shows nothing",
            other.OutlineShown == Swapping.GameLogic.Outline.White && locked.OutlineShown is null, $"{other.OutlineShown} / {locked.OutlineShown}");
        await LookAt(player, locked.GlobalPosition);
        player.ToggleHold();
        Check("swap: the locked box can still be held", player.Held == locked, $"held {player.Held?.Name ?? "nothing"}");
        await PhysicsFrames(10);
        player.Throw();
        await PhysicsFrames(2);
        Check("swap: and thrown", player.Held is null && locked.LinearVelocity.Length() > 1, $"speed {locked.LinearVelocity.Length()}");

        // The throw bar (user's request, 2026-11-03): a quick tap only drops; holding 1.5 s throws with half of the full energy.
        var left = (bool down) => player._UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = down });
        await PhysicsFrames(60);
        locked.Teleport(player.Eye.Origin - player.Eye.Basis.Z * 1.5f, Vector3.Zero);
        await PhysicsFrames(2);
        await LookAt(player, locked.GlobalPosition);
        player.ToggleHold();
        await PhysicsFrames(10);
        left(true);
        await PhysicsFrames(2);
        left(false);
        await PhysicsFrames(2);
        Check("throw bar: a quick tap only drops the box", player.Held is null && locked.LinearVelocity.Length() < 2,
            $"held {player.Held?.Name}, speed {locked.LinearVelocity.Length()}");

        await PhysicsFrames(60);
        locked.Teleport(player.Eye.Origin - player.Eye.Basis.Z * 1.5f, Vector3.Zero);
        await PhysicsFrames(2);
        await LookAt(player, locked.GlobalPosition);
        player.ToggleHold();
        await PhysicsFrames(10);
        var (ticks, rings, throws, knocks) = (player.ChargeTicks, player.ChargeRings, player.Throws, locked.Impacts);
        left(true);
        await PhysicsFrames(90);                                    // 1.5 s at 60 Hz
        var bar = player.ChargeFraction;
        left(false);
        await PhysicsFrames(1);
        Check("sounds: the half bar ticked 4–5 times (each 10 %, 50 % on the edge), no ring yet, and the throw whooshed",
            player.ChargeTicks - ticks is 4 or 5 && player.ChargeRings == rings && player.Throws == throws + 1,
            $"ticks {player.ChargeTicks - ticks}, rings {player.ChargeRings - rings}, throws {player.Throws - throws}");
        var expected = (float)Swapping.Rules.Physics.Formulas.ThrowSpeed(Swapping.GameLogic.ThrowCharge.EnergyJoules(1.5), locked.Mass);
        Check("throw bar: half full after 1.5 s", bar is { } b && Mathf.Abs((float)b - 0.5f) < 0.03f, $"bar {bar}");
        Check("throw bar: released, the box flies at √(2·E ÷ m) with half the energy", player.Held is null && Mathf.Abs(locked.LinearVelocity.Length() - expected) < 0.6f,
            $"speed {locked.LinearVelocity.Length()} expected {expected}");
        await PhysicsFrames(90);
        Check("sounds: the thrown box knocks when it lands", locked.Impacts > knocks, $"knocks {locked.Impacts - knocks}");

        // A full bar rings once.
        locked.Teleport(player.Eye.Origin - player.Eye.Basis.Z * 1.5f, Vector3.Zero);
        await PhysicsFrames(2);
        await LookAt(player, locked.GlobalPosition);
        player.ToggleHold();
        await PhysicsFrames(10);
        left(true);
        await PhysicsFrames(200);                                   // past the 3 s of a full bar
        left(false);
        await PhysicsFrames(1);
        Check("sounds: a full bar rings once", player.ChargeRings == rings + 1, $"rings {player.ChargeRings - rings}");
    }

    private const Swapping.Rules.Operations.RejectReason RejectReasonLocked = Swapping.Rules.Operations.RejectReason.Locked;

    // ---- Task 8 (SC 6, 7): deaths, restarts, pause, end of the run ----

    private async Task CheckRunFlow(Main main)
    {
        var flow = main.Flow;
        var player = main.Player;
        flow.StartLevel();
        await PhysicsFrames(5);
        Check("run: a new run starts in room 1.1 at zero", main.Runner.Room.Id == "1.1" && flow.Run.Swaps == 0 && flow.Run.Deaths == 0,
            $"room {main.Runner.Room.Id}, swaps {flow.Run.Swaps}");

        // Room 1.2: one swap, then into the pit.
        main.ShowRoom("1.2");
        await PhysicsFrames(10);
        var box = main.Runner.Bodies.Single();
        await LookAt(player, box.GlobalPosition);
        main.Swaps.TrySwap();
        await PhysicsFrames(10);
        var timeBefore = flow.Run.TimeMs;
        player.GlobalPosition = new Vector3(17.5f, -1.5f, 5);   // inside the kill zone of the pit
        player.ResetPhysicsInterpolation();
        for (var i = 0; i < 60 && !flow.IsDying; i++)         // falls into the kill zone
            await PhysicsFrames(1);
        for (var i = 0; i < 300 && flow.IsDying; i++)          // the death sequence (RunFlow.Die)
            await PhysicsFrames(1);
        var spawn = main.Runner.Room.Spawn.Position.ToGodot();
        box = main.Runner.Bodies.Single();
        Check("run (SC 6): the pit kills; the room is rebuilt and the player is at the spawn",
            !flow.IsDying && player.GlobalPosition.DistanceTo(spawn) < 0.1f && box.GlobalPosition.DistanceTo(box.Def.Position.ToGodot()) < 0.05f,
            $"player {player.GlobalPosition}, box {box.GlobalPosition}");
        Check("run (SC 6): time and swaps are kept, 1 death and 1 room restart",
            flow.Run.Swaps == 1 && flow.Run.TimeMs > timeBefore && flow.Run.Deaths == 1 && flow.Run.RoomRestarts == 1,
            $"swaps {flow.Run.Swaps}, time {flow.Run.TimeMs} (was {timeBefore}), deaths {flow.Run.Deaths}, restarts {flow.Run.RoomRestarts}");

        // A 9 m fall (≈ 13 m/s on landing) kills; a jump does not.
        player.GlobalPosition = new Vector3(13.5f, 9, 5);
        player.ResetPhysicsInterpolation();
        for (var i = 0; i < 150 && !flow.IsDying; i++)        // the fall, then the death sequence
            await PhysicsFrames(1);
        for (var i = 0; i < 300 && flow.IsDying; i++)
            await PhysicsFrames(1);
        Check("run (G3): landing after a 9 m fall kills", flow.Run.Deaths == 2, $"deaths {flow.Run.Deaths}");

        // The pause stops the time (the physics stops with the tree).
        flow.SetPaused(true);
        var paused = flow.Run.TimeMs;
        for (var i = 0; i < 20; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Check("run: the time does not advance while the pause menu is open", flow.IsPaused && flow.Run.TimeMs == paused,
            $"{paused} → {flow.Run.TimeMs}");
        flow.SetPaused(false);

        // SC 7: restart level from room 1.3.
        main.ShowRoom("1.3");
        await PhysicsFrames(5);
        flow.RestartLevel();
        await PhysicsFrames(2);
        Check("run (SC 7): restart level → room 1.1 with time and swaps at zero",
            main.Runner.Room.Id == "1.1" && flow.Run.Swaps == 0 && flow.Run.Deaths == 0 && flow.Run.TimeMs < 100,
            $"room {main.Runner.Room.Id}, swaps {flow.Run.Swaps}, time {flow.Run.TimeMs}");

        // The exit of the last room ends the run.
        main.ShowRoom("1.4");
        await PhysicsFrames(5);
        player.GlobalPosition = new Vector3(47.7f, 0, 5);
        player.ResetPhysicsInterpolation();
        await PhysicsFrames(5);
        Check("run: crossing the exit of 1.4 ends the run with the API fields and the end screen",
            flow.Result is { LevelId: "level-1" } && flow.EndScreenVisible && flow.EndText.Contains("Trocas"),
            $"result {flow.Result}, end screen {flow.EndScreenVisible}");
        Check("run: the HUD shows time and swaps", main.Hud.RunText.Contains("Trocas") && main.Hud.RunText.Contains(':'), main.Hud.RunText);
        flow.RestartLevel();

        // The user's log (2026-11-03): in room 1.3, a swap with the ball in the air at the ledge edge, then
        // the player fell through the floor (below y = -10).
        main.ShowRoom("1.3");
        await PhysicsFrames(5);
        var ball = main.Runner.Bodies.Single(b => b.ContainerId == "ball-1-3");
        player.GlobalPosition = new Vector3(29.70f, 0, 3.96f);
        player.ResetPhysicsInterpolation();
        await PhysicsFrames(5);
        ball.Teleport(new Vector3(30.43f, 2.89f, 3.88f), Vector3.Zero);
        await LookAt(player, ball.GlobalPosition);
        var deathsBefore = flow.Run.Deaths;
        var swap = main.Swaps.TrySwap();
        for (var i = 0; i < 6; i++)
        {
            await PhysicsFrames(10);
        }
        Check("run: the user's case — a swap with the ball at the ledge edge does not drop the player through the floor",
            swap is { IsApplied: true } && flow.Run.Deaths == deathsBefore && player.GlobalPosition.Y > -0.5f,
            $"{swap?.Reason}, deaths {flow.Run.Deaths - deathsBefore}, player {player.GlobalPosition}");

        // The user's real case: "I was squeezed by the box I was carrying and went through the wall".
        // Player against the start wall of room 1.1, holding the box, looking down: the hold point is inside the player.
        main.ShowRoom("1.1");
        await PhysicsFrames(5);
        var carried = main.Runner.Bodies.Single();
        player.GlobalPosition = new Vector3(0.35f, 0, 5);
        player.ResetPhysicsInterpolation();
        carried.Teleport(new Vector3(1.6f, 0.5f, 5), Vector3.Zero);
        await PhysicsFrames(5);
        await LookAt(player, carried.GlobalPosition);
        player.ToggleHold();
        Check("hold: the box next to the wall is held", player.Held == carried, $"held {player.Held?.Name ?? "nothing"}");
        player.Look(90, -1.45f);                               // facing the wall (-X), looking almost straight down
        for (var i = 0; i < 90; i++)
        {
            if (i % 30 == 0)
                player.Look(90 + i, -1.45f);                   // turning while looking down
            await PhysicsFrames(1);
        }
        Check("hold (user's case): the carried box never pushes the player through the wall",
            player.GlobalPosition.X > 0.25f && player.GlobalPosition.Y > -0.1f,
            $"player {player.GlobalPosition}, box {carried.GlobalPosition}");
        player.Drop();
        await PhysicsFrames(30);
        Check("hold: dropping the box on top of the player does not push the player through the wall either",
            player.GlobalPosition.X > 0.25f && player.GlobalPosition.Y > -0.1f, $"player {player.GlobalPosition}, box {carried.GlobalPosition}");

        // The user left the map: rooms have no ceiling. A ball over the wall pops back; a player outside dies.
        main.ShowRoom("1.3");
        await PhysicsFrames(5);
        ball = main.Runner.Bodies.Single(b => b.ContainerId == "ball-1-3");
        ball.Teleport(new Vector3(30, 7, -2), Vector3.Zero);   // above and beyond the south wall
        await PhysicsFrames(3);
        Check("run: a ball out of its room (over a wall) pops back to its start",
            ball.GlobalPosition.DistanceTo(ball.Def.Position.ToGodot()) < 0.3f, $"ball {ball.GlobalPosition}");
        deathsBefore = flow.Run.Deaths;
        player.GlobalPosition = new Vector3(30, 0, -3);
        player.ResetPhysicsInterpolation();
        await PhysicsFrames(40);
        Check("run: the player outside the map dies at once (\"saiu do mapa\")", flow.Run.Deaths == deathsBefore + 1,
            $"deaths {flow.Run.Deaths - deathsBefore}, player {player.GlobalPosition}");
        flow.RestartLevel();
    }

    // ---- Task 10 (SC 8, 10): level 1 played from the first room to the end screen, the planned way ----

    private async Task CheckFullRun(Main main)
    {
        var flow = main.Flow;
        var player = main.Player;
        var swaps = main.Swaps;
        flow.StartLevel();
        // Task 14 (SC 15): level 1 unlocks only position, so keys 2 and 3 do nothing.
        PressKey(main.Swaps, Key.Key2);
        PressKey(main.Swaps, Key.Key3);
        Check("keys (SC 15): in level 1 keys 2 and 3 are refused, the choice stays position",
            main.Swaps.Choice == Swapping.Rules.Model.PropertyKind.Position, $"{main.Swaps.Choice.Name}");
        Check("keys: the HUD shows the chosen property", main.Hud.RunText.Contains("Posição"), main.Hud.RunText);
        await PhysicsFrames(10);

        // 1.1 — carry the box onto the button, walk through the door and the exit grid.
        var box = main.Runner.Bodies.Single();
        await WalkTo(player, 4, 4.7f);
        await LookAt(player, box.GlobalPosition);
        player.ToggleHold();
        Check("full run 1.1: the box is picked up", player.Held == box, $"held {player.Held?.Name ?? "nothing"}");
        // Task 14: holding the rotate button, the mouse turns the held box and the view stays still.
        var yawBefore = box.GlobalBasis.GetEuler().Y;
        var viewBefore = player.Rotation.Y;
        Input.ActionPress(Controls.Name(GameAction.HoldToRotate));
        player._UnhandledInput(new InputEventMouseMotion { Relative = new Vector2(-100, 0) });
        Input.ActionRelease(Controls.Name(GameAction.HoldToRotate));
        var turned = Mathf.RadToDeg(Mathf.AngleDifference(yawBefore, box.GlobalBasis.GetEuler().Y));
        var expected = (float)HeldSpin.Degrees(-100, Player.MouseSensitivity);
        Check("full run 1.1: rotate button + mouse turns the held box", Mathf.Abs(turned - expected) < 0.5f && player.Held == box, $"turned {turned}°, expected {expected}°");
        Check("full run 1.1: the view does not turn while rotating the box", Mathf.IsEqualApprox(player.Rotation.Y, viewBefore), $"{player.Rotation.Y} vs {viewBefore}");
        // Up/down and diagonals: the mouse up tilts the box about the view's right axis (the view still stays put).
        var eyeBefore = player.Eye.Basis.Z;
        var upBefore = box.GlobalBasis.Y;
        Input.ActionPress(Controls.Name(GameAction.HoldToRotate));
        player._UnhandledInput(new InputEventMouseMotion { Relative = new Vector2(0, -100) });
        var tilted = Mathf.RadToDeg(upBefore.AngleTo(box.GlobalBasis.Y));
        var expectedTilt = (float)HeldSpin.TiltDegrees(-100, Player.MouseSensitivity);
        Check("full run 1.1: rotate button + mouse up/down tilts the held box", Mathf.Abs(tilted - expectedTilt) < 0.5f, $"tilted {tilted}°, expected {expectedTilt}°");
        var beforeDiagonal = box.GlobalBasis;
        player._UnhandledInput(new InputEventMouseMotion { Relative = new Vector2(-60, 60) });
        Input.ActionRelease(Controls.Name(GameAction.HoldToRotate));
        var diagonalYaw = Mathf.Abs(Mathf.RadToDeg(Mathf.AngleDifference(beforeDiagonal.GetEuler().Y, box.GlobalBasis.GetEuler().Y)));
        var diagonalUp = Mathf.RadToDeg(beforeDiagonal.Y.AngleTo(box.GlobalBasis.Y));
        Check("full run 1.1: a diagonal motion turns the box on both axes", diagonalYaw > 3 && diagonalUp > 3, $"yaw {diagonalYaw}°, up {diagonalUp}°");
        Check("full run 1.1: the view still did not move", player.Eye.Basis.Z.DistanceTo(eyeBefore) < 0.001f, $"{player.Eye.Basis.Z} vs {eyeBefore}");
        await WalkTo(player, 6.3f, 5);
        player.Look(-90, -0.35f);                                // facing +X, a bit down: the box hangs over the button
        await PhysicsFrames(30);
        player.Drop();
        await PhysicsFrames(60);
        Check("full run 1.1: the box on the button opens the door", main.Runner.Doors.Single().IsOpen,
            $"box {box.GlobalPosition}, pressed {main.Runner.Buttons.Single().IsPressed}");
        await WalkTo(player, 8, 7.2f);                          // around the button (the box stays on it)
        await WalkTo(player, 9.6f, 5);
        await WalkTo(player, 12.6f, 5);
        Check("full run 1.1: through the door and the exit grid into room 1.2", main.Runner.Room.Id == "1.2", $"room {main.Runner.Room.Id}, at {player.GlobalPosition}");

        // 1.2 — the first swap, across the pit.
        await WalkTo(player, 13.5f, 5);
        box = main.Runner.Bodies.Single();
        await LookAt(player, box.GlobalPosition);
        Check("full run 1.2: the swap across the pit", swaps.TrySwap() is { IsApplied: true }, $"{swaps.Crosshair}");
        await PhysicsFrames(20);
        await WalkTo(player, 24.6f, 5);
        Check("full run 1.2: into room 1.3", main.Runner.Room.Id == "1.3", $"room {main.Runner.Room.Id}, at {player.GlobalPosition}");

        // 1.3 (SC 8) — swap with the ball (up the ledge), stand on the button, swap with the box below.
        await WalkTo(player, 25.2f, 5);
        var ball = main.Runner.Bodies.Single(b => b.ContainerId == "ball-1-3");
        box = main.Runner.Bodies.Single(b => b.ContainerId == "box-1-3");
        await LookAt(player, ball.GlobalPosition);
        Check("full run 1.3: swap with the ball on the ledge", swaps.TrySwap() is { IsApplied: true }, $"{swaps.Crosshair}");
        await PhysicsFrames(70);                                // cooldown and landing on the ledge
        await WalkTo(player, 32.75f, 5);
        await LookAt(player, box.GlobalPosition);
        Check("full run 1.3: swap with the box from the button", swaps.TrySwap() is { IsApplied: true }, $"{swaps.Crosshair}");
        await PhysicsFrames(60);
        Check("full run 1.3 (SC 8): the box lands on the high button and the door opens", main.Runner.Doors.Single().IsOpen,
            $"box {box.GlobalPosition}, player {player.GlobalPosition}");
        await WalkTo(player, 29, 8);                            // along the ground corridor beside the ledge
        await WalkTo(player, 34.6f, 8);
        await WalkTo(player, 36.8f, 5);
        Check("full run 1.3: through the door into room 1.4", main.Runner.Room.Id == "1.4", $"room {main.Runner.Room.Id}, at {player.GlobalPosition}");

        // 1.4 — the locked box refuses; swap with the other one, then the exit ends the level.
        await WalkTo(player, 39.6f, 7);
        var free = main.Runner.Bodies.Single(b => !b.Def.Locked);
        await LookAt(player, free.GlobalPosition);
        Check("full run 1.4: swap with the box that accepts position", swaps.TrySwap() is { IsApplied: true }, $"{swaps.Crosshair}");
        await PhysicsFrames(20);
        await WalkTo(player, 47.9f, 5);
        await PhysicsFrames(5);

        Check("full run (SC 10): the level ends with the end screen", flow.Result is not null && flow.EndScreenVisible,
            $"room {main.Runner.Room.Id}, at {player.GlobalPosition}");
        var result = flow.Result!;
        Check("full run (SC 10): 4 swaps, no death, time above the API floor (1 000 ms × 4 rooms)",
            result is { LevelId: "level-1", Swaps: 4, Deaths: 0, RoomRestarts: 0 } && result.TimeMs >= 4_000,
            $"{result}");
        GD.Print($"[smoke] info full run of level 1: {result}");

        // Task 15 (SC 17): finishing level 1 opens level 2 and writes the progress file; the end screen has its buttons.
        Check("progress (SC 17): finishing level 1 records it and opens level 2",
            main.Progress.IsFinished("level-1") && main.Progress.IsUnlocked("level-2"), "level 1 not recorded");
        var saved = Godot.FileAccess.FileExists(main.ProgressPath) ? Godot.FileAccess.GetFileAsString(main.ProgressPath) : "";
        Check("progress (SC 17): the progress file is written with level 1", saved.Contains("level-1") && saved.Contains("\"timeMs\""), saved);
        Check("end screen: has the Menu button", Menu.FindButton(flow.EndPanel, "Menu") is not null, "missing");
        Check("end screen: has Próximo nível, hidden here because level 2 has no scene yet or shown when it has one",
            Menu.FindButton(flow.EndPanel, "Próximo nível") is not null
            && flow.NextButtonVisible == (main.NextPlayable("level-1") is not null), $"visible {flow.NextButtonVisible}");
        var realHasScene = main.HasScene;
        main.HasScene = _ => true;
        Check("end screen: Próximo nível leads to level 2 when its scene exists", main.NextPlayable("level-1") == "level-2" && main.NextPlayable("level-3") is null,
            $"{main.NextPlayable("level-1")}");
        main.HasScene = realHasScene;
        flow.RestartLevel();
    }

    // ---- Task 16 (SC 17): level 2 played through the planned way (rooms designed with the user, 2026-11-03) ----

    private async Task CheckFullRunLevel2(Main main)
    {
        var flow = main.Flow;
        var player = main.Player;
        var swaps = main.Swaps;
        main.StartLevel("level-2");
        await PhysicsFrames(10);
        Check("level 2: the scene loads and the run starts in room 2.1", main.Runner.Room!.Id == "2.1", $"room {main.Runner.Room.Id}");

        // 2.1 — walk through the object barrier, stand on the button, swap places with the box left behind.
        await WalkTo(player, 8.5f, 5);
        var box = main.Runner.Bodies.Single();
        await LookAt(player, box.GlobalPosition);
        Check("level 2, 2.1: swap places with the box across the object barrier", swaps.TrySwap() is { IsApplied: true }, $"{swaps.Crosshair}");
        await PhysicsFrames(60);
        Check("level 2, 2.1: the box lands on the button and the door opens", main.Runner.Doors.Single().IsOpen, $"box {box.GlobalPosition}");
        await WalkTo(player, 7, 7.5f);
        await WalkTo(player, 9.5f, 7.5f);
        await WalkTo(player, 9.6f, 5);
        await WalkTo(player, 12.6f, 5);
        Check("level 2, 2.1: into room 2.2", main.Runner.Room.Id == "2.2", $"room {main.Runner.Room.Id}, at {player.GlobalPosition}");

        // 2.2 — the player barrier: swap places with the box beyond it.
        await WalkTo(player, 13.5f, 5);
        box = main.Runner.Bodies.Single();
        await LookAt(player, box.GlobalPosition);
        Check("level 2, 2.2: swap places through the player barrier", swaps.TrySwap() is { IsApplied: true }, $"{swaps.Crosshair}");
        await PhysicsFrames(70);
        await WalkTo(player, 24.6f, 5);
        Check("level 2, 2.2: into room 2.3", main.Runner.Room.Id == "2.3", $"room {main.Runner.Room.Id}, at {player.GlobalPosition}");

        // 2.3 — borrow the crate's mass, carry it into the cage, give the mass back, pass with the ball.
        await WalkTo(player, 25.4f, 5);
        var crate = main.Runner.Bodies.Single(b => b.ContainerId == "crate-2-3");
        var ball = main.Runner.Bodies.Single(b => b.ContainerId == "ball-2-3");
        await LookAt(player, crate.GlobalPosition);
        player.ToggleHold();
        Check("level 2, 2.3 (HoldLoad): the 300 kg crate is too heavy to hold", player.Held is null, $"held {player.Held?.Name}");
        PressKey(swaps, Key.Key2);
        Check("level 2, 2.3: take the crate's mass", swaps.TrySwap() is { IsApplied: true } && Mathf.IsEqualApprox((float)player.MassKg, 300f),
            $"{swaps.Crosshair}, mass {player.MassKg}");
        await PhysicsFrames(70);
        await LookAt(player, crate.GlobalPosition);
        player.ToggleHold();
        Check("level 2, 2.3: now 300 kg, the player holds the 70 kg crate", player.Held == crate, $"held {player.Held?.Name}");
        await WalkTo(player, 29.65f, 5);
        player.Look(-90, -0.35f);
        await PhysicsFrames(30);
        player.Drop();
        await PhysicsFrames(60);
        var cage = main.Runner.Room.Barriers.Single(b => b.Id == "cage-2-3").Box;
        Check("level 2, 2.3: the crate passes the grid and rests in the cage, too light for the button",
            cage.Contains(crate.GlobalPosition.ToLevel()) && !main.Runner.Doors.Single().IsOpen, $"crate {crate.GlobalPosition}");
        await LookAt(player, crate.GlobalPosition);
        Check("level 2, 2.3: give the mass back through the grid", swaps.TrySwap() is { IsApplied: true }, $"{swaps.Crosshair}");
        await PhysicsFrames(60);
        Check("level 2, 2.3: the 300 kg crate presses the button and the door opens", main.Runner.Doors.Single().IsOpen, $"crate {crate.GlobalPosition}");
        PressKey(swaps, Key.Key1);
        await LookAt(player, ball.GlobalPosition);
        Check("level 2, 2.3: swap places with the ball past the grid", swaps.TrySwap() is { IsApplied: true }, $"{swaps.Crosshair}");
        await PhysicsFrames(70);
        await WalkTo(player, 33.3f, 5);
        await WalkTo(player, 36.8f, 5);
        Check("level 2, 2.3: through the door into room 2.4", main.Runner.Room.Id == "2.4", $"room {main.Runner.Room.Id}, at {player.GlobalPosition}");

        // 2.4 — take the 2 kg box's mass and jump high by the wall. Without a swap in the air, the fall kills.
        async Task LightenAndJump()
        {
            await WalkTo(player, 37.6f, 3.6f);
            var light = main.Runner.Bodies.Single(b => b.ContainerId == "light-box-2-4");
            PressKey(swaps, Key.Key2);
            await LookAt(player, light.GlobalPosition);
            Check("level 2, 2.4: take the 2 kg box's mass", swaps.TrySwap() is { IsApplied: true } && player.MassKg <= 2.01,
                $"{swaps.Crosshair}, mass {player.MassKg}");
            await PhysicsFrames(70);
            await WalkTo(player, 41.65f, 5);
            PressKey(swaps, Key.Key1);
            player.Jump();
        }
        var deaths = flow.Run.Deaths;
        await LightenAndJump();
        for (var i = 0; i < 400 && !flow.DeathScreenVisible; i++)
            await PhysicsFrames(1);
        Check("death screen: the cause is written in the middle", flow.DeathScreenVisible && flow.DeathText.StartsWith("VOCÊ MORREU\nQueda a"),
            $"visible {flow.DeathScreenVisible}, text {flow.DeathText}");
        await PhysicsFrames(30);
        Check("death screen: the view falls over while dying", player.DeathFall > 0.3f, $"fall {player.DeathFall}");
        for (var i = 0; i < 300 && flow.IsDying; i++)
            await PhysicsFrames(1);
        Check("level 2, 2.4: a 12 m jump with no swap in the air kills on landing", flow.Run.Deaths == deaths + 1, $"deaths {flow.Run.Deaths}");
        Check("death screen: gone after the restart, the view stands up again", !flow.DeathScreenVisible && player.DeathFall == 0,
            $"visible {flow.DeathScreenVisible}, fall {player.DeathFall}");

        await LightenAndJump();
        var block = main.Runner.Bodies.Single(b => b.ContainerId == "block-2-4");
        float? swappedAt = null, topY = 0;
        for (var i = 0; i < 240 && swappedAt is null; i++)
        {
            var to = block.GlobalPosition - player.Eye.Origin;
            player.Look(Mathf.RadToDeg(Mathf.Atan2(-to.X, -to.Z)), Mathf.Atan2(to.Y, new Vector2(to.X, to.Z).Length()));
            await PhysicsFrames(1);
            topY = Mathf.Max(topY.Value, player.GlobalPosition.Y);
            if (player.GlobalPosition.Y > 4 && swaps.TrySwap() is { IsApplied: true })
                swappedAt = player.Velocity.Length();
        }
        Check("level 2, 2.4: near the top the block shows over the wall and the swap works", swappedAt is not null,
            $"top {topY:0.0} m, crosshair {swaps.Crosshair}");
        GD.Print($"[smoke] info level 2, 2.4: jump top {topY:0.0} m, speed at the swap {swappedAt:0.0} m/s");
        await PhysicsFrames(90);
        Check("level 2, 2.4: landing beyond the wall, alive", flow.Run.Deaths == deaths + 1 && player.GlobalPosition.X > 42.5f,
            $"deaths {flow.Run.Deaths}, at {player.GlobalPosition}");
        await WalkTo(player, 47.9f, 5);
        await PhysicsFrames(5);
        Check("level 2: the exit of 2.4 ends the level", flow.Result is { LevelId: "level-2" } && flow.EndScreenVisible,
            $"room {main.Runner.Room.Id}, at {player.GlobalPosition}");
        GD.Print($"[smoke] info full run of level 2: {flow.Result}");
    }

    private static void PressKey(SwapController swaps, Key key) =>
        swaps._UnhandledInput(new InputEventKey { PhysicalKeycode = key, Pressed = true });

    /// <summary>Walks like a player: turns towards the point and holds "forward" until there (or a timeout).</summary>
    private async Task WalkTo(Player player, float x, float z, int maxFrames = 600)
    {
        var target = new Vector2(x, z);
        for (var i = 0; i < maxFrames; i++)
        {
            var at = new Vector2(player.GlobalPosition.X, player.GlobalPosition.Z);
            var to = target - at;
            if (to.Length() < 0.15f)
                break;
            player.Look(Mathf.RadToDeg(Mathf.Atan2(-to.X, -to.Y)), 0);
            player.ScriptedMove = new Vector2(0, 1);
            await PhysicsFrames(1);
        }
        player.ScriptedMove = null;
        await PhysicsFrames(2);
    }

    // ---- The test room (sandbox) loads with the levels' validator and builds every station ----

    private async Task CheckSandbox(Main main)
    {
        main.Runner.LoadSandbox();
        main.ShowRoom("0.1");
        await PhysicsFrames(60);
        var room = main.Runner.Room;
        Check("sandbox: valid and built (objects, button, door, barriers, kill zone)",
            main.Runner.PieceCount == main.Runner.ExpectedPieceCount && main.Runner.Bodies.Count == room.Objects.Count,
            $"{main.Runner.PieceCount} pieces of {main.Runner.ExpectedPieceCount}");
        foreach (var obj in room.Objects)
        {
            var body = main.Runner.Bodies.Single(b => b.ContainerId == obj.Id);
            Check($"sandbox: {obj.Id} rests where the file put it", body.Position.DistanceTo(obj.Position.ToGodot()) < 0.05f, $"{body.Position}");
        }
        // Task 7: swapping with the ball on the 2 m ledge puts the feet inside the ledge → the player is lifted onto it.
        main.Player.GlobalPosition = new Vector3(17, 0, 3);    // far enough to see the ball over the ledge edge
        main.Player.ResetPhysicsInterpolation();
        await PhysicsFrames(5);
        var ball = main.Runner.Bodies.Single(b => b.ContainerId == "ball-on-ledge");
        await LookAt(main.Player, ball.GlobalPosition);
        var swap = main.Swaps.TrySwap();
        Check("make room: the swap with the ball on the ledge moves the player up out of the ledge",
            swap is { IsApplied: true } && main.Swaps.LastMakeRoom == Swapping.GameLogic.MakeRoomOutcome.Moved,
            $"{swap?.Reason} {main.Swaps.LastMakeRoom}");
        await PhysicsFrames(40);
        Check("make room: the player stands on the ledge", main.Player.IsOnFloor() && Mathf.Abs(main.Player.GlobalPosition.Y - 2f) < 0.06f,
            $"feet {main.Player.GlobalPosition}, on floor {main.Player.IsOnFloor()}");
        var deepInFloor = MakeRoom.ForPlayer(main.Player, new Vector3(6, -2, 10));
        Check("make room: no free point within 1.5 m (deep in the floor) → death", deepInFloor.Outcome == Swapping.GameLogic.MakeRoomOutcome.Death,
            $"{deepInFloor}");

        // The user's case (game log 2026-11-03): the 2 kg ball rolled next to the 300 kg crate; swapping with it killed.
        main.ShowRoom("0.1");
        // Task 14 (SC 15): the sandbox unlocks all three; keys 2 and 3 choose mass and energy, key 1 position again.
        await PhysicsFrames(3);
        Check("keys: the sandbox starts on position", main.Swaps.Choice == Swapping.Rules.Model.PropertyKind.Position, $"{main.Swaps.Choice.Name}");
        PressKey(main.Swaps, Key.Key2);
        Check("keys: 2 chooses mass", main.Swaps.Choice == Swapping.Rules.Model.PropertyKind.Mass, $"{main.Swaps.Choice.Name}");
        await PhysicsFrames(2);
        Check("keys: the HUD shows Massa", main.Hud.RunText.Contains("Massa"), main.Hud.RunText);
        PressKey(main.Swaps, Key.Key3);
        Check("keys: 3 chooses energy", main.Swaps.Choice == Swapping.Rules.Model.PropertyKind.KineticEnergy, $"{main.Swaps.Choice.Name}");
        PressKey(main.Swaps, Key.Key1);
        Check("keys: 1 chooses position again", main.Swaps.Choice == Swapping.Rules.Model.PropertyKind.Position, $"{main.Swaps.Choice.Name}");
        main.Swaps._UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.WheelDown, Pressed = true });
        Check("wheel: down goes to the next property (position → mass)", main.Swaps.Choice == Swapping.Rules.Model.PropertyKind.Mass, $"{main.Swaps.Choice.Name}");
        main.Swaps._UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.WheelUp, Pressed = true });
        main.Swaps._UnhandledInput(new InputEventMouseButton { ButtonIndex = MouseButton.WheelUp, Pressed = true });
        Check("wheel: up twice from mass wraps to energy", main.Swaps.Choice == Swapping.Rules.Model.PropertyKind.KineticEnergy, $"{main.Swaps.Choice.Name}");
        PressKey(main.Swaps, Key.Key1);
        await PhysicsFrames(3);
        var rolledBall = main.Runner.Bodies.Single(b => b.ContainerId == "ball-2kg");
        rolledBall.Teleport(new Vector3(5.30f, 0.25f, 6.08f), Vector3.Zero);
        main.Player.GlobalPosition = new Vector3(7.67f, 0, 6.44f);
        main.Player.ResetPhysicsInterpolation();
        await PhysicsFrames(10);
        await LookAt(main.Player, rolledBall.GlobalPosition);
        var userCase = main.Swaps.TrySwap();
        Check("make room: the user's case — swapping with the ball next to the 300 kg crate finds room",
            userCase is { IsApplied: true } && main.Swaps.LastMakeRoom != Swapping.GameLogic.MakeRoomOutcome.Death,
            $"{userCase?.Reason} {main.Swaps.LastMakeRoom}, feet {main.Player.GlobalPosition}");

        // Diagnosis (user: "I just die, it does not even look for space"): swap with every object from a few spots.
        foreach (var from in new[] { new Vector3(2, 0, 10), new Vector3(8, 0, 2), new Vector3(8, 0, 17), new Vector3(20, 0, 9) })
        {
            foreach (var id in main.Runner.Room.Objects.Select(o => o.Id).Where(id => !id.StartsWith("bay")))
            {
                main.ShowRoom("0.1");
                await PhysicsFrames(3);
                main.Player.GlobalPosition = from;
                main.Player.ResetPhysicsInterpolation();
                await PhysicsFrames(3);
                var target = main.Runner.Bodies.Single(b => b.ContainerId == id);
                await LookAt(main.Player, target.GlobalPosition);
                var died = false;
                void OnDied() => died = true;
                main.Swaps.PlayerDied += OnDied;
                var r = main.Swaps.TrySwap();
                main.Swaps.PlayerDied -= OnDied;
                if (r is { IsApplied: true })
                {
                    Check($"make room: from {from} the swap with {id} finds room ({main.Swaps.LastMakeRoom})", !died && main.Swaps.LastMakeRoom != Swapping.GameLogic.MakeRoomOutcome.Death, $"feet {main.Player.GlobalPosition}");
                }
            }
        }

        main.ShowRoom("0.1");
        await PhysicsFrames(5);
        main.Player.GlobalPosition = new Vector3(17, 0, 17);
        main.Player.ResetPhysicsInterpolation();
        await PhysicsFrames(5);
        var behind = main.Runner.Bodies.Single(b => b.ContainerId == "behind-barrier");
        await LookAt(main.Player, behind.GlobalPosition);
        Check("sandbox: a swap through the variable barrier is refused with \"barreira no caminho\"",
            main.Swaps.Target == behind && main.Swaps.Crosshair.Reason == "barreira no caminho", $"{main.Swaps.Crosshair}");
    }

    // ---- Task 15 (SC 17): the menu, with no window. Run with --smoke-menu (no other argument: the default start) ----

    public async Task<int> RunMenu(Main main)
    {
        try
        {
            var menu = main.Menu;
            await PhysicsFrames(2);
            Check("menu: no arguments opens the menu with nothing loaded", menu.IsOpen && menu.Screen == "main" && main.Runner.Room is null,
                $"open {menu.IsOpen}, screen {menu.Screen}");
            Check("menu: the player does not run behind the menu", main.Player.ProcessMode == ProcessModeEnum.Disabled, $"{main.Player.ProcessMode}");
            foreach (var text in new[] { "Jogar", "Níveis", "Mapa de teste", "Controles", "Sair" })
                Check($"menu: button {text}", Menu.FindButton(menu, text) is not null, "missing");

            // Cards: level 1 has a scene; 2 and 3 do not yet → "Em construção" (even though they are also locked).
            main.HasScene = level => level.Id == "level-1";
            Press(menu, "Níveis");
            Check("menu: one card per catalog level", menu.Cards.Count == 3 && menu.Screen == "levels", $"{menu.Cards.Count} cards");
            Check("menu: level 1 open, levels without a scene \"Em construção\"",
                menu.StatusOf("level-1") == Swapping.GameLogic.LevelStatus.Available
                && menu.StatusOf("level-2") == Swapping.GameLogic.LevelStatus.UnderConstruction
                && menu.StatusOf("level-3") == Swapping.GameLogic.LevelStatus.UnderConstruction, "wrong status");

            // With scenes everywhere the same cards show the normal padlock.
            main.HasScene = _ => true;
            menu.Show("levels");
            Check("menu: with scenes, level 2 and 3 show the padlock and their cards are disabled",
                menu.StatusOf("level-2") == Swapping.GameLogic.LevelStatus.Locked
                && menu.StatusOf("level-3") == Swapping.GameLogic.LevelStatus.Locked
                && menu.Cards["level-2"].Disabled && menu.Cards["level-3"].Disabled && !menu.Cards["level-1"].Disabled, "wrong cards");
            var started = new List<string>();
            menu.LevelRequested += started.Add;
            menu.Cards["level-2"].EmitSignal(BaseButton.SignalName.Pressed);
            Check("menu: a locked card can't start", started.Count == 0 && menu.IsOpen, $"started {string.Join(",", started)}");

            // Under construction can't start either.
            main.HasScene = level => level.Id == "level-1";
            menu.Show("levels");
            menu.Cards["level-2"].EmitSignal(BaseButton.SignalName.Pressed);
            Check("menu: an under-construction card can't start", started.Count == 0, $"started {string.Join(",", started)}");
            menu.LevelRequested -= started.Add;

            // Level 1 from its card, then the pause menu's Menu button.
            menu.Cards["level-1"].EmitSignal(BaseButton.SignalName.Pressed);
            await PhysicsFrames(5);
            Check("menu: the level-1 card starts level 1", !menu.IsOpen && main.Runner.Level.Id == "level-1" && main.Runner.Room?.Id == "1.1",
                $"open {menu.IsOpen}");
            Check("menu: the player runs again in the level", main.Player.ProcessMode == ProcessModeEnum.Inherit, $"{main.Player.ProcessMode}");
            main.Flow.SetPaused(true);
            Press(main.Flow.PausePanel, "Menu");
            await PhysicsFrames(3);
            Check("pause: the Menu button goes back to the menu, unpaused, nothing loaded",
                menu.IsOpen && !main.Flow.IsPaused && !GetTree().Paused && main.Runner.Room is null, $"open {menu.IsOpen}");

            // "Jogar" continues: with fresh progress it is level 1.
            Press(menu, "Jogar");
            await PhysicsFrames(3);
            Check("menu: Jogar continues at level 1 on fresh progress", main.Runner.Level.Id == "level-1" && !menu.IsOpen, "");
            main.ShowMenu();

            // The test map.
            Press(menu, "Mapa de teste");
            await PhysicsFrames(3);
            Check("menu: Mapa de teste opens the sandbox", main.Runner.Level.Id == "sandbox" && main.Runner.Room?.Id == "0.1",
                $"{main.Runner.Level.Id}");
            main.ShowMenu();

            // Controls screen and back.
            Press(menu, "Controles");
            Check("menu: Controles shows the controls screen", menu.Screen == "controls", menu.Screen);
            Press(menu, "Voltar");
            Check("menu: Voltar returns to the first screen", menu.Screen == "main", menu.Screen);

            // The progress file: write, read back; an unreadable file is kept aside and progress starts fresh.
            main.Progress.Record(new Swapping.GameLogic.RunResult("level-1", 20_000, 4, 0, 0, DateTimeOffset.UtcNow));
            ProgressStore.Save(Main.SmokeProgressPath, main.Progress);
            var ids = new[] { "level-1", "level-2", "level-3" };
            var again = ProgressStore.Load(Main.SmokeProgressPath, ids);
            Check("progress: saved to the file and read back", again.IsFinished("level-1") && again.Best("level-1") is { TimeMs: 20_000, Swaps: 4 }, "not read back");
            using (var bad = Godot.FileAccess.Open(Main.SmokeProgressPath, Godot.FileAccess.ModeFlags.Write))
                bad!.StoreString("{ not json");
            var fresh = ProgressStore.Load(Main.SmokeProgressPath, ids);
            var corruptCopy = Godot.FileAccess.FileExists("user://progress.smoke.corrupt.json");
            Check("progress: an unreadable file starts fresh and is kept as .corrupt.json", fresh.IsCorrupt && !fresh.IsFinished("level-1") && corruptCopy,
                $"corrupt {fresh.IsCorrupt}, copy {corruptCopy}");
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath("user://progress.smoke.corrupt.json"));
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(Main.SmokeProgressPath));

            GD.Print($"[smoke] PASSED {_passed} checks");
            return 0;
        }
        catch (SmokeFailure failure)
        {
            GD.PrintErr($"[smoke] FAIL {failure.Message}");
            return 1;
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[smoke] FAIL unexpected {exception}");
            return 1;
        }
    }

    private void Press(Node root, string text)
    {
        var button = Menu.FindButton(root, text);
        Check($"button \"{text}\" exists", button is not null, "missing");
        button!.EmitSignal(BaseButton.SignalName.Pressed);
    }

    // ---- Barrier bays of the test map (corridor at x 0–4, bays z 34–60): one barrier type per bay ----

    private async Task CheckBarrierBays(Main main)
    {
        // The corridor: from the main area the player walks north to the barrier hall.
        main.ShowRoom("0.1");
        main.Player.GlobalPosition = new Vector3(2, 0, 17);
        main.Player.ResetPhysicsInterpolation();
        await PhysicsFrames(3);
        await WalkTo(main.Player, 2, 32, maxFrames: 600);
        Check("sandbox: the corridor leads from the main area to the barrier hall", main.Player.GlobalPosition.Z > 31,
            $"player {main.Player.GlobalPosition}");

        // (bay, what the barrier blocks: objects, player, swaps)
        var bays = new (int Bay, bool Objects, bool Player, bool Variables)[]
        {
            (1, true, false, false), (2, false, true, false), (3, true, true, false), (4, false, false, true), (5, true, false, true),
            (6, false, false, true),
        };
        foreach (var (bay, objects, player, variables) in bays)
        {
            main.ShowRoom("0.1");
            await PhysicsFrames(5);
            var barrier = main.Runner.Barriers.Single(b => b.Def.Id == $"bay{bay}-barrier");
            var expectedLayer = (objects ? Layers.BarrierObjects : 0u) | (player ? Layers.BarrierPlayer : 0u);
            Check($"bay {bay}: the barrier blocks objects={objects}, player={player} (collision layers)", barrier.CollisionLayer == expectedLayer,
                $"layer {barrier.CollisionLayer}");
            var cx = 3f + 6f * (bay - 1);

            // The player walks from the near side to the far side: blocked only when the barrier blocks the player.
            main.Player.GlobalPosition = new Vector3(cx + 1.5f, 0, 44.5f);   // a lane beside the box, so it is not pushed ahead
            main.Player.ResetPhysicsInterpolation();
            await PhysicsFrames(3);
            await WalkTo(main.Player, cx + 1.5f, 52, maxFrames: 240);
            var crossed = main.Player.GlobalPosition.Z > 48;
            Check($"bay {bay}: the player {(player ? "is stopped by" : "walks through")} the barrier", crossed == !player,
                $"player z {main.Player.GlobalPosition.Z}");

            // The box is pushed fast to the barrier: blocked only when the barrier blocks objects.
            var box = main.Runner.Bodies.Single(b => b.ContainerId == $"bay{bay}-box");
            box.Teleport(new Vector3(cx, 0.5f, 45.5f), new Vector3(0, 0, 8));
            await PhysicsFrames(45);
            var boxCrossed = box.GlobalPosition.Z > 47.3f;
            Check($"bay {bay}: the box {(objects ? "is stopped by" : "goes through")} the barrier", boxCrossed == !objects,
                $"box z {box.GlobalPosition.Z}");

            // Swap with the box on the far side, aiming from the near side: refused only when the barrier blocks variables.
            main.ShowRoom("0.1");                                  // the pushed box now sits in the line of sight
            main.Player.GlobalPosition = new Vector3(cx, 0, 43);
            main.Player.ResetPhysicsInterpolation();
            await PhysicsFrames(3);
            var far = main.Runner.Bodies.Single(b => b.ContainerId == $"bay{bay}-far");
            await LookAt(main.Player, far.GlobalPosition);
            var refused = main.Swaps.Crosshair.Reason == "barreira no caminho";
            Check($"bay {bay}: a swap through it is {(variables ? "refused" : "allowed")}", main.Swaps.Target == far && refused == variables,
                $"{main.Swaps.Crosshair}");
        }

        await CheckTouchingBlocksSwaps(main);
        await CheckRestoringBarrier(main);
    }

    /// <summary>User's rule: touching a variable barrier (the player or the object) counts as being inside it.</summary>
    private async Task CheckTouchingBlocksSwaps(Main main)
    {
        const string inside = "dentro de uma barreira";
        main.ShowRoom("0.1");
        await PhysicsFrames(3);
        var box = main.Runner.Bodies.Single(b => b.ContainerId == "bay4-box");
        var ball = main.Runner.Bodies.Single(b => b.ContainerId == "bay4-ball");
        box.Teleport(new Vector3(21, 0.5f, 46.42f), Vector3.Zero);       // 3 cm from the green barrier (z 46.95)
        main.Player.GlobalPosition = new Vector3(22.5f, 0, 43);
        main.Player.ResetPhysicsInterpolation();
        await PhysicsFrames(5);
        await LookAt(main.Player, box.GlobalPosition);
        Check("bay 4: a box touching the barrier cannot be swapped", main.Swaps.Target == box && main.Swaps.Crosshair.Reason == inside,
            $"{main.Swaps.Crosshair}");
        await LookAt(main.Player, ball.GlobalPosition);
        Check("bay 4: a box away from it can (control)", main.Swaps.Target == ball && main.Swaps.Crosshair.Reason != inside,
            $"{main.Swaps.Crosshair}");

        main.Player.GlobalPosition = new Vector3(19.5f, 0, 46.6f);        // the capsule 5 cm from the barrier
        main.Player.ResetPhysicsInterpolation();
        await PhysicsFrames(3);
        await LookAt(main.Player, ball.GlobalPosition);
        Check("bay 4: the player touching the barrier cannot swap", main.Swaps.Target == ball && main.Swaps.Crosshair.Reason == inside,
            $"{main.Swaps.Crosshair} at {main.Player.GlobalPosition}");
    }

    /// <summary>User's request: the pink barrier also gives the variables back, exchanging with whoever holds them.</summary>
    private async Task CheckRestoringBarrier(Main main)
    {
        main.ShowRoom("0.1");
        await PhysicsFrames(3);
        var box = main.Runner.Bodies.Single(b => b.ContainerId == "bay6-box");
        main.Player.GlobalPosition = new Vector3(34.5f, 0, 43);
        main.Player.ResetPhysicsInterpolation();
        await PhysicsFrames(3);
        PressKey(main.Swaps, Key.Key2);
        await LookAt(main.Player, box.GlobalPosition);
        var swap = main.Swaps.TrySwap();
        Check("bay 6: the player swaps mass with the box (player 10 kg, box 70 kg)",
            swap is { IsApplied: true } && Mathf.IsEqualApprox((float)main.Player.MassKg, 10) && Mathf.IsEqualApprox(box.Mass, 70),
            $"{swap?.Reason} player {main.Player.MassKg} box {box.Mass}");

        await WalkTo(main.Player, 34.5f, 50, maxFrames: 240);
        Check("bay 6: walking into the pink barrier gives the player its 70 kg back and the box its 10 kg",
            main.Swaps.LastRestored == Swapping.Levels.LevelValidator.PlayerId
            && Mathf.IsEqualApprox((float)main.Player.MassKg, 70) && Mathf.IsEqualApprox(box.Mass, 10),
            $"restored {main.Swaps.LastRestored}, player {main.Player.MassKg} box {box.Mass}");

        // User's bug (2026-11-03): after a mass swap, dying kept the swapped mass; the rebuilt room's rules said 70 kg.
        main.ShowRoom("0.1");
        await PhysicsFrames(3);
        box = main.Runner.Bodies.Single(b => b.ContainerId == "bay6-box");
        main.Player.GlobalPosition = new Vector3(34.5f, 0, 43);
        main.Player.ResetPhysicsInterpolation();
        await PhysicsFrames(3);
        await LookAt(main.Player, box.GlobalPosition);
        var again = main.Swaps.TrySwap();
        main.Flow.RestartRoom(death: true);
        await PhysicsFrames(3);
        Check("death: the player comes back with its own 70 kg after a mass swap",
            again is { IsApplied: true } && Mathf.IsEqualApprox((float)main.Player.MassKg, 70)
            && Mathf.IsEqualApprox((float)main.Runner.World.BodyUpdateFor(Swapping.Levels.LevelValidator.PlayerId).MassKg, 70),
            $"{again?.Reason} player {main.Player.MassKg}");
        PressKey(main.Swaps, Key.Key1);
    }

    /// <summary>Turns the player's view to a point and lets the aim update.</summary>
    private async Task LookAt(Player player, Vector3 point)
    {
        var to = point - player.Eye.Origin;
        var yaw = Mathf.RadToDeg(Mathf.Atan2(-to.X, -to.Z));
        var pitch = Mathf.Atan2(to.Y, new Vector2(to.X, to.Z).Length());
        player.Look(yaw, pitch);
        await PhysicsFrames(2);
    }

    private static void Teleport(RigidBody3D body, Vector3 position, Vector3 velocity = default)
    {
        var transform = new Transform3D(Basis.Identity, position);
        PhysicsServer3D.BodySetState(body.GetRid(), PhysicsServer3D.BodyState.Transform, transform);
        body.GlobalTransform = transform;
        body.LinearVelocity = velocity;
        body.AngularVelocity = Vector3.Zero;
        body.Sleeping = false;
    }

    private async Task PhysicsFrames(int count)
    {
        for (var i = 0; i < count; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    private void Check(string name, bool ok, string detail)
    {
        if (!ok)
            throw new SmokeFailure($"{name} — {detail}");
        _passed++;
        GD.Print($"[smoke] ok {name}");
    }

    private sealed class SmokeFailure(string message) : Exception(message);
}
