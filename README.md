# The Endless Rooms

A Unity 6 (URP) psychological horror game. You're a night-shift employee of the fictional Aldermere Business Park who takes a service stairwell during a power outage and never reaches the ground floor again — the office architecture has clearly kept building itself long after anyone was maintaining it. The mystery: who, or what, is still doing the maintenance.

Built PC-first with an additive Quest 2/3 VR rig sharing the same gameplay code (see [Dual-Platform Architecture](docs/MILESTONE_PLAN.md#dual-platform-architecture-pc--quest-23)).

## Docs

| Doc | What's in it |
|---|---|
| [`PRD.md`](PRD.md) | Full product requirements — source of truth for scope. |
| [`docs/MILESTONE_PLAN.md`](docs/MILESTONE_PLAN.md) | Architecture summary, system boundaries, milestone status table. |
| [`DECISIONS.md`](DECISIONS.md) | Dated architecture decision log (append-only). |
| [`docs/UNITY_SETUP.md`](docs/UNITY_SETUP.md) | Setting up the Unity project on a new machine. |
| [`docs/PLAYTEST_CHECKLIST.md`](docs/PLAYTEST_CHECKLIST.md) | Manual playtest pass checklist. |
| [`docs/QUEST_TESTING.md`](docs/QUEST_TESTING.md) | Sideloading and testing on a Quest headset. |
| [`docs/features/`](docs/features) | Per-milestone feature writeups. |

## Requirements

- Unity **6000.5.6f1** (Unity 6 LTS) via Unity Hub
- Universal Render Pipeline (installed as part of project setup)

See [`docs/UNITY_SETUP.md`](docs/UNITY_SETUP.md) for a from-scratch setup walkthrough.

## Running tests

EditMode tests live at `Assets/TheEndlessRooms/Tests/EditMode/`. From the Editor: **Window → General → Test Runner → EditMode → Run All**. Headless:

```
Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults results.xml
```

## Releases

Tagged snapshots of `main` at notable milestones. See the [Releases page](https://github.com/emaldonadot/Ernestos-backrooms/releases) for the full list and any attached build artifacts.

| Tag | What it marks |
|---|---|
| [`m9-level1-1`](https://github.com/emaldonadot/Ernestos-backrooms/releases/tag/m9-level1-1) | **Milestone 9 — Playable Office Levels.** Level 1: a fixed, hand-authored office building with a full horror loop — The Attendant (patrol/chase/capture), hiding spots, a key/lock puzzle chain, jump scares, thunderstorms, real art/audio throughout, and a collectible item + inventory system (flashlight, UV flashlight, keys, cassette + recorder message reveal). Source-only, no build attached. |
| [`m8-vr-test-build-1`](https://github.com/emaldonadot/Ernestos-backrooms/releases/tag/m8-vr-test-build-1) | **Milestone 8 — Expanded Vertical Slice.** Sideloadable Quest APK: room variety, the Atrium landmark room, a bookcase-hidden secret room with field notes, real materials/textures, The Attendant. |

Each milestone also lands as a merged PR into `main` — see the status table in [`docs/MILESTONE_PLAN.md`](docs/MILESTONE_PLAN.md#milestone-breakdown) for what's shipped vs. in progress.
