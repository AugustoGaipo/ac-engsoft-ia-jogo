# Tasks: `rules` module

Spec: [`docs/specs/SPEC-rules.md`](../docs/specs/SPEC-rules.md) · Plan: [`plan.md`](plan.md)
"SC n" = success criterion n of the spec.

Common verification (every task):
`export PATH="$PATH:/c/Program Files/dotnet"; dotnet build Swapping.sln && dotnet test Swapping.sln`

---

## Task 1: Scaffold the solution and test project
**Description:** Create `Swapping.sln`, the `Swapping.Rules` class library (`net8.0`, nullable on,
warnings as errors) and the `Swapping.Rules.Tests` xUnit project referencing it.
**Acceptance criteria:**
- [x] `dotnet build` succeeds with zero warnings; `dotnet test` runs a smoke test.
- [x] `Swapping.Rules.csproj` has no package or project references (SC 14 baseline).
- [x] `.gitignore` already covers `bin/` and `obj/`.
**Dependencies:** None
**Files:** `Swapping.sln`, `Directory.Build.props`, `src/Swapping.Rules/Swapping.Rules.csproj`,
`tests/Swapping.Rules.Tests/Swapping.Rules.Tests.csproj`, one smoke test
**Scope:** S

## Task 2: Physics formulas and media
**Description:** `Vec3` and the pure formulas: kinetic energy, speed from energy, density, buoyancy
force and state, static pressure, throw recoil, jump take-off speed and height; `Medium` with
density and drag (air, dense gas, water).
**Acceptance criteria:**
- [x] SC 3 (2 kg at 3 m/s → 8 kg → 1.5 m/s), SC 10 (pressure 9 800 Pa, glass limits), SC 11
      (swim / sink / swim-in-air), SC 12 (jump heights) pass as formula tests.
- [x] Recoil example: 0.05 kg throws 0.01 kg at 5 m/s → 1 m/s back; dropping → 0.
- [x] Buoyancy band edges (exactly 0.1× and 1.1×) are Swim.
**Dependencies:** Task 1
**Files:** `Model/Vec3.cs`, `Physics/Formulas.cs`, `Physics/Medium.cs`, `FormulaTests.cs`
**Scope:** M

### Checkpoint: Foundation
- [x] Build clean, all tests green, commit + push.

## Task 3: Variables, containers and the world invariant
**Description:** `PropertyKind` (Position, Mass, KineticEnergy, behind `IPropertyKind`), `Variable`
(id, kind, owner, value), `Container` (id, player/object, volume, contact area, locked, accepted
properties), `World` (registers containers, creates one owned variable per property, tracks who uses
which, `ExchangeVariables`, `CheckInvariant`).
**Acceptance criteria:**
- [x] A new container uses its own variables; the invariant holds.
- [x] `ExchangeVariables(a, b, kind)` swaps users of that kind only.
- [x] Unknown container id throws (programming error).
**Dependencies:** Task 2
**Files:** `Model/PropertyKind.cs`, `Model/Variable.cs`, `Model/Container.cs`, `World.cs`, `WorldTests.cs`
**Scope:** M

## Task 4: Swapping position, mass and energy
**Description:** `SwapRequest`, `SwapOperation` (behind `IOperation`), `SwapResult` and
`BodyUpdate`s computed from the new variables: position swap keeps velocities; mass swap keeps
energy and recomputes speed; energy swap moves the receiver the way the variable was moving.
Physics write-back: `World.SyncFromPhysics(id, position, velocity)`.
**Acceptance criteria:**
- [x] SC 2 first half (10/2/3 → 3/10/2), SC 3 via a mass swap of a moving body, SC 4 (ball/player), SC 5.
- [x] A 0 J receiver stops.
**Dependencies:** Task 3
**Files:** `Operations/SwapRequest.cs`, `Operations/SwapOperation.cs`, `Operations/SwapResult.cs`,
`Operations/BodyUpdate.cs`, `World.cs`, `SwapTests.cs`
**Scope:** M

## Task 5: Swap legality and cooldown
**Description:** `LevelRules` (unlocked properties, range per property, cooldown, mode and
multi-property flags), `SwapContext` (game-measured distance, line of sight, barrier between, inside
barrier), every `RejectReason`, `Cooldown`.
**Acceptance criteria:**
- [x] SC 8: one test per reason in the legality table, plus "distance == range is allowed".
- [x] SC 9: cooldown 2 s → t = 1.5 rejected, t = 2.0 applied.
- [x] Defaults: range 8 m, cooldown 1 s.
**Dependencies:** Task 4
**Files:** `Policies/LevelRules.cs`, `Policies/Cooldown.cs`, `Operations/SwapContext.cs`,
`Operations/RejectReason.cs`, `LegalityTests.cs`
**Scope:** M

### Checkpoint: Core
- [x] Build clean, all tests green, commit + push.

## Task 6: Barriers and restoration
**Description:** `BarrierFilter` (blocks objects, blocks player, blocks variables) with `Blocks...`
queries and `ExitGrid`; `Restoration` gives each of a container's own variables back from whoever holds
them, per property.
**Acceptance criteria:**
- [x] SC 2 second half (A enters → 3/2/10), SC 6 (position restoration), SC 7 (energy restoration
      reverses direction).
- [x] Restoring a container that uses only its own variables changes nothing.
- [x] Exit grid blocks objects and variables, lets the player pass.
**Dependencies:** Task 5
**Files:** `Model/BarrierFilter.cs`, `Operations/Restoration.cs`, `RestorationTests.cs`
**Scope:** M

## Task 7: Swap log, move count and randomized invariant test
**Description:** `SwapLog` records applied swaps; `MoveCount` counts swaps (simultaneous = 1),
restorations do not count. Randomized test: 1 000 steps of swaps and restorations from a fixed seed.
**Acceptance criteria:**
- [x] Restorations do not change the move count.
- [x] SC 13: invariant holds after each of 1 000 random steps.
**Dependencies:** Task 6
**Files:** `Operations/SwapLog.cs`, `World.cs`, `InvariantTests.cs`
**Scope:** S

## Task 8: Extension seams, coverage and documentation
**Description:** Make sure the seams of the spec exist (interfaces with one implementation:
`IPropertyKind`, `IOperation`, `ISwapDurationPolicy`, `ICooldownModifier`; flags for object↔object,
simultaneous swaps and entangled properties, unused). Measure coverage; add a test that the rules
assembly references no Godot assembly; document commands in the README.
**Acceptance criteria:**
- [ ] SC 14 (no Godot reference) has a test; SC 15 line coverage ≥ 90 %.
- [ ] SC 1: build with zero warnings.
- [ ] README explains how to build and test.
**Dependencies:** Task 7
**Files:** `Policies/*.cs`, `ArchitectureTests.cs`, `README.md`
**Scope:** M

### Checkpoint: Complete
- [ ] All 15 success criteria covered by passing tests.
- [ ] Diary updated with what was done and every decision taken alone.
- [ ] Commit + push; summary for the user's review.
