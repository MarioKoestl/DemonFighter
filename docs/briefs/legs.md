# Brief: legs

The locomotion part: a pair of squat legs that grows from the underside of the core and replaces crawling with walking (GAME_DESIGN.md, body parts; Sprint and Lunge come with it). One part is the whole pair. Legs are severed at zero HP: the pair drops as food.

- **Part id:** `part.legs`
- **Socket:** Locomotion, at the bottom center of the core.
- **Folder:** `Assets/_Project/Art/Models/BodyParts/`
- **Files:** `BP_Legs_Intact.fbx`; later `BP_Legs_Wounded.fbx`, `BP_Legs_Mangled.fbx` and `BP_Legs_Stump.fbx`.
- **Tool:** Meshy, Text to 3D (ASSET_GUIDE.md, step 3). Symmetry Auto or On.
- **Budget:** 2,000 to 5,000 triangles.

## Shape

- About 0.6 wide, 0.35 tall and 0.5 deep, for a core 1 tall.
- Two thick, bent legs joined at the top by a saddle of flesh. Short, wide, splayed, a crouching stance; three-toed feet with blunt claws. Sinew and bare muscle like the arm; some hide on the shins is fine.
- **Generate it standing on its feet, the way legs stand, toes toward you.**

## How the game attaches it

- **Pivot.** The game takes the point on the ground between the feet as the pivot and sets it under the middle of the core (D-088).
- **Standing.** The blob stands on its legs (D-094): the game lifts it until the lowest point of the legs touches the ground, whatever their length. A demon that loses its legs drops back onto its belly.
- **Size.** The game scales every model so its longest side is 1. **Scale 0.6** makes the pair 0.6 body heights wide.
- **Direction.** The Locomotion socket points down. **Euler (-90, 0, 0)** turns the legs upright again, exactly as they stood in Meshy, toes forward.
- **Offset (0, 0, 0.3)** lets the feet reach 0.3 body heights below the blob; the rest of the legs, the saddle, reaches up into it. Offset follows the socket here, so **Z moves the legs down** (larger: more leg shows and the blob stands higher) or up (smaller: the legs sink into the blob), and Y moves them forward (positive) or back.
- The legs keep their own texture (Dark role). They swing with the gait while the demon walks.

## Textures

Flesh red to purple, around 110 to 150 in red; claws dark horn with lighter tips. No baked shadows (core-blob.md, "Textures").

## Prompt words

```
realistic demon legs, a pair of squat bent legs joined by a saddle of raw flesh at the top, standing on three-toed clawed feet, toes toward the viewer, sinew and bare muscle, splayed crouching stance, wet, gory, body horror, game asset, PBR, detached part on its own, red flesh with dark horn claws, evenly lit texture, no baked shadows, not cartoon
```

Negative:

```
human legs, boots, hooves, single leg, torso, upper body, cartoon, stylized, base, pedestal, black texture
```

## Damage states (later)

- **Wounded:** gashes on both shins, one toe gone, muscle laid open on a thigh.
- **Mangled:** one leg broken at the knee and dragging, the other torn, bone showing.
- **Stump:** the saddle and two torn thigh roots, a bone end showing. It stays on the body after the legs are severed.

All three need the same shape and size as the intact legs, so they wait for Blender (ASSET_GUIDE.md, 2.9).

## Checks before import

- Triangles between 2,000 and 5,000.
- One pair of legs standing on its feet, toes toward you, no torso, no base.
- The flesh is clearly red in the preview, not near black.
- License entry in `docs/ASSET_LICENSES.md`.

## In Unity

1. Rename the file to `BP_Legs_Intact.fbx` and copy it with its textures into `Assets/_Project/Art/Models/BodyParts/`.
2. Run `Demon Fighter > Generate > Placeholder Assets`, then `Demon Fighter > Art > Validate Art Assets`.
3. Select `BP_Legs.asset` in `Assets/_Project/Content/BodyParts` and set `Visual > Meshes`: **Scale 0.6**, **Euler (-90, 0, 0)**, **Offset (0, 0, 0.3)**.
4. Play and look at a Stalker, which spawns with legs, or buy legs in the mutation menu (Tab).
5. Tune while playing.

| You see | Change on `BP_Legs.asset` |
|---|---|
| A gap between the legs and the blob | Offset Z smaller, in steps of 0.05 |
| Legs hidden in the blob, it stands too low | Offset Z larger, in steps of 0.05 |
| Legs too far forward or back under the body | Offset Y: positive moves them forward |
| Too big or too small | Scale, in steps of 0.05 |
| Toes point backward | Euler (90, 0, 180): turns them around the vertical, because the socket points down |
| Lying on their back or front | Euler X back to -90 |
