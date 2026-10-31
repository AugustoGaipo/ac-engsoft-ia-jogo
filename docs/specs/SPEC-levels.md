# Spec: `levels` — level files, loader and validator

> Module from [`CAPABILITY-MAP.md`](../../CAPABILITY-MAP.md). Produced with
> `agent-skills:spec-driven-development` (phase 1, Specify). Status: **approved by the user on 2026-10-31** (with the 6 assumptions below).
> Design source: [`docs/design/PROGRESSAO-NIVEIS.md`](../design/PROGRESSAO-NIVEIS.md) (approved).

## Objective

A C# library and a set of JSON files that describe **what is in each level** for gameplay purposes.
The level files are the single source of truth shared by the **game** (which builds the level) and
the **API** (which reads the level catalog: order and ids).

The library:
1. **parses** a level file into a typed model;
2. **validates** it and returns every problem found, with a path that shows where it is;
3. **converts** it into `rules` types (`LevelRules`, `Container`s and a ready `World` per room).

It also ships the **4 levels for Friday** (levels 1, 2, 3 and 5, 11 rooms) as JSON files that pass the validator.

### What lives where (decided with the user)
| Lives in the JSON (this module) | Lives in the Godot scene (`game` module) |
|---|---|
| Swap rules of the level (unlocked properties, range, cooldown, flags) | Solid scenery: floor, walls, ramps, ledges, holes in the floor |
| Rooms: id, what they teach, bounds, player spawn, exit | Lights, materials, decoration |
| Objects: kind, size, mass, accepted properties, locked, start velocity, bounce | Meshes and collision shapes of the scenery |
| Buttons, doors and which buttons open each door | |
| **Gameplay volumes:** barriers (filters 1/2/3), exit grids, kill zones (holes) | |

The JSON gives positions in **level space**. The Godot scene of the level is built around them: the
JSON says where the button and the kill zone are, and the scene puts the floor and the hole there.

> **Consequence:** the validator cannot see walls or holes, so it cannot check wall thickness or
> objects inside walls. Those checks move to the `game` module (continuous collision detection and a
> debug check in the editor). Kill zones *are* in the JSON, so "nothing spawns inside a hole" is checked here.

### Concepts
- **Level** (nível): one JSON file and one Godot scene. Its number is 1–11. It has 1 or more rooms in order.
- **Room** (sala): a region of the level (an axis-aligned box called `bounds`). It has one **player
  spawn** (where the player respawns after dying in that room) and one **exit**.
- **Room restart:** when the player dies or picks "restart room", the room is rebuilt from the file:
  every object of that room goes back to its start values. The time and swap count keep adding up
  (decided with the user; counting lives in `game`/`progress`).
- **Every room ends with an exit grid** (barrier 1 + barrier 3) on its exit. Nothing from one room
  (objects or swapped variables) reaches the next one, so restarting a room never depends on what
  happened in another room.
- **Object respawn:** an object that pops (no space after a swap) comes back at its start position,
  at rest, with its start values (decided with the user).

### File format (schema version 1)
Units are SI (m, kg, m³, m², m/s). Axes follow Godot: **Y is up**. A box is `{ "min": [x,y,z], "max": [x,y,z] }`.

```json
{
  "schemaVersion": 1,
  "id": "level-3",
  "number": 3,
  "name": "Peso",
  "scene": "res://levels/level_3.tscn",
  "rules": {
    "unlockedProperties": ["mass"],
    "rangeMeters": { "mass": 8.0 },
    "cooldownSeconds": 1.0,
    "allowObjectToObject": false,
    "allowSimultaneousSwaps": false
  },
  "rooms": [
    {
      "id": "3.1",
      "teaches": "Botão que exige mais peso que o jogador",
      "bounds": { "min": [0, 0, 0], "max": [12, 6, 10] },
      "spawn": { "position": [1, 0, 5], "yawDegrees": 90 },
      "exit": { "min": [11.5, 0, 4], "max": [12, 3, 6] },
      "rulesOverride": null,
      "objects": [
        { "id": "crate", "kind": "box", "position": [6, 0.5, 2], "size": [1, 1, 1],
          "massKg": 300, "accepts": null, "locked": false,
          "velocity": [0, 0, 0], "bounce": 0.1 }
      ],
      "buttons": [
        { "id": "heavy-button", "position": [6, 0, 7], "size": [1.5, 0.1, 1.5],
          "requiredMassKg": 150, "detects": "any" }
      ],
      "doors": [
        { "id": "door", "box": { "min": [10, 0, 4], "max": [10.3, 3, 6] }, "opensWith": ["heavy-button"] }
      ],
      "barriers": [
        { "id": "exit-grid", "preset": "exitGrid", "box": { "min": [11.5, 0, 4], "max": [11.6, 3, 6] } }
      ],
      "killZones": []
    }
  ]
}
```

