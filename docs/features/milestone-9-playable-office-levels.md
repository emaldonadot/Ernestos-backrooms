# Milestone 9: Playable Office Levels

## User story

As the player, I want to actually *play* the game start to finish — walk in, investigate rooms for clues, find keys, unlock doors in the right order, avoid (or escape) a real threat, survive the occasional harmless scare, and reach the exit to move on to the next level. Right now the project is a set of disconnected milestone test scenes with no win/lose loop and no level-to-level progression — this milestone turns that into one continuous playable experience, starting with the hand-authored office map you provided.

## Context

Three levels are planned, each a different generation *shape*, rolled out one at a time so each is fully playable and tested before the next starts:

- **Level 1 — Cross spine.** The exact hand-authored map you provided (14 offices, 2 bathrooms, 2 garden courtyards, a plus-shaped corridor). Fixed, not procedural — every room, door, and piece of furniture is placed by hand.
- **Level 2 — Straight spine.** Procedural, using a new generation mode that produces a single corridor with rooms on both sides.
- **Level 3 — L-shaped spine.** Procedural, a corridor that bends once partway through.

The existing organic room-graph generator (the one all of Milestones 2–8 use) isn't being replaced — it stays available for future content. This adds a **second, new generation mode** alongside it, for levels that need the "hallway building" shape instead of an organic branching layout.

## New: spine-based procedural generation (Levels 2 and 3)

The current `RoomGraphGenerator` grows an organic graph: from any room, it can extend in any of 4 directions, and branches can branch again. There's no concept of a through-line corridor with rooms strictly as leaves off it.

