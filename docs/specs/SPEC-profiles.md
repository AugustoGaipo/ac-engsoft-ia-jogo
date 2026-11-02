# Spec: `profiles` — player profiles, access codes and sessions (Game API)

> Module from [`CAPABILITY-MAP.md`](../../CAPABILITY-MAP.md). Written following the process of
> `agent-skills:spec-driven-development` (phase 1, Specify) during the autonomous loop.
> Status: **draft, waiting for the user's review**. No code is written until it is approved.
> Sources: [ideas bank](../ideas/BANCO-DE-IDEIAS.md) ("Perfil sem senha", "Código de acesso"),
> [C4 levels 1–2](../architecture/C4.md) (Game API + SQLite, bearer token, offline-first).

## Objective

The part of the **Game API** that knows **who the player is**, without passwords or e-mail. A player
picks a **name** and an **icon**; the server generates a readable **access code**
(e.g. `K7QM-4XRT-9WPA-HC3N`) and stores **only its hash**. The game shows the code once, keeps it in
the local save and asks the player to write it down. To play on another PC the player types
**name + code**. Losing both the code and the PC means losing the profile (accepted by the user).

The module:
1. **creates** a profile (name + icon) and returns its access code once;
2. **logs in** with name + code and returns a **session token** (the bearer token of the C4 diagram);
3. **locks** a profile for a while after repeated wrong codes;
4. **regenerates** the access code (from a PC that is still logged in), invalidating the old one;
5. **rate-limits** creation, login and result uploads (the upload policy is used by `progress`);
6. lets the logged-in player **read** their profile and **change the icon**.

`progress` (results, unlocking, ranking) is a separate spec; it only uses the authenticated
profile id that this module puts on the request.

### Concepts
- **Profile:** id (GUID), name, icon id, created at, access code hash, lockout state.
- **Name:** public (it appears in the rankings), unique ignoring case. 3–16 characters, ASCII letters,
  digits, `_` and `-`; leading/trailing spaces are trimmed. *Assumption A1.*
- **Icon:** an id from a fixed list the server knows (`icon-01` … `icon-12`). *Assumption A2.*
- **Access code:** 16 symbols of the Crockford Base32 alphabet (`0-9 A-Z` without `I L O U`), shown in
  4 groups of 4 with dashes. 16 × 5 = **80 random bits** from `RandomNumberGenerator`.
  Input is **normalized** before checking: uppercase, dashes and spaces removed, `O → 0`, `I`/`L → 1`
  (the usual Crockford rules, so a code copied by hand still works).
- **Access code hash:** SHA-256 of the normalized code, compared with
  `CryptographicOperations.FixedTimeEquals`. A slow hash (PBKDF2, bcrypt) is for low-entropy
  passwords; an 80-bit random code cannot be guessed offline, so SHA-256 is enough. *Assumption A3.*
- **Session token:** 32 random bytes, Base64url. The server stores only its SHA-256, the profile id
  and the expiry (**30 days** from login). Sent as `Authorization: Bearer <token>`. When it expires,
  the game logs in again with the code from the local save. *Assumption A4.*
- **Lockout:** **5** wrong codes in a row for a profile → the profile cannot log in for **15 minutes**.
  A correct login resets the counter. Sessions that already exist keep working while the profile is
  locked, so an attacker cannot lock the owner out of the PC they already use. *Assumption A5.*

### Endpoints (JSON over HTTPS)
| Method and path | Auth | Body → response |
|---|---|---|
| `POST /profiles` | — | `{ name, icon }` → **201** `{ id, name, icon, accessCode, token, tokenExpiresAt }` |
| `POST /sessions` | — | `{ name, accessCode }` → **200** `{ id, name, icon, token, tokenExpiresAt }` |
| `DELETE /sessions/current` | bearer | → **204** (the token stops working) |
| `GET /profiles/me` | bearer | → **200** `{ id, name, icon, createdAt }` |
| `PATCH /profiles/me` | bearer | `{ icon }` → **200** profile |
| `POST /profiles/me/access-code` | bearer | → **200** `{ accessCode }`; the old code stops working and every **other** session of the profile is revoked |
| `GET /icons` | — | → **200** `["icon-01", …]` |

Errors use the ASP.NET Core `ProblemDetails` format (`application/problem+json`) with a stable
`code` extension field:

| HTTP | `code` | When |
|---|---|---|
| 400 | `InvalidName` | name empty, too short/long, or with a forbidden character |
| 400 | `UnknownIcon` | icon not in the list |
| 400 | `InvalidBody` | malformed JSON, missing field, unknown field, body > 4 KB |
| 401 | `InvalidCredentials` | wrong code **or** unknown name (same message and status for both) |
| 401 | `InvalidToken` | missing, unknown, revoked or expired bearer token |
| 409 | `NameTaken` | the name already exists (ignoring case) |
| 423 | `ProfileLocked` | lockout active; `Retry-After` header with the seconds left |
| 429 | `TooManyRequests` | rate limit hit; `Retry-After` header |

