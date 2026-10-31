# Capability Map: Swapping

Produced in phase 0 of `agent-skills:spec-driven-development`. Each module gets its own spec,
named `docs/specs/SPEC-<module-id>.md`. Module ids are stable and never renamed.

| Module id | Responsibility | Depends on |
|---|---|---|
| `rules` | Pure C# library: containers, properties (position, weight, kinetic energy), operations (swap now; transfer later), cooldown, locks and color permissions, physics formulas | — |
| `levels` | JSON level format, loader and validator | `rules` |
| `profiles` | API: name + icon profile, server-generated access code (hash only), lockout after failed attempts, rate limiting | — |
| `progress` | API: in-order level unlocking, best result per level, ranking in two modes (time-first, moves-first), optional ghost upload slot | `profiles`, `levels` |
| `sync` | Pure C# library: local save and upload queue for when the game is online | `progress` API contract |
| `game` | Godot 4 client: player, aiming, objects, on-screen numbers, menus, icon choice | `rules`, `levels`, `sync` |

Dependencies point one way only; there are no cycles. `rules`, `levels` and `sync` know nothing about
Godot or the API, so they are fully covered by automated tests.

## Build order

```
rules → levels → game (level 1 only) → profiles → progress → sync → game (levels 2–10)
```

`game` starts early on purpose: if the swap is not playable in Godot by day 3, the scope is cut to
levels 1, 3, 5 and 10.

Priority tiers (P0–P3) live in [`docs/ideas/BANCO-DE-IDEIAS.md`](docs/ideas/BANCO-DE-IDEIAS.md#-rota-de-prioridade).

## Decisions behind the map
- Stack: .NET 8, Godot 4.7.2 .NET, one .NET solution with one project per module.
- Code and technical docs in English.
- Profiles have no password or e-mail: nothing personal to leak. A Steam account may replace the
  access code later.
- The player is a `CharacterBody3D` with simulated mass and energy; objects are `RigidBody3D`.
- The server keeps the best result, so re-sending from the upload queue is safe.
