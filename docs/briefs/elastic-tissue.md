# Brief: elastic tissue

A thick layer of rubbery, stretchy tissue that grows over the back of the core and soaks up blows (GAME_DESIGN.md, "Damage model"). Defense: strong against Blunt, neutral against Pierce, weak against Cut. It needs an evolution first. It is destroyed at zero HP, not severed: it tears and vanishes.

- **Part id:** `part.hide.elastic`
- **Socket:** Hide, on the upper back of the core. The socket holds one part at a time: thick hide, plates, elastic tissue or spines.
- **Folder:** `Assets/_Project/Art/Models/BodyParts/`
- **Files:** `BP_Hide_Elastic_Intact.fbx`; later `BP_Hide_Elastic_Wounded.fbx` and `BP_Hide_Elastic_Mangled.fbx`. No stump.
- **Tool:** Meshy, Text to 3D (ASSET_GUIDE.md, step 3). Symmetry Auto.
- **Budget:** 2,000 to 5,000 triangles.

## Shape

- About 0.75 tall, 0.65 wide and 0.15 thick on the body, for a core 1 tall. A curved, padded layer of translucent, glossy membrane stretched over thick bands of tendon that run across it like ribs; it looks like it would bounce back from a hit.
- **Generate it lying flat like an upturned shell: the hollow side underneath, the ribbed side up**, the top edge pointing away from you.

## How the game attaches it

- **Pivot.** The game takes the bottom center, the middle of the hollow side, and sets it onto the Hide anchor on the upper back (D-088).
- **Size.** **Scale 0.75** makes the layer 0.75 body heights tall; the game scales every model so its longest side is 1.
- **Direction.** **Euler (90, 0, 0)** stands the layer up on the back, the ribbed side facing backward. The edge that pointed away from you in Meshy becomes the top.
- **Offset** moves it along the socket: positive Z out of the body, negative Z into it, Y up or down.
- The layer wears the owner color as a light tint (Owner role), so it reads teal on the player and in the tier color on AI demons.

## Textures

Membrane pink to red and slightly translucent-looking, around 120 to 170 in red; tendon bands pale and glossy. Low roughness everywhere: it is wet and stretched. No baked shadows (core-blob.md, "Textures").

## Prompt words

```
realistic layer of rubbery elastic demon tissue lying flat, curved like an upturned shell, hollow side down, glossy translucent membrane stretched over thick bands of tendon running across it like ribs, wet, pink and red flesh, gory, body horror, game asset, PBR, detached part on its own, evenly lit texture, no baked shadows, not cartoon
```

Negative:

```
plastic, jelly, slime, balloon, metal, fur, scales, full creature, cartoon, stylized, base, pedestal, black texture
```

## Damage states (later)

- **Wounded:** two long cuts through the membrane, a tendon band snapped and curling.
- **Mangled:** the membrane torn open in flaps, most bands snapped, flesh beneath showing.

Both need the same shape and size as the intact layer, so they wait for Blender (ASSET_GUIDE.md, 2.9).

## Checks before import

- Triangles between 2,000 and 5,000.
- One curved ribbed layer lying flat, hollow side down, no creature, no base.
- Pink and red, glossy, not near black, in the preview.
- License entry in `docs/ASSET_LICENSES.md`.

## In Unity

1. Rename the file to `BP_Hide_Elastic_Intact.fbx` and copy it with its textures into `Assets/_Project/Art/Models/BodyParts/`.
2. Run `Demon Fighter > Generate > Placeholder Assets`, then `Demon Fighter > Art > Validate Art Assets`.
3. Select `BP_ElasticTissue.asset` in `Assets/_Project/Content/BodyParts` and set `Visual > Meshes`: **Scale 0.75**, **Euler (90, 0, 0)**.
4. Play. Elastic tissue needs an evolution: reach the first evolution level (test mode keeps you alive while you kill for XP), evolve, then buy it in the mutation menu (Tab).
5. Tune while playing.

| You see | Change on `BP_ElasticTissue.asset` |
|---|---|
| Sunk into the back | Offset Z up, in steps of 0.03 |
| Its edges stand off the back | Offset Z down, in steps of 0.03 |
| Too high or too low on the back | Offset Y, in steps of 0.05 |
| Too big or too small | Scale, in steps of 0.05 |
| The hollow side faces outward | Euler X -90 |
| Upside down | Euler (-90, 0, 180) |
