# Tasks: `levels` module

Spec: [`docs/specs/SPEC-levels.md`](../../docs/specs/SPEC-levels.md) · Plan: [`plan.md`](plan.md)
"SC n" = success criterion n of the spec.

Common verification (every task):
`export PATH="$PATH:/c/Program Files/dotnet"; dotnet build Swapping.sln && dotnet test Swapping.sln`
(all `rules` tests must stay green too).

---

## Task 1: Scaffold the projects and the model
**Description:** Create `Swapping.Levels` (`net8.0`, references `Swapping.Rules` only) and
`Swapping.Levels.Tests` (xUnit), add both to `Swapping.sln`. Add the model records of the spec:
`LevelFile`, `LevelRulesDef`, `RoomDef`, `SpawnDef`, `ObjectDef` (with derived `VolumeM3`, `ContactAreaM2`,
`DensityKgM3`), `ButtonDef`, `DoorDef`, `BarrierDef`, `KillZoneDef`, `Box` (`Contains`, `Overlaps` with
touching faces not overlapping), `LevelError` and `LevelErrorCode` (all codes of the spec).
**Acceptance criteria:**
- [x] Build has zero warnings; a smoke test runs.
- [x] Box 1 × 1 × 1 → volume 1 m³, contact 1 m²; ball d = 1 → volume π/6, contact 1 % of π/4.
- [x] Two boxes that only touch do not overlap; a box inside another is contained.
**Dependencies:** None
**Files:** `src/Swapping.Levels/Swapping.Levels.csproj`, `Model/*.cs`, `LevelError.cs`,
`tests/Swapping.Levels.Tests/Swapping.Levels.Tests.csproj`, `ModelTests.cs`
**Scope:** M

## Task 2: Parser
**Description:** `LevelParser.Parse(string json)` → `(LevelFile? level, IReadOnlyList<LevelError> errors)`
with `System.Text.Json` (camelCase). Missing required fields, wrong types and malformed text become
`InvalidJson` with the JSON path; no exception escapes. Vectors are 3-number arrays.
**Acceptance criteria:**
- [x] SC 2 (first half): the spec's example file parses into the expected values.
- [x] SC 4: malformed JSON, a missing `rooms` and a 2-number vector → `InvalidJson` with a path, no exception.
- [x] `accepts: null` and an absent `rulesOverride` parse as "not set".
**Dependencies:** Task 1
**Files:** `LevelParser.cs`, `tests/.../ParserTests.cs`, `tests/.../Fixtures.cs` (the spec example as the base valid level)
**Scope:** M

### Checkpoint: Foundation
- [x] Build clean, all tests green, commit + push.

## Task 3: Validator — value checks
**Description:** `LevelValidator.Validate(LevelFile)` with the checks that look at one field or one
list: `UnsupportedSchema`, `BadId`, `DuplicateId`, `NumberOutOfRange`, `UnknownProperty`, `NotPositive`,
`OutOfRange01`, `ImplausibleDensity`, `EmptyBox`, `NotAccepted`, `EmptyBarrier`.
**Acceptance criteria:**
- [ ] One test per code above: the base fixture with one field broken gives that code only, with the right path.
- [ ] SC 5 (room ids out of order → `BadId`; duplicate ids across rooms → `DuplicateId`), SC 6 (density 300 ok; 0.001 kg and 30 000 kg → `ImplausibleDensity`).
- [ ] The base fixture validates with zero errors.
**Dependencies:** Task 2
**Files:** `LevelValidator.cs`, `tests/.../ValidatorValueTests.cs`
**Scope:** M

