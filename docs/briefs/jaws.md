# Brief: jaws

The first mutation most demons take (GAME_DESIGN.md, body parts): a big tooth-lined maw that grows over the small mouth on the front of the core and makes Bite hit harder. It is the part the player looks at most, right in front of the camera in first person and on every enemy that comes at them.

- **Part id:** `part.jaws`
- **Socket:** Head (the `Socket_Head` anchor on the upper front of the core, +Z forward)
- **Folder:** `Assets/_Project/Art/Models/BodyParts/`
- **Files:** `BP_Jaws_Intact.fbx`, `BP_Jaws_Wounded.fbx`, `BP_Jaws_Mangled.fbx`. No stump: jaws are destroyed, not severed, and vanish when lost.
- **Budget:** 2,000 to 5,000 triangles per state, one 1024 texture set.

## Shape and size

- About 0.35 m long (along +Z), 0.4 m wide, 0.28 m tall, for a 1 m core. The view scales it with the body.
- A split mandible: upper and lower jaw joined at the back, open a little so the teeth show, rows of uneven teeth (long fangs at the front, crushing molars at the back), slick gums, strands of saliva are fine if the tool makes them.
- The back end is a ragged ring of flesh that merges into the core; it may overlap the core slightly, that hides the core's own mouth underneath.

## Pivot and axes

- Pivot at the back center of the mandible, where it meets the body. +Z points forward out of the body, along the teeth. +Y up.
- The whole mesh lies in front of the pivot (positive Z).

## Damage states

- **Intact:** every tooth in place, gums whole.
- **Wounded:** teeth broken off, a split through the lower jaw, gums torn.
- **Mangled:** half the upper jaw gone, the hinge exposed, remaining teeth bent, flesh hanging.

Same pivot and scale in all three.

## Textures

`BP_Jaws_BaseColor`, `BP_Jaws_Normal`, `BP_Jaws_Roughness`, `BP_Jaws_Metallic` (or `BP_Jaws_ORM`), 1024 px. Teeth off-white to yellow with dark roots, gums dark red, high gloss on saliva and gums, matte on teeth.

## Prompt words

realistic demon jaws, split mandible, rows of uneven teeth, long fangs, slick dark red gums, raw flesh ring at the back, wet, gory, body horror, game asset, PBR, detached part on its own, dark palette, not cartoon.

Negative: full head, skull, eyes, tongue sticking out, cartoon, stylized, symmetric perfect teeth.

## Checks before import

- Triangles between 2,000 and 5,000 per state.
- Pivot at the back center, mesh entirely in front of it (+Z), +Y up.
- Length about 0.35 m.
- Three states with the same pivot and scale.
- License entry in `docs/ASSET_LICENSES.md`.

## In Unity

After `Demon Fighter > Generate > Placeholder Assets` every demon with jaws wears the mesh at the head anchor. If it sits too deep or too high, adjust `Visual > Meshes > Offset` on `BP_Jaws.asset` (body units: 0.05 is 5 cm on a 1 m demon). The jaws keep the maw palette color only while no material is bound; a bound material shows its own texture tinted by the owner color when the part is set to the Owner role, and untinted with the Maw role (the default for jaws).
