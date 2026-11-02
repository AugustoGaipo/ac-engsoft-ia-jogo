# Spec: `sync` — local save and upload queue (offline-first)

> Module from [`CAPABILITY-MAP.md`](../../CAPABILITY-MAP.md). Produced with the skill
> `agent-skills:spec-driven-development` (phase 1, Specify) during the autonomous loop, on the user's PC.
> Status: **draft, waiting for the user's review**. No code is written until it is approved.
> Depends on the API contract of [`SPEC-profiles.md`](SPEC-profiles.md) (`POST /sessions`, bearer token,
> `401`/`423`/`429`) and [`SPEC-progress.md`](SPEC-progress.md) (`POST /results`, `GET /progress/me`, error table).
> Sources: [C4](../architecture/C4.md) (container "Local Save", decision "offline-first"), ideas bank
> ("Jogo offline; progresso enviado para a API quando há internet").

## Objective

A pure C# library, embedded in the Game Client, that lets the game work **fully offline** and still
keep the server in step when there is internet:
1. keeps the **local save**: the profile (name, icon, access code, session token), the local best
   results per level and the **upload queue**;
2. records a **finished run** locally at once: the local best and the unlocked levels change on the spot,
   with no network;
3. **flushes the queue** to the API when asked (game start, level finished, every few minutes), in order,
   with retry and backoff, and with a clear decision for every possible API answer;
4. **merges** the server's progress into the local one (best of both).

Re-sending is always safe, because the server keeps the best result (progress spec). So the queue
favors "send again" over "maybe lost": an item leaves the queue only after the server answered.

### Concepts
- **Local save** (schema version 1): one JSON file. In the game it lives in Godot `user://save.json`;
  the library only sees a store that reads and writes text.
  ```json
  {
    "schemaVersion": 1,
    "profile": { "id": "…", "name": "augusto", "icon": "icon-03",
                 "accessCode": "K7QM-4XRT-9WPA-HC3N", "token": "…", "tokenExpiresAt": "2026-12-01T00:00:00Z" },
    "best": { "level-1": { "time": { "timeMs": 61234, "swaps": 7 }, "moves": { "timeMs": 70100, "swaps": 5 } } },
    "queue": [ { "id": "3f2a…", "profileId": "…", "levelId": "level-2", "timeMs": 80211, "swaps": 9,
                 "deaths": 1, "roomRestarts": 0, "finishedAt": "2026-11-02T13:05:00Z",
                 "attempts": 0, "nextAttemptAt": null } ],
    "rejected": [ { "levelId": "level-9", "code": "UnknownLevel", "at": "…" } ]
  }
  ```
  `profile` is null before the player creates or enters a profile. Playing without a profile is allowed:
  results are kept locally and queued with `profileId: null`. They are sent once a profile exists
  (*Assumption C3*).
- **Best per mode:** the same two orders as the server:
  - `time`: lower time, then fewer swaps;
  - `moves`: fewer swaps, then lower time.
  A result replaces the local best of a mode only if it is strictly better (an equal result does not).
- **Unlocking (local):** the same rule as the server, on the level order given by the game (the catalog):
  - the first level is unlocked;
  - a level is unlocked once the previous one has a local best.
- **Queue coalescing:** only the best matters, so the queue keeps **at most 2 items per (profile, level)**:
  - the best for `time` and the best for `moves` (1 item if the same run is best in both);
  - a new run that is worse than both queued items of its level is not queued (it is still not the best);
  - more generally, a run is queued **only if it improved the local best in at least one mode**: if it did
    not, the server already has (or the queue already holds) something at least as good.
  - So the queue can never grow past 2 × number of levels per profile.
- **Flush:** sends the items of the **current profile**, oldest first, one at a time, and stops at the
  first answer that means "try later". Every API answer maps to one action:

