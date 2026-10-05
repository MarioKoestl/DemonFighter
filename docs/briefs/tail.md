# Brief: tail

A heavy, muscular tail that grows from the lower back of the core and grants Tail Swing, a wide Blunt arc that staggers (GAME_DESIGN.md, skills). It needs an evolution first. A tail is severed at zero HP: it drops as food.

- **Part id:** `part.tail`
- **Socket:** Tail, low on the back of the core, pointing back and a little down.
- **Folder:** `Assets/_Project/Art/Models/BodyParts/`
- **Files:** `BP_Tail_Intact.fbx`; later `BP_Tail_Wounded.fbx`, `BP_Tail_Mangled.fbx` and `BP_Tail_Stump.fbx`.
- **Tool:** Meshy, Text to 3D (ASSET_GUIDE.md, step 3). Symmetry Auto or On.
- **Budget:** 2,000 to 5,000 triangles.

## Shape

- About 0.8 long for a core 1 tall, 0.18 thick at the root, tapering to a heavy, knotted club of bone at the tip.
- Segmented muscle with a ridge of bone knobs along one side; the root is a ragged ring of flesh that sits against the core.
- **Generate it standing straight up, the root at the bottom and the club at the top**, like the arm.

## How the game attaches it

- **Pivot.** The game takes the bottom center, the root, and sets it onto the Tail anchor on the lower back (D-088).
- **Size.** The game scales every model so its longest side is 1. **Scale 0.8** makes the tail 0.8 body heights long.
- **Direction.** **Euler (90, 0, 0)** tips the tail from standing up to pointing out along the socket, back and a little down. The side that faced you in Meshy ends up underneath, so generate the bone ridge facing away. Euler X 110 lets it hang lower, X 70 holds it higher.
- **Offset** moves the root along the socket: positive Z out of the body, negative Z into it.
- The tail wears the owner color as a light tint (Owner role). It sways while walking and whips through Tail Swing by itself.

## Textures

Flesh red, around 110 to 150 in red; bone ridge and club pale, around 150 to 200. No baked shadows (core-blob.md, "Textures").

## Prompt words

```
realistic demon tail standing straight up, thick muscular root at the bottom, tapering to a heavy knotted bone club at the top, segmented flesh, a ridge of bone knobs along the back side, raw, wet, gory, body horror, game asset, PBR, detached part on its own, red flesh with pale bone, evenly lit texture, no baked shadows, not cartoon
```

Negative:

```
animal tail, fur, feathers, fish tail, coiled, curled, cartoon, stylized, base, pedestal, black texture
```

## Damage states (later)

- **Wounded:** deep gashes across the segments, a bone knob split.
- **Mangled:** the club cracked open, the tail half torn through and hanging.
- **Stump:** the root ring and a short torn end, a bone end showing. It stays on the body after the tail is severed.

All three need the same shape and size as the intact tail, so they wait for Blender (ASSET_GUIDE.md, 2.9).

## Checks before import

- Triangles between 2,000 and 5,000.
- One tail standing straight up on its root, club at the top, no base.
- The flesh is clearly red in the preview, not near black.
- License entry in `docs/ASSET_LICENSES.md`.

## In Unity

1. Rename the file to `BP_Tail_Intact.fbx` and copy it with its textures into `Assets/_Project/Art/Models/BodyParts/`.
2. Run `Demon Fighter > Generate > Placeholder Assets`, then `Demon Fighter > Art > Validate Art Assets`.
3. Select `BP_Tail.asset` in `Assets/_Project/Content/BodyParts` and set `Visual > Meshes`: **Scale 0.8**, **Euler (90, 0, 0)**.
4. Play. A tail needs an evolution: reach the first evolution level (test mode keeps you alive while you kill for XP), evolve, then buy the tail in the mutation menu (Tab).
5. Tune while playing.

| You see | Change on `BP_Tail.asset` |
|---|---|
| The root is buried in the body | Offset Z up, in steps of 0.03 |
| The root floats off the back | Offset Z down, in steps of 0.03 |
| Too long or too short | Scale, in steps of 0.05 |
| It should hang lower or stand higher | Euler X: 110 hangs lower, 70 holds it higher |
| It points into the body or at the sky | Euler X back to 90; check the tail was generated standing on its root |
| The bone ridge is underneath | Euler (-90, 180, 0): same direction, turned over |
