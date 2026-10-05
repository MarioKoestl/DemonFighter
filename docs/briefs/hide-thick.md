# Brief: thick hide

The first armor: a slab of thickened hide that grows over the back of the core and absorbs blows (GAME_DESIGN.md, "Damage model", the Hide socket with a defense type). It is destroyed at zero HP, not severed, so it has no stump and simply vanishes when lost, exposing the flesh beneath.

- **Part id:** `part.hide.thick`
- **Socket:** Hide (the `Socket_Hide` anchor on the upper back of the core, +Z pointing backward away from the body)
- **Folder:** `Assets/_Project/Art/Models/BodyParts/`
- **Files:** `BP_Hide_Thick_Intact.fbx`, `BP_Hide_Thick_Wounded.fbx`, `BP_Hide_Thick_Mangled.fbx`
- **Budget:** 2,000 to 5,000 triangles per state, one 1024 texture set.

## Shape and size

- For a 1 m core: 0.8 m tall, 0.7 m wide, 0.12 m thick. A curved slab that hugs the rounded back of the core: concave on the body side, convex outward.
- Leathery, calloused hide with deep creases, warts and scar ridges, thickest along the spine line, thinning toward the edges where it blends into flesh. Not scales, not plates (those are a different part, `part.hide.plates`), not fur.

## Pivot and axes

- Pivot at the center of the inner (concave) surface, the point that touches the back of the core. +Z points outward, away from the body, so the slab lies in front of the pivot along +Z with its thickness along Z and its height along Y.
- The inner surface should roughly follow a cylinder of 0.3 m radius so it wraps the capsule-sized core; a little overlap into the body hides the seam.

## Damage states

- **Intact:** whole slab, creased and calloused.
- **Wounded:** a chunk bitten from one edge, deep claw furrows across the middle, raw flesh showing in the furrows.
- **Mangled:** half the slab torn away, the rest hanging in flaps, flesh and blood beneath.

Same pivot and scale in all three.

## Textures

`BP_Hide_Thick_BaseColor`, `BP_Hide_Thick_Normal`, `BP_Hide_Thick_Roughness`, `BP_Hide_Thick_Metallic` (or `BP_Hide_Thick_ORM`), 1024 px. Hide in dark browns and grays, high roughness, the exposed flesh in the wounded states wet and red.

## Prompt words

realistic demon hide slab, thick calloused leathery skin, deep creases, warts and scar ridges, curved plate that wraps a back, raw flesh at the torn edges, gory, body horror, game asset, PBR, detached part on its own, dark palette, not cartoon.

Negative: scales, metal plates, fur, shell, turtle, cartoon, stylized, full creature.

## Checks before import

- Triangles between 2,000 and 5,000 per state.
- Pivot at the center of the inner surface, +Z outward, +Y up.
- Height about 0.8 m, thickness about 0.12 m.
- Three states with the same pivot and scale.
- License entry in `docs/ASSET_LICENSES.md`.

## In Unity

After binding, the slab sits on the back anchor of every demon that grew thick hide. Push it into or out of the body with `Visual > Meshes > Offset` on `BP_ThickHide.asset` (Z in body units). The part uses the Dark material role for its primitive; with a bound material the texture shows untinted.
