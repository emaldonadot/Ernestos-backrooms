# Milestone 6: VR Platform Support (Quest 2/3 + PC)

## User story

As the developer, I own a Quest 2 and a Quest 3 and want to play this on them, not just at a keyboard — but I don't want to give up the PC build to get there, and I don't want to redo the procedural generation, puzzle, map, or save systems to make it happen.

## Context

This milestone was requested mid-development, after Milestones 1-5 (Foundation through Persistence) were already built and verified for PC. The question that drove the scope here: how much of what's already built has to change? The answer, confirmed while designing this: very little. `ProceduralLevelBuilder`, `Door`/`PuzzleSwitch`/`ExitPoint`, `SwitchSequencePuzzle`, `FieldLogService`, and the entire `EndlessRooms.Persistence` layer are pure C# or physics-based world logic with zero dependency on mouse/keyboard or screen-space rendering — none of it needs to change. The cost is concentrated in exactly three places: the player rig (camera/movement), the interaction system's ray source, and UI presentation (screen-space vs. world-space). This milestone does all three additively — PC keeps working exactly as it does today; Quest is a second rig layered on top of the same world.

**Quest 2 is the performance floor.** Anything that runs acceptably on Quest 2 (Snapdragon XR2 Gen 1) runs on Quest 3 (XR2 Gen 2) with headroom, but not the reverse. Every Quest-facing decision in this milestone is made against Quest 2's ceiling.

## Scope

In scope:
- Android Build Support + OpenXR + XR Plug-in Management + XR Interaction Toolkit (XRI) installed and configured.
- A Quest player rig: head-tracked camera (no manual mouse-look), controller-driven locomotion via XRI's built-in providers (not hand-rolled — comfort-sensitive movement code, like teleport arcs and turn easing, is exactly what XRI already solves correctly).
- `InteractionCaster` gains an optional ray-origin override (a `Transform`) so a VR controller can drive the same interaction system PC already uses — `Door`, `PuzzleSwitch`, `ExitPoint`, and `PickupTestItem` need zero changes.
- World-space variants of the interaction prompt, Field Log map/marker panel, and level-complete screen. The underlying UI component scripts (`InteractionPromptUI`, `FieldLogView`, `FieldMarkerPanel`, `LevelCompleteUI`) are unchanged — only the canvas render mode and layout differ per platform.
- One new test scene reusing the *exact same* `ProceduralLevelBuilder`/`PuzzleGateController` setup as the PC scenes, with the Quest rig instead of the PC rig.
- `docs/QUEST_TESTING.md`: a standing reference for building, deploying, and debugging on a physical Quest headset from this machine.

