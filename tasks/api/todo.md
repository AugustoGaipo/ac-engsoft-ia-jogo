# Tasks: Game API (`profiles` + `progress`)

Specs: [`SPEC-profiles.md`](../../docs/specs/SPEC-profiles.md) ("P-SC n") ·
[`SPEC-progress.md`](../../docs/specs/SPEC-progress.md) ("G-SC n") · Plan: [`plan.md`](plan.md)

Common verification (every task):
`export PATH="$PATH:/c/Program Files/dotnet"; dotnet build Swapping.sln && dotnet test Swapping.sln`
(all `rules` and `levels` tests must stay green too). One commit per task, pushed.

---

## Task 1: Scaffold the API, the test factory and the host hardening
**Description:** Create `src/Swapping.Api` (`Microsoft.NET.Sdk.Web`, `net8.0`) and
`tests/Swapping.Api.Tests` (xUnit + `Microsoft.AspNetCore.Mvc.Testing`), add both to `Swapping.sln`.
Pin `Microsoft.EntityFrameworkCore.Sqlite` 8.x and `Microsoft.AspNetCore.Mvc.Testing` 8.x, turn on
NuGet lock files. Empty `GameDbContext` with `EnsureCreated()`; `ApiFactory` (test) with SQLite in
memory and a fake `TimeProvider`. Host rules: `ProblemDetails` with a `code` field and no exception
details, JSON strict (unknown fields rejected, max depth 8, integers only where ints are expected),
body limit 4 KB, `X-Content-Type-Options: nosniff`, `Development` launch settings on `127.0.0.1` only.
**Acceptance criteria:**
- [x] P-SC 1: build with zero warnings; a smoke test calls the host through the factory.
- [x] P-SC 16 (host part): a body over 4 KB (with or without `Content-Length`) → 400 `InvalidBody`,
      no stack trace; every response has `nosniff`; a 404 is problem+json with `code: NotFound`.
      *Moved to Task 3:* malformed JSON, depth > 8 and unknown fields need a route that reads a body.
- [x] P-SC 30 (part): `packages.lock.json` exists for both projects; restore in locked mode.
**Dependencies:** None
**Files:** `src/Swapping.Api/Swapping.Api.csproj`, `Program.cs`, `Hosting/*.cs`, `Data/GameDbContext.cs`,
`Properties/launchSettings.json`, `tests/Swapping.Api.Tests/*.csproj`, `ApiFactory.cs`, `HostTests.cs`
**Scope:** M

## Task 2: Access code and name rules
**Description:** Pure classes `AccessCode` (`Generate`, `Normalize`, `Hash`, `Matches` with
`FixedTimeEquals`) and `ProfileNames` (`Validate`, trim, `Key` ignoring case), following A1 and A3.
**Acceptance criteria:**
- [x] P-SC 3, 4: 1 000 codes distinct and in the format; normalization of case, dashes, spaces, `O`/`I`/`L`.
- [x] P-SC 6, 25: every name rule and boundary (3, 16), accents, spaces, `[b]x[/b]`, `<i>x`; trim.
- [x] P-SC 24 (part): `Robert'); DROP TABLE Profiles;--` is an invalid name.
**Dependencies:** Task 1
**Files:** `Profiles/AccessCode.cs`, `Profiles/ProfileNames.cs`, `tests/.../Profiles/AccessCodeTests.cs`,
`ProfileNamesTests.cs`
**Scope:** S

### Checkpoint: Foundation
- [x] Build clean, all tests green, commit + push.

## Task 3: Create a profile
**Description:** `ProfileEntity`, `SessionEntity`, `ProfilesOptions` (icons, lockout, lifetimes from
configuration), `ProfileService.CreateAsync`, `POST /profiles`, `GET /icons`. Only hashes are stored.
**Acceptance criteria:**
- [x] P-SC 2 (create part), 7, 8: 201 with a code in the format; `ANA` after `Ana` → 409 `NameTaken`;
      unknown icon → 400 `UnknownIcon`; `GET /icons` lists 12 icons.
- [x] P-SC 5, 31: the database holds only SHA-256 values; no table has an IP, e-mail or plain code/token column.
- [x] P-SC 26 (over-posting): a body with `id` or `accessCodeHash` → 400 `InvalidBody`, nothing created.
- [x] From Task 1: malformed JSON, JSON deeper than 8 levels, a string where a string is not expected →
      400 `InvalidBody`; a 500 has no exception details; the schema is created at startup.
