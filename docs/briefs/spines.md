# Brief: spines

A patch of long bone spines that grows on the back of the core. Whoever bites or claws a demon with spines takes part of the hit back as Pierce damage (D-058). Spines need an evolution first. They are destroyed at zero HP, not severed: they break off and vanish.

- **Part id:** `part.spines`
- **Socket:** Hide, on the upper back of the core. The socket holds one part at a time: thick hide, plates, elastic tissue or spines.
- **Folder:** `Assets/_Project/Art/Models/BodyParts/`
- **Files:** `BP_Spines_Intact.fbx`; later `BP_Spines_Wounded.fbx` and `BP_Spines_Mangled.fbx`. No stump.
- **Tool:** Meshy, Text to 3D (ASSET_GUIDE.md, step 3). Symmetry Off.
- **Budget:** 2,000 to 5,000 triangles.

## Shape

- A patch of raw flesh about 0.6 wide and 0.5 deep with six to ten spines of bone, 0.2 to 0.35 long, of different lengths, slightly curved, cracked near the tips, wet flesh swelling around their roots. For a core 1 tall.
- **Generate it lying flat: the flesh patch on the ground, the spines pointing straight up.**

## How the game attaches it

- **Pivot.** The game takes the bottom center, the middle of the flesh patch, and sets it onto the Hide anchor on the upper back (D-088).
- **Size.** **Scale 0.6** makes the patch 0.6 body heights across; the game scales every model so its longest side is 1.
- **Direction.** **Euler (90, 0, 0)** stands the patch up on the back with the spines pointing straight backward. **Euler X 60** tips them up, back and toward the sky, which reads more like a threat display; X 120 points them back and down.
- **Offset** moves it along the socket: positive Z out of the body, negative Z into it, Y up or down.
- The spines keep their own texture (Plate role); the owner color does not tint them.

## Textures

Spines pale bone, around 150 to 210 of 255, darker toward the cracked tips; flesh red, around 110 to 150 in red. No baked shadows (core-blob.md, "Textures").

## Prompt words

```
realistic patch of raw demon flesh lying flat with long sharp bone spines growing straight up, six to ten spines of different lengths, slightly curved, cracked bone, wet swollen flesh at the roots, gory, body horror, game asset, PBR, detached part on its own, pale bone and red flesh, evenly lit texture, no baked shadows, not cartoon
```

Negative:

```
hedgehog, porcupine, fur, crystals, metal, plant, cactus, full creature, cartoon, stylized, base, pedestal, black texture
```

## Damage states (later)

- **Wounded:** three spines snapped off, the flesh around them torn.
- **Mangled:** most spines broken to stubs, the patch ripped half off.

Both need the same shape and size as the intact patch, so they wait for Blender (ASSET_GUIDE.md, 2.9).

## Checks before import

- Triangles between 2,000 and 5,000.
- One flat flesh patch with spines pointing straight up, no creature, no base.
- Pale spines, red flesh, not near black, in the preview.
- License entry in `docs/ASSET_LICENSES.md`.

## In Unity

1. Rename the file to `BP_Spines_Intact.fbx` and copy it with its textures into `Assets/_Project/Art/Models/BodyParts/`.
2. Run `Demon Fighter > Generate > Placeholder Assets`, then `Demon Fighter > Art > Validate Art Assets`.
3. Select `BP_Spines.asset` in `Assets/_Project/Content/BodyParts` and set `Visual > Meshes`: **Scale 0.6**, **Euler (90, 0, 0)** or (60, 0, 0).
4. Play. Spines need an evolution: reach the first evolution level (test mode keeps you alive while you kill for XP), evolve, then buy them in the mutation menu (Tab).
5. Tune while playing.

| You see | Change on `BP_Spines.asset` |
|---|---|
| The patch is buried in the back | Offset Z up, in steps of 0.03 |
| The patch floats off the back | Offset Z down, in steps of 0.03 |
| Too high or too low on the back | Offset Y, in steps of 0.05 |
| Too big or too small | Scale, in steps of 0.05 |
| Spines should rise more or less | Euler X: smaller tips them up, larger down |
| Spines point into the body | Euler X -90 |
