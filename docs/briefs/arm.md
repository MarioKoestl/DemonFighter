# Brief: arm

A clawed forelimb that grows from the flank of the core and grants Claw and Grab (GAME_DESIGN.md, body parts). Demons take one, then a second on the other side. Arms are severed when they drop to zero HP: the detached arm falls to the ground as food.

- **Part id:** `part.arm`
- **Socket:** Limb. The first arm sits on the right flank, the second on the left (core-blob.md, "Sockets").
- **Folder:** `Assets/_Project/Art/Models/BodyParts/`
- **Files:** `BP_Arm_Intact.fbx`; later `BP_Arm_Wounded.fbx`, `BP_Arm_Mangled.fbx` and `BP_Arm_Stump.fbx`.
- **Tool:** Meshy, Text to 3D, like the core (ASSET_GUIDE.md, step 3). Symmetry Auto or On.
- **Budget:** 2,000 to 5,000 triangles.

## Shape

- One arm, about 0.55 of the core's height from shoulder to claw tips, about 0.15 thick at the shoulder.
- Thick at the shoulder, two joints, ending in three or four hooked claws. Sinew and exposed tendon rather than skin. The shoulder end is a ragged ring of flesh that sits against the core.
- **Generate it standing straight up, the shoulder at the bottom and the claws at the top.** This is the one thing that matters for the fit: the game takes the bottom of the arm as its shoulder.
- A slight bend toward the claws is fine; the arm as a whole stands upright.

## How the game attaches it

- **Shoulder.** The game puts the arm's pivot at the bottom center of the arm as it stood in Meshy, wherever Meshy put the origin, and sets that point onto the Limb anchor on the flank of the core (D-088).
- **Size.** The game scales every model so its longest side is 1. **Scale 0.55** therefore makes the arm 0.55 body heights long, whatever size Meshy gave it.
- **Direction.** Euler turns the arm around its shoulder; see the next section.
- **Offset** moves the shoulder along the anchor: positive Z pushes it out of the body, negative Z into it, Y moves it up, and negative X moves it forward.
- **Second arm.** It is the mirror image of the first, a left arm in shape and in pose, so one set of values serves both arms.

## Turning the arm: Euler

With X, Y and Z all at 0, the arm stands on its shoulder exactly as it stood in Meshy: upright, claws at the top. Unity applies the three turns in a fixed order. Leave Z at 0, and read X and Y like this:

1. **X tips the arm over** around its shoulder. At 90 it lies flat and points straight out of the body. Above 90 it hangs down a little; below 90 it points a little up.
2. **Y then swings the tipped arm around the vertical**, like a door on its hinge. At -90 it points forward instead of out, at -45 diagonally forward and out. Positive values swing it backward.

| The arm should | X | Y | Z |
|---|---|---|---|
| stand upright, claws up (as in Meshy) | 0 | 0 | 0 |
| point straight out of the flanks | 90 | 0 | 0 |
| point diagonally forward and out | 90 | -45 | 0 |
| point straight forward | 90 | -90 | 0 |
| point forward and hang a little | 105 | -90 | 0 |
| point forward, turned over so the other side faces down | -90 | 90 | 0 |

With (90, -90, 0), the side that faced you in Meshy's viewer faces the ground. If the claws then curl upward, use the last row instead.

## Textures

Flesh red to purple, claws dark horn with lighter tips, tendon pale and glossy. The core's brightness rule applies: red flesh around 110 to 150 of 255 in its red channel, no baked shadows (core-blob.md, "Textures").

## Prompt words

```
realistic demon arm, single forelimb standing straight up, shoulder at the bottom, claws at the top, sinew and exposed tendon, two joints, hooked claws, ragged flesh ring at the shoulder, raw red muscle, wet, gory, body horror, game asset, PBR, detached limb on its own, red flesh with dark horn claws, evenly lit texture, no baked shadows, not cartoon
```

Negative:

```
hand with fingers, skin, armor, fur, cartoon, stylized, pair of arms, torso, base, pedestal, black texture
```

## Damage states (later)

- **Wounded:** gashes along the forearm, a claw snapped, tendon showing.
- **Mangled:** the forearm hanging by sinew, bone exposed at the elbow, claws broken.
- **Stump:** only the shoulder ring and 0.1 of torn meat, a bone end showing. It stays on the body after the arm is severed.

All three need the same shape and size as the intact arm, which Meshy cannot guarantee, so they wait for Blender (ASSET_GUIDE.md, 2.9). Until then a severed arm leaves no stump on the body.

## Checks before import

- Triangles between 2,000 and 5,000.
- One arm, standing straight up on its shoulder, claws at the top, no base.
- The flesh is clearly red in the preview, not near black.
- License entry in `docs/ASSET_LICENSES.md`.

## In Unity

1. Rename the file to `BP_Arm_Intact.fbx` and copy it with its textures into `Assets/_Project/Art/Models/BodyParts/`.
2. Run `Demon Fighter > Generate > Placeholder Assets`, then `Demon Fighter > Art > Validate Art Assets`.
3. Select `BP_Arm.asset` in `Assets/_Project/Content/BodyParts` and set `Visual > Meshes`: **Scale 0.55**, and **Euler** from the table, for example (90, -90, 0) to point forward. Leave Offset at zero for now.
4. Play and find a demon with an arm: Hunters and Brutes spawn with one. Or eat a few blobs and buy an arm in the mutation menu (Tab); its preview shows your own body up close. With test mode on (Settings, Playtest), Biomass is no limit.
5. Tune while playing. Values on assets stay after Play Mode, and new demons and the menu preview use them at once.

| You see | Change on `BP_Arm.asset` |
|---|---|
| The shoulder is buried in the body | Offset Z up, in steps of 0.03 |
| The arm floats off the flank | Offset Z down, in steps of 0.03 |
| The arms sit too far back or forward on the flanks | Offset X: negative moves both forward, positive back |
| Too long or too short | Scale, in steps of 0.05 |
| It should hang more | Euler X up by 10 to 20 |
| The claws curl upward | The "turned over" row of the Euler table |

The arm wears the owner color as a light tint (Owner role), so the player's arm reads teal and an AI arm reads its tier color.
