# Implementation Plan: `levels` module

> Produced with `agent-skills:planning-and-task-breakdown` from
> [`docs/specs/SPEC-levels.md`](../../docs/specs/SPEC-levels.md) (approved 2026-10-31).
> Task checklist: [`todo.md`](todo.md). Each module keeps its plan in `tasks/<module-id>/`
> (the finished `rules` plan is in [`tasks/rules/`](../rules/)).

## Overview
Build `Swapping.Levels`, a C# library that parses, validates and converts level files into `rules`
types, plus the 4 level files for Friday (levels 1, 2, 3, 5; 11 rooms) and the catalog. Every success
criterion of the spec becomes at least one test, written before the code (TDD).

## Architecture Decisions
- **Same layout as `rules`:** `src/Swapping.Levels` + `tests/Swapping.Levels.Tests`, both in `Swapping.sln`.
  The library references only `Swapping.Rules`; JSON uses `System.Text.Json` (no NuGet).
- **Parse, then validate.** The parser only turns text into the typed model (shape errors → `InvalidJson`
  with a path). Every meaning check lives in the validator, so each error code is testable in isolation.
- **One valid level in the tests as the base fixture.** Each validator test copies it and breaks one
  field, so "produces this code and only this code" is easy to check.
- **The validator is split in two tasks:** value checks (one field at a time) and spatial/reference checks
  (boxes, overlaps, links). The spatial part is the riskiest, so it comes before the content files.
- **Content last.** The level files are written once the validator can catch mistakes in them; the room
  design tests are written before each file (TDD for content).
- **The only IO is in `LevelCatalog`** (reading `levels/`); everything else works on strings and objects.

## Dependency graph
```
Model records + Box                                  (Task 1)
  └── LevelParser (JSON → model)                     (Task 2)
        └── LevelValidator: value checks             (Task 3)
              └── LevelValidator: spatial + references (Task 4)
                    └── RulesMapping (LevelRules, BuildWorld) (Task 5)
                          └── Catalog + levels 1 and 2        (Task 6)
                                └── Levels 3 and 5            (Task 7)
                                      └── Architecture, coverage, docs (Task 8)
```

## Task List
### Phase 1: Foundation
- [x] Task 1: Scaffold the projects and the model
- [x] Task 2: Parser

### Checkpoint: Foundation

### Phase 2: Validation and mapping
- [ ] Task 3: Validator — value checks
- [ ] Task 4: Validator — spatial and reference checks
- [ ] Task 5: Mapping to `rules`

### Checkpoint: Core

### Phase 3: Content and wrap-up
- [ ] Task 6: Catalog and levels 1 and 2
- [ ] Task 7: Levels 3 and 5
- [ ] Task 8: Architecture test, coverage and documentation

### Checkpoint: Complete

## Risks and Mitigations
| Risk | Impact | Mitigation |
|---|---|---|
| Room numbers drafted without seeing the Godot scene don't play well | Med | The spec says numbers are a draft; design tests check lessons, not exact positions; tuning happens Tue/Wed |
| `System.Text.Json` exceptions leak out of the parser | Med | Catch `JsonException` at the boundary and turn it into `InvalidJson` with the JSON path; test it |
| Floating-point noise in box overlap (touching vs overlapping) | Med | Touching faces do **not** overlap (strict inequality); tests for touching boxes |
| Tests can't find `levels/` from the test bin folder | Low | Walk up to `Swapping.sln`, like `ArchitectureTests` in `rules` |
| The coverage tool injects types (seen in `rules`) | Low | Architecture tests ignore the `Coverlet` namespace from the start |

## Open Questions
- None blocking. Decisions taken alone during implementation are logged in `docs/processo/DIARIO.md`.
