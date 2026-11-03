# Tasks: `game` — level 1 in Godot

Spec: [`SPEC-game.md`](../../docs/specs/SPEC-game.md) ("SC n", decisions G1–G6) · Plan: [`plan.md`](plan.md)

Common verification (every task):
```
export PATH="$PATH:/c/Program Files/dotnet"
dotnet build Swapping.sln && dotnet test Swapping.sln          # includes the headless smoke test from Task 1 on
"$GODOT" --headless --path game -- --smoke-test level-1        # the same check, by hand, with its log
```
`GODOT` = `Godot_v4.7.2-stable_mono_win64_console.exe` in the WinGet folder (see `docs/CONTEXTO-SESSAO.md`).
All `rules`, `levels` and API tests stay green. One commit per task, pushed. Each Godot task adds its
checks to `SmokeTest.cs`.

---

## Task 1: Godot project, level files copy and headless smoke-test entry
**Description:** Create `game/` with `project.godot` (Godot 4.7, Forward+ renderer, physics 60 Hz,
collision layer names), `Swapping.Game.csproj` (`Godot.NET.Sdk` 4.7.2, `net8.0`, references
`Swapping.Rules` and `Swapping.Levels`) and add it to `Swapping.sln`. An MSBuild step copies
`levels/*.json` into `game/levels/` before the build (G6), git-ignored. Add `scenes/main.tscn` with a
`Main.cs` that reads the command line:
- `--smoke-test level-1` → runs `SmokeTest`: it loads the catalog from `res://levels`, prints one line
  per check and quits with 0 (all good) or 1 (with the failing step);
- `--screenshot <room>` → saves a PNG of the room.

A test in `tests/Swapping.GameLogic.Tests` (created here with only this test) runs the console exe
headless, and is skipped with a message when Godot is not found.
**Acceptance criteria:**
- [x] SC 1 (part): `dotnet build Swapping.sln` builds the Godot assembly with zero warnings.
- [x] SC 10 (harness): headless `--smoke-test level-1` loads `level_1.json` through `LevelCatalog` (4 rooms,
      no errors) and exits 0; a broken check exits 1 with its name in the output.
- [x] `game/levels/*.json` and `game/.godot/` are not tracked by git.
**Dependencies:** None
**Files:** `game/project.godot`, `game/Swapping.Game.csproj`, `game/scenes/main.tscn`, `game/scripts/Main.cs`,
`game/scripts/SmokeTest.cs`, `tests/Swapping.GameLogic.Tests/GodotSmokeTests.cs`, `.gitignore`
**Scope:** M

## Task 2: `Swapping.GameLogic` with the run tracker and the architecture test
**Description:** Create `src/Swapping.GameLogic` (`net8.0`, references `Swapping.Rules` and
`Swapping.Levels`, no Godot). It gets:
- `RunTracker`: time in ms from physics deltas, paused while paused; swaps added up over every room
  build; deaths; room restarts; `RestartRoom`, `RestartLevel`, `Finish` → the result fields of
  `POST /results`;
- `RunClock.Format`: `mm:ss.d`.

Architecture test: the library has no Godot reference.
**Acceptance criteria:**
- [x] Restart room keeps time and swaps and adds 1 room restart; restart level zeroes everything; pause
      stops the time; swaps = the sum of `world.MoveCount` over every room build.
- [x] `Finish` gives `levelId, timeMs, swaps, deaths, roomRestarts, finishedAt` with the API names.
- [x] SC 11 (part): `GameLogic` and its `.csproj` reference no Godot assembly or package.
**Dependencies:** Task 1
**Files:** `src/Swapping.GameLogic/*.csproj`, `RunTracker.cs`, `RunClock.cs`,
`tests/Swapping.GameLogic.Tests/RunTrackerTests.cs`, `ArchitectureTests.cs`
**Scope:** S

### Checkpoint: Foundation
- [x] Build clean, all tests green (including the headless smoke test), commit + push.

## Task 3: Scenery of level 1, objects and kill zones built from the file
**Description:** `levels/level_1.tscn` (`res://levels/level_1.tscn`, the `scene` field of the file):
- floor, walls at least 0.3 m thick, the pits of 1.2 and 1.4, the 2 m ledge of 1.3 and lights;
- static bodies on the `world` layer;
- simple shapes and flat colors.

`LevelRunner` builds the current room from the JSON:
- boxes and balls as `RigidBody3D` (mass, size, bounce → `PhysicsMaterial`, friction 0.6, CCD on,
  `objects` layer, a `ContainerId`);
- kill zones as `Area3D`;
- the `World` from `RulesMapping.BuildWorld`.

"Build room" frees the previous pieces first.
**Acceptance criteria:**
- [x] SC 2: for each of the 4 rooms, the smoke test builds it and finds every object and kill zone at
      the file's position and size (±0.01 m), with mass, bounce and CCD set.
- [x] Building a room twice leaves one set of pieces and a new `World` (no leftovers).
- [x] `--screenshot 1.2` saves a PNG that shows the pit and the box (opened and checked by the agent).
**Dependencies:** Task 2
**Files:** `game/levels/level_1.tscn`, `game/scripts/LevelRunner.cs`, `game/scripts/Pieces/Body.cs`,
`game/scripts/Pieces/KillZone.cs`, `game/scripts/SmokeTest.cs`
**Scope:** M