| API answer | Action |
|---|---|
| `200` from `POST /results` | remove the item; update `unlockedNext` hint; continue |
| `401 InvalidToken` | log in again with the saved name + access code (`POST /sessions`) once, then retry the item; if the login fails, stop and report `NeedsLogin` |
| `401 InvalidCredentials` on that login | stop; report `NeedsLogin` (the code saved is no longer valid, e.g. a new code was made on another PC) |
| `423 ProfileLocked` / `429 TooManyRequests` | stop; next attempt after the `Retry-After` seconds |
| `409 LevelLocked` | keep the item; stop (an earlier upload must be missing; retried on the next flush) |
| `400 InvalidBody`, `404 UnknownLevel`, `422 ImplausibleResult` | **permanent**: move the item to `rejected` (kept last 20) and continue. Never retried, so a bad item cannot block the queue forever |
| network error, timeout (10 s), `5xx` | keep the item; `attempts + 1`; `nextAttemptAt = now + backoff`; stop |

  Backoff: 5 s, 10 s, 20 s… doubling, at most 10 minutes. A successful send resets it.
- **Profile calls** (`CreateProfileAsync`, `EnterProfileAsync`) are **not** queued. They need the network
  now, and their answer goes straight back to the game so it can show it to the player:
  - `400 InvalidName` / `UnknownIcon`, `409 NameTaken` → "choose another name/icon";
  - `401 InvalidCredentials` → "name or code wrong";
  - `423` / `429` → "try again in N s";
  - a network error → "no internet: you can keep playing without a profile".

  On success the profile (with code and token) is saved, and every queued item with `profileId: null`
  takes the new id (C3).
- **Merge:** after a successful flush, `GET /progress/me` is read and merged: for each level and mode the
  better of local and server wins (the server's `bestTime` / `bestMoves` map to the save's `time` / `moves`).
  A server best that is better than the local one is copied down (the player played on another PC).

### Public API (shape, not final names)
```csharp
public sealed class SyncClient
{
    public SyncClient(ISaveStore store, IGameApi api, IReadOnlyList<string> levelOrder);

    public SaveData Load();                                   // reads the save (or a fresh one)
    public RunOutcome RecordRun(RunResult run, DateTimeOffset now);   // local best + queue; no network
    public bool IsUnlocked(string levelId);
    public Task<FlushReport> FlushAsync(DateTimeOffset now, CancellationToken ct);
    public Task<ProfileResult> CreateProfileAsync(string name, string icon, CancellationToken ct);
    public Task<ProfileResult> EnterProfileAsync(string name, string accessCode, CancellationToken ct);
    public void LeaveProfile();                               // forgets token and code on this PC
}
```
- `ISaveStore` reads and writes the save text. The library ships `FileSaveStore` (System.IO, **atomic**:
  write to `save.json.tmp`, then replace). The game gives it the real path of `user://`.
- `IGameApi` is the HTTP contract. The library ships `HttpGameApi` over `HttpClient` (part of .NET):
  - JSON;
  - bearer token;
  - reads `Retry-After`;
  - 10 s timeout.

  Tests use a fake `IGameApi` and, for `HttpGameApi`, a fake `HttpMessageHandler`.
- Time is always passed in (`now`); the library never reads the clock and has no timers. The **game**
  decides when to call `FlushAsync`.

### Damaged or old save
- Unreadable JSON or an unknown `schemaVersion` → the file is kept as `save.corrupt-<yyyyMMddHHmmss>.json`.
  Then a fresh save starts and `Load` reports `SaveRecovered`, so the game can tell the player. The
  profile can be recovered by entering name + code.
- A save is written after every change (run recorded, item sent, profile changed); never only at exit.

## Tech Stack
- C# 12, **.NET 8** class library `Swapping.Sync` (`net8.0`). **No project references** (it does not
  need `rules` or `levels`: the game passes the level order). **No NuGet**: `System.Text.Json` and
  `System.Net.Http` are part of .NET.
- Tests: xUnit, like the other libraries.

