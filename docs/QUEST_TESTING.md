# Testing on a Meta Quest 2 / Quest 3

Reference for building, deploying, and debugging `Milestone6_QuestTestScene` (or any scene) on a physical headset from this machine. Nothing here can be verified headlessly — head tracking, controller input, teleport/turn comfort, and on-device performance all need a human wearing the headset.

## One-time setup (per machine)

Already done on this machine as of Milestone 6:
- Unity's Android Build Support module (with bundled OpenJDK/Android SDK/NDK) installed via Unity Hub.
- `com.unity.xr.management`, `com.unity.xr.openxr`, `com.unity.xr.interaction.toolkit` added to the project (plus the transitively-resolved `com.unity.xr.core-utils`).
- Android Player Settings configured via script (IL2CPP, ARM64, min API 29) and the active build target switched to Android.

**Still needs a one-time manual step in the Editor UI**, not done via script: open **Edit → Project Settings → XR Plug-in Management**, switch to the **Android** tab, check **OpenXR**, then under the OpenXR sub-tab check the **Meta Quest Support** feature group and the **Meta Quest Touch Plus Controller Profile** interaction profile. This is deliberately a manual checkbox rather than a scripted step — the underlying settings API is internal-ish and version-specific, and getting it silently wrong in a headless script (with no way to visually confirm it from this environment) would be worse than a 30-second one-time click. Without this, a build will install but show a black screen or crash on launch (see Common Issues below).

If setting this up on a **different** machine:
1. Unity Hub → installs → the `6000.5.6f1` row → gear icon → **Add modules** → **Android Build Support** (this pulls in OpenJDK/SDK/NDK automatically).
2. Open the project once — the packages above are already in `Packages/manifest.json`, so Package Manager resolves them on first open.
3. Do the XR Plug-in Management manual step above.
4. `adb` needs to be on `PATH` — either install `android-tools-adb` via your package manager, or use the one bundled with Unity's Android SDK at `~/Unity/Hub/Editor/6000.5.6f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb`.

## One-time setup (per headset)

1. **Enable Developer Mode**: in the Meta Horizon mobile app (paired to a Meta account that's part of a developer organization — free to join at developer.oculus.com), go to the headset's device settings and toggle Developer Mode on. *(Already done on this project's Quest 2 and Quest 3 as of Milestone 6.)*
2. Put the headset on, connect it to this PC via a USB-C cable.
3. A permission prompt appears **inside the headset** — put it on and select **Allow** (and "always allow from this computer" to skip this in the future).

## Every session: confirm the headset is visible

```bash
adb devices
```

You should see one line per connected headset, e.g. `1WMHH8...  device`. If it says `unauthorized` instead of `device`, put the headset on — there's a pending permission prompt waiting for you to approve. If nothing shows up at all:
- Try a different USB-C cable/port (some cables are charge-only).
- `adb kill-server && adb start-server`, then `adb devices` again.
- Check `lsusb` shows the headset (look for a Meta/Oculus/Facebook vendor id) — if it's not there at all, it's a cable/port/USB-permissions issue, not an adb issue.

## Building and deploying from the Unity Editor

1. **File → Build Settings**, switch platform to **Android** if it isn't already (should already be switched from Milestone 6's setup).
2. Make sure the scene you want to test is in the build list (the Milestone asset builders already add their scenes via `EditorBuildSettings.scenes` — check the list, or add the scene manually with **Add Open Scenes**).
3. With the headset connected and showing up in `adb devices`:
   - **Build And Run** deploys straight to the headset and launches the app — the normal iteration loop.
   - Or **Build** an APK, then deploy manually: `adb install -r path/to/build.apk`.
4. Put the headset on — the app should launch automatically after Build And Run, or find it under **Unknown Sources**/**Library** in the headset (Developer Mode apps show up there, not in the regular Store library).

## Debugging a running build

- **Live logs**: `adb logcat -s Unity` streams Unity's `Debug.Log` output from the headset in real time — the same information you'd see in the Editor Console, just from the device.
- **Wireless debugging** (skip the cable after the first pairing): `adb tcpip 5555` while still on USB, then unplug and `adb connect <headset-ip>:5555` (find the IP in the headset's Wi-Fi settings). Re-run `adb devices` to confirm.
- **Performance**: the Meta Quest Developer Hub (a separate desktop app from Meta, not required but useful) shows live FPS/CPU/GPU metrics while a build runs. Unity's own Profiler can also attach to a running device build via **Window → Analysis → Profiler** → Attach to Player, over the same USB/Wi-Fi connection `adb` uses.

## What to actually check when testing

Since this is exactly the part no automated test in this project can cover:
- **Head tracking**: does the camera track your real head movement correctly (no drift, no lag)?
- **Locomotion**: does the configured movement (continuous move via the left thumbstick + smooth turn via the right thumbstick, per `docs/features/milestone-6-vr-platform-support.md`) feel controllable? Does it cause discomfort within the first few minutes?
- **Interaction**: does pointing the right controller at a door/switch/exit and pulling the trigger correctly show the prompt (a small text panel that follows your view) and trigger the interaction?
- **UI legibility**: is the interaction prompt and the "You Found The Exit" panel readable at a comfortable distance/angle as they follow your view?
- **Performance**: does the frame rate hold steady (Quest 2's target is 72Hz, Quest 3's is 90-120Hz) as you move through several generated rooms? Any visible reprojection judder is a sign the frame budget is being missed.

Not in this test scene yet: the Field Log map/marker panel has no VR-specific UI — that's deliberately deferred pending your call on how panning/zoom should work with controllers (see `DECISIONS.md`, 2026-08-07).

## Common issues

| Symptom | Likely cause |
|---|---|
| `adb devices` shows nothing | Cable/port issue, or Developer Mode not actually enabled — recheck the Horizon app |
| `adb devices` shows `unauthorized` | Put the headset on, approve the pending USB debugging prompt |
| Build fails with an IL2CPP/NDK error | Android module didn't fully install, or NDK version mismatch — reinstall via Unity Hub rather than pointing at a system-installed NDK |
| App installs but the headset shows a black screen / crashes on launch | Usually an OpenXR/XR Plug-in Management misconfiguration (Meta Quest Support feature not enabled) or a missing XR Origin in the scene |
| Everything works but framerate is poor | Expected on Quest 2 if scene complexity has grown past its performance floor — see the Art Direction "Quest note" in `PRD.md` |
