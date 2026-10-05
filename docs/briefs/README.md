# Art briefs

One brief per asset Mario generates with an AI 3D tool (ASSET_PIPELINE.md, stage 2). The briefs live here instead of the gitignored `ArtSource/` so they are versioned and reviewable (DECISIONS.md, D-079). The source files (prompts, seeds, raw exports) still go to `ArtSource/` on Mario's PC.

The click-by-click walkthrough from account to model in the game is `docs/ASSET_GUIDE.md`.

## What every brief gives you

- What the asset is and which socket it plugs into.
- Size in meters relative to a 1 m tall core. The view scales everything with the body, so proportions matter, absolute size less.
- Pivot and axes: pivot at the attachment point, +Z pointing away from the body, +Y up.
- The damage states to generate and the exact file names. In the first round make only the `_Intact` file; the other states come later (`docs/ASSET_GUIDE.md`, step 2.9).
- Budgets, texture set, style words for the prompt, and the checks before import.

## The pipeline in five steps

1. Generate. Try two or three tools with the same brief and keep the best.
2. Export **FBX** (binary) with the PBR textures (base color, normal, roughness, metallic; a packed ORM map is fine). Unity has no built-in GLB importer, so GLB would need an extra package; FBX needs nothing.
3. Name the files as the brief says and drop them into the folder the brief names under `Assets/_Project/Art/Models/`. The import rules (`ArtImportRules`) apply the settings from ASSET_PIPELINE.md on import: scale 1, no colliders, no animation, materials extracted into a `Materials` folder next to the model, textures sRGB only for base color, normal maps marked, sizes capped, BC7 for color and BC5 for normals.
4. Add the entry to `docs/ASSET_LICENSES.md` (template at the top of that file). The validator reports an error for every model without one.
5. In Unity run `Demon Fighter > Generate > Placeholder Assets` (binds every model to its part asset by name) and `Demon Fighter > Art > Validate Art Assets` (report in the Console and in `TestResults/art-validation.txt`). Then start a run: the part shows its mesh instead of the primitive, and swaps state as it takes damage.

## Naming

`BP_<Part>[_<Variant>]_<State>.fbx` where `<Part>[_<Variant>]` maps to the part id: `BP_Jaws` is `part.jaws`, `BP_Hide_Thick` is `part.hide.thick`. `<State>` is one of `Intact`, `Wounded`, `Mangled`, `Stump`. One file per state is the normal case; a single file with child objects named by state (`BP_Jaws_Intact`, or just `Intact`) works too.

Texture names decide their import: anything with `normal`, `_nrm`, `_nor` or `_n` is a normal map; anything with `rough`, `metal`, `_ao`, `occlusion`, `orm`, `mask`, `height`, `displace`, `smooth` or `gloss` is linear data; everything else is base color (sRGB).

## Tuning after import

The part asset (`Assets/_Project/Content/BodyParts/BP_<Part>.asset`) shows the bound meshes under `Visual > Meshes`. Three knobs fit a mesh without re-exporting: `Scale` (uniform, 1 fits a one meter body), `Offset` (shift from the socket anchor in body units, 1 = body height) and `Euler` (extra rotation). The binder fills them once, from the model, and never touches them again. The core asset also carries the socket anchors under `Visual > Anchors`, in body units with the origin at the base; a core model with `Socket_` transforms rewrites them, otherwise the defaults for the capsule stay and can be tuned by hand.

## The first five

| Brief | Part id | Folder |
|---|---|---|
| `core-blob.md` | `part.core` | `Assets/_Project/Art/Models/Core/` |
| `jaws.md` | `part.jaws` | `Assets/_Project/Art/Models/BodyParts/` |
| `arm.md` | `part.arm` | `Assets/_Project/Art/Models/BodyParts/` |
| `legs.md` | `part.legs` | `Assets/_Project/Art/Models/BodyParts/` |
| `hide-thick.md` | `part.hide.thick` | `Assets/_Project/Art/Models/BodyParts/` |

Start with the core and the jaws: together they already change the look of every demon in the cavern.