## Commands
Run from the repository root (in Git Bash, `export PATH="$PATH:/c/Program Files/dotnet"` first).
```
Build:    dotnet build Swapping.sln
Test:     dotnet test Swapping.sln
Coverage: dotnet test Swapping.sln --collect:"XPlat Code Coverage"
```

## Project Structure
```
src/Swapping.Sync/
  SaveData.cs          → the save model (profile, best per level and mode, queue, rejected)
  SaveSerializer.cs    → JSON ⇄ SaveData, schema version check, corrupt-file recovery decision
  BestResults.cs       → the two orders, "strictly better", local unlocking (pure)
  UploadQueue.cs       → coalescing, order, backoff, the answer → action table (pure)
  SyncClient.cs        → ties store, queue and API together
  ISaveStore.cs, FileSaveStore.cs
  IGameApi.cs, HttpGameApi.cs, ApiAnswer.cs (status + code + Retry-After)
tests/Swapping.Sync.Tests/
  → BestResultsTests, UploadQueueTests (one test per row of the answer table), SaveSerializerTests,
    SyncClientTests (fake store + fake API), FileSaveStoreTests (temp folder), HttpGameApiTests (fake handler)
```

## Code Style
Same conventions as `rules` and `levels`. The answer table is data-like code, one branch per row:

```csharp
public static QueueAction Decide(ApiAnswer answer) => answer switch
{
    { Status: 200 } => QueueAction.Remove,
    { Status: 401, Code: "InvalidToken" } => QueueAction.ReLoginAndRetry,
    { Status: 423 or 429 } => QueueAction.StopUntil(answer.RetryAfter),
    { Status: 409 } => QueueAction.KeepAndStop,
    { Status: 400 or 404 or 422 } => QueueAction.Reject,
    _ => QueueAction.BackOff,   // network error, timeout, 5xx, anything unexpected
};
```

## Testing Strategy
- **Unit tests** for `BestResults` (both orders, ties, unlocking with a gap in the numbers) and
  `UploadQueue` (one test per row of the answer table, coalescing, backoff values, the 10-minute cap).
- **SyncClient tests** with a fake store and a fake API that answers from a script: offline run, flush
  of 3 items with a 5xx in the middle, re-login after an expired token, merge from the server.
- **FileSaveStore tests** in a temp folder: atomic replace (a crash between write and rename leaves the
  old save valid), corrupt-file recovery.
- **HttpGameApi tests** with a fake `HttpMessageHandler`: bearer header, `Retry-After` parsing, timeout → network error.
- Coverage goal: **≥ 90 % of lines** in `Swapping.Sync`. No real network and no Godot in tests.

## Boundaries
- **Always:** write the save after every change; keep an item until the server answered; treat every
  unknown answer as "try later"; run `dotnet test` before each commit.
- **Ask first:** encrypting the save; sending runs in parallel; storing a history of every run; adding NuGet.
- **Never:** reference Godot; read the clock or start timers inside the library; log the access code or
  the token; drop a queued item because of a network error.

## Security
- **Secrets in the save:** the access code and the token are in the save in plain text. It is the
  player's own PC, and anyone with access to the user folder could also just play as them. *Assumption C1:*
  accepted for this version. Encrypting (e.g. Windows DPAPI) is listed as an open question because Godot
  runs on other systems too.
- **Never in logs:** the `FlushReport` and any error text carry codes and counts, never the code or the token.
- **Untrusted server data:** a `GET /progress/me` answer is checked like any input:
  - unknown level ids are ignored;
  - negative or absurd values are ignored;
  - the save never takes a field it does not know.
- **No path from the catalog or the server:** the save path comes only from the game (`user://`).
- **Transport** (added in the review with `agent-skills:security-and-hardening`):
  - `HttpGameApi` refuses a base URL that is not `https://`, unless the host is loopback (`127.0.0.1`,
    `localhost`, `::1`), which is the presentation setup;
  - it never sets a custom certificate check (`ServerCertificateCustomValidationCallback`), so a bad
    certificate fails as a network error instead of being accepted;
  - redirects are not followed, so the token is never sent to another host.
