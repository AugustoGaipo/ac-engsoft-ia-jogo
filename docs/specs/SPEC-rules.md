# Spec: `rules` — swap rules library

> Module from [`CAPABILITY-MAP.md`](../../CAPABILITY-MAP.md). Produced with
> `agent-skills:spec-driven-development` (phase 1, Specify). Status: **draft, awaiting approval**.

## Objective

A pure C# library that decides **what a swap does** and **whether it is allowed**. It knows nothing
about Godot, files, the network or the clock: the game asks it questions and applies the answers to
the physics bodies. Because it is pure, every rule is covered by automated tests.

It must support the 10 planned levels **and** leave clean extension points for the ideas in the
[ideas bank](../ideas/BANCO-DE-IDEIAS.md), so that future features never need hacks.

### Core idea: swap variables, not values
- Every container (the player or an object) is **born with one variable per property**: `Xa.position`,
  `Xa.mass`, `Xa.energy`. The container that is born with a variable is its **owner**.
- A container always **uses** exactly one variable of each property. A swap only changes **who uses
  which variable**.
- Physics keeps changing the value of a variable while someone else uses it (the game writes the
  current position and kinetic energy back into the variables every physics step).
- **Invariant:** variables are only shuffled. Every variable is used by exactly one container, and
  every container uses exactly one variable per property.
- **Restoration** (a variable barrier): a container gets back each of its own variables from whoever
  holds it, and that holder takes the variable the container was using. Each property is restored
  independently, so position may come back from one container and mass from another.

### Properties in this version
| Property | Variable value | Unit | Changes on its own? |
|---|---|---|---|
| Position | point in space | m | yes (movement) |
| Mass | scalar; **volume never changes**, so density = mass ÷ volume | kg | no |
| Kinetic energy | magnitude + direction of travel; E = ½·m·v² | J | yes (friction, collisions, gravity) |

### Behaviour of each swap
- **Position:** the two containers exchange places. Velocities do not change.
- **Mass:** the masses are exchanged. Each container **keeps its kinetic energy**, so its speed is
  recomputed: v = √(2·E ÷ m).
  Example: 2 kg at 3 m/s → E = 9 J. After becoming 8 kg → v = √(2·9 ÷ 8) = 1.5 m/s.
- **Kinetic energy:** the energy variables are exchanged. The receiver moves **in the world direction the
  giver was moving**, at v = √(2·E ÷ m_receiver). A receiver of 0 J stops.
  Example: a 1 kg ball moving right at 4 m/s (8 J) and the player (70 kg) standing still. After the
  swap: player moves right at √(2·8 ÷ 70) ≈ 0.48 m/s, the ball stops.

### Swap legality
A swap request is rejected, with a reason, when:
| Reason | Rule |
|---|---|
| `SameContainer` | a container cannot swap with itself |
| `ModeNotEnabled` | the level only enables player ↔ object (object ↔ object is supported but off) |
| `PropertyNotUnlocked` | the level has not unlocked this property |
| `PropertyNotAccepted` | this object only accepts some properties, and not this one |
| `Locked` | the object is locked and swaps nothing |
| `OutOfRange` | distance > the property's maximum range for this level |
| `NoLineOfSight` | something solid is between them (the game measures it) |
| `BlockedByBarrier` | a variable barrier is between them (the game measures it) |
| `InsideVariableBarrier` | either container is inside a variable barrier; the player inside one cannot swap at all |
| `OnCooldown` | less than the level's cooldown has passed since the last swap (any property) |
| `TooManyProperties` | more than one property in one swap, unless the level allows simultaneous swaps |

Distance, line of sight and barrier crossing are **measured by the game** (raycasts) and passed in.
The library only decides.

### Barriers
Three basic, combinable filters. A wall is just a thin volume; a zone is a big one.
| Barrier | Effect decided by `rules` |
|---|---|
| 1. Blocks objects | objects cannot pass; a held object is released when either side fully crosses (the game detects the crossing) |
| 2. Blocks the player | the player cannot pass |
| 3. Blocks variables | entering restores **all** variables of the container; no swap through it or inside it |

The **exit grid** of every level is barrier 1 + barrier 3. The filter model can later block only some
properties; the game always uses "all" for now.

### Physics helpers (needed now for density in P1)
- Density ρ = m ÷ V.
- **Three fluid media**, each one a zone with a density (air is the default everywhere):
  | Medium | Density (default, tunable) |
  |---|---|
  | Air | 1.2 kg/m³ |
  | Dense gas | 6.1 kg/m³ (like real sulfur hexafluoride) |
  | Water | 1 000 kg/m³ |
