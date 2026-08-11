# Asset Requests — Milestone 8 (Secret Room + Wall Texture)

Reference doc for assets you might source/generate for Milestone 8. **None of these are required** — the game works and tests fine fully grey-boxed. This exists so if you *do* want to add some visual texture, you know exactly what to make, at what size, and how.

## What ChatGPT can and can't actually do here

ChatGPT itself generates 2D images and text — it cannot directly output a 3D mesh file or an audio file. But for the props, there's a real path around that: **have ChatGPT write a Blender Python (`bpy`) script that builds the geometry procedurally and exports it to FBX.** Blender (free) actually executes that script and produces a genuine, correctly-scaled Unity-ready mesh — this isn't a workaround or a "good enough" substitute, it's a legitimate asset pipeline. The prompts below are written for that workflow.

- **Wall texture** — ChatGPT/DALL-E generates this directly as an image, usable close to as-is.
- **3D props** (bookcase, desk, cabinet, binder) — ChatGPT writes a `bpy` script; you run it once inside Blender, and it writes out the FBX itself (the script includes the export step). Workflow: open Blender → **Scripting** tab → **New** → paste the script → **Run Script** (▶ or Alt+P) → the FBX appears next to your `.blend` file → drag it into the Unity project.
- **Normal/roughness maps** — still not a good ChatGPT-image job; these need real surface height/material data. If you want them, run the finished albedo through a free normal-map generator tool instead.
- **Audio** (the reveal sting, the voiced personnel log) — ChatGPT can't generate sound. For the sting, a site like freesound.org (free, CC-licensed effects) is the realistic path. For the voiced log, I've given a narration *script* prompt — run that text through a separate text-to-speech tool.

## Reference dimensions (Unity: 1 unit = 1 meter — Blender's default metric unit already matches this, so build directly at real-world scale)

| Reference | Size |
|---|---|
| Room footprint | 6m × 6m |
| Wall height | 3m |
| Wall thickness | 0.2m |
| Wall segment (post door-split) | 2m wide |
| Door width / height | 2m / 3m |

## Asset table

| Asset | Type | Priority | Format | Size | How to get it |
|---|---|---|---|---|---|
| `Wall_Office_Albedo.png` | Texture (base color) | Recommended | PNG | 1024×1024, represents ~2m×2m of wall, seamless tile | ChatGPT/DALL-E image prompt, used directly |
| `Wall_Office_Normal.png` | Texture (normal map) | Optional | PNG | 1024×1024, matching albedo | Not ChatGPT — derive from albedo via a normal-map generator tool |
| `Wall_Office_Roughness.png` | Texture (roughness) | Optional | PNG (grayscale) | 1024×1024, matching albedo | Not ChatGPT — same as above |
| `Bookcase_Disguise.fbx` | 3D prop (secret door disguise) | Recommended if doing props at all | FBX | 2.0m W × 2.2m H × 0.4m D | ChatGPT writes a Blender `bpy` script → run in Blender → FBX comes out |
| `Desk_Office.fbx` | 3D prop | Optional | FBX | 1.4m × 0.7m × 0.75m | Same Blender-script workflow |
| `FilingCabinet.fbx` | 3D prop | Optional | FBX | 0.45m × 0.6m × 1.3m | Same Blender-script workflow |
| `Binder_PersonnelLogs.fbx` | 3D prop | Optional | FBX | 0.3m × 0.25m × 0.05m | Same Blender-script workflow |
| `SecretRoom_Reveal_Sting.ogg` | Audio (sting) | Optional | OGG | 1-3 seconds | Not ChatGPT — source from freesound.org or similar |
| `PersonnelLog_01_VO` | Audio (voiced log) | Optional | OGG/WAV | ~30-60 seconds | Script prompt below, then a separate TTS tool |

## Prompts

### `Wall_Office_Albedo.png` — ready to use in ChatGPT/DALL-E

