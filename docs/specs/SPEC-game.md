# Spec: `game` — Godot client, first slice (level 1)

> Module from [`CAPABILITY-MAP.md`](../../CAPABILITY-MAP.md) (first slice: "game (level 1 only)").
> Produced with the skill `agent-skills:spec-driven-development` (phase 1, Specify) during the autonomous
> loop, on the user's PC. Status: **approved by the user on 2026-11-03** (decisions G1–G6 as written). Plan: [`tasks/game/plan.md`](../../tasks/game/plan.md).
> Uses: [`SPEC-rules.md`](SPEC-rules.md), [`SPEC-levels.md`](SPEC-levels.md), and later
> [`SPEC-sync.md`](SPEC-sync.md). Sources: [level progression](../design/PROGRESSAO-NIVEIS.md),
> [ideas bank](../ideas/BANCO-DE-IDEIAS.md), the `rules` and `levels` reports (pending items for `game`).

## Objective

The first playable slice of **Swapping** in Godot: the player walks through the **4 rooms of level 1**
(1.1 → 1.4) in first person, holds and throws boxes, **swaps position** with objects, dies in pits and
restarts the room, and finishes the level. The time and the swap count are always on screen.

This slice proves the riskiest part of the project: that the pure `rules` library drives Godot physics
correctly. Levels 2, 3 and 5 reuse the same pieces (barriers, mass, energy) and come on Wednesday,
following the same spec (a later revision adds what they need).

The guiding rule: **Godot does physics and drawing; the decisions come from the libraries.**
- `rules` decides if a swap is legal and what each body becomes.
- `levels` says what is in each room.
- The game only measures (distance, line of sight, barriers between) and applies.

### What the player can do (level 1)
| Input | Action |
|---|---|
| Mouse / WASD / Space | look, walk (3 m/s), jump (take-off speed from `Formulas.JumpTakeoffSpeed(mass)`) |
| `E` | hold the aimed object (≤ 2.5 m) / drop it; holding counts for density later |
| Hold and release left button | **throw** the held object (simple version: fixed throw energy; the charge bar is a later item) |
| Right button | **swap** with the aimed object (the property is the only one unlocked; levels with more properties pick it with `1`/`2`/`3` later) |
| `R` / `Esc` | restart the room / pause menu (resume, restart room, restart level, quit) |

### How a swap is applied (the core loop)
1. **Aim:** a ray from the camera hits a body that has a container id. The crosshair state comes from a
   **dry run**: `LevelRules.Check(...)` with the measured context (no change):
   - **white** = would be applied;
   - **red** = refused; the reason is shown small, e.g. "fora de alcance";
   - **hidden** = nothing aimed.
2. **Measure** the `SwapContext`:
   - distance between the two body **centers**;
   - line of sight (a ray that ignores both bodies);
   - whether a barrier that blocks variables is crossed by the segment;
   - whether each body is inside such a barrier.
3. **Apply:** `SwapOperation.Apply(world, request, now)`. On `Applied`, write each `BodyUpdate` back to its body:
   - position = center;
   - velocity;
   - mass on the `RigidBody3D`, or the player's simulated mass.
4. **Make room** (position swaps). A position swap exchanges **centers**: a box center is 0.5 m above the
   floor and the player's is 0.9 m, so the player can end up with the feet inside the floor. After a
   position update, a shape query checks the body:
   - **free** → done;
   - **not free** → search the **nearest free point** (`FreeSpotSearch`): up first, then rings around, in a
     fixed order, inside a radius of **1.5 m**, never through a wall (ray from the target point) and never
     across a barrier;
   - **no free point** → **death** (player) or **pop** (object: smoke effect, then it respawns at its
     start position, at rest, with its start values). These are the user's decisions.
5. **Every physics step:** `world.SyncFromPhysics(id, center, velocity)` for every body, so the variables
   follow the physics (the `rules` model: physics changes the value of the variable in use).

