# Brief: legs

The locomotion part: a pair of squat legs that grows from the underside of the core and replaces crawling with walking (GAME_DESIGN.md, body parts; the move speed bonus is data on the part). One part is the whole pair. Legs are severed at zero HP: the pair drops as food and a stump stays on the body.

- **Part id:** `part.legs`
- **Socket:** Locomotion (the `Socket_Locomotion` anchor at the base of the core, +Z pointing down)
- **Folder:** `Assets/_Project/Art/Models/BodyParts/`
- **Files:** `BP_Legs_Intact.fbx`, `BP_Legs_Wounded.fbx`, `BP_Legs_Mangled.fbx`, `BP_Legs_Stump.fbx`
- **Budget:** 2,000 to 5,000 triangles per state, one 1024 texture set.

## Shape and size

- For a 1 m core: 0.35 m tall, 0.6 m wide, 0.5 m deep. Two thick bent legs joined by a saddle of flesh at the top that merges into the underside of the core. Short, wide, splayed, a crouching stance; three-toed feet with blunt claws.
- Sinew and bare muscle like the arm, some hide on the shins is fine.

## Pivot and axes

- The pivot is at the top center of the saddle, where the pair meets the body. Because the locomotion anchor turns the part to point +Z down, model the legs extending along +Z from the pivot: in the modeling tool the legs lie "forward" along +Z, the feet at the far end. +Y of the mesh becomes the forward direction of the demon after placement, so put the toes on the +Y side.
- If that is awkward in the tool, model the legs upright (feet at -Y) with the saddle at the origin and set `Visual > Meshes > Euler` to (-90, 0, 0) on `BP_Legs.asset` after import.

## Damage states

- **Intact:** both legs whole.
- **Wounded:** gashes on both shins, one toe gone, muscle laid open on a thigh.
- **Mangled:** one leg broken at the knee and dragging, the other torn, bone showing.
- **Stump:** the saddle and two torn thigh roots, 0.1 m long, same pivot.

Same pivot and scale in all four.

## Textures

`BP_Legs_BaseColor`, `BP_Legs_Normal`, `BP_Legs_Roughness`, `BP_Legs_Metallic` (or `BP_Legs_ORM`), 1024 px.

## Prompt words

realistic demon legs, pair of squat bent legs joined by a flesh saddle, three-toed clawed feet, sinew, bare muscle, splayed crouching stance, raw, wet, gory, body horror, game asset, PBR, detached part on its own, dark palette, not cartoon.

Negative: human legs, boots, hooves, single leg, torso, cartoon, stylized.

## Checks before import

- Triangles between 2,000 and 5,000 per state.
- Pivot at the top of the saddle, +Y up, the legs along +Z (or upright with the Euler fix noted above).
- Height about 0.35 m, width about 0.6 m.
- Four states with the same pivot and scale.
- License entry in `docs/ASSET_LICENSES.md`.

## In Unity

After binding, demons with legs stand on the mesh at the locomotion anchor. The feet should touch the ground: if they float or sink, move `Visual > Meshes > Offset` on `BP_Legs.asset` along Y (body units). Procedural stepping comes with the animation item of M5; until then the legs are rigid.