```
Create a seamless, tileable texture of a worn corporate office wall surface —
plain painted drywall in a dull beige or off-white tone, with subtle
imperfections: faint scuff marks, very slight water staining, minor
scratches, and a shallow horizontal wainscoting seam line about a third of
the way up. The texture should represent roughly a 2 meter by 2 meter section
of wall. Flat, even studio lighting with no visible directional shadows or
highlights, so it reads as a neutral base material under separate game
lighting. No text, no logos, no visible perspective. Square image, high
detail, photorealistic material texture, edges must tile seamlessly
edge-to-edge with no visible seam when repeated.
```

### Normal/roughness maps — not a ChatGPT prompt

These need actual surface height/material data that an image generator can't produce meaningfully. If you want them, run the finished albedo texture through a free normal-map generator tool rather than prompting for one directly.

### `Bookcase_Disguise.fbx` — Blender script prompt

```
Write a complete, runnable Blender Python (bpy) script that procedurally
builds a low-poly, game-ready 3D model of a plain wooden office bookcase, and
exports it to FBX. I will paste this directly into Blender's Scripting tab
and run it, so it needs to work standalone with no manual steps afterward.

Requirements:
- Units: build directly at real-world meter scale (Blender's default metric
  unit already equals 1 meter — do not change unit settings).
- Overall dimensions: 2.0m wide, 2.2m tall, 0.4m deep.
- Structure: a back panel, two side panels, a top panel, a bottom panel, and
  5 evenly-spaced horizontal shelf boards between them (open-fronted, no
  doors). Add 6-10 simple rectangular "book" boxes of varying width, height,
  and color scattered across 2-3 of the shelves for visual interest.
- Keep geometry low-poly and game-ready: basic box primitives with
  extrusions/bevels only where needed. No subdivision or sculpting detail.
- Origin: set the object's origin to the bottom-center of the bookcase (the
  floor contact point, centered on width and depth) — not the geometric
  center of the whole mesh.
- Materials: simple flat-color Principled BSDF materials — a brown "wood"
  material for the frame/shelves, and 3-4 varied plain colors for the book
  boxes. No texture images needed.
- UVs: apply a basic Smart UV Project unwrap to every object so it's
  texture-ready even without a texture applied now.
- Hierarchy: parent all pieces under a single empty named "Bookcase_Disguise",
  then join everything into one final mesh object with that same name.
- At the end of the script, export the result to FBX at
  "//Bookcase_Disguise.fbx" (relative to the current .blend file) using
  bpy.ops.export_scene.fbx() with default forward/up axis settings and
  "Apply Transform" (apply unit/scale) enabled, so it imports correctly into
  Unity with no rotation or scale surprises.

Output only the complete Python script in a single code block, nothing else.
```

### `Desk_Office.fbx` — Blender script prompt

```
Write a complete, runnable Blender Python (bpy) script that procedurally
builds a low-poly, game-ready 3D model of a plain corporate office desk, and
exports it to FBX. I will paste this directly into Blender's Scripting tab
and run it, so it needs to work standalone with no manual steps afterward.

Requirements:
- Units: build directly at real-world meter scale (Blender's default metric
  unit already equals 1 meter).
- Overall dimensions: 1.4m wide, 0.7m deep, 0.75m tall.
- Structure: a flat rectangular desktop surface on a simple metal-frame leg
  structure (4 legs or an H-frame), plus one shallow drawer box on one side
  (static/closed, doesn't need to open).
- Keep geometry low-poly and game-ready: basic box primitives only.
- Origin: set the object's origin to the bottom-center of the desk (floor
  contact point, centered on width and depth).
- Materials: a plain beige/tan laminate material for the desktop, a dark
  gray metal material for the legs/frame. Simple Principled BSDF, no
  textures.
- UVs: apply a basic Smart UV Project unwrap to every object.
- Hierarchy: parent all pieces under a single empty named "Desk_Office", then
  join into one final mesh object with that same name.
- Export the result to FBX at "//Desk_Office.fbx" (relative to the current
  .blend file) using bpy.ops.export_scene.fbx() with default forward/up axis
  settings and "Apply Transform" enabled.

Output only the complete Python script in a single code block, nothing else.
```

### `FilingCabinet.fbx` — Blender script prompt