### Rooms, deaths and the run
- **Room session:** entering a room builds its pieces from the level file and a new `World`
  (`RulesMapping.BuildWorld`).
- **Restart room** (death or menu): the room's bodies are removed and built again from the file. The
  player goes to the room spawn. **Time and swaps keep adding up** (decided with the user).
- **Restart level** (menu): back to room 1.1, time and swaps back to zero (a new run).
- **Deaths:**
  - **kill zone** (the bodies under the pits of rooms 1.2 and 1.4);
  - **fall:** landing faster than **12 m/s**, about a 7 m drop; the threshold is a draft and is tuned by playing;
  - **no space after a swap.**
  - Drowning comes with water (level 4, not this slice).
- **Exit:** crossing a room's exit (with its exit grid) starts the next room. Crossing the exit of the
  last room ends the run: the screen shows the time and swaps; later `sync.RecordRun` keeps it.
- **Run tracker** (pure C#): time in ms (paused while the pause menu is open), swaps (= `world.MoveCount`
  added up over every room build), deaths, room restarts.

### What the player sees
- **HUD**, always, from room 1.1:
  - time `mm:ss.d` and swaps;
  - the player's numbers: mass kg, speed m/s, energy J;
  - when aiming at an object: its numbers, too;
  - the swap cooldown as a small ring around the crosshair.
- **Outline** on the aimed or held object. Its color tells what the object accepts:
  - **white** = everything the room unlocked;
  - **gray** = nothing (locked);
  - one color per single property: position, mass, energy (palette = open question).

  In level 1 only white and gray appear (spec of `levels`: locked = refuses position).
- **Barriers:** their own color and the crossed icon decided in the ideas bank (📦, 🧍, ⇄). The exit grid
  shows both icons.
- **Visual style:** simple geometric shapes and flat colors (ideas bank).
- **Player names** (ranking, profile screen) are always shown in a plain `Label`, never in a
  `RichTextLabel` with BBCode on, so a name can never inject markup (profiles spec, S9).
- **What a finished run hands to `sync`:** `levelId` (the catalog id, e.g. `level-1`), `timeMs`, `swaps`,
  `deaths`, `roomRestarts` and `finishedAt`. These are the same fields and names as `POST /results` in
  the progress spec. The time is paused while the pause menu is open; the server's floor of 1 s per room
  is far below any real run.

### Physics settings (from the reports)
- **Continuous collision detection** on every `RigidBody3D` (fast light objects after an energy swap
  would pass through thin walls), plus walls at least **0.3 m** thick in the scenes.
- **Bounce** from the level file → `PhysicsMaterial.Bounce`; friction 0.6 (draft).
- The player is a `CharacterBody3D` with simulated mass (70 kg) and velocity. When it walks into a
  `RigidBody3D`, it pushes it with an impulse that depends on both masses, so a 300 kg crate barely moves.
- Collision layers: `world`, `player`, `objects`, `barrier-objects` (only objects collide),
  `barrier-player` (only the player collides). Barrier 3 and kill zones are `Area3D`s.

## Tech Stack
- **Godot 4.7.2 .NET** (installed), C# 12, .NET 8; Godot's own physics (default 3D engine).
- Godot project in `game/` (`project.godot`, `Swapping.Game.csproj` with `Godot.NET.Sdk`). It references
  `Swapping.Rules`, `Swapping.Levels` and, later, `Swapping.Sync`.
- **`Swapping.GameLogic`**: a plain `net8.0` library with **no Godot reference**, holding every decision
  that can be tested without the engine: run tracker, crosshair state, outline color, free-spot search
  order, fall-death check, input → action mapping. The Godot scripts stay thin.
- Level files: an MSBuild step copies `levels/*.json` into `game/levels/` before the build (`res://levels/`;
  the copy is git-ignored), so the game and the API read the same files.

