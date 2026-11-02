# Spec: `progress` — level unlocking, best results and rankings (Game API)

> Module from [`CAPABILITY-MAP.md`](../../CAPABILITY-MAP.md). Written following the process of
> `agent-skills:spec-driven-development` (phase 1, Specify) during the autonomous loop.
> Status: **draft, waiting for the user's review**. No code is written until it is approved.
> Depends on: [`SPEC-profiles.md`](SPEC-profiles.md) (bearer token → profile id, `uploads` rate limit)
> and [`SPEC-levels.md`](SPEC-levels.md) (`levels/catalog.json` and the level files).
> Sources: [ideas bank](../ideas/BANCO-DE-IDEIAS.md) ("Ranking com dois modos", "Níveis, salas e
> reinícios", "Fantasma"), [level progression](../design/PROGRESSAO-NIVEIS.md), [C4](../architecture/C4.md).

## Objective

The part of the **Game API** that keeps **how far each player got and how well**:
1. **unlocks levels in order**, following `levels/catalog.json` (the same file the game reads);
2. receives a **finished run** of a level and keeps the **best result** per level and per ranking mode;
3. serves the **rankings** of each level in **two modes**;
4. leaves a **slot for the ghost** (a recording of the best runs), designed now and built later.

It does **not** replay or check the run itself: the swap rules run in the client and Godot physics is
not deterministic (C4, decision 2). The server only checks that a result is **plausible** (level exists,
is unlocked, values in range). Re-sending the same result is always safe, because the server keeps the
best (needed by the `sync` upload queue).

### Concepts
- **Run** (tentativa): one attempt at a whole level, from entering its first room to crossing the exit
  of its last room. Its result is **time** (ms) and **swaps** (count), summed over all rooms.
- **Restart rules** (decided with the user; counted by the `game`, documented here because they define
  what a result means):
  | Event | Time and swaps | Same run? |
  |---|---|---|
  | Death (fall, kill zone, no space) | keep adding | yes, the room is rebuilt |
  | Menu "restart room" | keep adding | yes, the room is rebuilt |
  | Menu "restart level" | back to zero | **no**: the current run is dropped and a new one starts |
  | Quitting the level before the end | — | the run is dropped, nothing is sent |
  A swap counts as 1, a simultaneous swap as 1, restorations by a barrier as 0 (`rules` spec).
  The run also reports `deaths` and `roomRestarts` for information; they are **not** ranked.
- **Unlocking:** the first level of the catalog is always unlocked. A level is unlocked for a profile
  once the **previous level in the catalog** has a result from that profile. Gaps in the numbers do
  not matter (Friday: 1 → 2 → 3 → 5).
- **Ranking modes:**
  - `time`: lowest time first; ties broken by fewer swaps, then by who got it first;
  - `moves`: fewest swaps first; ties broken by lower time, then by who got it first.
- **Best result:** for each (profile, level) the server keeps **two** rows, one per mode, because the
  fastest run and the run with fewest swaps may be different runs. A new result replaces a row only if
  it is **strictly better in that mode's order**. Nothing else is stored about a run.

### Endpoints (JSON over HTTPS)
| Method and path | Auth | Body → response |
|---|---|---|
| `POST /results` | bearer, `uploads` limit | `{ levelId, timeMs, swaps, deaths, roomRestarts, finishedAt }` → **200** `{ improvedTime, improvedMoves, unlockedNext }` |
| `GET /progress/me` | bearer | → **200** `[{ levelId, number, name, unlocked, bestTime: {timeMs, swaps} \| null, bestMoves: {…} \| null }]` in catalog order |
| `GET /rankings/{levelId}?mode=time\|moves&top=N` | — | → **200** `{ levelId, mode, entries: [{ rank, name, icon, timeMs, swaps }] }`, `top` 1–100, default 20 |
| `GET /rankings/{levelId}/me?mode=…` | bearer | → **200** `{ rank, timeMs, swaps, totalPlayers }` or **404** `NoResult` |
| `GET /levels` | — | → **200** `[{ levelId, number, name, rooms }]` from the catalog |

Ranks are plain positions 1, 2, 3…: the last tie-break (who got it first) makes every rank unique.
`finishedAt` is informational (shown later in the game); the order uses the **server** time of the
first upload of that result, so a client clock cannot jump the queue. *Assumption B5.*

### Plausibility checks (`POST /results`)
| HTTP | `code` | When |
|---|---|---|
| 400 | `InvalidBody` | malformed JSON, missing/unknown field, body > 4 KB |
| 404 | `UnknownLevel` | `levelId` is not in the catalog |
| 409 | `LevelLocked` | the previous level has no result from this profile |
| 422 | `ImplausibleResult` | `timeMs` < 1 000 × number of rooms of the level, or > 86 400 000 (24 h); `swaps` < 0 or > 10 000; `deaths` or `roomRestarts` < 0 or > 10 000 |
| 400 | `InvalidQuery` | `mode` not `time`/`moves`, `top` not an integer 1–100 (ranking routes) |
| 401 / 429 | from `profiles` | bad token / `uploads` limit (30 per minute per profile) |

Re-sending a result that is not better returns **200** with `improvedTime: false, improvedMoves: false`
(not an error), so the `sync` queue can drop it. *Assumption B3.*

`LevelLocked` is a **409, not a 4xx the queue drops**: `sync` sends in completion order, so it only happens
if an upload before it failed; `sync` retries it later (rule for the `sync` spec).

### Ghost slot (P2, not built in this version)
- Each best-result row has a nullable `GhostId` column from day one, so adding ghosts needs no table change.
- Planned (not implemented): `PUT /results/{levelId}/{mode}/ghost` (binary body, ≤ 256 KB, format
  version byte + samples of 20 Hz × (x, y, z, yaw, pitch) as `float32`) accepted only if it matches the
  current best row; `GET /rankings/{levelId}/ghosts?mode=…` for the top 3. Size check and format check
  happen before anything is stored.
- In this version those routes do not exist (404), and `POST /results` rejects a `ghost` field as an
  unknown field (`InvalidBody`).

## Tech Stack
Same as [`SPEC-profiles.md`](SPEC-profiles.md): C# 12, .NET 8, ASP.NET Core minimal APIs in
`Swapping.Api`, EF Core 8 + SQLite in the shared `GameDbContext`, `TimeProvider`, xUnit +
`WebApplicationFactory` with SQLite in memory. **Plus** a project reference to `Swapping.Levels`
(the API reads the catalog and the level files with `LevelCatalog.Load` at startup). No new NuGet package.
The path of the `levels/` folder comes from configuration `Progress:LevelsDirectory`. The API refuses to
start if the catalog has errors.

## Commands
Run from the repository root (in Git Bash, `export PATH="$PATH:/c/Program Files/dotnet"` first).
```
Build:    dotnet build Swapping.sln
Test:     dotnet test Swapping.sln
Coverage: dotnet test Swapping.sln --collect:"XPlat Code Coverage"
Run API:  dotnet run --project src/Swapping.Api
```

## Project Structure
```
src/Swapping.Api/
  Progress/
    LevelDirectory.cs       → catalog loaded once: order, ids, numbers, names, room count (from Swapping.Levels)
    BestResultEntity.cs     → (ProfileId, LevelId, Mode) key; TimeMs, Swaps, Deaths, RoomRestarts,
                              FinishedAt, AchievedAt (server), GhostId (nullable, unused)
    RankingOrder.cs         → the two orders as pure comparers (unit-tested)
    ResultCheck.cs          → plausibility rules (pure, unit-tested)
    ProgressService.cs      → submit, unlocking, progress view, rankings
    ProgressEndpoints.cs    → MapProgress(): the routes above
tests/Swapping.Api.Tests/
  Progress/ → RankingOrderTests, ResultCheckTests, ProgressServiceTests, ProgressEndpointTests
            (test catalog with 3 levels and a gap, built in the test from small level files)
```
`Progress/` may use `Profiles/` only through the authenticated profile id on the request and the
`uploads` rate-limit policy name.

## Code Style
Same conventions as `profiles`. The two modes are data, not two code paths:

```csharp
public static class RankingOrder
{
    public static IComparer<ResultScore> For(RankingMode mode) => mode switch
    {
        RankingMode.Time  => Comparer<ResultScore>.Create((a, b) =>
            (a.TimeMs, a.Swaps, a.AchievedAt).CompareTo((b.TimeMs, b.Swaps, b.AchievedAt))),
        RankingMode.Moves => Comparer<ResultScore>.Create((a, b) =>
            (a.Swaps, a.TimeMs, a.AchievedAt).CompareTo((b.Swaps, b.TimeMs, b.AchievedAt))),
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    public static bool IsBetter(RankingMode mode, ResultScore candidate, ResultScore? current) =>
        current is null || For(mode).Compare(candidate with { AchievedAt = current.AchievedAt }, current) < 0;
}
```
(The candidate is compared without the date, so an equal result arriving later never replaces the old one.)

## Testing Strategy
- **Unit tests** for `RankingOrder` (both modes, both tie-breaks) and `ResultCheck` (every limit and its boundary).
- **Service tests** with SQLite in memory: unlocking with a catalog gap, best-per-mode replacement,
  idempotent re-send.
- **Endpoint tests** through `WebApplicationFactory`: one test per row of the error table, the happy
  paths, and rankings with several profiles.
- The test catalog is built in the test (3 small valid levels, numbers 1, 2, 4), not the real `levels/`,
  so changing the shipped levels never breaks these tests. One extra test starts the API with the real
  `levels/` folder and checks `GET /levels` returns the 4 Friday levels.
- Coverage goal: **≥ 90 % of lines** in `Swapping.Api/Progress`.

## Boundaries
- **Always:** read level order only from the catalog; keep only best rows; treat client values as
  untrusted (check every field); run `dotnet test` before each commit.
- **Ask first:** storing every run (history); adding anti-cheat replay; accepting ghost uploads;
  changing what counts as a swap or a restart (that is a gameplay rule decided with the user).
- **Never:** hard-code the list of levels; let a profile write another profile's results; return a
  ranking entry with anything but name, icon, time and swaps.

## Security
Security review of this draft, done during the autonomous loop following the process of
`agent-skills:security-and-hardening` (threat → protection → testable criterion). The transport and host
protections (HTTPS, no CORS, security headers, untrusted `X-Forwarded-For`, no raw SQL) are shared with
[`SPEC-profiles.md`](SPEC-profiles.md#security) (S6–S12) because both modules run in one host; here they
get one criterion each on the `progress` routes. Criteria 18–27. Checked on the PC with the skill
`agent-skills:security-and-hardening` on 2026-11-02: P1–P10 were already right; P11 was added.

| # | Threat | Protection | Criterion |
|---|---|---|---|
| P1 | **Fake result** (a modified client sends a plausible, too-good time) | **Accepted risk**, stated openly: without a server replay (out of scope, C4 decision 2) the server cannot prove a run happened. Limits: authenticated profile only, plausibility floor/ceiling (12), levels in order (4), tie-break by server time (8). A fake result is visible in the ranking and can be removed by hand from the database. | 4, 8, 12 |
| P2 | **Writing results for another profile** | The profile id comes **only** from the bearer token. The body has no profile field; a `profileId` (or any unknown field) → 400 `InvalidBody`. | 18 |
| P3 | **Uploads in excess** (flooding the database or the ranking) | `uploads` 30/min per profile (14); storage is bounded by design: at most **2 rows per (profile, level)**, whatever the number of uploads. No upload without a valid token, checked before any database work. | 14, 19, 20 |
| P4 | **Injection through the route or the query** (`levelId`, `mode`, `top`) | `levelId` is looked up in the in-memory catalog **before** any query (allow-list: unknown → 404 `UnknownLevel`); `mode` only `time` or `moves` (else 400 `InvalidQuery`); `top` an integer 1–100. EF Core LINQ only (profiles S8). The levels folder path comes only from configuration, and the catalog loader already refuses `../` and subfolders (`levels` spec). | 10, 21 |
| P5 | **Wrong number types** (`1.5`, `"100"`, `1e30`, values beyond `int`) to break parsing or overflow sums | All numbers are JSON integers bound to `int`; a float, a string or a value out of `int` range → 400 `InvalidBody`; the plausibility ranges (12) keep every sum far from overflow. | 22 |
| P6 | **Privacy leak in public rankings** | Entries carry only `rank, name, icon, timeMs, swaps` (9); no profile id, no `deaths`, no dates. `…/me` needs a token. | 9, 11 |
| P7 | **Expensive ranking queries** (denial of service) | `top` ≤ 100; `default` limit 120/min per IP; an index on (`LevelId`, `Mode`, `TimeMs`, `Swaps`, `AchievedAt`) and on (`LevelId`, `Mode`, `Swaps`, `TimeMs`, `AchievedAt`); `…/me` computes the rank with a `COUNT` query, not by loading the table. | 10, 23 |
| P8 | **CORS on the public rankings** | No CORS policy (shared host, profiles S11): no `Access-Control-Allow-Origin`. A future web ranking gets an explicit origin list, `GET` only. | 24 |
| P9 | **Stolen token replayed to post results** | HTTPS outside `Development`, `127.0.0.1` only in `Development` (profiles S6); the token stops working on logout and on code regeneration. | 25 |
| P10 | **Ghost uploads (future): huge or malformed binary** | Design rule for later: per-route `MaxRequestBodySize` of 256 KB enforced **before** reading the body; version byte and sample count checked before storing; only for the profile's own current best row. Not built now: the routes return 404. | 13, 26 |
| P11 | **Repudiation: a cheater or a broken client leaves no trace** (added in the review with the skill) | Refused results (`422 ImplausibleResult`, `409 LevelLocked`) write a `Swapping.Security` event (profiles S14) with the profile id, the level id and the code, so a fake result found in the ranking can be traced to its profile. The values are logged; the body is not stored. Dependencies and privacy are shared with profiles S15 and S16 (one host, one database). | 27 |

## Success Criteria
1. `dotnet build` and `dotnet test` pass with no warnings (warnings are errors).
2. A new profile: `GET /progress/me` shows the first catalog level unlocked and every other locked, all with null bests.
3. Test catalog 1, 2, 4: a result for level 1 → `unlockedNext: true` and level 2 unlocked; a result for
   level 2 unlocks level **4** (the gap is skipped).
4. A result for a locked level → 409 `LevelLocked`; for an id not in the catalog → 404 `UnknownLevel`.
5. Results for level 1 in order: (60 000 ms, 12 swaps), then (50 000, 20), then (70 000, 8) →
   best time = (50 000, 20), best moves = (70 000, 8).
6. Re-sending (50 000, 20) → 200 with both `improved…` false; the stored rows and `AchievedAt` are unchanged.
7. Ranking `time` with A (50 000, 9), B (50 000, 7), C (40 000, 30) → C, B, A.
   Ranking `moves` with the same rows → B, A, C.
8. Two profiles with identical results → the one uploaded first ranks higher in both modes.
9. `GET /rankings/{id}` entries contain exactly `rank, name, icon, timeMs, swaps` (no profile id).
10. `top=0` or `top=101` → 400; default returns at most 20.
11. `GET /rankings/{id}/me` returns the right rank and `totalPlayers`; a profile with no result → 404 `NoResult`.
12. Plausibility: for a 3-room level, `timeMs` 2 999 → 422, 3 000 → accepted; 86 400 001 → 422;
    `swaps` -1 or 10 001 → 422; 10 000 → accepted.
13. A body with a `ghost` field or any unknown field → 400 `InvalidBody`; the ghost routes return 404.
14. The 31st `POST /results` within a minute by one profile → 429 (the `uploads` policy of `profiles`).
15. The API with the real `levels/` folder: `GET /levels` lists levels 1, 2, 3, 5 with 4, 3, 2, 2 rooms.
16. The best-result table has a nullable `GhostId` column.
17. Line coverage of `Swapping.Api/Progress` ≥ 90 %.

Security criteria (see the Security section):
18. **P2:** a `POST /results` body with `"profileId"` → 400 `InvalidBody`; a result sent with profile A's
    token never appears under profile B in `GET /progress/me` or the rankings.
19. **P3:** with the `uploads` limit raised in the test configuration, 1 000 accepted uploads by one
    profile for one level leave exactly 2 best rows for that (profile, level).
20. **P3:** `POST /results` without a token or with an invalid one → 401 and no row is written.
21. **P4:** `GET /rankings/..%2F..%2Fetc?mode=time` and `GET /rankings/x'%20OR%201=1?mode=time` →
    404 `UnknownLevel`; `mode=TIME;DROP` → 400 `InvalidQuery`; `top=abc` → 400 `InvalidQuery`.
22. **P5:** `timeMs` sent as `1.5`, as `"60000"`, or as `3000000000` → 400 `InvalidBody`.
23. **P7:** the EF Core model has the two ranking indexes listed in P7.
24. **P8:** `GET /rankings/{id}` and an `OPTIONS` preflight on `POST /results` with
    `Origin: https://evil.example` → no `Access-Control-Allow-Origin` header.
25. **P9:** after `DELETE /sessions/current`, `POST /results` with that token → 401 and no row is written.
26. **P10:** `PUT /results/{levelId}/time/ghost` with any body → 404 in this version (no route).
27. **P11:** a `422 ImplausibleResult` and a `409 LevelLocked` each write one `Swapping.Security` event
    with the profile id and the level id (test logger); the event never has the body.

## Decisions / assumptions (taken alone, reversible — waiting for the user's review)
- **B1. Two best rows per (profile, level)**, one per mode, instead of storing every run.
- **B2. Unlocking needs one result of the previous level**, any value; no minimum score.
- **B3. A worse re-send is a 200**, not an error, so the upload queue can always drop it.
- **B4. Plausibility floor:** 1 s per room and a 24 h ceiling; no per-level par times (there are none yet).
- **B5. Tie-break by server time** of the first upload of that result, not the client's `finishedAt`.
- **B6. Rankings are public** (no token needed) and show only name, icon, time and swaps.
- **B7. Deaths and room restarts** are stored with the best rows for information but never ranked.
- **B8. Ghost:** only the nullable column and the route design now; 256 KB limit, 20 Hz × 5 `float32`.
- **B9. Fake results are an accepted risk** (security review, P1): plausibility checks only, no replay.
  Cleaning a cheated entry is done by hand in the database for this version.
- **B10. Bad query values** (`mode`, `top`) answer 400 with a new code `InvalidQuery`, separate from
  `InvalidBody` (security review, P4).

## Open Questions
(Also in [`docs/processo/PERGUNTAS-PARA-O-USUARIO.md`](../processo/PERGUNTAS-PARA-O-USUARIO.md).)
- Should the ranking show the deaths too, or a mark for runs without deaths? (B7)
- Minimum plausible time per room of 1 s: fine, or should each level have its own par time in the JSON? (B4)
- Should the game show the full ranking (top 20) or only the top 10 plus the player's own position?
- Fake results cannot be prevented without a server replay (P1, B9). Is "plausibility only + remove by
  hand" acceptable for the class?
