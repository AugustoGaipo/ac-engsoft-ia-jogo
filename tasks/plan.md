# Implementation Plan: `rules` module

> Produced with `agent-skills:planning-and-task-breakdown` from
> [`docs/specs/SPEC-rules.md`](../docs/specs/SPEC-rules.md) (approved 2026-10-31).
> Task checklist: [`tasks/todo.md`](todo.md).

## Overview
Build `Swapping.Rules`, the pure C# library that decides what a swap does and whether it is allowed,
plus its xUnit test project. Every success criterion of the spec becomes at least one test, written
before the code (TDD).

## Architecture Decisions
- **One solution, one project per module.** `Swapping.sln` at the root; `src/Swapping.Rules`,
  `tests/Swapping.Rules.Tests`. Later modules (`levels`, `profiles`…) get their own projects.
- **Warnings are errors** (`Directory.Build.props`) so criterion 1 ("no warnings") is enforced by the build.
- **Physics first, then the model.** The formulas are pure functions with no dependencies, so they are
  the safest foundation and they pin down every number in the spec early.
- **The world is a registry of variables.** `World` owns all `Variable`s and a "who uses what" map;
  every mutation goes through `World.ExchangeVariables`, which is the only place the invariant can break,
  so it is checked there.
- **Game-measured facts come in a context object.** Distance, line of sight and barrier crossing are
  fields of `SwapContext`; the library never raycasts.
- **Extension points are interfaces, not features.** `IPropertyKind`, `IOperation`, duration and cooldown
  policies exist as seams with a single implementation each. Nothing speculative is implemented.

## Dependency graph
```
Vec3 ── Formulas / Medium                (Task 2)
  │
  └── PropertyKind ── Variable ── Container ── World (invariant)      (Task 3)
                                                 │
                         SwapOperation + BodyUpdates                  (Task 4)
                                                 │
                         LevelRules + SwapContext + legality + cooldown (Task 5)
                                                 │
                         Barriers + Restoration                        (Task 6)
                                                 │
                         SwapLog + move count + random invariant test  (Task 7)
                                                 │
                         Seams + coverage + docs                       (Task 8)
```

## Task List
See [`todo.md`](todo.md) for acceptance criteria and verification of each task.

### Phase 1: Foundation
- [x] Task 1: Scaffold the solution and test project
- [x] Task 2: Physics formulas and media

### Checkpoint: Foundation

### Phase 2: Core model and swaps
- [x] Task 3: Variables, containers and the world invariant
- [ ] Task 4: Swapping position, mass and energy
- [ ] Task 5: Swap legality and cooldown

### Checkpoint: Core

### Phase 3: Barriers and bookkeeping
- [ ] Task 6: Barriers and restoration
- [ ] Task 7: Swap log, move count and randomized invariant test
- [ ] Task 8: Extension seams, coverage and documentation

### Checkpoint: Complete

## Risks and Mitigations
| Risk | Impact | Mitigation |
|---|---|---|
| Floating-point noise breaks exact comparisons | Med | All numeric asserts use a tolerance (`1e-3` unless the spec says otherwise) |
| Energy direction undefined when speed is 0 | Med | Variables keep the last non-zero direction; a 0 J receiver stops (spec) |
| The model grows into a game engine | High | Only what the spec's criteria need; everything else stays a seam |
| `dotnet` not on PATH in Git Bash | Low | Commands in the spec prepend `/c/Program Files/dotnet` |
| Coverage tool needs extra packages | Low | `coverlet.collector` comes with the xUnit template, no new dependency |

## Open Questions
- None blocking. Decisions taken alone during implementation are logged in `docs/processo/DIARIO.md`.