## Task 4: Validator — spatial and reference checks
**Description:** The checks that compare pieces: `OutsideRoom`, `RoomsOverlap`, `UnknownReference`,
`DoorWithoutButton`, `UnusedButton`, `MissingExitGrid`, `Overlap` (objects with each other, with buttons,
doors, kill zones and the 0.6 × 1.8 × 0.6 m spawn area), `SpawnUnsafe`.
**Acceptance criteria:**
- [ ] One test per code above (base fixture with one thing broken → that code only).
- [ ] SC 7: a room whose exit has no exit grid → `MissingExitGrid`; an exit grid made of explicit flags (1 + 3) also counts.
- [ ] SC 3 complete: every code of the spec table has a passing test (Tasks 2–4 together).
**Dependencies:** Task 3
**Files:** `LevelValidator.cs` (or `SpatialChecks.cs`), `tests/.../ValidatorSpatialTests.cs`
**Scope:** M

## Task 5: Mapping to `rules`
**Description:** `RulesMapping.ToLevelRules(level, roomId)` (room override applied) and
`RulesMapping.BuildWorld(level, roomId)` → a new `World` with the player (`"player"`, 70 kg, 0.07 m³ at the
spawn) and one `Container` per object (derived volume and contact area, locked, accepted properties) at its
start position, mass and velocity.
**Acceptance criteria:**
- [ ] SC 8: `unlockedProperties: []` override → every swap in that room is `PropertyNotUnlocked`.
- [ ] SC 9: room 3.1 of the spec example → player ↔ crate mass swap applied, player then 300 kg, invariant holds.
- [ ] SC 10: two `BuildWorld` calls give independent worlds.
**Dependencies:** Task 4
**Files:** `RulesMapping.cs`, `tests/.../MappingTests.cs`
**Scope:** S

### Checkpoint: Core
- [ ] Build clean, all tests green, commit + push.

## Task 6: Catalog and levels 1 and 2
**Description:** `LevelCatalog` (reads `levels/catalog.json` and the files it lists; `CatalogOrder`
check). Write the room design tests for 1.1–1.4 and 2.1–2.3 first (spec table), then `levels/level_1.json`
and `levels/level_2.json` following `docs/design/PROGRESSAO-NIVEIS.md`.
**Acceptance criteria:**
- [ ] `CatalogOrder` has tests (numbers not increasing, missing file, id/number mismatch).
- [ ] Both files pass the validator (`ShippedLevelsTests`) and the 7 room design tests pass.
**Dependencies:** Task 5
**Files:** `LevelCatalog.cs`, `levels/catalog.json`, `levels/level_1.json`, `levels/level_2.json`,
`tests/.../ShippedLevelsTests.cs`, `tests/.../RoomDesignTests.cs`
**Scope:** M

## Task 7: Levels 3 and 5
**Description:** Room design tests for 3.1, 3.2, 5.1, 5.2 first, then `levels/level_3.json` and
`levels/level_5.json`; add both to the catalog.
**Acceptance criteria:**
- [ ] SC 11: the 4 level files and the catalog are valid (11 rooms total).
- [ ] SC 12: all 11 room design tests pass.
**Dependencies:** Task 6
**Files:** `levels/level_3.json`, `levels/level_5.json`, `levels/catalog.json`, `tests/.../RoomDesignTests.cs`
**Scope:** S

## Task 8: Architecture test, coverage and documentation
**Description:** `ArchitectureTests` for `Swapping.Levels` (references only `Swapping.Rules`, no Godot,
no NuGet; ignore the `Coverlet` namespace in any reflection test). Measure coverage. Document the level
format and commands in the README (a short section that points to the spec).
**Acceptance criteria:**
- [ ] SC 13 has a test; SC 14: line coverage of `Swapping.Levels` ≥ 90 %, measured with the coverage
      collector **and** with every test passing in that same run.
- [ ] SC 1: build with zero warnings.
**Dependencies:** Task 7
**Files:** `tests/.../ArchitectureTests.cs`, `README.md`
**Scope:** S

### Checkpoint: Complete
- [ ] All 14 success criteria covered by passing tests.
- [ ] Diary updated with what was done and every decision taken alone.
- [ ] `docs/processo/RELATORIO-levels.md` written (same structure as the `rules` report).
- [ ] Commit + push; summary for the user's review.
