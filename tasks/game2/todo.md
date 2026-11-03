# Slice 2 of `game` — menu, local progress, property keys, levels 2 and 3

Spec: `docs/specs/SPEC-game.md`, section "Slice 2" (SC 13–18). Made with `agent-skills:planning-and-task-breakdown`.
Each task: red test → code → mutation → diary → tick → commit and push.

- [x] **Task 12 — `LevelProgress` (pure, S).** SC 13, 14. `src/Swapping.GameLogic/LevelProgress.cs` + tests.
- [x] **Task 13 — New catalog (M).** `levels/level_2.json` = Peso (2.1, 2.2, old 3.1/3.2 as 2.3/2.4),
      `level_3.json` = Energia (old 5.1/5.2), `level_5.json` removed, `accepts`; Levels design tests and
      API tests updated. SC 16.
- [ ] **Task 14 — Property keys and wheel rotation (S/M).** `PropertyKeys` in GameLogic (tests), keys in
      `Controls`, HUD shows the property, `SwapController` uses it; wheel turns the held object. SC 15.
- [x] **Task 15 — Menu, level select, test map, end and pause buttons (M).** `Menu.cs`, `Main.cs`,
      `RunFlow.cs`, progress file, headless menu checks. SC 17.
- [ ] **Task 16 — Level 2 scene and barrier looks (M).** `level_2.tscn`, scripted run. SC 17.
- [ ] **Task 17 — Level 3 scene (M).** `level_3.tscn`, scripted run. SC 17.
- [ ] **Task 18 — Close (S).** Coverage, play-test sheet, report, README, C4, diary. SC 18.

Checkpoint after Task 15: the user plays the menu and level 1. After Task 17: the user plays everything.