- New `SpineGraphGenerator` (alongside the existing `RoomGraphGenerator`, not replacing it): builds one or two straight corridor *spines* first (a `Corridor`-category node chain), then attaches exactly one room to the left and/or right of each spine cell. Rooms never connect to each other directly — only back to their spine cell — matching the reference map's shape.
- Three spine shapes as a `SpineShape` enum: `Straight` (one line, entry at one end, exit at the other), `LShaped` (one line that turns 90° once at a random or configured point), `Cross` (two lines crossing at one point — used to *validate* the generator against Level 1's known shape, even though Level 1 itself is hand-authored, not generated).
- **Mandatory paired bathrooms**: after the spine + rooms skeleton is placed, a new placement rule specifically selects two leaf slots that are either adjacent on the same side of the corridor or mirrored across it, and assigns them `Bathroom_Men` / `Bathroom_Women` categories (new `RoomCategory` entries) instead of the normal random filler pool. Every spine-generated level gets exactly 2, always paired — not configurable per the confirmed requirement.
- Still fully deterministic — `System.Random` seeded, never `UnityEngine.Random`, following the exact same principle as the existing generator (see the seed-generation explanation from earlier in this conversation).
- EditMode-tested the same way `RoomGraphGeneratorTests` covers the existing generator: shape correctness (rooms never connect to each other directly, exactly one entry/exit, bathroom pairing holds) across many seeds.

## New: the playable game loop

- A `GameFlowController` (or similar — exact name TBD during implementation) owns level sequencing: which level is active, transition to the next level on exit-reached, restart/game-over on capture.
- Minimal start/level-transition/game-over UI — functional, not styled (matches this project's "real materials/UI polish is a separate pass" precedent from Milestone 8).
- Level 1 loads first; reaching its exit loads Level 2; reaching Level 2's exit loads Level 3. (Levels beyond 3 are out of scope until you decide there should be more.)

## New: investigate → clue → key → locked door puzzle framework

Every level always has this puzzle structure, replacing/extending the existing switch-sequence puzzle (`SwitchSequencePuzzle` stays as-is for anywhere it's already used — this is a new, separate puzzle type):

- **Clues**: an extension of the existing `FieldNote` pattern (already built in Milestone 8) — an `IInteractable` placed in a room that reveals a text fragment. Clues point toward where a key/tool is, or hint at what a locked door needs.
- **Carryable items**: a new `InventoryItem` concept — keys, a chain cutter, a pry bar — each with a unique ID.
- **Locks**: doors gain a required-item-ID field (confirmed: **named item → named door**, e.g. "Brass Key" only opens the Storage Room door specifically — precise, level-authored, not a generic category match). A locked door's interaction either consumes/checks the required item from inventory and unlocks, or shows a "needs X" prompt if the player doesn't have it yet.
- This is what creates step ordering: door B needs the item found via a clue in room A, so the player must visit A (or wherever the item actually sits) before B opens — authored per-level, not algorithmically generated.

## New: inventory (backpack)

- Up to **10 items** carried at once (confirmed cap).
- A new `Inventory` component on the player (PC and VR both, mirroring how `PlayerController`/`VRNoiseSource` both implement `IDetectable` today) — add/remove/query items, a simple slot-based or list HUD (functional first, visual pass later, consistent with the project's grey-box-first approach).
- Picking up a key/tool is an `IInteractable` that adds to inventory instead of unlocking anything directly — the *door* does the unlock check, not the pickup.

## New: harmless jump-scare entity

Confirmed as a **separate, new, lightweight entity** — not a mode on the Attendant. No AI state machine, no perception system, no danger:

- A simple `JumpScareTrigger` component in specific rooms: on player entering (or getting close), plays a scare beat (a quick visual — a ghost/monster appearing briefly — plus a sting sound) and then deactivates itself permanently. One-shot per room, never becomes a threat.
- Visual representation is a placeholder (existing `DebugColor` convention) until you provide art — noted below.

## The Attendant (real threat) — reused, not rebuilt

Confirmed: the corridor-patrolling, chase-on-sight, hide-to-escape creature is the **existing Attendant** from Milestone 7, unchanged mechanically (`AttendantController`/`AttendantPerception`/`AttendantStateMachine`, hiding via `HidingSpot`/`IHideable` — all already built and tested). No new AI work needed for this to work in the office levels.

**Noted for later, not part of this milestone's implementation**: you mentioned you'll eventually provide a ghost image (and potentially a 3D model later) to replace the current placeholder capsule visual, and that this should be swappable for other monster/ghost visuals down the line. When that happens, this just needs the Attendant's visual representation to become a swappable prefab/material reference rather than the hardcoded `DebugColor` capsule — a small, contained change whenever you're ready to hand off the asset(s), not something to build speculatively now.

## Level 1 specifics (the cross-spine office map)

Built by hand from your reference image: 14 offices (R01–R14), 2 bathrooms (adjacent, per the reference), 2 open-air garden courtyards, one main corridor (start ↔ exit) crossed by one cross corridor, furniture per the furniture guide table.

**Still needed from you**: real-world scale. The reference image has no dimension/scale marker. Proposed defaults, for you to confirm or correct before I build anything: offices ~4m × 5m, corridors ~2.5m wide, courtyards slightly larger given they're open-to-sky. All furniture placeholder collision will maintain the confirmed 1.6m minimum clear width (two players' worth of space) between furniture, and between furniture and walls, verified by actually walking every room before calling it done — same process as the Atrium fixes.

## Out of scope for this milestone

- Levels 2 and 3 are *planned* here but not built until Level 1 is complete and confirmed playable — per your own sequencing.
- Real art (the ghost/monster visuals, final furniture models) — placeholder/grey-box, swapped in once you provide assets.
- Inventory/HUD visual polish beyond functional.
- Anything beyond 3 levels.

## Plan

1. Confirm this doc, plus Level 1's real-world scale.
2. `Bathroom_Men`/`Bathroom_Women` `RoomCategory` entries (small, shared by both the fixed Level 1 map and the future spine generator).
3. Inventory system + `InventoryItem` + named-item door locks.
4. Investigate → clue → key → locked door framework, built and content-authored for Level 1 specifically.
5. Jump-scare entity (`JumpScareTrigger`), placed in a few Level 1 rooms.
6. Hand-build Level 1's fixed scene (rooms, furniture collision, doors, clues, keys, locks, Attendant, jump scares) per the reference map.
7. `GameFlowController` + minimal start/win/lose/transition UI, wired to Level 1 (Levels 2/3 plugged in later, once their generators exist).
8. Headless verification (EditMode tests for anything pure-logic — inventory, lock-matching, clue/key wiring where feasible; compile + existing suite zero-regression check) + your hands-on playtest.
9. Only after Level 1 ships: `SpineGraphGenerator` (Straight/L-shaped/Cross shapes) + bathroom-pairing rule + tests, then Level 2, then Level 3.