- Buoyancy force: (ρ_fluid − ρ_object) · V · g, upward when positive.
- **Three buoyancy states per medium** (a gameplay simplification of real physics, where only an exact
  match is neutral). Same proportions in every medium, tunable:
  | State | Density range | Behaviour |
  |---|---|---|
  | **Sink** | ρ > 1.1 · ρ_fluid | goes down; cannot rise on its own |
  | **Swim** | 0.1 · ρ_fluid ≤ ρ ≤ 1.1 · ρ_fluid | moves freely up and down (swims in water, "flies" in air or gas) |
  | **Float** | ρ < 0.1 · ρ_fluid | pushed up to the surface or ceiling; cannot dive |
  Water: sink above 1 100, swim 100–1 100, float below 100 kg/m³.
  Air: sink above 1.32, swim 0.12–1.32, float below 0.12 kg/m³.
- **Holding an object counts.** While the player holds something, the pair is treated as one body:
  ρ = (m_player + m_held) ÷ (V_player + V_held). Dropping it recomputes the state.
  Example: a player lighter than air holding a rock does not rise; letting go, they rise. A player
  holding a big light balloon in water may switch from swim to float.
- Every container follows these rules, not only the player.
- **Recoil when throwing (conservation of momentum).** When a container that is **not standing on
  the ground** (swimming or floating) throws an object, it is pushed the opposite way:
  v_thrower = m_object · v_added ÷ m_thrower, where **v_added is only the speed the throw adds** to the
  object (relative to the thrower). Gravity is not the thrower's push, so **dropping** (just letting go)
  gives **no recoil**; the thrower may still rise, but because it became lighter (buoyancy).
  Drop and throw are two different actions. On the ground, floor friction cancels the recoil.
  How fast the recoil fades depends on the medium's drag: little in air or gas (drifts, like in
  space), a lot in water (stops quickly). Example: a 0.05 kg player flying in air throws a 0.01 kg
  ball at 5 m/s → drifts back at 1 m/s.
- Static pressure P = m·g ÷ A_contact, with g = 9.8 m/s²; a fragile piece breaks when P > its limit.
  Example: 10 kg on 0.01 m² → 9 800 Pa; on 0.1 m² → 980 Pa.

### Extension points (designed now, not implemented)
| Extension point | Enables |
|---|---|
| `IPropertyKind` registry | momentum, temperature, elasticity, the 2 future numeric abilities |
| `IOperation` (swap now; transfer later) | time (steal/give), the gun that stores values |
| Swap mode flag | object ↔ object |
| Swap duration policy (permanent now) | unstable objects that revert after X s |
| Cooldown modifiers | power-up, "glorious weight", time-limited downgrade |
| Multi-property swap flag | simultaneous swaps (level 11) |
| Swap log | swap count for the ranking; future ghost/replay |

## Tech Stack
- C# 12, **.NET 8** class library (`net8.0`), no NuGet dependencies.
- Own `Vec3` struct (no Godot types).
- Tests: **xUnit** (the default `dotnet new xunit` template, no extra assertion library).

## Commands
Run from the repository root (in Git Bash, `export PATH="$PATH:/c/Program Files/dotnet"` first).
```
Build:    dotnet build Swapping.sln
Test:     dotnet test Swapping.sln
Coverage: dotnet test Swapping.sln --collect:"XPlat Code Coverage"
Format:   dotnet format Swapping.sln
```

## Project Structure
```
Swapping.sln
src/Swapping.Rules/
  Model/        → Vec3, Container, Variable, PropertyKind, BarrierFilter
  Operations/   → IOperation, SwapOperation, Restoration, SwapRequest, SwapResult, RejectReason
  Policies/     → LevelRules (unlocked properties, ranges, cooldown, flags), Cooldown
  Physics/      → Formulas (kinetic energy, speed from energy, density, pressure)
  World.cs      → holds containers and variables, enforces the invariant, keeps the swap log
tests/Swapping.Rules.Tests/
  → one test class per rule area (PositionSwapTests, MassSwapTests, EnergySwapTests,
    LegalityTests, CooldownTests, RestorationTests, InvariantTests, FormulaTests)
```

## Code Style
```csharp
public sealed class SwapOperation : IOperation
{
    public SwapResult Apply(World world, SwapRequest request, double now)
    {
        var rejection = world.Rules.Check(world, request, now);
        if (rejection is not null)
            return SwapResult.Rejected(rejection.Value);

        foreach (var kind in request.Properties)
            world.ExchangeVariables(request.First, request.Second, kind);

        world.Log.Record(request, now);
        return SwapResult.Applied(world.BodyUpdatesFor(request.First, request.Second));
    }
}
```
- `PascalCase` for types and members, `camelCase` for locals, file-scoped namespaces,
  `Swapping.Rules.*`.
