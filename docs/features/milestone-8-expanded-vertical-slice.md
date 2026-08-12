# Milestone 8: Expanded Vertical Slice

## User story

As the player, I've now got exploration, mapping, puzzles, saves, VR, and one creature — but every room looks like every other room, there's no "wow, what is this place" moment, no reason to poke into a suspicious corner, and no sense that anyone was ever really here. This milestone is about making The Continuance feel like a *place* with a history, not a procedural test scene.

## Context

Per the PRD, this milestone is deliberately broad: more procedural variety, a landmark room, a secret room, environmental storytelling, a lighting/materials/audio/performance pass, and structured playtesting. Three of those six items are things I can build; the other three need to be scoped honestly against what's actually possible right now:

- **A real materials/art pass is not achievable in this milestone.** The project has zero art assets — no textures, no models, no real lighting setup — only default-primitive grey-box geometry (now debug-colored for testing, per Milestone 7). A genuine art pass needs either commissioned/purchased assets or a deliberate decision to source free ones, and that's a call for you to make, not something to default into. What *is* achievable now: basic dynamic lighting (flickering fluorescents, matching the PRD's audio direction) and the ambient/state-cue audio groundwork already laid in Milestone 7.
- **Performance** isn't yet a real bottleneck — the scene is still simple grey-box geometry. I'll do a basic sanity check (object counts, avoiding obvious per-frame allocation issues) rather than a profiling pass that has nothing meaningful to measure yet.
- **Structured playtesting is fundamentally something you do, not something I can do for you.** What I can build is a lightweight, consistent feedback checklist so your playtesting sessions produce comparable, useful notes instead of open-ended impressions — similar in spirit to `docs/QUEST_TESTING.md`.

## Proposed creative content (for your review before I build it)

Grounded in the identity already established in `docs/MILESTONE_PLAN.md` — Aldermere Business Park, the service stairwell, "who's still doing the maintenance," Strata/Sectors/Threshold Rooms, The Attendant.

**Landmark room — "The Atrium."** A tall, open multi-story lobby space with a broken escalator and a mezzanine walkway overhead — visually distinct from every corridor/junction/standard room by sheer scale (2-3x normal room height, open floor plan instead of four walls). It reads as the building's original ground-floor lobby, now stranded many Strata deep, which is exactly the kind of "architecture that kept building itself" wrongness the premise is built on. Placed as a guaranteed, special-cased node in the room graph (not from the regular `RoomDefinition` pool) — visually and spatially, players should recognize it as *the* landmark, not just another generated room.

**Secret room — "Maintenance Sub-Office."** A small hidden room behind a bookcase/panel (a `Door`-like interactable disguised as ordinary wall dressing, not visually flagged as a door) containing the strongest environmental storytelling piece: personnel logs suggesting whoever's "still doing the maintenance" isn't entirely human anymore. Reachable only by noticing the disguised panel — rewards exploration per PRD Section 14, not required for the exit.

**Environmental storytelling — a `FieldNote` pickup.** A new `IInteractable` (not `ISaveable`-collectible like `PickupTestItem` — reading it doesn't remove it, you can revisit) that shows a short text fragment in a UI panel on interact. Scattered through the level: work orders, inspection tags, PA announcement transcripts, personnel logs — building the "who's maintaining this" mystery without cutscenes or voice-over, per PRD Section 14's "reveal information without depending entirely on text exposition" (short fragments, found optionally, not a wall of lore text blocking progress).

**Procedural variety.** Two new `RoomDefinition` categories: a **Storage** room (dense with obstacles — more interesting for hide-and-chase than an empty box) and an **Office Cluster** (multiple small connected rooms in one grid cell's footprint, breaking the current "every room is one open box" monotony).

If any of this doesn't fit your vision for the place, tell me now — this is the one part of this milestone that's genuinely about creative direction, not mechanics, and it's cheap to change before I build it, expensive after.

## Technical design

### New: landmark + secret room
- `ProceduralLevelBuilder` gains an optional "guaranteed landmark node": pick one non-entry/non-exit node in the generated graph (e.g., the node with the most connections, or a fixed minimum-hop distance from entry) and instantiate the hand-built Atrium prefab there instead of a pooled `RoomDefinition` — additive to the existing per-node instantiation, not a rework of the graph algorithm itself.
- The secret room is a small prefab attached to one room via a disguised `Door` variant (`SecretDoor` : same `IInteractable` pattern as `Door`, but no default visual "this is a door" cue) — placed procedurally against a probability/seed roll, same determinism principle as everything else (`System.Random`, never `UnityEngine.Random`).

### New: `EndlessRooms.World.FieldNote`
- `IInteractable`, not `ISaveable` (nothing to persist — reading it is idempotent). `Interact` raises a UI event with a text fragment string; a new `FieldNoteUI` (in `EndlessRooms.UI`) displays it, dismissible, reusing the existing prompt-canvas pattern from `InteractionPromptUI`.
- Fragment content lives as data (a list of strings per note, or a small ScriptableObject), not hardcoded per-instance, so future notes are content changes, not code changes.

### New: `RoomDefinition` categories
- `Storage`, `OfficeCluster` — same `RoomDefinition` ScriptableObject pattern as `Standard`/`Corridor`/`Junction`/`DeadEnd`, new prefabs with more interior geometry (obstacles/dividers). `RoomGraphGenerator`/`RoomGraphValidator` need no changes — they already treat categories as data, not special-cased logic.

### Lighting/audio (within grey-box constraints)
- A `FlickeringLight` component (basic `Light` + a simple on/off/intensity-jitter routine) on rooms and the Atrium, matching PRD Section 15's "fluorescent light hum" direction — paired with `RoomAmbience` (already built in Milestone 7) for the hum itself.
- No real materials/textures — deferred, as above, pending your call on asset sourcing.

### `docs/PLAYTEST_CHECKLIST.md`
- A structured checklist (not a report I generate for you) covering: first-five-minutes impressions, whether the Atrium/secret room/notes were noticed without being told where to look, whether The Attendant's difficulty felt fair across a full playthrough, and open-ended notes — so multiple sessions produce comparable feedback instead of one-off impressions.

## Out of scope (deliberately)
- Real art assets/materials — needs your decision on sourcing, not a default.
- A performance profiling pass — nothing meaningful to profile yet at grey-box scale.
- Doing the playtesting itself — that's you; I'm building the checklist that structures it.

## Plan

1. Feature branch `feature/milestone-8-vertical-slice`, stacked on `main` (post Milestone 7 merge).
2. Confirm the creative content proposals above with you.
3. `RoomDefinition` Storage/OfficeCluster categories + prefabs.
4. Landmark Atrium room + guaranteed-placement logic in `ProceduralLevelBuilder`.
5. Secret room + `SecretDoor` + placement logic.
6. `FieldNote` + `FieldNoteUI`, a handful of story-fragment notes placed through the level.
7. `FlickeringLight`, paired with existing `RoomAmbience`.
8. `docs/PLAYTEST_CHECKLIST.md`.
9. New `Milestone8_VerticalSliceTestScene` headlessly built, reusing existing systems.
10. Headless verification (compile + EditMode tests, zero regressions). Commit, push, open a PR against `main`, clearly marking what needs your Play-mode/playtest check.
