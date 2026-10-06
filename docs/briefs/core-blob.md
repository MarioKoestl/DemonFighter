# Brief: core blob

The body every demon starts as (GAME_DESIGN.md, "The body: parts and sockets"): a formless lump of demon flesh with no limbs and no eyes, but with a mouth of its own, because every blob can bite from birth (GAME_DESIGN.md, skills: Bite comes from the core jaws). The Jaws mutation later grows a much bigger maw over this mouth. All other parts plug into the core at its sockets. This is the most visible asset in the game, so it gets the biggest budget.

- **Part id:** `part.core`
- **Folder:** `Assets/_Project/Art/Models/Core/`
- **Files:** `BP_Core_Intact.fbx`; later `BP_Core_Wounded.fbx` and `BP_Core_Mangled.fbx`. No stump: a destroyed core is death, and the mangled mesh becomes the corpse.
- **Tool:** Meshy, Text to 3D (ASSET_GUIDE.md, step 2).
- **Budget:** 8,000 triangles per state.

## Shape

- An upright lump, taller than wide: about 0.6 to 0.8 wide and 0.6 to 0.7 deep for a height of 1. It sits on the ground under its own weight: a flattened base, the back slightly higher than the front, a bulge forward around the mouth. Think of a skinned, muscular slug that reared up, not a ball.
- **Mouth:** on the upper front, 0.55 to 0.7 of the height up, about 0.3 wide and 0.12 tall. A ragged horizontal slit with thick torn lips, open a little so a row of small, uneven teeth shows. Modest on purpose: the Jaws mutation grows the big tooth-lined maw over it.
- Subtle asymmetry reads as alive: one bulge larger, veins not mirrored. Symmetry off in Meshy.
- No limbs, no eyes, no tail, no spikes, no base or pedestal. Every other part is a separate mesh.

## What Meshy does on its own

You set no pivot, axis or unit in Meshy. Its files always come out like this, and the game handles all of it (D-088):

- Meshy puts the origin sometimes at the bottom and sometimes in the middle of a model, and gives models different sizes. The game ignores both: it takes the bottom center of the model as it stood in Meshy as its pivot and scales its longest side to 1. For the core the base stands on the ground anyway, and the core is sized to the body.
- The file carries Blender's upright turn and a centimeter scale on its object. The binder reads both and puts them underneath the fit knobs, so the blob stands upright with every knob at zero.
- The front is the side that faced you in Meshy's viewer. If the mouth ends up at the back in the game, set Euler Y to 180 (see "In Unity").

## Textures

Meshy's PBR textures (base color, metallic, roughness, and normal when offered) are all the game needs. Check two things in the preview:

- **Brightness.** The base color is the flesh under even light, without baked shadows. Red muscle should sit around 110 to 150 of 255 in its red channel. A texture that is mostly below 30 renders as a black shape in the cave, because the darkness comes from the lighting (ASSET_PIPELINE.md, "World surfaces", D-087). The second core averages 126 in red and reads well; the first averaged 65 and read too dark.
- **Metallic** close to zero: flesh is not metal.

## Prompt words

```
realistic demon flesh blob, formless, upright lump, wet translucent membrane over red muscle, visible veins, raw, slick, gory, body horror, ragged mouth on the upper front, slightly open, small uneven jagged teeth, thick torn lips, no limbs, no eyes, flattened base, PBR textures, game asset, deep red flesh with plum and bruise purple veins, evenly lit texture, no baked shadows, not cartoon, not stylized
```

Negative:

```
cartoon, cute, smooth plastic, symmetric, eyes, limbs, wings, horns, tongue, huge jaws, wide open mouth, base, pedestal, black texture
```

## Sockets

Sockets are the places where parts grow out of the core. They already exist as anchors on `BP_Core.asset` under `Visual > Anchors`, so you do not model them. Positions are in body units: 1 is the body height, X to the right, Y up, Z forward, measured from the center of the base.

| Socket | Default position (x, y, z) | Points | Parts |
|---|---|---|---|
| Head | (0, 0.62, 0.28) | forward | Jaws, Eyes |
| Limb, first copy | (0.29, 0.52, 0.06) | right | Arm |
| Limb, second copy | (-0.29, 0.52, 0.06) | left | second Arm |
| Locomotion | (0, 0.04, 0) | down | Legs |
| Hide | (0, 0.55, -0.28) | back | Thick Hide, Plates, Elastic Tissue, Spines |
| Tail | (0, 0.3, -0.28) | back and down | Tail |

The game puts a part's origin onto its anchor and turns the part so it points where the anchor points. If the core's shape leaves an anchor inside the flesh or in the air, move the anchor. For example, if the arms sink into a wide blob, change the Limb X values from 0.29 and -0.29 to 0.33 and -0.33. An anchor serves every part in its socket, on every demon.

Someone who models in Blender can instead place empties named `Socket_Head`, `Socket_LimbL`, `Socket_LimbR`, `Socket_Locomotion`, `Socket_Hide` and `Socket_Tail` in the model, their +Z pointing away from the body; the binder then writes the anchors from them. Meshy cannot do this, and the anchors above are enough. The validator's warning "no Socket_ transforms" is expected.

## Damage states (later)

- **Wounded:** two or three bite-shaped craters torn into the sides, raw red muscle and torn membrane flaps, the silhouette still whole.
- **Mangled:** large chunks missing, the muscle laid open, bone-white gristle showing, membranes hanging. Still recognizably the same shape, about the same bounds.

Both need the same origin, size and mouth position as the intact mesh, which Meshy cannot guarantee. They wait for Blender (ASSET_GUIDE.md, 2.9). Until then the game shows damage on the intact mesh: wounded parts darken, and blood soaks into the skin where hits land.

## Checks before import

- Triangles at or under 8,000. Use Remesh in Meshy if above.
- One upright lump with a mouth on the upper front and small teeth showing. No eyes, no limbs, no base.
- The flesh is clearly red in the preview, not near black.
- License entry in `docs/ASSET_LICENSES.md`: tool, date, plan, terms, commercial use yes.

## In Unity

1. To replace a core, first delete the old one in Unity's Project window: the `.fbx`, its `.fbm` folder, its material in `Materials`, and the loose texture files. Unity removes their `.meta` files with them.
2. Rename the new file to `BP_Core_Intact.fbx` and copy it with its textures into the folder (ASSET_GUIDE.md, 2.6).
3. Run `Demon Fighter > Generate > Placeholder Assets`, then `Demon Fighter > Art > Validate Art Assets`.
4. Play. Every demon wears the core, upright, as tall as its body size. The player's teal and the AI tier colors are a light tint on the texture (`Texture Tint Blend` in `DemonViewSettings.asset`). The placeholder capsule and its snout disappear.
5. The fit knobs on `BP_Core.asset` under `Visual > Meshes` normally stay at zero. If the mouth faces backward, set Euler Y to 180. If the blob floats or sinks, change Offset Y in small steps such as 0.02.