## Task 4: Buttons, doors, barriers and the exit grid
**Description:** The pure `ButtonState` in `GameLogic`: pressed when the mass on it is at least
`requiredMassKg`, counting what `detects` allows (`objects` ignores the player). In Godot:
- `Button` (an `Area3D` that sums the masses on it);
- `Door` (opens when all its buttons are pressed and closes again);
- `Barrier` with the presets: the exit grid blocks objects only, and its look shows both icons;
- the `Exit` area of each room.
**Acceptance criteria:**
- [x] Unit tests: threshold boundaries; `objects` ignores the player; `any` counts the player.
- [x] Smoke test, room 1.1: the box dropped on the button opens the door; removing it closes the door;
      the box cannot cross the exit grid, and the player can.
- [x] Barriers and doors at the file's positions in all rooms.
**Dependencies:** Task 3
**Files:** `src/Swapping.GameLogic/ButtonState.cs`, `tests/.../ButtonStateTests.cs`,
`game/scripts/Pieces/Button.cs`, `Door.cs`, `Barrier.cs`
**Scope:** M

## Task 5: The player: walk, jump, look, hold, throw, push
**Description:** `player.tscn` + `Player.cs`: a `CharacterBody3D` (0.6 × 1.8 × 0.6 m) with simulated mass
(70 kg) and velocity. It has:
- mouse look and walking at 3 m/s;
- a jump at the speed `Formulas.JumpTakeoffSpeed(mass)`;
- `E` holds the aimed object up to 2.5 m (G5): the object follows a point in front of the camera with a
  spring. `E` again drops it;
- the left button throws the held object with a fixed energy (G4);
- walking into a rigid body pushes it with an impulse that depends on both masses.

The pure `InputMap` in `GameLogic` turns keys and buttons into actions (G1).
**Acceptance criteria:**
- [x] Unit tests: every binding of G1 maps to its action; the push impulse is smaller for a heavier body.
- [x] Smoke test: the player spawns standing at the room spawn with the file's yaw; after a scripted jump
      the take-off speed matches `JumpTakeoffSpeed(70)` (±5 %); holding and dropping the box of 1.1 works.
**Dependencies:** Task 4
**Files:** `game/scenes/player.tscn`, `game/scripts/Player.cs`, `src/Swapping.GameLogic/InputMap.cs`,
`PushImpulse.cs`, `tests/.../InputMapTests.cs`
**Scope:** M

## Task 6: Aim, crosshair state, measured context, apply and sync
**Description:** The pure `CrosshairState` in `GameLogic`: the result of the dry run →
white, red with a short Portuguese reason, or hidden. It covers every `RejectReason`.
`SwapController` in Godot:
- a ray from the camera picks a body with a `ContainerId`;
- it measures the `SwapContext`: distance between centers; line of sight with a ray that ignores both
  bodies; a variable barrier crossed; each body inside one;
- dry run with `LevelRules.Check`; on the right button, `SwapOperation.Apply`;
- each `BodyUpdate` (position = center, velocity, mass) is written back.

Every physics step calls `world.SyncFromPhysics` for every body.
**Acceptance criteria:**
- [x] Unit tests: one crosshair case per `RejectReason`; `OutOfRange` → "fora de alcance".
- [x] SC 3: in room 1.2, the box within 8 m → white; the player moved back to 8.5 m → red "fora de alcance".
- [x] SC 5: the locked box of 1.4 → red `Locked`; it can still be held and thrown (smoke test).
- [x] After an applied swap, `world.CheckInvariant()` holds and the swap count is 1.
**Dependencies:** Task 5
**Files:** `src/Swapping.GameLogic/CrosshairState.cs`, `tests/.../CrosshairStateTests.cs`,
`game/scripts/SwapController.cs`, `game/scripts/ContextMeter.cs`, `game/scripts/SmokeTest.cs`
**Scope:** M

## Task 7: Make room after a position swap
**Description:** The pure `FreeSpotSearch` in `GameLogic`:
- the fixed order of candidates: straight up first, then rings around the target, step 0.25 m, radius
  1.5 m (G2);
- a candidate that a callback says is blocked or behind a wall is skipped;
- no candidate → `Death` (player) or `Pop` (object).

In Godot, a shape query checks the body after a position update and asks the search for a free point.
A popped object respawns at its start values.
**Acceptance criteria:**
- [x] Unit tests: the order, the radius limit, a wall between, the first free candidate wins, none → death/pop.
- [x] SC 4: the position swap of room 1.2 → the player stands on the floor across the pit (feet at y = 0
      ±0.05, not inside it), the box where the player was; the swap counts 1.
- [x] A swap that leaves no free space kills the player (smoke test with a small closed box built for the
      test, not a level room).
**Dependencies:** Task 6
**Files:** `src/Swapping.GameLogic/FreeSpotSearch.cs`, `tests/.../FreeSpotSearchTests.cs`,
`game/scripts/MakeRoom.cs`, `game/scripts/SwapController.cs`, `game/scripts/SmokeTest.cs`
**Scope:** M

