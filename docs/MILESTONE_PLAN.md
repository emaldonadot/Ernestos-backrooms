# The Endless Rooms — Architecture & Milestone Plan

This document is the durable, in-repo record of the approved architecture and milestone breakdown for "The Endless Rooms." See [`PRD.md`](../PRD.md) for the full product requirements and [`DECISIONS.md`](../DECISIONS.md) for the dated rationale behind individual technical choices. Update this file only when a milestone's scope changes; log the *why* in `DECISIONS.md`.

## Identity & Premise

**World name:** *The Continuance* — an office-industrial architecture that keeps extending itself past its own blueprint. Layers are **Strata**; local room clusters within a stratum are **Sectors**. Exit/progression rooms are **Threshold Rooms**. Unexplained environmental anomalies are **Drift**.

**Premise:** The player is a night-shift employee of the fictional Aldermere Business Park who takes a service stairwell during a power outage and never reaches the ground floor again. Evidence that the space was once a real office complex (work orders, inspection tags, PA announcements, personnel logs) is everywhere, but the architecture has clearly kept building itself long after anyone was maintaining it. The mystery: who or what is still doing the maintenance.

**Original creatures:**
- **The Attendant** — territorial per-Sector patroller; investigates recently opened doors and disturbed noise. First prototype creature (Milestone 6).
- **The Boarder** *(later, rare)* — passive mimic that holds still like a person/furniture and only advances while unobserved.

**Map terminology:** the player-built map is the **Field Log**; personal annotations are **Field Marks**.

## Unity Project Structure

```
Assets/
  TheEndlessRooms/
    Scripts/
      Core/            (asmdef: EndlessRooms.Core)         - interfaces, event bus, service locator
      Player/          (asmdef: EndlessRooms.Player)       - controller, camera, interaction, stamina
      Procedural/      (asmdef: EndlessRooms.Procedural)   - room graph gen, chunk streaming
      World/           (asmdef: EndlessRooms.World)        - room/door/lock runtime behaviors
      Map/             (asmdef: EndlessRooms.Map)          - Field Log discovery + rendering
      Puzzles/         (asmdef: EndlessRooms.Puzzles)
      AI/              (asmdef: EndlessRooms.AI)           - creature framework (from Milestone 6)
      Persistence/     (asmdef: EndlessRooms.Persistence)  - save/load
      UI/              (asmdef: EndlessRooms.UI)
      Multiplayer/     (asmdef: EndlessRooms.Multiplayer.Abstractions) - interfaces only, no netcode dep yet
    Prefabs/
    ScriptableObjects/
    Scenes/
    Art/ (Materials, Models, Textures)
    Audio/
    Settings/ (URP asset, Input Actions asset)
Tests/
  EditMode/            (asmdef: EndlessRooms.Tests.EditMode) - references Core + Procedural only
```

## Major Systems & Boundaries

| System | Responsibility | Key types |
|---|---|---|
| Core | Cross-cutting interfaces, events, service access | `IInteractable`, `ISaveable`, `IDetectable`, `IWorldCommand`, `GameEvents`, `GameServices` |
| Player | FPS movement, camera, interaction raycast, stamina | `PlayerController`, `PlayerMovementConfig`, `InteractionCaster` |
| Procedural | Deterministic room-graph generation + validation, chunk streaming | `RoomDefinition`, graph generator, reachability validator |
| World | Runtime door/lock/switch behavior, executes world commands | `Door`, `Lock`, `WorldCommandExecutor` |
| Map | Field Log discovery state + rendering | `FieldLogService`, `FieldLogView`, `FieldMark` |
| Puzzles | Modular puzzle framework, seed-derived solutions | puzzle interfaces + events (Milestone 4) |
| AI | Creature perception/behavior framework | (Milestone 6) |
| Persistence | Save/load, versioned save data | (Milestone 5) |
| UI | HUD, prompts, menus, map screen | uGUI-based views |
| Multiplayer.Abstractions | Command routing/ownership seams for future netcode | `IWorldCommand` consumers, stable `Guid` IDs |

## Procedural Generation Strategy

