# Implementation Plan: `game` — Godot client, first slice (level 1)

> Produced with `agent-skills:planning-and-task-breakdown` from
> [`docs/specs/SPEC-game.md`](../../docs/specs/SPEC-game.md) (approved by the user on 2026-11-03, with
> decisions G1–G6 as written). Task checklist: [`todo.md`](todo.md). Success criteria of the spec are
> cited as "SC n".

## Overview
Build the first playable slice of Swapping in Godot 4.7.2 .NET. The player walks the 4 rooms of level 1,
holds and throws boxes, swaps position, dies and restarts rooms, and finishes the level with time and
swaps on screen. Every decision that can live outside the engine goes into a new pure library,
`Swapping.GameLogic`, tested with xUnit. A **headless smoke test** drives the real Godot scenes and
solves level 1 by script, so the agent can check the game without seeing it. The play test by the user
covers feel and looks.

## Architecture Decisions
- **Three layers:**
  - `rules` and `levels`: they decide. They already exist and are not changed;
  - `Swapping.GameLogic` (`net8.0`, no Godot): it decides what is left (run totals, crosshair, outline
    color, free-spot search order, fall death, input mapping, button state);
  - Godot scripts in `game/`: they measure and apply. They are thin.
- **Fail fast on the toolchain:** Task 1 is the Godot project, the copy of the level files, and the
  headless `--smoke-test` entry point running from `dotnet test`. If Godot + C# + our libraries do not
  build and run headless on day 1, everything else is moot.
- **The smoke test grows with each task.** Every Godot task adds checks to `SmokeTest.cs` (pieces at the
  file positions, crosshair states, the made-room swap, death and restart, the scripted solution), so
  each task has an automated check even though it touches the engine.
- **Screenshots for the agent:**
  - `--screenshot <room>` (window on, not headless) saves a PNG of a room;
  - the agent opens it to check scenery and HUD;
  - the PNGs are evidence for the report (`docs/evidencias/`).
- **Rooms are built from the JSON at run time** (`LevelRunner`). `level_1.tscn` holds only the scenery:
  floor, walls at least 0.3 m thick, the pits and the ledge of 1.3. "Restart room" = free the room's
  pieces and build them again with `RulesMapping.BuildWorld`.
- **The game project is in `Swapping.sln`,** so `dotnet build Swapping.sln` builds the Godot assembly
  before the smoke test runs.
- **Collision layers:** `world` 1, `player` 2, `objects` 3, `barrier-objects` 4, `barrier-player` 5.
  Kill zones and barrier 3 are `Area3D`s. CCD is on for every `RigidBody3D`.

## Dependency graph
```
Godot project + level copy + headless smoke entry (Task 1)
  └── GameLogic library + RunTracker + architecture test (Task 2)
        └── Scenery + objects and kill zones from the JSON (Task 3)
              └── Buttons, doors, barriers, exit grid (Task 4)
                    └── Player: walk, jump, look, hold, throw, push (Task 5)
                          └── Swap: aim, crosshair, measure, apply, sync (Task 6)
                                └── Make room: free-spot search after a position swap (Task 7)
                                      └── Deaths, restarts, exits, run end, pause menu (Task 8)
                                            └── HUD and outline (Task 9)
                                                  └── Level 1 solved by script end to end (Task 10)
                                                        └── Architecture, coverage, play test, report (Task 11)
```

## Task List
### Phase 1: Foundation
- [x] Task 1: Godot project, level files copy and headless smoke-test entry
- [x] Task 2: `Swapping.GameLogic` with the run tracker and the architecture test

### Checkpoint: Foundation
- [x] Godot builds with zero warnings; `dotnet test` runs the headless smoke test (exit 0)

### Phase 2: The room and the player
- [x] Task 3: Scenery of level 1, objects and kill zones built from the file
- [x] Task 4: Buttons, doors, barriers and the exit grid
- [x] Task 5: The player: walk, jump, look, hold, throw, push

### Phase 3: The swap (the riskiest part)
- [x] Task 6: Aim, crosshair state, measured context, apply and sync
- [ ] Task 7: Make room after a position swap

### Checkpoint: The swap is playable
- [ ] Room 1.2 works by script (SC 3, 4) and **the user plays rooms 1.1–1.2** before Phase 4

### Phase 4: The level
- [ ] Task 8: Deaths, restarts, exits, end of run and pause menu
- [ ] Task 9: HUD and outline
- [ ] Task 10: Level 1 solved by script, end to end

### Phase 5: Wrap-up
- [ ] Task 11: Architecture, coverage, play-test sheet, screenshots and report

### Checkpoint: Complete
- [ ] SC 1–11 have an automated check; SC 12 and the feel are in the user's play-test sheet

## Risks and Mitigations
| Risk | Impact | Mitigation |
|---|---|---|
| Godot C# build fails or warns (source generators + `TreatWarningsAsErrors`), or the NuGet restore of `Godot.NET.Sdk` fails | High | Task 1 first; the Godot install ships the 4.7.2 nupkgs locally (`GodotSharp/Tools/nupkgs`), usable as a local source; any suppressed warning is named in the diary |
| The agent cannot see the game | High | Headless smoke test with scripted checks in every Godot task, `--screenshot` PNGs that the agent opens, play test by the user at the swap checkpoint |
| Headless physics is not like the real game (timing, `_PhysicsProcess` order) | Med | The smoke test steps physics frames and waits for bodies to settle before checking, with tolerances (±0.05 m). Feel is judged only by the user |
| Position swap puts bodies inside the floor or walls | Med | Task 7 isolates it: the pure `FreeSpotSearch` (order, radius 1.5 m, walls) plus a Godot shape query; SC 4 checks it |
| Player pushing a rigid body gives jitter or launches it | Med | Simple impulse ∝ mass ratio, capped; tuned in the play test (Boundaries: ask before changing feel after the user played) |
| Time: Tuesday afternoon + Wednesday for 11 tasks | High | The swap checkpoint (after Task 7) is the minimum demo. Tasks 8–10 complete the level, and Task 11 can move to Thursday morning. Levels 2, 3 and 5 are **not** in this plan; they get a spec revision later |
| Stale Godot assembly in a headless run | Low | The smoke-test runner checks that `Swapping.Game.dll` is newer than every `game/scripts/*.cs`, and fails with a message otherwise |

## Open Questions
- None blocking. The taste questions of the spec (palette, jump feel, mouse, effects, HUD layout) go to
  the play-test sheet. Decisions taken alone go to `docs/processo/DIARIO.md`.
