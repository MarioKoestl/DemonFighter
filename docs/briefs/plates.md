# Brief: plates

Bony armor plates that grow over the back of the core (GAME_DESIGN.md, "Damage model"). Defense: strong against Pierce and Cut, weak against Blunt, which cracks them. Plates need an evolution first. They are destroyed at zero HP, not severed: they crack apart and vanish.

- **Part id:** `part.hide.plates`
- **Socket:** Hide, on the upper back of the core. The socket holds one part at a time: thick hide, plates, elastic tissue or spines.
- **Folder:** `Assets/_Project/Art/Models/BodyParts/`
- **Files:** `BP_Hide_Plates_Intact.fbx`; later `BP_Hide_Plates_Wounded.fbx` and `BP_Hide_Plates_Mangled.fbx`. No stump.
- **Tool:** Meshy, Text to 3D (ASSET_GUIDE.md, step 3). Symmetry Auto.
- **Budget:** 2,000 to 5,000 triangles.

## Shape

- About 0.85 tall, 0.75 wide and 0.15 thick on the body, for a core 1 tall. A curved shell of overlapping bony plates, like a pangolin's back, grown out of raw flesh: the plates overlap downward, wet flesh shows in the gaps, the edges are chipped and cracked.
- **Generate it lying flat like an upturned shell: the hollow side underneath, the plates up**, the top edge pointing away from you.

## How the game attaches it

- **Pivot.** The game takes the bottom center, the middle of the hollow side, and sets it onto the Hide anchor on the upper back (D-088).
- **Size.** **Scale 0.85** makes the shell 0.85 body heights tall; the game scales every model so its longest side is 1.
- **Direction.** **Euler (90, 0, 0)** stands the shell up on the back, the plates facing backward. The edge that pointed away from you in Meshy becomes the top.
- **Offset** moves it along the socket: positive Z out of the body, negative Z into it, Y up or down.
- The plates keep their own texture (Plate role); the owner color does not tint them.

## Textures

Bone and horn pale, around 140 to 200 of 255, with darker cracks; the flesh between the plates red, around 110 to 150 in red. No baked shadows (core-blob.md, "Textures").

## Prompt words

```
realistic demon back armor lying flat, curved like an upturned shell, hollow side down, overlapping bony plates grown out of raw flesh, chipped and cracked edges, wet red flesh in the gaps, pale bone and horn, gory, body horror, game asset, PBR, detached part on its own, evenly lit texture, no baked shadows, not cartoon
```

Negative:

```
metal armor, knight, turtle, fish scales, fur, full creature, cartoon, stylized, base, pedestal, black texture
```

## Damage states (later)

- **Wounded:** two plates cracked through, one torn off, raw flesh beneath.
- **Mangled:** most plates shattered or hanging, the flesh beneath torn open.

Both need the same shape and size as the intact shell, so they wait for Blender (ASSET_GUIDE.md, 2.9).

## Checks before import

- Triangles between 2,000 and 5,000.
- One curved shell of plates lying flat, hollow side down, no creature, no base.
- Pale bone, red flesh, not near black, in the preview.
- License entry in `docs/ASSET_LICENSES.md`.

## In Unity

1. Rename the file to `BP_Hide_Plates_Intact.fbx` and copy it with its textures into `Assets/_Project/Art/Models/BodyParts/`.
2. Run `Demon Fighter > Generate > Placeholder Assets`, then `Demon Fighter > Art > Validate Art Assets`.
3. Select `BP_Plates.asset` in `Assets/_Project/Content/BodyParts` and set `Visual > Meshes`: **Scale 0.85**, **Euler (90, 0, 0)**.
4. Play. Plates need an evolution: reach the first evolution level (test mode keeps you alive while you kill for XP), evolve, then buy them in the mutation menu (Tab).
5. Tune while playing.

| You see | Change on `BP_Plates.asset` |
|---|---|
| Sunk into the back | Offset Z up, in steps of 0.03 |
| Its edges stand off the back | Offset Z down, in steps of 0.03 |
| Too high or too low on the back | Offset Y, in steps of 0.05 |
| Too big or too small | Scale, in steps of 0.05 |
| The hollow side faces outward | Euler X -90 |
| Upside down, plates overlapping upward | Euler (-90, 0, 180) |
