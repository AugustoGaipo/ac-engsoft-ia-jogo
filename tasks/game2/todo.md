# Slice 2 of `game` — menu, local progress, levels 2, 3 and 5

Spec: `docs/specs/SPEC-game.md`, section "Slice 2" (SC 13–17). Made with `agent-skills:planning-and-task-breakdown`.
Each task: red test → code → mutation → diary → tick → commit and push.

- [ ] **Task 12 — `LevelProgress` (pure, S/M).** Unlock in catalog order, two bests, continue level,
      JSON round-trip, corrupt → fresh. SC 13, 14. Files: `src/Swapping.GameLogic/LevelProgress.cs`, tests.
- [ ] **Task 13 — Menu, level select, controls, end and pause buttons (M).** `--level <id>`; no args →
      menu; progress saved in `user://progress.json` after each level. SC 15. Files: `game/scripts/Menu.cs`,
      `Main.cs`, `RunFlow.cs`, `SmokeTest.cs`.
- [ ] **Task 14 — Level 2 scene and barrier looks (M).** `game/levels/level_2.tscn`, barrier colors,
      scripted run of 2.1–2.3. SC 16.
- [ ] **Task 15 — Level 3 scene (M).** `level_3.tscn`, scripted run of 3.1–3.2 (heavy button, mass jump). SC 16.
- [ ] **Task 16 — Level 5 scene (M).** `level_5.tscn`, scripted run of 5.1–5.2 (take and give energy). SC 16.
- [ ] **Task 17 — Close (S).** Coverage, play-test sheet for levels 2/3/5 and the menu, report, README, C4, diary. SC 17.

Checkpoint after Task 13: the user plays the menu with level 1. After Task 16: the user plays all levels.