**Dependencies:** Task 2
**Files:** `Profiles/ProfileEntity.cs`, `SessionEntity.cs`, `ProfilesOptions.cs`, `ProfileService.cs`,
`ProfilesEndpoints.cs`, `tests/.../Profiles/CreateProfileTests.cs`
**Scope:** M

## Task 4: Bearer token, my profile and logout
**Description:** `BearerAuth` authentication handler (token hash → profile id), `GET /profiles/me`,
`PATCH /profiles/me` (icon), `DELETE /sessions/current`, 30-day expiry with the fake clock.
**Acceptance criteria:**
- [x] P-SC 2 (token part): the token from `POST /profiles` works on `GET /profiles/me`.
- [x] P-SC 14: at 30 days − 1 s works; at 30 days → 401 `InvalidToken`; after logout → 401.
- [x] Missing or unknown token → 401 `InvalidToken`; `PATCH` with an unknown icon → 400 `UnknownIcon`.
**Dependencies:** Task 3
**Files:** `Profiles/BearerAuth.cs`, `ProfileService.cs`, `ProfilesEndpoints.cs`, `tests/.../Profiles/SessionTests.cs`
**Scope:** M

## Task 5: Login and lockout
**Description:** `ProfileService.LoginAsync`, `POST /sessions`, lockout with one atomic SQL update
(S2), `423 ProfileLocked` with `Retry-After`.
**Acceptance criteria:**
- [ ] P-SC 9, 10: right code in any case/with or without dashes → 200; wrong code and unknown name →
      the **same** 401 body.
- [ ] P-SC 11, 12: 6th attempt → 423 with `Retry-After`; at +15 min it works and the counter is 0;
      an older token works while locked.
- [ ] P-SC 19: 20 concurrent wrong logins → exactly 5 × 401 and 15 × 423.
**Dependencies:** Task 4
**Files:** `Profiles/ProfileService.cs`, `ProfileEntity.cs`, `ProfilesEndpoints.cs`, `tests/.../Profiles/LoginTests.cs`
**Scope:** M

## Task 6: Regenerate the code and the security log
**Description:** `POST /profiles/me/access-code` needing the current code (A11), revoking the other
sessions; `SecurityLog` (`ILogger` category `Swapping.Security`) for lockout, regeneration, refused
login, rate limit hit; `Cache-Control: no-store` on routes that return a code or a token.
**Acceptance criteria:**
- [ ] P-SC 13, 20: old code → 401, new code logs in; the calling token works, the others → 401;
      missing/wrong current code → 401 and the failure counter goes up.
- [ ] P-SC 22, 29: a test logger sees one event per lockout and per regeneration, and no line in the
      whole flow contains a code or a token.
- [ ] P-SC 23: `no-store` on the 3 routes that return secrets.
**Dependencies:** Task 5
**Files:** `Profiles/ProfileService.cs`, `ProfilesEndpoints.cs`, `Hosting/SecurityLog.cs`,
`tests/.../Profiles/RegenerateTests.cs`, `tests/.../TestLogger.cs`
**Scope:** M

## Task 7: Rate limits and transport rules
**Description:** Rate limiter policies `create-profile`, `login`, `uploads`, `default` from
`Profiles:RateLimits`; 429 with `Retry-After`; `X-Forwarded-For` not trusted; no CORS policy;
HTTPS redirection + HSTS outside `Development`.
**Acceptance criteria:**
- [ ] P-SC 15: 6th `POST /profiles` in an hour → 429 with `Retry-After`; 11th `POST /sessions` in a minute → 429.
- [ ] P-SC 27, 28: `Origin: https://evil.example` gets no `Access-Control-Allow-Origin`; 11 logins
      with different `X-Forwarded-For` → the 11th is 429.
- [ ] P-SC 21: under `Production`, `http://` → redirect to `https://` and HSTS on HTTPS responses.
**Dependencies:** Task 6
**Files:** `Hosting/RateLimits.cs`, `Program.cs`, `tests/.../Profiles/RateLimitTests.cs`, `tests/.../TransportTests.cs`
**Scope:** M

### Checkpoint: `profiles` complete
- [ ] Every P-SC 1–31 has a test (except 17, 18, 30 → Task 11); build clean, all green, commit + push.