## Commands
Run from the repository root (in Git Bash, `export PATH="$PATH:/c/Program Files/dotnet"` first;
`GODOT` = the path of `Godot_v4.7.2-stable_mono_win64_console.exe`).
```
Build libs + tests:   dotnet build Swapping.sln && dotnet test Swapping.sln
Build game:           dotnet build game/Swapping.Game.csproj
Open editor:          "$GODOT" --path game --editor
Play:                 "$GODOT" --path game
Headless smoke test:  "$GODOT" --headless --path game -- --smoke-test level-1
```

## Project Structure
```
game/
  project.godot, Swapping.Game.csproj
  scenes/      → main.tscn, player.tscn, hud.tscn, pause_menu.tscn
  levels/      → level_1.tscn (scenery only: floor, walls, ledges, pit holes) + copied JSON (git-ignored)
  scripts/     → LevelRunner.cs (loads JSON, builds room pieces, owns the World),
                 Player.cs, Pieces/ (Box.cs, Ball.cs, Button.cs, Door.cs, Barrier.cs, KillZone.cs, Exit.cs),
                 SwapController.cs (aim, measure context, apply updates), Hud.cs, SmokeTest.cs
  shaders/     → outline.gdshader
src/Swapping.GameLogic/   → RunTracker, CrosshairState, OutlineColor, FreeSpotSearch, FallDeath, InputMap
tests/Swapping.GameLogic.Tests/
```

## Code Style
Same conventions as the libraries. Godot scripts are `partial` classes, use exported fields for scene
links, and never decide rules themselves:

```csharp
public partial class SwapController : Node3D
{
    public SwapResult TrySwap(Body3D target, double now)
    {
        var request = new SwapRequest(PlayerId, target.ContainerId, _room.ActiveProperty)
        {
            Context = _measure.Context(_player, target),   // distance, line of sight, barriers
        };
        var result = _swap.Apply(_room.World, request, now);
        if (result.IsApplied)
            _room.Apply(result.Updates);                   // positions, velocities, masses + make room
        return result;
    }
}
```

## Testing Strategy
- **`Swapping.GameLogic.Tests` (xUnit):** every decision moved out of Godot:
  - run tracker: restart room keeps the totals, restart level resets, the pause stops the time;
  - crosshair state for each `RejectReason`;
  - outline colors;
  - free-spot search: order, radius, blocked by a wall, none found → death/pop;
  - fall-death threshold.

  Coverage goal ≥ 90 % of lines.
- **Headless smoke test:** `--smoke-test level-1` loads the level, builds each room in turn, performs
  the scripted swaps of each room's solution, checks the expected positions, and quits with code 0
  (or 1 with the failing step). It runs from `dotnet test` through a test that starts the console exe,
  skipped with a message when Godot is not installed (e.g. in the cloud); on the user's PC it always runs.
- **Play test by the user:**
  - a checklist per room (lesson understood? number readable? pit fair?);
  - results go to a `docs/processo/PLAYTEST-nivel-1.md` (a template written with the user).

## Boundaries
- **Always:** keep decisions in `rules` / `levels` / `GameLogic`; read rooms only from the level files;
  set CCD on rigid bodies; run the tests and the smoke test before each commit that touches `game/`.
- **Ask first:** changing the feel (jump, speeds, fall threshold) after the user played; adding a NuGet or
  a Godot add-on; changing a room's lesson.
- **Never:** hard-code room pieces in a scene; decide swap legality in a Godot script; commit the copied
  `game/levels/*.json` or the `.godot/` cache.

## Success Criteria
1. `dotnet build` of the solution and of `game/` has no warnings; all tests pass.
2. Level 1 loads from `levels/level_1.json`; each of the 4 rooms builds its pieces at the file's positions.
3. In room 1.2, aiming at the box beyond the pit within 8 m shows a white crosshair; beyond 8 m, red with "fora de alcance".
4. A position swap with that box puts the player on the other side **standing on the floor** (made
   room), and the box where the player was.
5. Swapping with the locked box of room 1.4 is refused (red crosshair, `Locked`). The box can still be
   held and thrown.