```
Write a complete, runnable Blender Python (bpy) script that procedurally
builds a low-poly, game-ready 3D model of a plain metal 4-drawer office
filing cabinet, and exports it to FBX. I will paste this directly into
Blender's Scripting tab and run it, so it needs to work standalone with no
manual steps afterward.

Requirements:
- Units: build directly at real-world meter scale (Blender's default metric
  unit already equals 1 meter).
- Overall dimensions: 0.45m wide, 0.6m deep, 1.3m tall.
- Structure: a rectangular cabinet body with 4 stacked drawer faces (flat
  boxes with a simple raised handle bar on each — closed/static, don't need
  to open).
- Keep geometry low-poly and game-ready: basic box primitives only.
- Origin: set the object's origin to the bottom-center of the cabinet (floor
  contact point, centered on width and depth).
- Materials: a plain gray or beige metal material for the body, a slightly
  darker gray for the drawer handles. Simple Principled BSDF, no textures.
- UVs: apply a basic Smart UV Project unwrap to every object.
- Hierarchy: parent all pieces under a single empty named "FilingCabinet",
  then join into one final mesh object with that same name.
- Export the result to FBX at "//FilingCabinet.fbx" (relative to the current
  .blend file) using bpy.ops.export_scene.fbx() with default forward/up axis
  settings and "Apply Transform" enabled.

Output only the complete Python script in a single code block, nothing else.
```

### `Binder_PersonnelLogs.fbx` — Blender script prompt

```
Write a complete, runnable Blender Python (bpy) script that procedurally
builds a low-poly, game-ready 3D model of a worn ring binder/folder (the kind
used for paper personnel files), and exports it to FBX. I will paste this
directly into Blender's Scripting tab and run it, so it needs to work
standalone with no manual steps afterward.

Requirements:
- Units: build directly at real-world meter scale (Blender's default metric
  unit already equals 1 meter).
- Overall dimensions: 0.3m wide, 0.25m tall, 0.05m thick.
- Structure: a simple rectangular box with slightly beveled edges,
  representing a closed ring binder. Add a thin raised rectangular strip
  along the spine to suggest a label area (no legible text needed).
- Keep geometry low-poly and game-ready: a single box primitive with a
  bevel modifier applied is enough — apply the modifier (make it real
  geometry) before export.
- Origin: set the object's origin to the bottom-center of the binder (the
  face it would rest flat on, centered on width and thickness).
- Materials: a dark navy or gray vinyl-look material for the cover, a
  slightly lighter gray for the spine label strip. Simple Principled BSDF,
  no textures.
- UVs: apply a basic Smart UV Project unwrap.
- Naming: name the final object "Binder_PersonnelLogs".
- Export the result to FBX at "//Binder_PersonnelLogs.fbx" (relative to the
  current .blend file) using bpy.ops.export_scene.fbx() with default
  forward/up axis settings and "Apply Transform" enabled.

Output only the complete Python script in a single code block, nothing else.
```

### `SecretRoom_Reveal_Sting.ogg` — not a ChatGPT prompt

Search freesound.org (or similar CC-licensed sound libraries) for something like "low drone sting," "unsettling stinger," or "horror reveal cue" — 1-3 seconds, low-frequency, no melody. Not something to generate via text/image/script prompting.

### `PersonnelLog_01_VO` — narration script prompt (text only, then needs a separate TTS tool)

```
Write a short (30-45 second spoken, roughly 80-100 words) personnel log
entry, in the voice of a mid-level facilities maintenance employee at a
fictional 1990s corporate office complex called Aldermere Business Park.
The entry should sound mundane and procedural at first — routine complaints
about broken elevators, budget requests, a coworker's shift change — but end
on one small, unsettling detail suggesting the building's maintenance
schedule doesn't match any known employee roster, without explicitly
stating anything supernatural. Deadpan, bureaucratic tone throughout, no
horror-movie language.
```

That gives you the text; running it through any text-to-speech tool (or ChatGPT's separate voice features, if you have access) gets you the audio file.

## A note on the bookcase's pivot

I asked for bottom-center origin on all four props, including the bookcase, to keep the Blender-side script simple and consistent. The bookcase disguises a functional secret door that swings open on a hinge — I'll handle lining up its pivot with the door's swing behavior on the Unity side when I wire it in; that's not something the model itself needs to account for.
