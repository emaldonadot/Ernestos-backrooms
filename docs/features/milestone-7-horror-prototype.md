# Milestone 7: Horror Prototype (The Attendant)

## User story

As the player, after learning to navigate, map, and solve puzzles in The Continuance, I should feel genuinely threatened by something that can catch me — one creature, simple enough to be the first prototype per the PRD ("the first prototype should contain only one simple creature"), but built on a perception/state framework general enough that later creatures (The Boarder, others) are new data + new state logic, not a new framework.

## Context

**The Attendant** is already named in `docs/MILESTONE_PLAN.md`'s identity section: "territorial per-Sector patroller; investigates recently opened doors and disturbed noise." That description is the design brief — a patroller (not a straight-line chaser), whose two perception hooks are audio (player noise, already exposed via `IDetectable.NoiseLevel` on `PlayerController` since Milestone 1) and *door state changes* specifically (not just any world event), matching the PRD's "creature that follows recently opened doors" archetype (Section 12) and avoiding the PRD's explicit warning against "every enemy simply sees the player and runs directly toward them."

PRD Section 12 requires: idle, patrol, suspicion, investigation, detection, chase, search, losing the player, returning to territory, attack/capture, audio perception, visual perception, data-asset-configurable behavior, difficulty scaling, spawn restrictions/encounter cooldowns. Section 13 requires hiding places and a checkpoint/restart on capture. Combat is explicitly out of scope — "the preferred actions are avoidance, observation, hiding, distraction, and escape."

## Scope

