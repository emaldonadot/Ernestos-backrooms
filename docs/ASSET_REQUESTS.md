# Asset Requests — Milestone 8 (Secret Room + Wall Texture)

Reference doc for assets you might source/generate for Milestone 8. **None of these are required** — the game works and tests fine fully grey-boxed. This exists so if you *do* want to add some visual texture, you know exactly what to make, at what size, and how.

## What ChatGPT can and can't actually do here

Important before you spend time on this: **ChatGPT generates 2D images. It cannot generate 3D mesh files (FBX/OBJ) or audio files.**

- **Textures** (flat 2D images) — ChatGPT can generate these directly and they can be used close to as-is.
- **3D props** (bookcase, desk, cabinet, binder) — ChatGPT can only produce a *reference/concept image* of what the object should look like. That image is not a usable Unity asset by itself — you'd still need to either (a) model it manually in a 3D tool (e.g. Blender, free) using the image as a guide, or (b) run the image through a separate image-to-3D generation tool (not ChatGPT). If neither of those appeals to you, skip the props entirely — the secret room works fine with grey-box primitives standing in for furniture.
- **Audio** (the reveal sting, the voiced personnel log) — ChatGPT can't generate sound at all. For the sting, a site like freesound.org (free, CC-licensed sound effects) is the realistic path. For the voiced personnel log, I've given you a narration *script* prompt instead — you'd run that text through a separate text-to-speech tool, not ChatGPT's image generator.

Given that, realistically **the wall texture is the one item here you can go get from ChatGPT right now and actually use.** Everything else either needs an extra tool/step or isn't a ChatGPT job to begin with.

## Reference dimensions (Unity: 1 unit = 1 meter)

| Reference | Size |
|---|---|
| Room footprint | 6m × 6m |
| Wall height | 3m |
| Wall thickness | 0.2m |
| Wall segment (post door-split) | 2m wide |
| Door width / height | 2m / 3m |

## Asset table

| Asset | Type | Priority | Format | Size | Can ChatGPT make it? |
|---|---|---|---|---|---|
| `Wall_Office_Albedo.png` | Texture (base color) | Recommended | PNG | 1024×1024, represents ~2m×2m of wall, seamless tile | **Yes — directly usable** |
| `Wall_Office_Normal.png` | Texture (normal map) | Optional | PNG | 1024×1024, matching albedo | No — see note below |
| `Wall_Office_Roughness.png` | Texture (roughness) | Optional | PNG (grayscale) | 1024×1024, matching albedo | No — see note below |
| `Bookcase_Disguise` | 3D prop (secret door disguise) | Recommended if doing props at all | Concept image only (PNG) | ~2m W × 2.2m H × 0.4m D | Concept reference only, not a mesh |
| `Desk_Office` | 3D prop | Optional | Concept image only (PNG) | ~1.4m × 0.7m × 0.75m | Concept reference only, not a mesh |
| `FilingCabinet` | 3D prop | Optional | Concept image only (PNG) | ~0.45m × 0.6m × 1.3m | Concept reference only, not a mesh |
| `Binder_PersonnelLogs` | 3D prop | Optional | Concept image only (PNG) | ~0.3m × 0.25m × 0.05m | Concept reference only, not a mesh |
| `SecretRoom_Reveal_Sting.ogg` | Audio (sting) | Optional | OGG | 1-3 seconds | No — source from freesound.org or similar |
| `PersonnelLog_01_VO` | Audio (voiced log) | Optional | OGG/WAV | ~30-60 seconds | No — script prompt provided, needs a TTS tool separately |

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

These need actual surface height/material data that an image generator can't produce meaningfully — asking ChatGPT for one just yields a flat gray or purple image with no real bump/roughness information. If you want these, the practical path is running the finished albedo texture through a free normal-map generator tool (there are several web-based ones) rather than prompting for them directly.

### `Bookcase_Disguise` concept reference

```
Create a concept reference image of a plain wooden office bookcase/shelving
unit, viewed straight-on from the front with no perspective distortion
(orthographic front elevation), against a flat plain white background.
Approximately 2 meters wide and 2.2 meters tall, 4-5 shelves, styled as
generic late-20th-century corporate office furniture — plain, functional,
slightly worn with age, no modern branding or text. Even flat lighting, no
strong shadows, no background elements. This is a modeling reference image,
not a finished rendered scene.
```

### `Desk_Office` concept reference

```
Create a concept reference image of a plain corporate office desk, viewed
straight-on from the front with no perspective distortion (orthographic
front elevation), against a flat plain white background. Approximately 1.4
meters wide, 0.75 meters tall. Styled as generic late-20th-century office
furniture — laminate surface, metal legs, functional and worn, no branding.
Even flat lighting, no strong shadows, no background elements. This is a
modeling reference image, not a finished rendered scene.
```

### `FilingCabinet` concept reference

```
Create a concept reference image of a plain metal 4-drawer office filing
cabinet, viewed straight-on from the front with no perspective distortion
(orthographic front elevation), against a flat plain white background.
Approximately 0.45 meters wide and 1.3 meters tall. Grey or beige metal,
slightly worn/scuffed, no branding or text. Even flat lighting, no strong
shadows, no background elements. This is a modeling reference image, not a
finished rendered scene.
```

### `Binder_PersonnelLogs` concept reference

```
Create a concept reference image of a worn ring binder/folder, the kind used
for paper personnel files, viewed straight-on from the front with no
perspective distortion, against a flat plain white background. Approximately
0.3 meters wide and 0.25 meters tall, thin (about 5cm). Dark gray or navy
vinyl cover, faded label area on the spine (no legible text needed), edges
worn from handling. Even flat lighting, no strong shadows, no background
elements. This is a modeling reference image, not a finished rendered scene.
```

### `SecretRoom_Reveal_Sting.ogg` — not a ChatGPT prompt

Search freesound.org (or similar CC-licensed sound libraries) for something like "low drone sting," "unsettling stinger," or "horror reveal cue" — 1-3 seconds, low-frequency, no melody. Not something to generate via text/image prompting.

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
