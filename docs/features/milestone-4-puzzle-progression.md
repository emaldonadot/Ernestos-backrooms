# Milestone 4: Puzzle & Progression

## User story

As a player, I want a real obstacle between me and the exit — not just a maze to walk through — so that reaching it feels earned.

As a developer, I want puzzle-solving logic that never touches a `MonoBehaviour`, so "does this sequence solve the puzzle" is a fact I can assert in a test, the same way room-graph generation already is.

## Scope (per PRD Milestone 4 + Section 11 + MVP Section 21)

In scope:
- A puzzle framework (`IPuzzle`) generic enough for later puzzle types, with one concrete implementation: a switch-sequence puzzle ("activating switches in a particular sequence," PRD Section 11), its solution derived from the level's seed.
- One locked route: the connection into the Exit room starts locked and only opens once the puzzle is solved.
- One exit condition: an `ExitPoint` in the Exit room that ends the level on interact.
- Puzzle logic (`EndlessRooms.Puzzles`) fully separated from the physical switches/door that present it (`EndlessRooms.World`), mirroring the existing `Procedural`/`World` split.

Out of scope (later milestones per the roadmap):
- Save/load of puzzle progress and door lock state (Milestone 5 — `SwitchSequencePuzzle`'s state is plain data, so it'll serialize cleanly then, but no save/load code lands now).
- Other puzzle categories from Section 11 (symbols, light/sound patterns, furniture manipulation, cooperative mechanisms) — one concrete puzzle type is enough to prove the framework; more arrive as later content, not architecture.
- Placing the puzzle via the procedural generator's `RoomCategory.Puzzle` (which has existed unused since Milestone 1). Randomly placing a puzzle room isn't deterministic enough for reliable testing yet, so for this milestone the puzzle switches spawn at a fixed offset from the always-reachable entry room instead. Generator-driven puzzle-room placement is a natural Milestone 7 ("expanded procedural variety") follow-up, noted here rather than built now.

## Technical design

### Puzzle logic — `EndlessRooms.Puzzles` (new assembly; references `EndlessRooms.Core` only, no `MonoBehaviour`)

- `IPuzzle`: `IsSolved`, `event Action Solved`, `Reset()`. Minimal on purpose — anything that can be "solved" can gate a locked route the same way, regardless of puzzle type.
- `SwitchSequencePuzzle`: constructed with a required activation order (`IReadOnlyList<int>`); `Activate(int switchIndex)` extends the player's progress if it matches the next expected index, solves once the full sequence is entered, and resets progress (not the overall puzzle if already solved) on a wrong index. `GenerateSequence(int seed, int switchCount)` is a static helper that shuffles `0..switchCount-1` with a seeded `System.Random` — the PRD's "randomized solutions should be derived from the world seed."

### Physical presentation — `EndlessRooms.World` (already references Core/Procedural; add `EndlessRooms.Puzzles`)

- `PuzzleSwitch`: an `IInteractable` lever with a fixed `SwitchIndex`; interacting reports to whichever `PuzzleGateController` owns it.
- `PuzzleGateController`: owns one `SwitchSequencePuzzle`, references its `PuzzleSwitch`es and the `Door` to unlock. Builds the required sequence from `ProceduralLevelBuilder.Seed` once the level exists (via `LevelBuilt`, same hookup pattern `MapBootstrap` already uses). On `Solved`, submits an `UnlockDoorCommand` through `WorldCommandExecutor` — the same command-based path `Door`'s own open/close already uses, so this is one more thing a future server-authoritative executor can gate without touching call sites.
- `Door` gains `IsLocked`/`SetLocked(bool)`. `CanInteract` stays `true` while locked (so the interaction prompt still appears — PRD wants puzzles/locks communicated through the environment, not hidden) but `Interact()` refuses and logs feedback instead of toggling while locked.
- `ProceduralLevelBuilder` gains a `Seed` getter, an `ExitDoor` property (the `Door` instance placed on whichever connection touches the exit node — always exists, since `RoomGraphValidator` already guarantees the exit is reachable), and spawns an `ExitPoint` inside the exit room during instantiation.
- `ExitPoint`: an `IInteractable` placed in the exit room; interacting raises `GameEvents.LevelCompleted` once and disables itself.

### UI — `EndlessRooms.UI`

- `LevelCompleteUI`: a simple "You Found The Exit" panel shown on `GameEvents.LevelCompleted`.

### Reachability

The puzzle switches spawn at a fixed offset from `GetEntryWorldPosition()`, not inside generator-placed geometry, so they're reachable by construction regardless of seed — there's no new graph-topology validation needed here (unlike Milestone 2's room-graph reachability, this isn't graph-dependent).

### Tests

`SwitchSequencePuzzleTests` (EditMode): correct-order activation solves; a wrong index resets progress without solving or losing an already-solved state; `GenerateSequence` is deterministic for a given seed and produces a permutation of `0..count-1`.

## Plan

1. Feature branch `feature/milestone-4-puzzle-progression` off `main` (PRs #1-#3 all merged).
2. `EndlessRooms.Puzzles` (`IPuzzle`, `SwitchSequencePuzzle`) + tests.
3. `Door` lock state, `PuzzleSwitch`, `PuzzleGateController`, `UnlockDoorCommand`, `ExitPoint`, `ProceduralLevelBuilder.Seed`/`ExitDoor` in `EndlessRooms.World`.
4. `GameEvents.LevelCompleted` in Core; `LevelCompleteUI` in UI.
5. Headlessly build `Milestone4_PuzzleTestScene` (level + player + spawner + map UI + puzzle gate + level-complete UI) — verify compiles clean, EditMode tests pass, scene builds with no exceptions.
6. Commit, push, open a PR against `main`. Stop for confirmation before Milestone 5 (Persistence).