Out of scope (deliberately deferred, not overlooked):
- Hand tracking / passthrough (Quest Touch Plus controllers only for the MVP — PRD doesn't require hand tracking, and it adds a second input path to test).
- Locomotion comfort options as a settings menu (teleport vs. smooth, snap vs. smooth turn) — the *default* choice is made below, but a player-facing comfort settings screen is Milestone 8 (Vertical Slice) UI-polish territory, not this milestone.
- Any visual/lighting rework for Quest 2's performance ceiling — the current scenes are grey-box; this becomes relevant once Milestone 8 adds real art.
- Removing or replacing anything PC-specific. This is additive.

## Locomotion/comfort decision

Flagged for the user's call rather than decided unilaterally, since it materially affects player comfort. Two choices: movement (continuous thumbstick vs. teleport) and turning (smooth analog vs. snap/discrete-step). Recommended teleport + snap turn as the comfort-safest default; **user chose continuous move + smooth turn** instead, prioritizing normal-feeling movement over maximum comfort-safety. Implemented via XRI's `ActionBasedContinuousMoveProvider` + `ActionBasedContinuousTurnProvider` (config choice, not extra code — XRI ships teleport/snap-turn equivalents too). A comfort-options toggle (letting a player switch to teleport/snap if continuous movement doesn't agree with them) remains a good Milestone 8 settings-screen addition, not required now.

## Technical design

### Environment (installed once, not project code)
- Unity Android Build Support module (+ bundled OpenJDK/SDK/NDK) for the 6000.5.6f1 Editor.
- Project packages: `com.unity.xr.management`, `com.unity.xr.openxr`, `com.unity.xr.interaction.toolkit`. All three are official Unity packages (not third-party vendor code) — justified as the standard, Unity-maintained path for OpenXR/Quest support, avoiding a hand-rolled `TrackedPoseDriver` + custom locomotion/comfort implementation that XRI already solves correctly.
- Player Settings (Android): IL2CPP scripting backend (Mono isn't supported on Quest), ARM64-only target architecture (Quest requires 64-bit), minimum API level ~29.
- XR Plug-in Management: OpenXR provider enabled for Android, Meta Quest Support feature group enabled.

### `EndlessRooms.Player` — additive change only
- `InteractionCaster` gains `[SerializeField] private Transform _rayOriginOverride;`, preferred over `_viewCamera.transform` when set. Existing PC scenes are untouched (the field defaults to unset, falling back to the camera exactly as before) — this is the only change to already-shipped Milestone 1-5 code.

### New: Quest rig + scene (all new files, nothing replaced)
- Rather than hand-wiring an XR Origin from scratch, Milestone 6 imports XR Interaction Toolkit's own **"Starter Assets" sample** (`Milestone6Bootstrap.ImportXriStarterAssets`) and reuses its vetted `XR Origin (XR Rig)` prefab. That prefab ships on XRI 3.5.1's current (non-Legacy) locomotion architecture — a `LocomotionMediator` + `XRBodyTransformer` on a "Locomotion" child, with `ContinuousTurnProvider`/`SnapTurnProvider` on "Turn" and a `DynamicMoveProvider` on "Move" — not the `ActionBasedContinuousMoveProvider`/`ActionBasedContinuousTurnProvider` originally named above, which turned out to be `[Obsolete]` in this XRI version. `Milestone6AssetBuilder.BuildVrRig` disables everything out of scope on the instantiated rig (`SnapTurnProvider`, `TeleportationProvider`, `ClimbProvider`, both `GrabMoveProvider`s, `TwoHandedGrabMoveProvider`, `JumpProvider`, and the Poke/Near-Far/Teleport interactor GameObjects on both controllers) and leaves `ContinuousTurnProvider` + `DynamicMoveProvider` + `GravityProvider` enabled — continuous move, smooth turn, gravity-respecting `CharacterController`, nothing else.
- Reusing a full-featured sample rig instead of hand-authoring one from primitives was a judgment call made mid-implementation: this headless environment can't visually verify a hand-wired `LocomotionMediator`/`XRBodyTransformer`/input-reader graph, and Unity's own sample is exactly the kind of already-vetted asset that risk calculus favors over a blind guess.
- Interaction: no separate `XRRayInteractor` object is used for `Door`/`PuzzleSwitch`/`ExitPoint` (those are plain `IInteractable`, not XRI's own `XRBaseInteractable`). Instead, `InteractionCaster._rayOriginOverride` points at the right controller's own transform, and the existing "Interact" input action gained one new binding (`<XRController>{RightHand}/triggerPressed`, under a new "Meta Quest" control scheme in `TheEndlessRooms.inputactions`) so the exact same action PC's E key drives is now also driven by the controller trigger — zero new C# for input wiring.
- `LevelPlayerSpawner` (PC's existing script, unchanged) is reused directly for VR too — pointed at the XR Origin root and its `CharacterController` — rather than adding a redundant `VRLevelPlayerSpawner`, since the script never referenced anything PC-specific in the first place.
- `Milestone6_QuestTestScene`: the same `ProceduralLevelBuilder` + `PuzzleGateController` setup as the PC scenes, with the Quest rig and world-space UI substituted for the PC rig and screen-space UI. The Field Log map/marker panel's world-space variant is **deferred** — panning/zooming a map with VR controllers is a different interaction paradigm than mouse drag/scroll and deserves an explicit user decision (thumbstick pan+zoom? wrist-mounted mini-map? grab-and-inspect panel?) rather than a guess, consistent with how the locomotion choice was handled. `MapBootstrap`/`FieldLogService` themselves are untouched and still work; only the map's *VR-specific UI* is not yet built.

### Verification reality check

Everything above compiles and headlessly verifies the same way Milestones 1-5 did (compile checks, EditMode tests for anything that's pure C#). What headless tooling in this sandbox categorically cannot verify: actual head tracking, controller input, teleport/turn comfort, or on-device performance — there's no headset attached to this machine. `docs/QUEST_TESTING.md` exists specifically so verifying those falls to you, on your own hardware, with clear steps rather than guesswork.

## Plan

1. Feature branch `feature/milestone-6-vr-platform-support`, stacked on `feature/milestone-5-persistence` (PR #5 isn't merged yet).
2. Install Android Build Support (Unity Hub) + add the three XR packages.
3. Configure Android Player Settings + XR Plug-in Management.
4. `InteractionCaster`'s additive ray-origin override.
5. Build the Quest rig + `Milestone6_QuestTestScene`, reusing existing procedural/puzzle/map setup verbatim.
6. `docs/QUEST_TESTING.md`.
7. Headless verification (compile + EditMode tests — PC path must show zero regressions). Commit, push, open a PR against `main`, clearly marking what needs your on-device Quest check.