## Task 8: Catalog, ranking order and plausibility
**Description:** `LevelDirectory` (loads `levels/catalog.json` with `LevelCatalog.Load` at startup from
`Progress:LevelsDirectory`; the API refuses to start on catalog errors), pure `RankingOrder` and
`ResultCheck`, `GET /levels`. Test catalog with levels 1, 2, 4 built in the test.
**Acceptance criteria:**
- [ ] G-SC 7 (as pure comparers), 12: both orders and tie-breaks; every plausibility boundary.
- [ ] G-SC 15: with the real `levels/`, `GET /levels` lists 1, 2, 3, 5 with 4, 3, 2, 2 rooms.
- [ ] A broken catalog stops the host from starting (test).
**Dependencies:** Task 7
**Files:** `Progress/LevelDirectory.cs`, `RankingOrder.cs`, `ResultCheck.cs`, `ProgressEndpoints.cs`,
`tests/.../Progress/RankingOrderTests.cs`, `ResultCheckTests.cs`, `LevelsEndpointTests.cs`
**Scope:** M

## Task 9: Send a result and see my progress
**Description:** `BestResultEntity` (2 rows per profile and level, nullable `GhostId`, the two ranking
indexes), `ProgressService.SubmitAsync` and `GetProgressAsync`, `POST /results` (uses the `uploads`
policy), `GET /progress/me`, security events for refused results.
**Acceptance criteria:**
- [ ] G-SC 2–6: unlocking with the gap 1 → 2 → 4; `LevelLocked`/`UnknownLevel`; best per mode;
      a worse re-send → 200 with both `improved…` false and rows unchanged.
- [ ] G-SC 13, 16, 18–20, 22, 25–27: unknown fields/`ghost`/`profileId` → 400; `GhostId` nullable;
      1 000 uploads → 2 rows; no token → 401 and no row; `1.5`/`"60000"`/`3000000000` → 400;
      ghost route → 404; refused results write a `Swapping.Security` event.
- [ ] G-SC 14: the 31st upload in a minute → 429.
**Dependencies:** Task 8
**Files:** `Progress/BestResultEntity.cs`, `ProgressService.cs`, `ProgressEndpoints.cs`, `Data/GameDbContext.cs`,
`tests/.../Progress/SubmitResultTests.cs`
**Scope:** M

## Task 10: Rankings
**Description:** `GET /rankings/{levelId}?mode&top` and `GET /rankings/{levelId}/me` (rank with a
`COUNT` query), `400 InvalidQuery`.
**Acceptance criteria:**
- [ ] G-SC 7–11: the two orders over HTTP; first upload wins ties; entries have only
      `rank, name, icon, timeMs, swaps`; `top` 0/101 → 400, default 20; `…/me` rank, `totalPlayers`, 404 `NoResult`.
- [ ] G-SC 21, 23, 24: path traversal and SQL-looking ids → 404 `UnknownLevel`; bad `mode`/`top` →
      400 `InvalidQuery`; the two indexes exist in the EF model; no CORS header.
**Dependencies:** Task 9
**Files:** `Progress/ProgressService.cs`, `ProgressEndpoints.cs`, `tests/.../Progress/RankingTests.cs`
**Scope:** M

## Task 11: Architecture, coverage, dependency audit and report
**Description:** Architecture tests (`Profiles/` does not use `Progress/`; no Godot; no raw SQL calls),
coverage of both folders, `dotnet list package --vulnerable --include-transitive`, build with
`--locked-mode`, report `docs/processo/RELATORIO-api.md` (criterion → test), C4 and README updated.
**Acceptance criteria:**
- [ ] P-SC 17, 24 (source part), G-SC 17: architecture tests green.
- [ ] P-SC 18, G-SC 17: line coverage ≥ 90 % in `Profiles/` and in `Progress/`.
- [ ] P-SC 30: the audit result is in the diary; the report maps every criterion to a test.
**Dependencies:** Task 10
**Files:** `tests/.../ArchitectureTests.cs`, `docs/processo/RELATORIO-api.md`, `docs/processo/DIARIO.md`,
`docs/architecture/C4.md`, `README.md`
**Scope:** M

### Checkpoint: Complete
- [ ] All criteria of both specs have a test; build clean; all green; pushed.
