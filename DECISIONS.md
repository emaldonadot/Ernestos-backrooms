# Architecture Decision Log — The Endless Rooms

Append-only log of decisions with dates and rationale. Revisit an entry by adding a new dated entry that supersedes it rather than editing history.

## 2026-08-07 — MonoBehaviour Awake/OnEnable/Start/Update never run outside Play mode

Discovered while verifying Milestone 5: `GameBootstrap.Awake()`, `MapBootstrap.Awake()`, `PuzzleGateController.Awake()`, and `Door`/`PickupTestItem`'s `OnEnable()` all silently never ran — not just in a bare `-executeMethod` script, but even inside real EditMode tests (`-runTests -testPlatform EditMode`) using `AddComponent`. Root cause: without `[ExecuteAlways]`/`[ExecuteInEditMode]`, Unity only calls `Awake`/`OnEnable`/`Start`/`Update` in an actual Play session — never from Edit mode, regardless of whether the object came from scene deserialization, `Instantiate`, or `AddComponent`, and regardless of whether the calling context is a bare script, EditMode tests, or plain scene loading. Plain C# objects (not `MonoBehaviour`) are unaffected — their constructors always run normally, which is why `FieldLogService` (a plain class) worked fine in the same headless scripts while `MonoBehaviour`-based bootstrap components didn't.

**How to apply:** any `MonoBehaviour` whose `Awake`/`OnEnable` does one-time setup that headless Editor tooling needs to trigger should expose that logic as a public method too (e.g. `GameBootstrap.EnsureRegistered()`, `MapBootstrap.EnsureInitialized()`, `PuzzleGateController.EnsureInitialized()`) — call it from `Awake` for real gameplay, and call it directly from Editor scripts/tests that never enter Play mode. Don't write EditMode tests asserting on `Awake`/`OnEnable` side effects — they will never pass; test the plain-C# logic those methods call into instead (see `SaveableRegistryTests`, which tests `SaveableRegistry` directly rather than asserting `Door.OnEnable` registers it). Genuine Play-mode-only wiring (e.g. `Door.OnEnable` calling `SaveableRegistry.Register`) has to be confirmed by a human in a real Play session — headless tooling cannot verify it.

## 2026-08-05 — Test assemblies must live under Assets/

`Assets/TheEndlessRooms/Tests/EditMode/` (moved there from a repo-root `Tests/EditMode/`). Rationale: Unity's AssetDatabase only scans `Assets/` and `Packages/` — an asmdef outside those is invisible to the Editor with no error, just zero tests silently discovered. Caught while verifying Milestone 2's EditMode tests (`testcasecount="0"` with no compile error was the symptom). Any future test assembly must be created under `Assets/`.

## 2026-08-04 — Repository setup

- **Repo name:** `Ernestos-backrooms`, **visibility:** private. Rationale: unreleased IP (story, creature designs, puzzles) should stay confidential during early development. User chose private explicitly.
- **Version control:** Git + Git LFS for binary asset types (textures, models, audio — see `.gitattributes`). Unity projects accumulate large binary assets quickly; LFS keeps repo size and clone times sane from day one instead of migrating later.
- **CI/CD:** Deferred. Unity CI via `game-ci` actions requires storing personal Unity license credentials (`UNITY_EMAIL`/`UNITY_PASSWORD`/`UNITY_LICENSE`) as GitHub secrets. Decided to wait until there is real code worth gating rather than front-load that credential/security overhead. Branch/PR discipline (feature branches + manual review) still applies from Milestone 1 onward. Revisit once Procedural (Milestone 2) has EditMode tests worth automating.

## 2026-08-04 — Engine and rendering

- **Engine:** Unity 6 LTS. Rationale: current stable LTS at project start, per PRD Section 3 ("current stable LTS release"); avoids starting on a soon-to-be-superseded LTS.
- **Render pipeline:** URP (Universal Render Pipeline). Rationale: strong post-processing/volumetric support for horror atmosphere (Section 16) at a performance cost acceptable for procedural streaming, without HDRP's heavier authoring/hardware requirements.
- **Input:** new Input System package (`com.unity.inputsystem`), not the legacy Input Manager. Rationale: PRD Section 17 requires remappable controls, hold/toggle options, and eventual gamepad support — the new Input System is Unity's supported path for all three and avoids a later migration.

## 2026-08-04 — Code architecture

- **No third-party DI framework.** Use a small hand-rolled `GameServices` static service locator plus plain C# events (`GameEvents`) instead of Zenject/VContainer/etc. Rationale: PRD Section 19 requires justifying any third-party package; the project's cross-system wiring needs (Core → Player/World/Map/Puzzles) are simple enough that a hand-rolled locator avoids an external dependency and its learning curve, while still keeping call sites interface-based and swappable.
- **Assembly definitions per module** (`EndlessRooms.Core`, `.Player`, `.Procedural`, `.World`, `.Map`, `.Puzzles`, `.AI`, `.Persistence`, `.UI`, `.Multiplayer.Abstractions`, plus `.Tests.EditMode`). Rationale: enforces PRD's "keep procedural-generation logic testable outside visual presentation" and "keep gameplay logic independent of the UI" at compile time, not just by convention; also keeps incremental compiles fast as the project grows.
- **Determinism:** procedural generation and any other seed-driven logic uses `System.Random` (or an explicit seeded PRNG wrapper), never `UnityEngine.Random`. Rationale: `UnityEngine.Random`'s global state is not safely reproducible across frames/platforms/future networked clients; a self-contained seeded RNG is required for the PRD's "same seed reproduces same layout" acceptance criterion and for co-op determinism later.
- **World-mutating interactions route through `IWorldCommand` + a single `WorldCommandExecutor`.** Rationale: PRD Section 20 requires explicit command/service routing instead of direct references, specifically so a future host/server-authority check can be inserted at one seam instead of every call site.
- **UI toolkit choice:** uGUI (not UI Toolkit) for the MVP HUD/prompts/map screen. Rationale: PRD Section 21 prioritizes functional validation over polish for the MVP; uGUI has lower setup overhead for simple prompts and menus at this stage. Revisit if the Field Log map screen's zoom/pan/marker needs outgrow uGUI's layout system.

## 2026-08-04 — World/creature naming (originality requirement)

- World name **The Continuance**; layers **Strata**; local room clusters **Sectors**; exit rooms **Threshold Rooms**; unexplained anomalies **Drift**; the player-built map is the **Field Log**, personal annotations **Field Marks**. First prototype creature: **The Attendant**. Later rare creature: **The Boarder**. Rationale: PRD explicitly requires original terminology, names, and lore with no direct copies from existing Backrooms media — this vocabulary is used consistently in code (namespaces, enum values, asset names) to avoid incidental reuse of existing IP terms.

## 2026-08-04 — Milestone discipline

- Only Milestone 1 (Project Foundation) is implemented now. Per PRD Section "Instructions for the Development Agent," each subsequent milestone stops for explicit user confirmation before starting. Do not pre-build Milestone 2+ systems (procedural generation, map, puzzles, AI) beyond the interface stubs already required by Milestone 1's data definitions.