- SI units everywhere (m, kg, J, s, Pa); unit in the name when ambiguous (`rangeMeters`).
- Time is always passed in (`now`, in seconds); the library never reads a clock or random numbers.
- Rejections are values (`SwapResult.Rejected(reason)`), not exceptions. Exceptions only for
  programming errors (unknown container id).
- The library returns **body updates** (new position, mass, velocity per container); the game applies them.

## Testing Strategy
- **Unit tests** for every rule, using the numeric examples of this spec as test cases.
- **Invariant test:** hundreds of random swaps and restorations from a fixed seed; after each step,
  every variable has exactly one user and every container uses one variable per property.
- **One test per rejection reason**, plus the boundary (e.g. distance exactly equal to the range is allowed).
- Coverage goal: **≥ 90 % of lines** in `Swapping.Rules`.
- No Godot in tests: everything runs with `dotnet test`.

## Boundaries
- **Always:** run `dotnet test` before each commit; keep the library pure (no Godot, IO, clock or
  randomness); add a test for every rule; keep the invariant.
- **Ask first:** adding a NuGet package; changing the public API after the game starts using it;
  changing a physics rule decided with the user.
- **Never:** reference Godot from `Swapping.Rules`; use static mutable state; delete or skip a failing test.

## Success Criteria
1. `dotnet build` and `dotnet test` pass with no warnings in `Swapping.Rules`.
2. Mass swap: player 10 kg, A 2 kg, B 3 kg; swap player↔A then player↔B → player 3, A 10, B 2.
   A enters a variable barrier → player 3, A 2, B 10.
3. Mass change keeps energy: 2 kg at 3 m/s becoming 8 kg → 1.5 m/s (± 0.001).
4. Energy swap: 1 kg ball at 4 m/s to the right ↔ 70 kg player at rest → player ≈ 0.478 m/s to the
   right, ball at rest.
5. Position swap exchanges places and keeps both velocities.
6. Position restoration: a moving box enters a variable barrier and swaps places with the resting
   holder of its position variable; the box keeps moving, the other keeps its own (zero) energy.
7. Energy restoration: box A moving right with Xb = 10 J gets back Xa = 2 J (holder moving left) →
   A moves left at √(2·2 ÷ m_A).
8. Each rejection reason in the legality table has a passing test.
9. Cooldown 2 s: swap at t = 0 → a swap at t = 1.5 is `OnCooldown`, at t = 2.0 is applied.
10. Pressure: 10 kg on 0.01 m² → 9 800 Pa; breaks a 5 000 Pa glass, not a 10 000 Pa one.
11. Buoyancy: the player (70 kg, 0.07 m³) in water → **Swim**; holding a 30 kg, 0.01 m³ rock →
    ρ = 100 ÷ 0.08 = 1 250 → **Sink**; with 0.05 kg in air → ρ ≈ 0.71 → **Swim** (flies).
12. The invariant test passes for 1 000 random steps.
13. `Swapping.Rules.csproj` has no reference to Godot.
14. Line coverage ≥ 90 %.

## Decisions confirmed with the user
- The **player** is 70 kg with 0.07 m³ (density 1 000 kg/m³ → swims in water by default).
- A swap counts as **1 move** in the ranking (a simultaneous swap too). **Restorations by a barrier
  do not count.**
- Defaults: range **8 m** for every property, cooldown **1 s** in levels 1–6; levels 7–10 decided
  while playtesting. Tunable per level, but should rarely change.
- **Accepted properties are shown by the outline color, not the object color.** When the player aims
  at or holds an object, it gets an outline whose color tells what it accepts (e.g. blue = mass only).
  Objects that accept everything the level unlocked keep the default **white** outline. The library
  only knows *which* properties each object accepts (`PropertyNotAccepted`); the palette is decided
  in the `game` spec.
- **Direction travels with the energy variable**, in swaps *and* restorations. The variable always
  mirrors how its current holder is moving (magnitude and direction, written every physics step);
  whoever takes it moves that way. Example: box A moves right using Xb = 10 J; it crosses a variable
  barrier and gets back Xa = 2 J, whose holder was moving left → A now moves **left** with 2 J.
  One rule for everything: *you move the way your energy variable was moving*.

## Open Questions
- Outline color palette (one color per accepted property, white = all) → `game` spec.