Field rules:
- `accepts`: `null` = every property the level unlocked (white outline); otherwise a non-empty list
  of `"position"`, `"mass"`, `"kineticEnergy"`.
- `kind`: `"box"` (size = edges, volume = x·y·z) or `"ball"` (size = [d, d, d], volume = π·d³ ÷ 6).
  Volume is **derived**, never written, so it can't disagree with the size. The contact area is the
  bottom face for a box (x·z) and 1 % of the cross-section for a ball (it rolls on one point).
- `bounce`: 0–1, a fixed attribute of the object (Godot's physics material `bounce`; swapping it is a P2 idea).
- `detects`: `"any"` (player or objects) or `"objects"` (only objects press it).
- A door opens while **all** of its `opensWith` buttons are pressed.
- A barrier has either a `preset` (`"exitGrid"`) or explicit flags `blocksObjects`, `blocksPlayer`,
  `blocksVariables` (the three basic barriers, combinable).
- `rulesOverride`: optional; any field of `rules` set here replaces the level's value **for that room
  only** (room 1.1 uses `"unlockedProperties": []` because the player has no swap device yet).
- The player is always 70 kg, 0.07 m³ (`rules` spec); it is not written in the file.

### Catalog
`levels/catalog.json` lists the levels in play order: `[{ "id": "level-1", "file": "level_1.json" }, …]`.
Numbers must strictly increase; gaps are allowed (Friday ships 1, 2, 3, 5). The API reads only the catalog
and the level ids/numbers.

### Validation
The validator never throws on bad data: it returns a list of `LevelError(path, code, message)`,
e.g. `rooms[1].objects[0].massKg` · `NotPositive`. A file is valid when the list is empty.
Malformed JSON also becomes an error (`InvalidJson`), not an exception.

| Code | When |
|---|---|
| `InvalidJson` | not parseable, or a required field is missing or has the wrong type |
| `UnsupportedSchema` | `schemaVersion` ≠ 1 |
| `BadId` | level id is not `level-<number>`, or a room id is not `<number>.<n>` with n = 1, 2, 3… in order |
| `DuplicateId` | two pieces with the same id in the same level (all kinds share one namespace) |
| `NumberOutOfRange` | level number not in 1–11 |
| `UnknownProperty` | a property name that is not `position`, `mass` or `kineticEnergy` |
| `NotPositive` | mass, size, range or required mass ≤ 0; cooldown < 0 |
| `OutOfRange01` | bounce outside 0–1 |
| `ImplausibleDensity` | density outside 1–22 600 kg/m³ (lighter than any solid… denser than osmium): the "world principle" |
| `EmptyBox` | a box with min ≥ max on some axis |
| `OutsideRoom` | spawn, exit, or any piece not fully inside its room's bounds |
| `RoomsOverlap` | two rooms' bounds overlap |
| `UnknownReference` | a door names a button that is not in the same room |
| `DoorWithoutButton` / `UnusedButton` | a door with an empty `opensWith`, or a button no door uses |
| `EmptyBarrier` | a barrier that blocks nothing |
| `MissingExitGrid` | the room's exit is not covered by an exit grid (barrier 1 + 3) |
| `Overlap` | at start, two objects overlap, or an object overlaps a button, door, kill zone or the spawn area |
| `SpawnUnsafe` | the spawn is inside a kill zone or inside a barrier that blocks the player |
| `NotAccepted` | `accepts` lists a property the level (or room) has not unlocked |
| `CatalogOrder` | catalog numbers not strictly increasing, missing file, or id/number mismatch |

The spawn area is a box 0.6 m × 1.8 m × 0.6 m (player size) standing on the spawn position.

### Conversion to `rules`
- `level.ToLevelRules(roomId)` → `LevelRules`, with the room's override applied.
- `room.BuildWorld(levelRules)` → a new `World` with the player (`"player"`, 70 kg, 0.07 m³, at the
  spawn) and one `Container` per object (volume derived, contact area, locked, accepted properties),
  using the start position, mass and velocity. Restarting a room = calling it again.

### The 4 levels for Friday
Each file follows [`PROGRESSAO-NIVEIS.md`](../design/PROGRESSAO-NIVEIS.md). Besides passing the
validator, each room has a **design test**: a test that checks that the pieces the room's lesson needs exist.

| Room | Lesson | Design test |
|---|---|---|
| 1.1 | Walk, hold, button, door, exit grid (no swap) | no unlocked property; ≥ 1 box, 1 button, 1 door |
| 1.2 | First position swap across a gap | position unlocked; a kill zone between the spawn and the exit, and a box beyond it ≤ 8 m from the spawn |
| 1.3 | Swap to put the box on a high button | button with `detects: "objects"`; a box that accepts position |
| 1.4 | Colored outline: one box refuses position | ≥ 1 box with `accepts` without position and ≥ 1 that accepts it |
| 2.1 | Barrier 1: the box crosses by swapping | a barrier blocking objects only, a button on its other side |
| 2.2 | Barrier 2: the player crosses by swapping | a barrier blocking the player only, a box on its other side |
| 2.3 | Barrier 3: prepare swaps before the zone | a barrier blocking variables, big enough to be a zone (≥ 2 m on X and Z) |
| 3.1 | Heavy button | mass unlocked only; button with `requiredMassKg` > 70 and a box with mass ≥ it |
| 3.2 | Jump depends on mass | a box with mass ≤ 10 kg; the exit is ≥ 1.2 m above the spawn floor |
| 5.1 | Receive energy from a moving ball | energy unlocked only; a ball with non-zero velocity and bounce ≥ 0.9 |
| 5.2 | Give energy to a box at rest | a box at rest that accepts energy and a button with `detects: "objects"` |

The numbers (positions, masses) are a first draft; they are tuned on Tuesday/Wednesday while building
and playing the Godot scenes. Whether a room is *solvable* is checked by playing, not by the validator.

## Tech Stack
- C# 12, **.NET 8** class library `Swapping.Levels` (`net8.0`), referencing `Swapping.Rules`.
- JSON with `System.Text.Json` (part of .NET, **no NuGet package**).
- Tests: xUnit, like `rules`.

## Commands
Run from the repository root (in Git Bash, `export PATH="$PATH:/c/Program Files/dotnet"` first).
```
Build:    dotnet build Swapping.sln
Test:     dotnet test Swapping.sln
Coverage: dotnet test Swapping.sln --collect:"XPlat Code Coverage"
Validate: dotnet test Swapping.sln --filter "FullyQualifiedName~ShippedLevels"
```

## Project Structure
```
levels/                      → level_1.json, level_2.json, level_3.json, level_5.json, catalog.json
src/Swapping.Levels/
  Model/                     → LevelFile, RoomDef, ObjectDef, ButtonDef, DoorDef, BarrierDef, Box, SpawnDef
  LevelParser.cs             → string → (LevelFile?, errors)
  LevelValidator.cs          → LevelFile → errors
  LevelError.cs              → path, code, message
  LevelCatalog.cs            → catalog parsing and checks (the only class that reads files)
  RulesMapping.cs            → ToLevelRules, BuildWorld
tests/Swapping.Levels.Tests/
  → ParserTests, ValidatorTests (one test per error code), MappingTests,
    ShippedLevelsTests (the 4 real files + catalog), RoomDesignTests (the table above)
```

## Code Style
Same conventions as `rules` (PascalCase, file-scoped namespaces `Swapping.Levels.*`, SI units with the unit
in the name when ambiguous). Errors are values:

```csharp
public static IReadOnlyList<LevelError> Validate(LevelFile level)
{
    var errors = new List<LevelError>();
    for (var r = 0; r < level.Rooms.Count; r++)
    {
        var room = level.Rooms[r];
        foreach (var (obj, i) in room.Objects.Select((o, i) => (o, i)))
            if (obj.MassKg <= 0)
                errors.Add(new($"rooms[{r}].objects[{i}].massKg", LevelErrorCode.NotPositive, "Mass must be > 0 kg."));
    }
    return errors;
}
```

## Testing Strategy
- **One test per error code**, built from a small valid level in the tests and changing one field (the
  same "one test per reason" style as `rules`).
- **Shipped levels:** every file in `levels/` and the catalog are valid; this test runs in `dotnet test`,
  so a broken level fails the build pipeline.
- **Room design tests:** the table above, one test per room.
- **Mapping:** the `World` built from a room keeps the `rules` invariant; a swap that the lesson needs
  (e.g. player ↔ crate mass in 3.1) is legal with the level's rules.
- Coverage goal: **≥ 90 % of lines** in `Swapping.Levels`.

## Boundaries
- **Always:** keep `Swapping.Levels` free of Godot; return errors as values; run `dotnet test` before
  each commit; change the format only together with its tests and this spec.
- **Ask first:** adding a NuGet package; changing schema version 1 after the game reads it; changing a
  room's lesson (that is the approved progression).
- **Never:** reference Godot; throw on bad level data; write a level file that fails the validator.

## Success Criteria
1. `dotnet build` and `dotnet test` pass with no warnings.
2. The example file of this spec parses and is valid.
3. Each error code in the table has a passing test that produces it (and only it).
4. Malformed JSON returns `InvalidJson` with a message; no exception escapes the parser.
5. Room ids out of order (`1.1`, `1.3`) → `BadId`; duplicate piece ids across rooms → `DuplicateId`.
6. A box 1 × 1 × 1 m with 300 kg → density 300, valid; 0.001 kg → `ImplausibleDensity`; 30 000 kg → `ImplausibleDensity`.
7. A room whose exit has no exit grid → `MissingExitGrid`.
8. `rulesOverride` with `unlockedProperties: []` makes every swap in that room rejected with `PropertyNotUnlocked`.
9. `BuildWorld` for room 3.1 gives a world where the player ↔ crate mass swap is applied and the player
   then has 300 kg; the invariant holds.
10. Calling `BuildWorld` twice gives two independent worlds (room restart).
11. `levels/level_1.json`, `level_2.json`, `level_3.json`, `level_5.json` and `catalog.json` are valid
    (11 rooms total).
12. Each of the 11 room design tests passes.
13. `Swapping.Levels.csproj` references only `Swapping.Rules` (no Godot, no NuGet).
14. Line coverage ≥ 90 %.

## Decisions confirmed with the user
1. **One Godot scene per level**, rooms are regions of it (not one scene per room). Simpler to make continuous.
2. **Every room ends with an exit grid**, so a room restart never depends on another room.
3. **Holes are kill zones in the JSON** (a box under the hole); falling into one is death by falling.
   Death by a high fall (without a hole) is a `game` rule, not part of this file.
4. Door: opens while **all** of its buttons are pressed and closes when one is released.
5. Plausible density range **1–22 600 kg/m³** (the world principle). Note: the player itself can get
   lighter than that after a swap. The range only applies to how objects are **written** in the file.
6. Ball contact area = 1 % of the cross-section (needed by pressure; glass is not in Friday's levels).

## Open Questions
- None blocking. Glass, water/gas zones, launch bar and the nearest free point come with later rooms
  (schema version 1 leaves room for new piece lists such as `media` and `glass`).
