# Milestone 8 Secret-Room Prop Generator

Generates the four secret-room props (`Bookcase_Disguise`, `Desk_Office`, `FilingCabinet`, `Binder_PersonnelLogs`) as ready-to-import FBX files, at the exact real-world dimensions specified in `docs/ASSET_REQUESTS.md`. Tested end-to-end on Blender 4.0.2 — geometry, materials, UVs, origin placement, and FBX export all verified.

## Requirements

- [Blender](https://www.blender.org/download/) (free). On Ubuntu/Debian: `sudo apt install blender`.

## Usage

```bash
./generate_assets.sh
```

That's it — one command. It runs Blender headlessly, builds all four props, and writes the FBX files to `output/`.

If you'd rather run it manually:

```bash
blender --background --python generate_props.py
```

## What you get

| File | Dimensions |
|---|---|
| `output/Bookcase_Disguise.fbx` | 2.0m × 2.2m × 0.4m (W×H×D) |
| `output/Desk_Office.fbx` | 1.4m × 0.75m × 0.7m |
| `output/FilingCabinet.fbx` | 0.45m × 1.3m × 0.6m |
| `output/Binder_PersonnelLogs.fbx` | 0.3m × 0.25m × 0.05m |

Each has its origin at its own bottom-center (floor contact point), simple flat-color materials already assigned, and a basic UV unwrap on every piece — ready to drag into `Assets/` in Unity.

## Troubleshooting

**`ModuleNotFoundError: No module named 'numpy'`** — some Linux package-manager Blender builds (e.g. Ubuntu's apt package) link against the system Python but don't bundle numpy, which Blender's own FBX exporter needs. Fix: `sudo apt install python3-numpy` (or the equivalent for your distro's Blender build).