### Rate limits (ASP.NET Core built-in rate limiter, fixed window)
| Policy | Key | Limit | Used by |
|---|---|---|---|
| `create-profile` | client IP | 5 per hour | `POST /profiles` |
| `login` | client IP | 10 per minute | `POST /sessions` |
| `uploads` | profile id | 30 per minute | `progress` (result uploads) |
| `default` | client IP | 120 per minute | every other endpoint |

The numbers are configuration (`appsettings.json`, section `Profiles:RateLimits`), so they can be
tuned without code. *Assumption A6.*

## Tech Stack
- C# 12, **.NET 8**, ASP.NET Core **minimal APIs** in one web project `Swapping.Api` (`net8.0`).
- **EF Core 8 + SQLite** (`Microsoft.EntityFrameworkCore.Sqlite`, NuGet). Database file from the
  connection string `ConnectionStrings:Game` (default `Data Source=swapping.db`).
  The schema is created with `EnsureCreated()` at startup for this version (no migrations). *Assumption A7.*
- Hashing and random numbers: `System.Security.Cryptography` (no NuGet).
- Time: `TimeProvider` (built into .NET 8), injected, so tests control the clock.
- Rate limiting: `Microsoft.AspNetCore.RateLimiting` (part of the shared framework, no NuGet).
- Tests: xUnit + `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`, NuGet) against SQLite
  **in memory** (one open connection per test), plus a small hand-written fake `TimeProvider`.
- New NuGet packages (listed here so that approving the spec approves them):
  `Microsoft.EntityFrameworkCore.Sqlite` 8.x and `Microsoft.AspNetCore.Mvc.Testing` 8.x.

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
  Program.cs                → host: DI, rate limiter, ProblemDetails, MapProfiles(), MapProgress()
  Data/GameDbContext.cs     → one DbContext for the whole API (profiles + progress tables)
  Profiles/
    ProfileEntity.cs, SessionEntity.cs
    AccessCode.cs           → Generate(), Normalize(), Hash() (pure, unit-tested)
    ProfileNames.cs         → validation and the case-insensitive key
    ProfileService.cs       → create, login, lockout, regenerate, change icon (uses TimeProvider)
    BearerAuth.cs           → token → profile id on the request (custom authentication handler)
    ProfilesEndpoints.cs    → MapProfiles(): the routes above
    ProfilesOptions.cs      → icons, lockout and rate-limit numbers from configuration
  Progress/                 → `progress` spec
tests/Swapping.Api.Tests/
  Profiles/ → AccessCodeTests, ProfileNamesTests, ProfileServiceTests (clock-driven lockout),
              ProfilesEndpointTests (HTTP through WebApplicationFactory), RateLimitTests
  ArchitectureTests.cs      → Profiles/ does not use Progress/; no Godot
