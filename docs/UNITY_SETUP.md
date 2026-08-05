# Unity Setup — Milestone 1

> **Update:** Unity 6000.5.6f1 was subsequently installed in the dev sandbox and the project was bootstrapped headlessly (packages, URP, both test scenes) — see `Assets/Editor/Setup/`. The steps below are kept for reference / for setting the project up on another machine, but the project no longer needs them run from scratch.

The scripts, assembly definitions, and Input Actions asset for Milestone 1 are already committed under `Assets/TheEndlessRooms/` (EditMode tests live at `Assets/TheEndlessRooms/Tests/EditMode/` — Unity only compiles assemblies located under `Assets/` or `Packages/`, so this cannot live at the repo root).

## 1. Create the Unity project in this repo folder

1. Install **Unity Hub**, then install **Unity 6 LTS** (the latest `6000.x` LTS release available in Hub) with default modules for your dev OS.
2. In Unity Hub: **New Project** → template **"3D (URP)"** (or "3D Core" + add URP manually if your Hub version doesn't offer the URP template directly).
3. Set the project **location to this repo's root folder** (`.../MyBackrooms`, the folder containing `PRD.md`, `.git`, and the existing `Assets/TheEndlessRooms/` folder). Unity will add `Assets/Settings/`, `Assets/Scenes/`, `Packages/`, `ProjectSettings/`, etc. alongside the files already committed — it will not overwrite `Assets/TheEndlessRooms/`.
4. Let Unity finish the initial import. You should see `Assets/TheEndlessRooms/Scripts/{Core,Player,World,Procedural,UI}` and `Assets/TheEndlessRooms/Settings/TheEndlessRooms.inputactions` in the Project window.

## 2. Install required packages

Open **Window → Package Manager**:

1. Install **Input System** (`com.unity.inputsystem`). Unity will prompt to restart and change the active input backend — choose **"Both"** (keeps legacy UI input working while the new Input System drives gameplay; simplest for Milestone 1's plain Text prompt).
2. Confirm **Test Framework** (`com.unity.test-framework`) is present — it ships enabled by default on new projects; if not, add it from the package list.

## 3. Verify compilation

Scripts should compile with **no console errors**. If you see `Unity.InputSystem` reference errors in `EndlessRooms.Player.asmdef`, re-check step 2 (the Input System package must be installed before that asmdef resolves).

Run **Window → General → Test Runner → EditMode → Run All**: the two `RoomDefinitionTests` should pass.

## 4. Build the Milestone 1 test scene

Create a new scene `Assets/TheEndlessRooms/Scenes/Milestone1_TestScene.unity` (File → New Scene → Basic (Built-in), then File → Save As into that path).

### Bootstrap
- Create an empty GameObject named `GameBootstrap`, add the **GameBootstrap** component (`EndlessRooms.Core`).

### Player
1. Create an empty GameObject `Player`, add **Character Controller** (Height 1.8, Radius 0.35, Center (0, 0.9, 0)) and **PlayerController** (`EndlessRooms.Player`).
2. As a child of `Player`, create an empty `CameraPivot` at local position (0, 1.6, 0); as a child of `CameraPivot`, add a **Camera** (this becomes the view camera). Remove/disable any other camera in the scene so there is exactly one active `Camera`.
3. On `Player`, add **InteractionCaster** (`EndlessRooms.Player`); set its **View Camera** field to the camera created above.
4. Create a `PlayerMovementConfig` asset: right-click in `Assets/TheEndlessRooms/ScriptableObjects/` → **Create → The Endless Rooms → Player Movement Config**. Defaults are reasonable to start. Assign it to `PlayerController`'s **Config** field.
5. Assign Input Action References on `PlayerController` and `InteractionCaster` by dragging `Assets/TheEndlessRooms/Settings/TheEndlessRooms.inputactions` into each field and picking the matching action (`Gameplay/Move`, `Gameplay/Look`, `Gameplay/Sprint`, `Gameplay/Crouch` on `PlayerController`; `Gameplay/Interact` on `InteractionCaster`). Set `PlayerController`'s **Camera Pivot** field to `CameraPivot`.

### Grey-box room
1. Build a simple room from Cubes/Planes: floor, four walls, ceiling — any size, e.g. a 6×3×6 m box.
2. **Door**: add a thin Cube ("DoorPanel") in a wall gap; parent it under an empty `DoorHinge` positioned at the panel's hinge edge (not its center) so rotation swings correctly. Add the **Door** component (`EndlessRooms.World`) to the `DoorHinge` GameObject (or to a wrapper GameObject that contains `DoorHinge`), set its **Hinge** field to `DoorHinge`. The panel needs a non-trigger `BoxCollider` so it both blocks movement and is raycast-hittable.
3. **Pickup**: add a small Cube/Sphere ("TestPickup") with a **trigger** `BoxCollider`/`SphereCollider` (so it doesn't block the player) and the **PickupTestItem** component (`EndlessRooms.World`).

### Interaction prompt UI
1. Create a **Canvas** (Screen Space – Overlay is fine; no EventSystem is needed since nothing is clickable in Milestone 1).
2. As a child of the Canvas, create a `PromptRoot` panel GameObject containing a `Text` (or TextMeshPro `Text`) child showing the prompt string; position it near the bottom-center of the screen.
3. Add **InteractionPromptUI** (`EndlessRooms.UI`) to the Canvas (or another convenient GameObject), and assign: **Interaction Caster** → the `Player`'s `InteractionCaster`; **Prompt Text** → the Text component; **Prompt Root** → the `PromptRoot` GameObject.

## 5. Test

Press Play:
- **WASD** moves, mouse looks around, **Left Shift** sprints (drains/regens a stamina value you can watch via `PlayerController.CurrentStamina` in the Inspector while in Play mode), **Left Ctrl** crouches (capsule height changes).
- Aiming at the door or pickup within ~2.5 m shows the prompt ("Open Door" / "Pick up Test Item"); pressing **E** triggers it — the door swings open/closed, the pickup logs to the Console and disappears.
- No errors/warnings in the Console.

## Common failure cases

| Symptom | Likely cause |
|---|---|
| `PlayerController` disables itself, logs missing config error | `PlayerMovementConfig` not assigned in the Inspector |
| Player doesn't move | Input Action References not assigned, or the `.inputactions` asset's actions weren't individually picked (dragging the asset alone isn't enough — you must also select the specific action) |
| Interaction prompt never appears | `View Camera` not set on `InteractionCaster`, or nothing is within `Interaction Range` along the camera's forward ray, or the target's collider is missing |
| Door doesn't visually rotate | **Hinge** field left empty/pointed at the wrong transform, or the hinge pivot is at the panel's center instead of its edge |
| Compile errors mentioning `Unity.InputSystem` | Input System package not installed, or Active Input Handling still set to "Input Manager (Old)" only |
