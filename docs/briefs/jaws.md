# Brief: jaws

The first mutation most demons take (GAME_DESIGN.md, body parts): a big tooth-lined maw that grows over the small mouth on the front of the core and makes Bite hit harder. It is the part the player looks at most, on every enemy that comes at them. Jaws are destroyed at zero HP, not severed: they vanish and leave the core's own mouth.

- **Part id:** `part.jaws`
- **Socket:** Head, on the upper front of the core. The eyes share it and sit above the jaws.
- **Folder:** `Assets/_Project/Art/Models/BodyParts/`
- **Files:** `BP_Jaws_Intact.fbx`; later `BP_Jaws_Wounded.fbx` and `BP_Jaws_Mangled.fbx`. No stump.
- **Tool:** Meshy, Text to 3D (ASSET_GUIDE.md, step 3). Symmetry Auto or On.
- **Budget:** 2,000 to 5,000 triangles.

## Shape

- About 0.4 wide, 0.35 from front to back and 0.28 tall, for a core 1 tall.
- A split mandible: upper and lower jaw joined at the back, open a little so the teeth show. Rows of uneven teeth, long fangs at the front, crushing teeth further back, slick gums.
- The back end is a ragged ring of flesh that merges into the core and covers its small mouth.
- **Generate it facing you, the way it sits on a head:** the open maw toward you, the flesh ring at the back.

## How the game attaches it

- **Pivot.** The game takes the bottom center of the jaws, under the lowest teeth and halfway back, and sets it onto the Head anchor on the upper front of the core (D-088).
- **Size.** The game scales every model so its longest side is 1. **Scale 0.4** makes the jaws 0.4 body heights wide.
- **Direction.** With **Euler (0, 0, 0)** the jaws face forward, the way they faced you in Meshy. If they face backward, set Euler Y to 180.
- **Offset (0, -0.12, 0.05)** moves them down onto the core's mouth and a little forward. Y moves up or down, positive Z moves forward out of the face.
- The jaws keep their own texture (Maw role); the owner color does not tint them. When a demon bites, the jaws snap open and shut by themselves.

## Textures

Teeth off-white to yellow with dark roots, around 170 to 220 of 255; gums and flesh red, around 110 to 150 in red; high gloss on gums and saliva, matte teeth. No baked shadows (core-blob.md, "Textures").

## Prompt words

```
realistic demon jaws facing the viewer, split mandible slightly open, rows of uneven jagged teeth, long fangs at the front, slick red gums, raw ragged flesh ring at the back, wet, gory, body horror, game asset, PBR, detached part on its own, red flesh and yellowed teeth, evenly lit texture, no baked shadows, not cartoon
```

Negative:

```
full head, skull, eyes, tongue sticking out, cartoon, stylized, perfect symmetric teeth, base, pedestal, black texture
```

## Damage states (later)

- **Wounded:** teeth broken off, a split through the lower jaw, gums torn.
- **Mangled:** half the upper jaw gone, the hinge exposed, remaining teeth bent, flesh hanging.

Both need the same shape and size as the intact jaws, so they wait for Blender (ASSET_GUIDE.md, 2.9).

## Checks before import

- Triangles between 2,000 and 5,000.
- One maw facing you, flesh ring at the back, no skull, no eyes, no base.
- Teeth light, flesh clearly red in the preview, not near black.
- License entry in `docs/ASSET_LICENSES.md`.

## In Unity

1. Rename the file to `BP_Jaws_Intact.fbx` and copy it with its textures into `Assets/_Project/Art/Models/BodyParts/`.
2. Run `Demon Fighter > Generate > Placeholder Assets`, then `Demon Fighter > Art > Validate Art Assets`.
3. Select `BP_Jaws.asset` in `Assets/_Project/Content/BodyParts` and set `Visual > Meshes`: **Scale 0.4**, **Euler (0, 0, 0)**, **Offset (0, -0.12, 0.05)**.
4. Play and look at a Brute, which spawns with jaws, or buy jaws in the mutation menu (Tab). With test mode on (Settings, Playtest), Biomass is no limit.
5. Tune while playing; values on assets stay after Play Mode, and new demons use them at once.

| You see | Change on `BP_Jaws.asset` |
|---|---|
| Too high or too low on the face | Offset Y, in steps of 0.03 |
| Sunk into the face | Offset Z up, in steps of 0.03 |
| Floating in front of the face | Offset Z down, in steps of 0.03 |
| Too big or too small | Scale, in steps of 0.05 |
| Facing backward | Euler Y 180 |
| Tilted up or down | Euler X: positive tips the front down, negative up, in steps of 10 |