```
`profiles` and `progress` are two **folders of one project**, not two projects: they share one host,
one database and one test server. An architecture test keeps `Profiles/` independent of `Progress/`.
*Assumption A8.*

## Code Style
Same conventions as `rules` and `levels` (PascalCase, file-scoped namespaces `Swapping.Api.Profiles`).
The service returns results as values; the endpoints only translate them to HTTP:

```csharp
public async Task<LoginResult> LoginAsync(string name, string accessCode, CancellationToken ct)
{
    var profile = await db.Profiles.SingleOrDefaultAsync(p => p.NameKey == ProfileNames.Key(name), ct);
    if (profile is null)
        return LoginResult.InvalidCredentials();          // same answer as a wrong code

    var now = time.GetUtcNow();
    if (profile.LockedUntil > now)
        return LoginResult.Locked(profile.LockedUntil.Value - now);

    if (!AccessCode.Matches(accessCode, profile.AccessCodeHash))
    {
        profile.RegisterFailure(now, options.MaxFailures, options.LockoutDuration);
        await db.SaveChangesAsync(ct);
        return LoginResult.InvalidCredentials();
    }

    profile.ResetFailures();
    var session = SessionEntity.Create(profile.Id, now, options.TokenLifetime, out var token);
    db.Sessions.Add(session);
    await db.SaveChangesAsync(ct);
    return LoginResult.Ok(profile, token, session.ExpiresAt);
}
```
- Never log an access code or a token, not even at debug level.
- Only the API reads the clock (`TimeProvider`) and random numbers (`RandomNumberGenerator`).

## Testing Strategy
- **Unit tests** (no HTTP) for `AccessCode` (format, alphabet, normalization, hash compare) and
  `ProfileNames` (every rule, boundaries 3 and 16).
- **Service tests** with SQLite in memory and the fake clock: lockout after 5 failures, still locked
  at 14:59, open at 15:00, reset after success; token expiry at day 30.
- **Endpoint tests** through `WebApplicationFactory`: one test per row of the error table (status and
  `code`), plus the happy paths. The rate limits are lowered in the test configuration to keep tests fast.
- **Database check:** after creating a profile, no column of any table contains the plain code or token.
- Coverage goal: **≥ 90 % of lines** in `Swapping.Api/Profiles`.

## Boundaries
- **Always:** store only hashes of codes and tokens; give the same answer for "unknown name" and
  "wrong code"; run `dotnet test` before each commit; keep `Profiles/` independent of `Progress/`.
- **Ask first:** adding a NuGet package not listed above; collecting any personal data (e-mail, real
  name, IP stored in the database); changing the code format after the game starts saving it.
- **Never:** store or log a plain access code or token; add passwords or e-mail; return a different
  error for an unknown name than for a wrong code.

## Success Criteria
1. `dotnet build` and `dotnet test` pass with no warnings (warnings are errors).
2. `POST /profiles` with a valid name and icon → 201 with a code matching
   `^[0-9A-HJKMNP-TV-Z]{4}(-[0-9A-HJKMNP-TV-Z]{4}){3}$` and a token that works on `GET /profiles/me`.
3. 1 000 generated codes are all different and all match the format.
4. `Normalize("k7qm 4xrt-9wpa-hc3n") == Normalize("K7QM-4XRT-9WPA-HC3N")`, and `O`/`I`/`L` are read as `0`/`1`/`1`.
5. After creating a profile, the database holds the SHA-256 of the code and of the token, never the plain values.
6. Name rules: `"ab"` and a 17-character name → `InvalidName`; `"abc"` and a 16-character name → valid;
   `"Ana Maria"` (space) and `"Zoë"` → `InvalidName` (A1); `"  Ana  "` is saved as `"Ana"`.
7. Creating `"Ana"` and then `"ANA"` → 409 `NameTaken`.
8. An icon not in the list → 400 `UnknownIcon`; `GET /icons` returns the list.
9. Login with the right name + code (any case, with or without dashes) → 200 with a new token.
10. Wrong code and unknown name both → 401 `InvalidCredentials` with the **same** body.
11. 5 wrong codes → the 6th attempt (even with the right code) → 423 `ProfileLocked` with `Retry-After`;
    at +15 min the right code works and the counter is back to 0.
12. While a profile is locked, a token created before the lock still works.
13. `POST /profiles/me/access-code` → the old code gives 401, the new one logs in; the token that made
    the request still works and every other token of that profile gives 401.
14. A token at 30 days minus 1 s works; at 30 days → 401 `InvalidToken`. After `DELETE /sessions/current` → 401.
15. Rate limits: the 6th `POST /profiles` from one IP within an hour → 429 with `Retry-After`;
    the 11th `POST /sessions` within a minute → 429 (with the production numbers, set through the test configuration).
16. A body over 4 KB, malformed JSON or an unknown field → 400 `InvalidBody`; no stack trace in any error response.
17. Architecture: nothing in `Swapping.Api.Profiles` references `Swapping.Api.Progress`; no Godot reference.
18. Line coverage of `Swapping.Api/Profiles` ≥ 90 %.

## Decisions / assumptions (taken alone, reversible — waiting for the user's review)
- **A1. Name:** ASCII letters, digits, `_`, `-`, 3–16 characters, unique ignoring case. No accents or
  spaces, to avoid look-alike names (`Ana` with a Cyrillic `а`) in the ranking. Easy to widen later.
- **A2. Icons:** 12 fixed ids `icon-01`…`icon-12`; the drawings are chosen in the `game` spec.
- **A3. Hash:** SHA-256 of the normalized code (high-entropy random secret), constant-time compare.
- **A4. Session token** separate from the code, valid 30 days; the code is only sent to log in.
  The game keeps both the code and the token in the local save (`sync`).
- **A5. Lockout:** 5 failures → 15 minutes, per profile, not growing. Existing sessions survive it.
- **A6. Rate limits:** the numbers in the table, per IP (creation, login) and per profile (uploads),
  read from configuration. In-memory counters (one server process is enough for the class).
- **A7. Schema** created with `EnsureCreated()`; migrations only if the schema must change after there is real data.
- **A8. One API project** with folders `Profiles/` and `Progress/` instead of one project per module,
  because both share one host and one database.
- **A9.** `409 NameTaken` reveals that a name exists. That is accepted: names are public in the ranking anyway.
  The login, however, never tells whether a name exists.
- **A10. No profile deletion and no name change** in this version (both are small additions later).

## Open Questions
(Also in [`docs/processo/PERGUNTAS-PARA-O-USUARIO.md`](../processo/PERGUNTAS-PARA-O-USUARIO.md).)
- Accept accents and spaces in names (e.g. `João`, `Ana Maria`)? (A1)
- How many icons, and which drawings? (A2, `game` spec)
- Session length of 30 days and lockout of 5 failures / 15 minutes: OK? (A4, A5)
- Where will the API run during the presentation (local PC or a hosted server)? It decides HTTPS and CORS
  (covered in the security review, item 3 of the autonomous list).
- Should a player be able to delete their profile or change their name? (A10)