- **Size caps:** a response body larger than 256 KB is read as a network error, so a broken or hostile
  server cannot fill the memory. A save file larger than 1 MB is treated as corrupt (recovered as above).

## Success Criteria
1. `dotnet build` and `dotnet test` pass with no warnings; `Swapping.Sync.csproj` has no project or package reference.
2. **Offline run:** with an API that always fails, `RecordRun` updates the local best and unlocks the next
   level, and the run is in the queue after a reload of the save.
3. **Best per mode:**
   - Runs (60 s, 7 swaps), then (70 s, 5 swaps) → the local `time` best is the first and the `moves` best is the second.
   - A third run (60 s, 7 swaps) changes nothing.
4. **Coalescing:** 10 runs of the same level leave at most 2 items in the queue, and they are the two bests.
5. **Unlock with a gap:** level order [1, 2, 3, 5]: after a result in level 3, level 5 is unlocked.
6. Each row of the answer table has a test that produces its action.
7. **Order and stop:** queue A, B, C; the API answers 200, 503, — → A removed, B kept with `attempts = 1`
   and `nextAttemptAt = now + 5 s`, C not sent.
8. **Backoff:** attempts 1, 2, 3, … give 5, 10, 20 … s, and never more than 600 s.
9. **Re-login:** a `401 InvalidToken` makes one `POST /sessions` with the saved name and code, then the item
   is retried and removed; a second `401` stops with `NeedsLogin` and no loop.
10. **Rejected items:** `422 ImplausibleResult` moves the item to `rejected` and the next item is still sent.
11. **Merge:** a server best better than the local one is copied down; a worse one is ignored; an unknown
    level id from the server is ignored.
12. **Atomic save:** if the process dies after writing `save.json.tmp` and before the replace, `Load` still
    reads the old valid save.
13. **Corrupt save:** an unreadable file is kept as `save.corrupt-*.json`, `Load` returns a fresh save, and
    the report says `SaveRecovered`.
14. **HttpGameApi:** sends `Authorization: Bearer <token>`, parses `Retry-After: 30` as 30 s, and turns a
    10 s timeout into a network error (not an exception).
15. **No secrets in reports:** no `FlushReport` or error text contains the access code or the token (checked by a test).
16. Line coverage ≥ 90 %.
17. **Transport:**
    - `HttpGameApi` with `http://example.com` fails at construction; `http://127.0.0.1:5000` is accepted;
    - a `302` answer is not followed (fake handler: no second request) and counts as "try later".
18. **Size caps:** a 300 KB response becomes a network error, and a 2 MB save file is recovered as corrupt.

## Decisions to confirm (assumptions in this draft)
- **C1:** access code and token stay in plain text in the save (player's own PC).
- **C2:** at most 2 queued items per level and profile (coalescing), because only the best matters.
- **C3:** playing without a profile is allowed; those runs are sent when a profile is created or entered.
- **C4:** permanent errors (`400`, `404`, `422`) drop the item into `rejected` (last 20 kept) instead of retrying.
- **C5:** backoff 5 s doubling, capped at 10 minutes; request timeout 10 s.
- **C6:** the library has no timers; the game calls `FlushAsync` at start, after each level and every
  2 minutes while online.
- **C7:** "leave profile" removes the token and the code from this PC; the profile's queued items stay
  until the same profile enters again.

## Open Questions
- Should the save be encrypted on Windows (DPAPI), knowing it would not work the same on other systems?
- When a player enters a **different** profile on the same PC, should the local bests of the previous
  profile be kept (per profile) or replaced? *(Draft: the local `best` belongs to the current profile; on
  a profile change it is replaced by the server's progress of the new profile, and the old profile's
  queue items stay waiting.)*
