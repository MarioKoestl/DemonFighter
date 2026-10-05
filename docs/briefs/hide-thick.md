# Brief: thick hide

The first armor: a slab of thickened hide that grows over the back of the core and absorbs blows (GAME_DESIGN.md, "Damage model"). Defense: strong against Cut, neutral against Pierce, weak against Blunt. It is destroyed at zero HP, not severed: it vanishes and exposes the flesh beneath.

- **Part id:** `part.hide.thick`
- **Socket:** Hide, on the upper back of the core. The socket holds one part at a time: thick hide, plates, elastic tissue or spines.
- **Folder:** `Assets/_Project/Art/Models/BodyParts/`
- **Files:** `BP_Hide_Thick_Intact.fbx`; later `BP_Hide_Thick_Wounded.fbx` and `BP_Hide_Thick_Mangled.fbx`. No stump.
- **Tool:** Meshy, Text to 3D (ASSET_GUIDE.md, step 3). Symmetry Off.
- **Budget:** 2,000 to 5,000 triangles.

## Shape

- About 0.8 tall, 0.7 wide and 0.12 thick on the body, for a core 1 tall. A curved slab that hugs the rounded back: hollow on the body side, bulging outward.
- Leathery, calloused hide with deep creases, warts and scar ridges, thickest along the middle, thinning toward the edges where it blends into flesh. Not scales, not bony plates (that is `plates.md`), not fur.
- **Generate it lying flat like an upturned shell: the hollow side underneath, the bulge up**, its top edge pointing away from you.

## How the game attaches it

- **Pivot.** The game takes the bottom center, the middle of the hollow side, and sets it onto the Hide anchor on the upper back (D-088).
- **Size.** The game scales every model so its longest side is 1. **Scale 0.8** makes the slab 0.8 body heights tall.
- **Direction.** **Euler (90, 0, 0)** stands the slab up on the back, the bulge facing backward. The edge that pointed away from you in Meshy becomes the top.
- **Offset** moves it along the socket: positive Z out of the body, negative Z into it, Y up or down.
- The hide keeps its own texture (Dark role); the owner color does not tint it.

## Textures

Hide in browns and grays, around 70 to 110 of 255, not near black; high roughness; flesh at the edges red. No baked shadows (core-blob.md, "Textures").

## Prompt words

```
realistic slab of thick calloused demon hide lying flat, curved like an upturned shell, hollow side down, deep creases, warts and scar ridges, leathery skin, raw red flesh at the torn edges, gory, body horror, game asset, PBR, detached part on its own, brown and gray hide, evenly lit texture, no baked shadows, not cartoon
```

Negative:

```
scales, metal plates, armor, fur, turtle shell, full creature, cartoon, stylized, base, pedestal, black texture
```

## Damage states (later)

- **Wounded:** a chunk bitten from one edge, deep claw furrows across the middle, raw flesh in the furrows.
- **Mangled:** half the slab torn away, the rest hanging in flaps, flesh and blood beneath.

Both need the same shape and size as the intact slab, so they wait for Blender (ASSET_GUIDE.md, 2.9).

## Checks before import

- Triangles between 2,000 and 5,000.
- One curved slab lying flat, hollow side down, no creature, no base.
- Browns and grays, not near black, in the preview.
- License entry in `docs/ASSET_LICENSES.md`.

## In Unity

1. Rename the file to `BP_Hide_Thick_Intact.fbx` and copy it with its textures into `Assets/_Project/Art/Models/BodyParts/`.
2. Run `Demon Fighter > Generate > Placeholder Assets`, then `Demon Fighter > Art > Validate Art Assets`.
3. Select `BP_ThickHide.asset` in `Assets/_Project/Content/BodyParts` and set `Visual > Meshes`: **Scale 0.8**, **Euler (90, 0, 0)**.
4. Play and look at a Brute from behind, which spawns with thick hide, or buy it in the mutation menu (Tab).
5. Tune while playing.

| You see | Change on `BP_ThickHide.asset` |
|---|---|
| Sunk into the back | Offset Z up, in steps of 0.03 |
| Its edges stand off the back | Offset Z down, in steps of 0.03 |
| Too high or too low on the back | Offset Y, in steps of 0.05 |
| Too big or too small | Scale, in steps of 0.05 |
| The hollow side faces outward | Euler X -90 |
| Upside down | Euler (-90, 0, 180) |
