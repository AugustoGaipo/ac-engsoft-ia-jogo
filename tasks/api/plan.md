# Implementation Plan: Game API (`profiles` + `progress`)

> Produced with `agent-skills:planning-and-task-breakdown` from
> [`docs/specs/SPEC-profiles.md`](../../docs/specs/SPEC-profiles.md) and
> [`docs/specs/SPEC-progress.md`](../../docs/specs/SPEC-progress.md) (both approved 2026-11-02).
> Task checklist: [`todo.md`](todo.md). The two modules share one plan folder because they are two
> folders of **one** project (`Swapping.Api`, assumption A8); module ids stay `profiles` and `progress`.

## Overview
Build `Swapping.Api`, an ASP.NET Core minimal API with EF Core + SQLite, and its test project. First the
`profiles` module (create, login, lockout, sessions, code regeneration, rate limits, security headers),
then the `progress` module (catalog-driven unlocking, best results per mode, rankings). Every success
criterion of both specs becomes at least one test, written before the code (TDD).

## Architecture Decisions
- **Same layout as `rules` and `levels`:** `src/Swapping.Api` + `tests/Swapping.Api.Tests`, both in
  `Swapping.sln`; folders `Profiles/`, `Progress/`, `Data/`, `Hosting/`.
- **Pure parts first in each module** (`AccessCode`, `ProfileNames`, `RankingOrder`, `ResultCheck`):
  small unit tests, no HTTP, no database.
- **Vertical slices for the routes:** each task adds a route end to end (entity → service → endpoint →
  HTTP test through `WebApplicationFactory`).
- **Host hardening in the first task** (ProblemDetails without exception details, JSON strictness,
  4 KB body limit, `nosniff`, `127.0.0.1` in `Development`), so every later route gets it for free.
- **SQLite in memory per test** (one open connection kept by the factory) and a fake `TimeProvider`,
  so lockout and token expiry are tested without waiting.
- **Atomic lockout counter** with `ExecuteUpdateAsync` (S2), tested with 20 concurrent requests.
- **Pinned NuGet versions + lock files** from the first task (S15).

## Dependency graph
```
Host + test factory + hardening                      (Task 1)
  └── AccessCode + ProfileNames (pure)               (Task 2)
        └── POST /profiles, GET /icons               (Task 3)
              └── Bearer auth, /profiles/me, logout, expiry (Task 4)
                    └── POST /sessions + lockout     (Task 5)
                          └── Regenerate code + security log + no-store (Task 6)
                                └── Rate limits, HTTPS, CORS, forwarded headers (Task 7)
                                      └── Progress pure parts + GET /levels (Task 8)
                                            └── POST /results + /progress/me (Task 9)
                                                  └── Rankings (Task 10)
                                                        └── Coverage, audit, docs (Task 11)
```

## Task List
### Phase 1: Foundation
- [x] Task 1: Scaffold the API, the test factory and the host hardening
- [x] Task 2: Access code and name rules

### Checkpoint: Foundation

### Phase 2: `profiles`
- [x] Task 3: Create a profile
- [x] Task 4: Bearer token, my profile and logout
- [x] Task 5: Login and lockout
- [x] Task 6: Regenerate the code and the security log
- [x] Task 7: Rate limits and transport rules

### Checkpoint: `profiles` complete

### Phase 3: `progress`
- [x] Task 8: Catalog, ranking order and plausibility
- [x] Task 9: Send a result and see my progress
- [ ] Task 10: Rankings

### Phase 4: Wrap-up
- [ ] Task 11: Architecture, coverage, dependency audit and report

### Checkpoint: Complete

## Risks and Mitigations
| Risk | Impact | Mitigation |
|---|---|---|
| Rate limiter state leaks between tests (one static limiter per host) | Med | Each test class builds its own factory; limits lowered or raised through test configuration |
| SQLite in memory disappears when the connection closes | Med | The factory opens one `SqliteConnection` and keeps it for the test's lifetime |
| Concurrent lockout test is flaky | Med | The counter is one SQL `UPDATE … WHERE` (S2); the test asserts counts, not order |
| `ExecuteUpdateAsync` not translated by SQLite provider for some expression | Low | Keep the update expression simple (`FailedAttempts + 1`, `LockedUntil`); test it first in Task 5 |
| NuGet restore needs the internet; lock-file mode breaks restore after a version change | Low | Pin versions in Task 1, commit `packages.lock.json`, regenerate only on purpose |
| Day runs out before Task 11 | High | Tasks 1–10 each leave the API working; Task 11 is docs and checks, can slip to Wednesday |

## Open Questions
- None blocking. Decisions taken alone during implementation are logged in `docs/processo/DIARIO.md`.
