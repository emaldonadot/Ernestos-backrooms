# Milestone 5: Persistence

## User story

As a player, I want to quit and come back to find the exact maze I'd already partly solved — not a new one, and not one where my progress silently vanished.

As a developer, I want save/load to be "regenerate the deterministic world, then reattach saved per-object state to it," not "serialize a snapshot of every GameObject," so the save file stays small and save data never has to model spatial geometry.

## Prerequisite bug fix: room/door Guids weren't actually deterministic

Milestone 2's `RoomGraphGenerator` reproduces the same **layout** (positions, categories, connections) for a given seed — verified by `Generate_WithSameSeed_ProducesIdenticalLayout` — but each `RoomNode.Id` was assigned via `Guid.NewGuid()`, which is different every run. That's fine for a single play session, but persistence needs the *same* seed to produce the *same* room/door identities across a save and a later load, or saved per-object state (which room a door connects, which room a mark belongs to) can never reattach correctly after reload.

Fixed by threading the generator's existing seeded `System.Random` through node placement and deriving each `Guid` from 16 bytes pulled from that same stream, instead of calling `Guid.NewGuid()`. Since the sequence and count of random draws for a given seed was already deterministic (that's what made the layout reproducible), this makes the *identities* reproducible too, for free. Covered by a new test asserting two generations from the same seed produce identical node Guids, not just identical positions.

The same problem existed one level down: procedurally-placed `Door`s all shared the literal GameObject name `"DoorHinge"`, and `Door.SaveId` defaults to the GameObject name — every door in a level reported the same `SaveId`. Fixed by giving `Door` an `internal SetSaveId`, called by `ProceduralLevelBuilder` with an id derived from the (now-deterministic) connection's two room Guids.

## Scope (per PRD Milestone 5 + Section 18)

In scope:
- Save: world seed, player position, door open/locked state, item-collected state, puzzle progress, Field Log discovery state, and Field Marks.
- Load: regenerate the level from the saved seed (deterministic, per the fix above), then reattach every saved per-object state to the newly-instantiated objects by `SaveId`/room `Guid`.
- A version field on the save schema, per PRD's "future updates can migrate older saves."
- Manual trigger via two keybinds (quicksave/quickload) — functional correctness over UI polish, consistent with the PRD's MVP framing. A dedicated save/load *menu* (Section 17) is a UI-layer feature for a later milestone, not required here.

Out of scope (later milestones / explicitly deferred):
- Chunk identities (Milestone 2 doesn't stream chunks yet — nothing to persist there until it does).
- Creature state, discovered secrets (neither system exists yet).
- Multiple save slots, a save/load menu, autosave.
- Actual migration logic between save versions (there's only ever been version 1 so far — the field and the version-mismatch check exist; a migrator only gets written once there's a version 2 to migrate *from*).

## Technical design

### `EndlessRooms.Core` addition

- `SaveableRegistry`: mirrors `WorldCommandExecutor`'s shape — `Register`/`Unregister`/`GetAll` for live `ISaveable` instances. `Door` and `PickupTestItem` register in `OnEnable`, unregister in `OnDisable`. Registered into `GameServices` by `GameBootstrap`, same as `WorldCommandExecutor`.

### `EndlessRooms.Persistence` (new assembly; references Core, Procedural, World, Map, Puzzles — this is the one system that legitimately needs to know about all of them, to pull their state together)

- `SaveData` (and small nested `[Serializable]` DTOs) — a plain-data schema for `UnityEngine.JsonUtility` (built-in, no new package): `Version`, `Seed`, `PlayerPosition`, a list of `{ SaveId, TypeId, StateJson }` for every generic `ISaveable` (`TypeId` picks the concrete type to deserialize `StateJson` into — `JsonUtility` doesn't support polymorphism, so this is an explicit, small switch rather than reflection magic), plus dedicated entries for Field Log discovery/marks and puzzle progress, since those aren't behind `ISaveable`.
- `SaveService` (`MonoBehaviour`): `Save()` walks `SaveableRegistry.GetAll()` plus `FieldLogService`/`PuzzleGateController`/the player transform and writes `SaveData` as JSON to `Application.persistentDataPath`. `Load()` calls `ProceduralLevelBuilder.BuildLevel(savedSeed)` (regenerating the deterministic world fresh), then reattaches every saved entry by id.
- `SaveLoadController` (`MonoBehaviour`): reads two new input actions (`QuickSave`/`QuickLoad`) and calls `SaveService`. Feedback is a `Debug.Log` line, not a new toast widget — visible to a tester in the Console, and to me via the headless log, without adding UI scope this milestone doesn't need.

### Extensions needed for full-fidelity restore

- `SwitchSequencePuzzle` gains `Progress` (the actual activated-index list, not just a count) and `RestoreProgress(progress, isSolved)` — a saved *partial* sequence needs to resume exactly, not just remember whether it was solved.
- `FieldLogService` gains `RestoreDiscoveryState(roomId, state)` and `RestoreMark(mark)` — load-path methods that set exact state, bypassing `MarkRoomEntered`'s neighbor-promotion logic (which is only correct for the live-discovery case, not restoring a snapshot).
- `PuzzleGateController` gains `CaptureState()`/`RestoreState(...)`.
- `ProceduralLevelBuilder.BuildLevel` gains an overload taking an explicit seed, so `Load()` can regenerate from the *saved* seed rather than whatever's configured in the Inspector.

### Tests

Everything above except the `MonoBehaviour` coordinators (`SaveService`/`SaveLoadController`) is plain C#, so it's covered by EditMode tests: the new Guid-determinism assertion, `SwitchSequencePuzzle` progress restore, `FieldLogService` restore-path methods, and a `JsonUtility` round-trip test for `SaveData` itself. The `MonoBehaviour` integration (does pressing F5 then F9 in a live scene actually restore the world) is verified the same way Milestones 1-4's scene-level behavior was: headless scene build with no exceptions, then a manual Play-mode check.

## Plan

1. Feature branch `feature/milestone-5-persistence` off `main` (PRs #1-#4 all merged).
2. Fix the Guid-determinism and Door-SaveId bugs first; extend existing tests to cover them.
3. `SaveableRegistry` (Core); wire `Door`/`PickupTestItem`.
4. `EndlessRooms.Persistence`: `SaveData`, `SaveService`, `SaveLoadController`.
5. Restore-path extensions to `SwitchSequencePuzzle`, `FieldLogService`, `PuzzleGateController`, `ProceduralLevelBuilder`.
6. `QuickSave`/`QuickLoad` input actions.
7. Headlessly build `Milestone5_PersistenceTestScene` (Milestone 4's scene plus a pickup item and the save/load wiring) — verify compiles clean, EditMode tests pass, scene builds with no exceptions.
8. Commit, push, open a PR against `main`. Stop for confirmation before Milestone 6 (Horror Prototype).
