# Art briefs

One brief per asset Mario generates with an AI 3D tool (ASSET_PIPELINE.md, stage 2). The briefs live here instead of the gitignored `ArtSource/` so they are versioned and reviewable (DECISIONS.md, D-079). The source files (prompts, seeds, raw exports) still go to `ArtSource/` on Mario's PC.

The click-by-click walkthrough from account to model in the game is `docs/ASSET_GUIDE.md`.

## What every brief gives you

- What the part is, which socket it grows from, and what it does in the game.
- Its size relative to a core 1 tall, and **the pose to generate it in**.
- Prompt words and negative words to paste into Meshy, in code boxes.
- Textures and their brightness, the damage states for later, the checks before import.
- The values to set in Unity (Scale, Euler, Offset) and a table of fixes for what you might see.

## The three poses

The game takes the bottom center of a model, as it stood in Meshy, as the point where the part meets the body, and scales its longest side to 1 (D-088). Where Meshy put the origin and how big it made the model do not matter. What matters is the pose:

| Pose | Parts | Euler to set |
|---|---|---|
| Standing upright on the end that touches the body | arm, tail | (90, 0, 0) points it out along its socket; for the arm, (90, -90, 0) points it forward |
| Facing you, the way it sits on a body | jaws, eyes | (0, 0, 0) |
| Standing on its feet, toes toward you | legs | (-90, 0, 0) |
| Lying flat, the side that touches the body underneath | thick hide, plates, spines, elastic tissue | (90, 0, 0) |

**Scale** is the part's longest side in body heights, for example 0.55 for the arm. **Offset** moves the part along its socket: positive Z away from the body, Y up. Every brief has its exact values.

## Textures

A base color texture is the color of the surface under even light, without baked shadows. Flesh around 110 to 150 of 255 in its red channel, bone and teeth lighter, hide no darker than about 70. A texture that is mostly below 30 renders as a black shape in the cave (ASSET_PIPELINE.md, "World surfaces", D-087).

## The pipeline in five steps

1. Generate in Meshy with the brief's pose and prompt words.
2. Export **FBX** (binary) with the PBR textures (base color, normal, roughness, metallic; a packed ORM map is fine). Unity has no built-in GLB importer, so GLB would need an extra package; FBX needs nothing.
3. Name the files as the brief says and drop them into the folder the brief names under `Assets/_Project/Art/Models/`. The import rules (`ArtImportRules`) apply the settings from ASSET_PIPELINE.md on import: scale 1, no colliders, no animation, materials extracted into a `Materials` folder next to the model, textures sRGB only for base color, normal maps marked, sizes capped, BC7 for color and BC5 for normals.
4. Add the entry to `docs/ASSET_LICENSES.md` (template at the top of that file). The validator reports an error for every model without one.
5. In Unity run `Demon Fighter > Generate > Placeholder Assets` (binds every model to its part asset by name) and `Demon Fighter > Art > Validate Art Assets` (report in the Console and in `TestResults/art-validation.txt`). Set the values from the brief on the part asset, then start a run: the part shows its mesh instead of the primitive, and swaps state as it takes damage.

## Naming

`BP_<Part>[_<Variant>]_<State>.fbx` where `<Part>[_<Variant>]` maps to the part id: `BP_Jaws` is `part.jaws`, `BP_Hide_Thick` is `part.hide.thick`. `<State>` is one of `Intact`, `Wounded`, `Mangled`, `Stump`. One file per state is the normal case; a single file with child objects named by state (`BP_Jaws_Intact`, or just `Intact`) works too.

Texture names decide their import: anything with `normal`, `_nrm`, `_nor` or `_n` is a normal map; anything with `rough`, `metal`, `_ao`, `occlusion`, `orm`, `mask`, `height`, `displace`, `smooth` or `gloss` is linear data; everything else is base color (sRGB).

## Tuning after import

The part asset (`Assets/_Project/Content/BodyParts/BP_<Part>.asset`) shows the bound meshes under `Visual > Meshes` and three knobs: `Scale`, `Offset` and `Euler`. They start neutral on the first bind and the binder never touches them again; the turn and size the file itself carries sit underneath them (D-088). The second arm takes every value mirrored. The core asset carries the socket anchors under `Visual > Anchors` (core-blob.md, "Sockets").

## All briefs

| Brief | Part id | Model file | Socket |
|---|---|---|---|
| `core-blob.md` | `part.core` | `BP_Core_Intact.fbx` in `Models/Core/` | (the body) |
| `jaws.md` | `part.jaws` | `BP_Jaws_Intact.fbx` | Head |
| `eyes.md` | `part.eyes` | `BP_Eyes_Intact.fbx` | Head |
| `arm.md` | `part.arm` | `BP_Arm_Intact.fbx` | Limb |
| `legs.md` | `part.legs` | `BP_Legs_Intact.fbx` | Locomotion |
| `tail.md` | `part.tail` | `BP_Tail_Intact.fbx` | Tail |
| `hide-thick.md` | `part.hide.thick` | `BP_Hide_Thick_Intact.fbx` | Hide |
| `plates.md` | `part.hide.plates` | `BP_Hide_Plates_Intact.fbx` | Hide |
| `spines.md` | `part.spines` | `BP_Spines_Intact.fbx` | Hide |
| `elastic-tissue.md` | `part.hide.elastic` | `BP_Hide_Elastic_Intact.fbx` | Hide |

Part models go into `Assets/_Project/Art/Models/BodyParts/`. Tail, plates, spines and elastic tissue need an evolution before a demon can grow them.
