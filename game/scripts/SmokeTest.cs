using Godot;
using Swapping.Game.Pieces;
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
            await CheckSandbox(main);

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
            GD.PrintErr($"[smoke] FAIL unexpected {exception.GetType().Name}: {exception.Message}");
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
        player.ToggleHold();
        Check("swap: the locked box can still be held", player.Held == locked, $"held {player.Held?.Name ?? "nothing"}");
        await PhysicsFrames(10);
        player.Throw();
        await PhysicsFrames(2);
        Check("swap: and thrown", player.Held is null && locked.LinearVelocity.Length() > 1, $"speed {locked.LinearVelocity.Length()}");
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
        await PhysicsFrames(50);                                // fade 0.5 s
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
        await PhysicsFrames(120);
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
            foreach (var id in main.Runner.Room.Objects.Select(o => o.Id))
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
