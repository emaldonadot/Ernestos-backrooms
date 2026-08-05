# Milestone 3: Map System

## User story

As a player, I want a map that only ever shows me what I've actually found, so that navigating The Continuance feels like building my own understanding of the place rather than reading a spoiler.

As a developer, I want room-discovery logic decoupled from how it's rendered, so a future physical in-world map, minimap, or co-op shared/individual view (PRD Section 10, 20) can reuse the same data without touching the discovery rules.

## Scope (per PRD Milestone 3 + Section 10)

In scope:
- Detect player entry into a room (trigger volume on the modular room prefab).
- Reveal only rooms the player has entered, plus the immediate existence of rooms adjacent to an entered room (a "glimpsed" state — you've seen a doorway lead somewhere, but not what's through it) — this is the PRD's "see doors or routes that were discovered but not entered."
- Show the player's current room on the map.
- Pan and zoom the map view.
- Place and remove custom markers (`Danger`, `Puzzle`, `LockedDoor`, `Supplies`, `HidingPlace`, `ExitCandidate`, `Secret`) with a short note, at the player's current room.
- `FieldLogService` (pure C#, testable) fully decoupled from `FieldLogView`/`FieldMarkerPanel` (UI).

Out of scope (later milestones per the roadmap):
- Save/load of discovery state and markers (Milestone 5 — the data model here is already plain-old-data so it will serialize cleanly then, but no save/load code lands now).
- Multiple floors/Strata rendered as separate map layers (no vertical generation exists yet; single-layer only).
- A physical in-world map object, or per-player vs. shared discovery for co-op (both explicitly deferred future options in Section 10/20 — the `OwnerId` placeholder field on `FieldMark` exists for this, unused for now).

## Technical design

### Discovery data model — `EndlessRooms.Map` (new assembly; references `EndlessRooms.Core`, `EndlessRooms.Procedural`, `EndlessRooms.World`)

- `RoomDiscoveryState`: `Unknown` (never shown) → `Glimpsed` (position known, category hidden — adjacent to an entered room but not itself entered) → `Entered` (fully known).
- `FieldLogRoomView`: a read-only projection (`RoomId`, `GridPosition`, `Category?`, `State`) — the only thing `FieldLogService` exposes about a room; never the underlying `RoomNode`.
- `FieldMarkType`: `Danger`, `Puzzle`, `LockedDoor`, `Supplies`, `HidingPlace`, `ExitCandidate`, `Secret`.
- `FieldMark`: `Id`, `RoomId`, `LocalOffset`, `Type`, `Note`, `OwnerId` (placeholder, unused until co-op).
- `FieldLogService`: holds the ground-truth `RoomGraph` (set via `Initialize`) privately; exposes only `GetKnownRooms()`, `GetKnownConnections()` (a connection is shown once either endpoint is `Entered`), `CurrentRoomId`, and marker CRUD. Subscribes to `GameEvents.RoomEntered` itself; `MarkRoomEntered(roomId)` promotes that room to `Entered`, promotes its immediate neighbors from `Unknown` to `Glimpsed` (never demotes an already-`Entered` neighbor), and updates `CurrentRoomId`. Raises `DiscoveryChanged`/`MarksChanged` for the view to redraw on.
- `MapBootstrap` (`MonoBehaviour`): the one place that needs to know both `FieldLogService` (Map) and `ProceduralLevelBuilder` (World) exist — registers a fresh `FieldLogService` into `GameServices` on `Awake`, wires it to the builder's `LevelBuilt` event, and disposes (unsubscribes from `GameEvents`) on `OnDestroy` so repeated Play sessions in the Editor don't accumulate stale subscribers.

### World-side detection — `EndlessRooms.World`

- `RoomTrigger` (`MonoBehaviour`): a trigger collider on the modular room prefab; on the player entering, calls `GameEvents.RaiseRoomEntered(RoomId)`. `RoomId` is wired by `ProceduralLevelBuilder` at instantiation time, the same way it already wires `RoomInstance`.
- `ProceduralLevelBuilder`: gains a public `event Action<RoomGraph> LevelBuilt` (raised at the end of `BuildLevel()`) and `GetEntryWorldPosition()`, so World-level consumers (the player spawner, `MapBootstrap`) don't need their own copy of the grid-to-world math.
- `LevelPlayerSpawner` (`MonoBehaviour`): builds the level once, then moves the player to `GetEntryWorldPosition()` (toggling the `CharacterController` off/on around the teleport, the standard way to avoid it fighting the manual position set).

### Rendering — `EndlessRooms.UI` (references `EndlessRooms.Map` in addition to what it already references)

- `FieldLogView`: toggled by a new `ToggleMap` input action; renders each `FieldLogRoomView` as a small icon positioned by `GridPosition` (category-colored when `Entered`, dimmed/unknown-colored when `Glimpsed`), the current room highlighted, and a thin rotated `Image` per known connection (`UILineFactory` helper). Pan/zoom read two more new input actions (`PanMap`, `ZoomMap`) only while the map is open — arrow keys and `+`/`-`, not mouse drag, so this is verifiable without relying on precise pointer input.
- `FieldMarkerPanel`: one button per `FieldMarkType`, a note `InputField`, an "Add at current room" button, and a small list of the current room's existing marks with per-mark "Remove" buttons. Needs a scene `EventSystem` (not present until now — Milestone 1/2 had no clickable UI).

### Tests

`FieldLogServiceTests` (EditMode): a small hand-built `RoomGraph` fixture (no scene needed) — asserts `MarkRoomEntered` promotes the right rooms to `Entered`/`Glimpsed` and never regresses an `Entered` room, that `GetKnownConnections` only returns connections touching a known room, and that adding/removing marks behaves correctly.

## Plan

1. Feature branch `feature/milestone-3-map-system` off `main` (both prior PRs are merged).
2. `GameEvents.RoomEntered`; `EndlessRooms.Map` assembly with the discovery model + tests.
3. `RoomTrigger`, `ProceduralLevelBuilder.LevelBuilt`/`GetEntryWorldPosition`, `LevelPlayerSpawner` in `EndlessRooms.World`.
4. `FieldLogView` + `FieldMarkerPanel` + `UILineFactory` in `EndlessRooms.UI`; two new input actions (`ToggleMap`, `PanMap`, `ZoomMap`) added to the existing `.inputactions` asset.
5. Headlessly: add a `RoomTrigger` + trigger collider to the existing `ModularRoomBase` prefab, tag the player "Player", build `Milestone3_MapTestScene` (level + player + spawner + map UI + `EventSystem`) — verify compiles clean, EditMode tests pass, scene builds with no exceptions.
6. Commit, push, open a PR against `main`. Stop for confirmation before Milestone 4 (Puzzle & Progression).
