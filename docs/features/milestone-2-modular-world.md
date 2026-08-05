# Milestone 2: Modular World

## User story

As a player, I want each playthrough's layout of rooms and corridors to be different but always solvable, so that exploration stays fresh without ever leaving me stuck behind an impossible layout.

As a developer, I want room-graph generation to be pure, seeded, and unit-testable, so that "does this seed always produce a valid layout" is a fact I can check automatically across hundreds of seeds, not something I discover by playing the game.

## Scope (per PRD Milestone 2 + Section 9)

In scope:
- A modular room prefab (reused across categories — visual variety per category is Milestone 7, not this one) with four independently toggleable walls and directional connector sockets.
- `RoomCategory`-aware graph generation: a critical path from entry to exit, optional branches, connector/category compatibility checks.
- Deterministic, seeded generation using `System.Random` (never `UnityEngine.Random`), so the same seed reproduces the same graph.
- Overlap prevention by construction (one room per grid cell) rather than post-hoc collision checks.
- A reachability validator (entry → exit, entry → every mandatory room) with bounded retry on a derived sub-seed if a layout is invalid.
- Runtime instantiation: walk the validated graph, place rooms on a grid, open walls and place a `Door` at each connection.
- Debug visualization (Scene view Gizmos) showing node categories, connections, and validity.
- EditMode tests generating many seeds and asserting every one validates.

Out of scope (later milestones per the PRD roadmap):
- Chunk streaming/unloading (Milestone 2 targets one finite test maze, not open-world streaming).
- Puzzle-gated routes, locked doors, exits that require an item (Milestone 4).
- Room categories beyond what's needed to demonstrate the system: `Standard`, `Corridor`, `Junction`, `DeadEnd`, `Exit`. The other 10 enum values already exist from Milestone 1 and get their own `RoomDefinition` assets/prefabs as later milestones need them.
- Landmark/lore/secret/anomaly rooms, monster encounter areas (Milestones 6-7).

## Technical design

### Pure graph model — `EndlessRooms.Procedural` (no `MonoBehaviour`, testable without a scene)

- `Direction` (N/E/S/W enum) + `Opposite()`.
- `RoomNode`: `Guid Id`, `RoomDefinition Definition`, `Vector2Int GridPosition`.
- `RoomConnection`: `Guid FromId`, `Guid ToId`, `Direction FromDirection`.
- `RoomGraph`: `Nodes` (by `Guid`), `Connections`, `EntryNodeId`, `ExitNodeId`; helpers `GetNeighborConnections(nodeId)`.
- `RoomGraphGenerationSettings`: `Seed`, `RoomCount`, `AvailableDefinitions`.
- `RoomGraphGenerator.Generate(settings)`: seeds a `System.Random`, walks a critical path from an entry `Standard` room to an `Exit` room honoring each `RoomDefinition.AllowsNeighbor`, then branches optional rooms off the path until `RoomCount` is reached, then adds any additional connections between rooms that end up adjacent and category-compatible (this is what turns some dead-end branches into loops/junctions instead of a strict tree).
- `RoomGraphValidator.Validate(graph)`: BFS from the entry node; fails if the exit or any `IsMandatory` room isn't reachable.
- `RoomGraphGenerator.GenerateValidated(settings, maxAttempts)`: generate → validate → on failure, retry with a sub-seed deterministically derived from the original (`seed, attempt` pair), up to `maxAttempts`; throws if none validate (a config problem, not a runtime one — e.g. `RoomCount` too small for a critical path to reach an `Exit` definition).

### Runtime instantiation — `EndlessRooms.World` (references `EndlessRooms.Procedural`)

- `RoomInstance` (`MonoBehaviour`): exposes the four wall `GameObject`s and four socket `Transform`s on a room prefab instance, found once via `[SerializeField]` references set up on the prefab (not `GameObject.Find`).
- `ProceduralLevelBuilder` (`MonoBehaviour`): holds `Seed`, `RoomCount`, `AvailableDefinitions`, `CellSize`; `BuildLevel()` calls `RoomGraphGenerator.GenerateValidated`, instantiates each node's `RoomDefinition.RoomPrefab` at `GridPosition * CellSize`, and for each `RoomConnection` disables the matching wall on both rooms' `RoomInstance` and instantiates a `Door` (reusing the Milestone 1 `Door` component) at the shared boundary.
- Gizmos on `ProceduralLevelBuilder`: draw a colored dot per node (color by `RoomCategory`), a line per connection, and highlight in red if the last generation attempt failed validation — this is the "debugging tools to visualize connections, seeds, room categories, invalid layouts" the PRD asks for.

### Data assets

- One `ModularRoomBase` prefab: Floor, Ceiling, four `Wall_*` children, four socket marker `Transform`s, a `RoomInstance` component wiring all of the above.
- Five `RoomDefinition` assets (`Standard`, `Corridor`, `Junction`, `DeadEnd`, `Exit`) all pointing at `ModularRoomBase`, differing only in category/rarity/`AllowedNeighborCategories`/`IsMandatory` (only `Exit` is mandatory for M2).

### Tests

`RoomGraphGeneratorTests` (EditMode): for a range of seeds (e.g. 0-199), `GenerateValidated` must succeed and `RoomGraphValidator.Validate` on its result must report valid — this is the automated many-seeds check the PRD's risk section calls for.

## Plan

1. Feature branch `feature/milestone-2-modular-world` off `main`.
2. Implement the pure graph model + generator + validator in `EndlessRooms.Procedural`, with EditMode tests.
3. Implement `RoomInstance` + `ProceduralLevelBuilder` (+ Gizmos) in `EndlessRooms.World`.
4. Headless-build the `ModularRoomBase` prefab and the five `RoomDefinition` assets, and a `Milestone2_ProceduralTestScene` wiring a `ProceduralLevelBuilder`, the same way Milestone 1's scene was built — verify via batch-mode Editor runs (compiles clean, generation produces a valid graph, instantiation runs with no exceptions).
5. Commit, push, open a PR against `main`. Stop for confirmation before Milestone 3 (Map System).