1. **Abstract graph phase** (pure C#, no `MonoBehaviour`, no `UnityEngine.Random`): grow a room graph from a seeded RNG, reserving a critical path from entry to exit through mandatory gates first, then branching optional rooms per `RoomDefinition` category/rarity/connector rules.
2. **Spatial layout phase**: place modular prefabs on a grid along the validated graph, snapping connectors, rejecting/retrying on collision.
3. **Validation pass**: BFS/DFS reachability from entry to exit and all mandatory nodes; failed layouts regenerate from a derived sub-seed instead of looping.
4. **Chunk streaming**: graph clusters (~8-12 rooms) are load/unload units; visited-room state persists in a `WorldState` keyed by a stable `Guid` assigned deterministically at generation time, independent of whether the chunk is currently loaded.

This keeps the generation logic in `EndlessRooms.Procedural`, testable from `EndlessRooms.Tests.EditMode` without a loaded scene — the target for "automated generation tests across many seeds."

## Map Representation Strategy

- `FieldLogService` mirrors the world graph but exposes only discovered nodes/edges; `PlayerEnteredRoom` events mark rooms discovered and reveal edges to known neighbors.
- `FieldLogView` (rendering) is fully decoupled from `FieldLogService` (data) — a future physical in-world map or co-op shared/individual view can reuse the same discovery data.
- `FieldMark` records (room `Guid` + local offset + type + note + placeholder `ownerId`) support future per-player vs. shared discovery without a schema change.

## Co-op Readiness Approach

- No singleton "the player" references in gameplay-relevant code; interaction/detection calls take explicit context.
- World-mutating actions go through `IWorldCommand` → `WorldCommandExecutor`, the single seam for future authority checks.
- Persistent objects (rooms, doors, items, puzzles) get stable `Guid`s at generation/placement time, reusable later as network identities.
- Generation is deterministic and `UnityEngine.Random`-free so future co-op clients reproduce identical layouts from a shared seed.

## Milestone Breakdown

| # | Milestone | Status |
|---|---|---|
| 1 | Project Foundation (folders, asmdefs, input, player movement, interaction interfaces, data definitions, test scene) | Complete (PR #1) |
| 2 | Modular World (room prefabs, connectors, seed-based layout, connectivity validation, debug viz) | **In progress** — see [milestone-2-modular-world.md](features/milestone-2-modular-world.md) |
| 3 | Map System (discovery, Field Log rendering, pan/zoom, markers) | Not started |
| 4 | Puzzle & Progression (puzzle framework, locked route, exit room) | Not started |
| 5 | Persistence (save/load seed, map, puzzle, door, item, marker state) | Not started |
| 6 | Horror Prototype (The Attendant: perception/investigate/chase/search, hiding, capture/restart) | Not started |
| 7 | Expanded Vertical Slice (variety, landmark room, secret room, storytelling, polish, playtesting) | Not started |
| — | Online Co-op (future) | Not started — architecture prepared, no networking package added yet |

Per the PRD's explicit process, each milestone after the current one requires user confirmation before implementation begins.

## Milestone 1 — Implementation Steps

1. Bootstrap Unity 6 LTS + URP project, folder structure and asmdefs above, Input Actions asset (Move, Look, Sprint, Crouch, Interact, Pause).
2. Core interfaces & services: `IInteractable`, `ISaveable`, `IWorldCommand`, `WorldCommandExecutor`, `GameEvents`, `GameServices`.
3. Player controller: `CharacterController`-based walk/run/crouch + mouse look + ScriptableObject-configured stamina.
4. Interaction system: camera raycast detecting `IInteractable`, minimal uGUI prompt.
5. Data definitions: `RoomDefinition` ScriptableObject (category, connector sockets, rarity, difficulty weight), `PlayerMovementConfig` ScriptableObject.
6. Manual test scene: grey-box room, a door prefab (`IInteractable`), a pickup stub (`IInteractable` + `ISaveable`), to validate the controller and interaction system end-to-end.

**Acceptance:** player walks/runs/crouches/looks around; interacting with the test door/pickup logs feedback and shows a UI prompt; project compiles cleanly across all asmdefs with no console errors/warnings; work lands via PR from `feature/milestone-1-foundation` into `main`.
