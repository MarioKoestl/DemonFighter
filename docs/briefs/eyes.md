# Brief: eyes

A cluster of eyes that grows on the front of the core above the mouth and gives the demon Perception (GAME_DESIGN.md, "Stats": Perception from sensory parts). A blob starts blind; eyes let it see prey and threats from farther away. Eyes are destroyed at zero HP, not severed: they burst and vanish.

- **Part id:** `part.eyes`
- **Socket:** Head, on the upper front of the core. The jaws share it; the eyes sit above them.
- **Folder:** `Assets/_Project/Art/Models/BodyParts/`
- **Files:** `BP_Eyes_Intact.fbx`; later `BP_Eyes_Wounded.fbx` and `BP_Eyes_Mangled.fbx`. No stump.
- **Tool:** Meshy, Text to 3D (ASSET_GUIDE.md, step 3). Symmetry Off, so the eyes differ in size.
- **Budget:** 1,000 to 3,000 triangles.

## Shape

- About 0.3 wide, 0.15 tall and 0.12 deep, for a core 1 tall.
- Three to five bulging, wet, bloodshot eyeballs of different sizes, set in a ridge of raw flesh that grows out of the body. Not a face: no brows, no lids with lashes, no nose.
- **Generate it facing you:** the eyes look at you, the flesh ridge behind them.

## How the game attaches it

- **Pivot.** The game takes the bottom center of the cluster and sets it onto the Head anchor, raised by the offset so it sits above the jaws (D-088).
- **Size.** The game scales every model so its longest side is 1. **Scale 0.3** makes the cluster 0.3 body heights wide.
- **Direction.** With **Euler (0, 0, 0)** the eyes look forward, the way they looked at you in Meshy. If they look backward, set Euler Y to 180.
- **Offset (0, 0.12, 0.02)** is already set: it lifts the eyes above the jaws and a little out of the face.
- The eyes keep their own texture (Eye role); the owner color does not tint them.

## Textures

Eyeballs yellowed to pale, around 170 to 220 of 255, dark pupils, red veins; the flesh ridge red, around 110 to 150 in red; very glossy eyes, wet flesh. No baked shadows (core-blob.md, "Textures").

## Prompt words

```
realistic cluster of demon eyes facing the viewer, three to five bulging wet bloodshot eyeballs of different sizes, set in a ridge of raw flesh, red veins, glossy, gory, body horror, game asset, PBR, detached part on its own, yellowed eyeballs with dark pupils and red flesh, evenly lit texture, no baked shadows, not cartoon
```

Negative:

```
face, head, skull, nose, eyelashes, eyebrows, cute, cartoon, stylized, single eye, base, pedestal, black texture
```

## Damage states (later)

- **Wounded:** one eye burst and leaking, another bloodshot and swollen shut.
- **Mangled:** most eyes burst, the sockets torn open, the ridge hanging in flaps.

Both need the same shape and size as the intact eyes, so they wait for Blender (ASSET_GUIDE.md, 2.9).

## Checks before import

- Triangles between 1,000 and 3,000.
- Several eyes of different sizes facing you, no face, no base.
- Eyeballs light and glossy, flesh clearly red in the preview, not near black.
- License entry in `docs/ASSET_LICENSES.md`.

## In Unity

1. Rename the file to `BP_Eyes_Intact.fbx` and copy it with its textures into `Assets/_Project/Art/Models/BodyParts/`.
2. Run `Demon Fighter > Generate > Placeholder Assets`, then `Demon Fighter > Art > Validate Art Assets`.
3. Select `BP_Eyes.asset` in `Assets/_Project/Content/BodyParts` and set `Visual > Meshes`: **Scale 0.3**, **Euler (0, 0, 0)**. Leave Offset at (0, 0.12, 0.02).
4. Play and look at a Stalker, which spawns with eyes, or buy eyes in the mutation menu (Tab).
5. Tune while playing.

| You see | Change on `BP_Eyes.asset` |
|---|---|
| Overlapping the jaws, or too high | Offset Y, in steps of 0.03 |
| Sunk into the face | Offset Z up, in steps of 0.02 |
| Floating in front of the face | Offset Z down, in steps of 0.02 |
| Too big or too small | Scale, in steps of 0.05 |
| Looking backward | Euler Y 180 |
| Looking at the sky or the ground | Euler X: positive tips them down, negative up, in steps of 10 |