### Checkpoint: The swap is playable
- [ ] Build clean, all tests and the smoke test green, commit + push.
- [ ] **The user plays rooms 1.1–1.2** (window, mouse and keyboard) and says what feels wrong before Phase 4.

## Task 8: Deaths, restarts, exits, end of run and pause menu
**Description:**
- The pure `FallDeath` in `GameLogic`: death when landing faster than 12 m/s (G3).
- Death by kill zone, by fall or by no space → 0.5 s fade → restart the room.
- `R` restarts the room; `Esc` opens the pause menu (resume, restart room, restart level, quit).
- Crossing a room's exit builds the next room. The exit of 1.4 ends the run and shows the time and swaps.
- `RunTracker` is driven by all of this.
**Acceptance criteria:**
- [x] Unit tests: `FallDeath` at 11.9 / 12.0 / 12.1 m/s.
- [x] SC 6: the player dropped into the pit of 1.2 → the room is rebuilt, the player at the spawn, time and
      swaps kept, 1 death and 1 room restart.
- [x] SC 7: "restart level" from 1.3 → room 1.1, time and swaps zero.
- [x] The time does not advance while the pause menu is open; the exit of 1.4 shows the end screen.
**Dependencies:** Task 7
**Files:** `src/Swapping.GameLogic/FallDeath.cs`, `tests/.../FallDeathTests.cs`, `game/scripts/RunFlow.cs`,
`game/scenes/pause_menu.tscn` + `PauseMenu.cs`, `game/scripts/SmokeTest.cs`
**Scope:** M

## Task 9: HUD and outline
**Description:**
- `hud.tscn` + `Hud.cs`:
  - time `mm:ss.d` and swaps;
  - the player's mass, speed and energy, and the aimed object's;
  - the crosshair with its color and reason;
  - the cooldown ring.
- The pure `OutlineColor` in `GameLogic`: white = everything unlocked, gray = locked or refuses all,
  one color per single property (draft palette).
- `outline.gdshader` on the aimed or held object.
- Names are always shown in a plain `Label`.
**Acceptance criteria:**
- [x] Unit tests: the outline color for every case; energy and speed text formats.
- [x] SC 9: smoke test reads the HUD labels in room 1.1 (time ticking, swaps 0, player 70 kg) and, aiming
      at the box of 1.2, its numbers.
- [x] `--screenshot 1.4` shows the white and the gray outlines (opened and checked by the agent).
**Dependencies:** Task 8
**Files:** `src/Swapping.GameLogic/OutlineColor.cs`, `tests/.../OutlineColorTests.cs`, `game/scenes/hud.tscn`
+ `game/scripts/Hud.cs`, `game/shaders/outline.gdshader`
**Scope:** M

## Task 10: Level 1 solved by script, end to end
**Description:** `SmokeTest` plays the whole level the planned way, with scripted input on the real
scenes:
- 1.1: box on the button → door → exit;
- 1.2: the swap across the pit;
- 1.3: swap with the ball → stand on the button → swap with the box → the door opens;
- 1.4: use the box that accepts position.

It checks the end screen and the run result. Any room that turns out unsolvable is fixed in
`level_1.json` with its design test in `levels` (the room's lesson does not change: ask first).
**Acceptance criteria:**
- [ ] SC 8: room 1.3 solved the planned way (the door opens).
- [ ] SC 10: the full scripted run of level 1 exits 0; the run result has 4 rooms' worth of swaps, `timeMs`
      ≥ the API floor (1 000 × 4) and the API field names.
- [ ] `dotnet test Swapping.sln` runs it on the PC.
**Dependencies:** Task 9
**Files:** `game/scripts/SmokeTest.cs`, `game/scripts/ScriptedInput.cs`, maybe `levels/level_1.json` and
`tests/Swapping.Levels.Tests/RoomDesignTests.cs`
**Scope:** M

## Task 11: Architecture, coverage, play-test sheet, screenshots and report
**Description:**
- Coverage of `Swapping.GameLogic`.
- A play-test sheet `docs/processo/PLAYTEST-nivel-1.md`: a checklist per room and the taste questions of
  the spec.
- Screenshots of the 4 rooms in `docs/evidencias/`.
- `docs/processo/RELATORIO-game.md` (criterion → check).
- C4 components of the Game Client, README (how to play, controls).
**Acceptance criteria:**
- [ ] SC 11: architecture test green; line coverage of `Swapping.GameLogic` ≥ 90 %.
- [ ] SC 1: the solution and `game/` build with zero warnings; everything green.
- [ ] SC 12 and the feel: the sheet is ready for the user; the report maps SC 1–11 to their checks.
**Dependencies:** Task 10
**Files:** `docs/processo/PLAYTEST-nivel-1.md`, `docs/processo/RELATORIO-game.md`, `docs/architecture/C4.md`,
`README.md`, `docs/processo/DIARIO.md`
**Scope:** M

### Checkpoint: Complete
- [ ] SC 1–11 have an automated check; build clean; all green; pushed; the user has the play-test sheet.