In scope:
- `EndlessRooms.AI` module: perception (visual cone + occlusion raycast, audio via `IDetectable`), a small state machine (Idle/Patrol → Investigate → Chase → Search → Returning), graph-based patrol/pursuit movement reusing the existing `RoomGraph`/`ProceduralLevelBuilder` infrastructure (no NavMesh — the level *is* a graph already, and PRD asks for one simple creature, not full pathfinding sophistication).
- Door-open reactivity: `Door` gains a `DoorToggled` event; The Attendant investigates the door's room when it's within hearing/relevance range.
- Hiding: designated hiding spots the player can enter/exit; visual perception always fails against a hidden player regardless of raycast result.
- Capture → restart: reuses `SaveService`'s existing save/load path as the checkpoint mechanism (a checkpoint *is* a save slot) rather than inventing a second persistence system.
- `AttendantConfig` ScriptableObject: every tunable (speeds, ranges, angles, durations, capture range) is data, not hardcoded, per PRD's "configurable behavior through data assets."
- Basic ambient/directional audio hooks: room ambience source, creature state-linked sound cues (different clips per state, so sound teaches the player what's happening), not full audio occlusion simulation (deferred — flagged below).
- Dual-platform: a small `IDetectable` implementation on the VR rig (mirroring `PlayerController`'s), since the Quest rig currently has no noise output and The Attendant's core archetype is sound-driven.
- New `Milestone7_HorrorTestScene` reusing the existing procedural/puzzle setup, with one Attendant instance.

Out of scope (deliberately deferred):
- Difficulty scaling and encounter cooldowns/spawn restrictions — meaningful once there's more than one creature or a longer session to scale across (Milestone 8 territory).
- Full audio occlusion through walls/doors (Section 15) — a basic room-ambience/state-cue hook ships now; low-pass-through-walls DSP is a polish-pass item.
- The Boarder or any second creature — PRD explicitly scopes the first prototype to one creature.
- NavMesh-based pathfinding — graph-based movement is the deliberate choice for this milestone; revisit only if graph-based movement proves too limited once real (non-grey-box) room geometry exists.

## VR comfort decision

Flagged for the user's call before building chase/capture effects, since camera shake is a well-known VR nausea trigger. Recommended camera shake on PC only, non-camera cues (vignette, audio, haptics) on VR; **user chose identical camera shake effects on both platforms** instead, prioritizing consistent feel over the more comfort-conservative default. Implemented as a single shared effect path with no platform branch — if this proves uncomfortable in practice on the headset, revisit as a per-platform config value, not a code change.

## Technical design

### `EndlessRooms.Core` — additive changes only
- `Door` gains `public event Action<Door> DoorToggled;`, raised from the existing `SetOpen` — additive, no existing call sites change behavior.
- `ProceduralLevelBuilder` gains `public bool TryGetRoomWorldPosition(Guid nodeId, out Vector3 position)`, mirroring `GetEntryWorldPosition()`'s existing lookup but for any node — needed for graph-based patrol/pursuit movement.
- New `IHideable`-adjacent concept lives in `EndlessRooms.World` (a `HidingSpot` component), not Core — it doesn't need to be an interface since there's exactly one implementation.

### New `EndlessRooms.AI` module (asmdef references Core, Procedural, World)
- `AttendantConfig` (ScriptableObject): `VisionRangeMeters`, `VisionAngleDegrees`, `HearingRangeMeters`, `NoiseDetectionThreshold`, `PatrolSpeed`, `ChaseSpeed`, `InvestigateDurationSeconds`, `SearchDurationSeconds`, `CaptureRangeMeters`, `TerritoryRoomRadius` (how many graph hops from its spawn room it patrols/investigates within).
- `AttendantState` enum: `Patrol`, `Investigate`, `Chase`, `Search`, `Returning`.
- `AttendantPerception` (pure C#, no `MonoBehaviour`): given creature position/forward, a target's position + `IDetectable.NoiseLevel` + hidden flag, and an injected occlusion-check delegate (`Func<Vector3, Vector3, bool>` — "is there a clear line between these points"), computes whether the target is visually detected (angle + range + occlusion + not-hidden) and/or audibly detected (distance-falloff noise above threshold). The occlusion delegate is injected specifically so this class is unit-testable without a scene.
- `AttendantStateMachine` (pure C#): given current state, perception results, elapsed time, and door-toggle events within range, computes the next state and a target point (room-graph node or last-known position). This is the core of what gets EditMode-tested.
- `AttendantController` (MonoBehaviour): owns a `CharacterController`, ticks the state machine every frame with real perception (raycasts against the player's `IDetectable.DetectionPoint`, real elapsed time), converts the state machine's target into a graph path via BFS over `ProceduralLevelBuilder.LastGraph.GetNeighborIds` (same technique the reachability validator already uses), and moves toward the next path waypoint's world position each frame. Subscribes to `Door.DoorToggled` for the door-reactivity archetype.
- `HidingSpot` (in `EndlessRooms.World`, since it's a world object like `Door`/`PuzzleSwitch`): `IInteractable` (enter/exit), tracks whether the player is currently hidden; `AttendantPerception` queries a hidden flag (via `GameServices`-style lookup, consistent with how other systems avoid singleton "the player" references) rather than The Attendant knowing about `HidingSpot` directly.
- Capture: `AttendantController` detects capture range during `Chase`, raises a new `GameEvents.PlayerCaptured` event. A new `RespawnController` (in `EndlessRooms.World`, alongside `LevelPlayerSpawner`) listens for it and reloads the last save via the existing `SaveService`/`SaveLoadController` path — the checkpoint *is* a save slot, not a second system.
- Camera shake / capture FX: a small `CameraShakeEffect` component (in `EndlessRooms.Player`, since it's presentation on the player rig) triggered by `GameEvents.PlayerCaptured` and a chase-intensity signal from `AttendantController`. Identical on PC and VR per the decision above — no platform branch.

### Audio (basic hooks, not full occlusion)
- `RoomAmbience` (in `EndlessRooms.World`, one per room prefab or spawned per room instance): loops a room-appropriate ambience clip (fluorescent hum, ventilation) at low volume.
- `AttendantController` plays a state-linked `AudioSource` cue on each state transition (different clip for Patrol/Investigate/Chase), satisfying "creature sounds that communicate behavior" without building occlusion DSP.

### Verification reality check
Perception math and the state machine are pure C#, fully EditMode-testable across many synthetic scenarios (in view vs. out of view, in range vs. out of range, hidden vs. not, noise above/below threshold, state transition sequencing). What headless tooling can't verify: actual creature movement feel, whether the vision cone/hearing range feel fair in a real playthrough, whether the door-investigate behavior reads as intentional rather than random, or the VR capture/chase-FX experience — all of that needs your Play-mode (and Quest) check, same as every prior milestone's dynamic behavior.

## Plan

1. Feature branch `feature/milestone-7-horror-prototype`, stacked on `main` (post Milestone 5+6 merge).
2. Core additive changes: `Door.DoorToggled`, `ProceduralLevelBuilder.TryGetRoomWorldPosition`.
3. `EndlessRooms.AI` module: config, perception, state machine (pure C#, tested first).
4. `AttendantController` + graph-based movement + door reactivity.
5. `HidingSpot` + hidden-flag lookup; `RespawnController` + `GameEvents.PlayerCaptured`; `CameraShakeEffect`.
6. Ambient/state-cue audio hooks.
7. VR: `IDetectable` implementation for the Quest rig.
8. EditMode tests for perception + state machine.
9. `Milestone7_HorrorTestScene` (headless-built), reusing procedural/puzzle setup, one Attendant, a couple of hiding spots.
10. Headless verification (compile + EditMode tests, zero regressions to Milestones 1-6). Commit, push, open a PR against `main`.