6. Falling into a pit → death → the room is rebuilt, the player is at the room spawn, time and swaps kept.
7. "Restart level" → room 1.1, time and swaps zero.
8. Room 1.3 can be solved the planned way: swap with the ball → stand on the button → swap with the box
   → the door opens.
9. The HUD shows time and swaps from room 1.1, and the numbers of the player and of the aimed object.
10. The headless smoke test of level 1 passes (exit code 0).
11. `Swapping.GameLogic` has no Godot reference (architecture test) and ≥ 90 % line coverage.
12. 60 fps on the user's PC with the 4 rooms loaded (checked by the user during the play test).

## Decisions (approved 2026-11-03)
- **G1:** controls as in the table (right button swaps, `E` holds, left button throws).
- **G2:** nearest free point radius **1.5 m** for normal swaps; the quantum one (later) has its own radius.
- **G3:** fall death when landing faster than **12 m/s** (about 7 m), tuned by playing.
- **G4:** the throw in this slice uses a fixed energy; the charge bar (already decided) comes with level 5.
- **G5:** the hold distance is **2.5 m**; a held object follows a point in front of the camera with a spring.
- **G6:** the level files are copied into `game/levels/` at build time.

## Open Questions (taste: the user decides by playing)
- **Outline palette:** which color for position-only, mass-only and energy-only?
- **Jump feel:** keep the physically correct jump (70 kg → 0.5 m) or compare with a tuned "game" jump
  (the ideas bank asks for this comparison)?
- **Mouse sensitivity**, field of view, head bob (none in the draft).
- **Death and pop effects:** how much smoke, sound, screen fade (draft: 0.5 s fade to black)?
- **HUD layout:** numbers next to the crosshair or in a corner?

---

# Slice 2: menu, local progress and levels 2, 3 and 5 (2026-11-03)

Asked by the user on 2026-11-03: simple levels for the other skills, and a game menu. Levels 2
(Barreiras), 3 (Peso) and 5 (Energia) already exist in `levels/` with their design tests; each level
unlocks **one** property, so no property picker is needed.

### What changes
- **Main menu** (first screen): title, **Jogar** (continues: the first unlocked level not yet
  finished, or the last level), **Níveis**, **Controles**, **Sair**.
- **Level select:** one card per catalog level (1, 2, 3, 5), in catalog order. A locked card shows a
  lock and cannot be chosen. A finished card shows the best time and the fewest swaps.
- **Local progress** in `user://progress.json`: finished levels and the best time and best swaps per
  level (two separate bests, like the API's two rankings). Level 1 is always unlocked; finishing a level
  unlocks the next one in the catalog. An unreadable or unknown file → fresh progress (the old file is
  kept as `progress.corrupt.json`). Unknown level ids in the file are ignored.
- **End screen:** buttons **Próximo nível** (hidden on the last level) and **Menu**.
- **Pause menu:** gains **Menu**.
- **Levels 2, 3 and 5** get their Godot scenery (`level_N.tscn`), built the same way as level 1.
  Barriers are seen-through colored walls: objects barrier, player barrier, no-swap zone (three colors).
- `--level <id>` starts that level directly (tests and screenshots); with no arguments the menu opens.
  The smoke test takes the level id as before.

### Not in this slice
`sync` with the API, level 4, the charge bar of the throw, sound.

### Success criteria (slice 2)
13. `LevelProgress` (in `Swapping.GameLogic`, no Godot) unlocks in catalog order, keeps the two bests
    independently, ignores unknown ids, and round-trips to JSON; a corrupt text gives fresh progress.
14. "Continue" picks the first unlocked level not finished; when all are finished, the last level.
15. The headless menu check: the menu opens with no arguments; a locked card can't be started; finishing
    level 1 in the smoke run unlocks level 2 and writes the progress file.
16. Headless scripted runs of levels 2, 3 and 5 finish each level (exit code 0), each room solved the
    planned way.
17. Line coverage of `Swapping.GameLogic` stays ≥ 90 %; zero warnings; all tests green.
