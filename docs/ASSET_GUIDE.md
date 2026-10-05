# Asset guide: your first meshes, sounds and music, step by step

This guide is for Mario. It assumes you have never made a game asset. It walks you through every click from an empty account to a model, a sound and a music track running in the game, and the paperwork that keeps the game sellable.

The game already runs completely with placeholders: capsules for bodies, synthesized beeps and drones for sound. Nothing breaks while you work. Every file you add replaces a placeholder, one at a time, and you can stop after any step.

Prices and license terms below were checked on 2026-10-05. They change often. Before you pay, read the current terms page of the service and save a copy (step 1.3).

## What you do and what you do not do

- **You do not write the briefs.** They are already written: `docs/briefs/core-blob.md`, `jaws.md`, `arm.md`, `legs.md`, `hide-thick.md`. A brief is a recipe. You copy its prompt into an AI tool and check the result against its checklist.
- **You do not model anything by hand.** An AI tool makes the 3D model from the text. You pick the best result and download it.
- **You do not edit code.** You copy files into folders, click two menu entries in Unity and drag clips into fields in the Inspector.
- **You keep a license record** for every file you add. That is the one thing nobody else can do for you.

## Overview: the order of work

| Step | What | Tool | Cost | Time |
|---|---|---|---|---|
| 1 | One-time setup: folders, Audacity, license proof folder | Windows, Audacity | free | 20 min |
| 2 | Core blob model | Meshy | about 10 to 20 USD for one month | 1 to 2 h the first time |
| 3 | Jaws, arm, legs, hide models | Meshy (same month) | included | 30 min each |
| 4 | Sound effects (18 sounds) | Sonniss, Freesound, Pixabay; optional ElevenLabs | free; optional about 6 USD | 3 to 4 h |
| 5 | Two ambient loops (cave, lava) | same as step 4 | free | 1 h |
| 6 | Three music tracks (menu, calm, combat) | Suno | about 10 USD for one month | 1 to 2 h |
| 7 | License entries | text editor | free | 10 min per batch |
| 8 | Commit | Git, as usual | free | 10 min |

You can do steps 4 to 6 before steps 2 and 3. They do not depend on each other.

Total money: roughly 25 to 40 USD if you subscribe to Meshy and Suno for one month each and cancel after downloading. Everything else is free.

---

## Step 1: one-time setup

### 1.1 The ArtSource folder

Raw downloads, prompts and license proofs do not go into Git (D-021, GitHub's file quotas). They go into a folder next to the code that Git ignores.

1. Open Windows Explorer and go to `C:\Development\DemonFighter`.
2. Create a folder named `ArtSource`. Git already ignores it.
3. Inside it create these folders:
   - `models` (the zip files the 3D tool gives you, unchanged)
   - `audio` (original sound files, before you trim them)
   - `music` (original music downloads)
   - `licenses` (screenshots and PDFs of terms and invoices)
4. Inside `ArtSource` create a text file `prompts.md`. Every time you generate something, paste the date, the tool, the exact prompt and the settings into it. This is your proof of how each asset was made.
5. Back the folder up. The simplest way: right-click `ArtSource`, choose "Copy", paste it into your OneDrive folder once a week. Or copy it to a USB drive. If your PC dies, Git has the game but not these originals.

### 1.2 Install Audacity

Audacity is a free sound editor. You need it to cut, fade and level sounds.

1. Go to https://www.audacityteam.org and click "Download".
2. Install with the default options.
3. Start it once so Windows registers it. That's all for now.

### 1.3 How to keep license proof

For every paid service and every download site:

1. Open the service's "Terms of Service" or "License" page (usually linked at the bottom of the website).
2. Press Ctrl+P and choose "Microsoft Print to PDF", save it into `ArtSource\licenses\` with the date in the name, for example `2026-10-12_meshy_terms.pdf`.
3. Do the same for your invoice or subscription confirmation email.

Why: years from now Steam, a publisher or a lawyer may ask "were you allowed to use this?". The PDF from the day you generated the asset is the answer.

### 1.4 The two Unity menu entries you will use

Open the project in Unity as usual. In the menu bar at the top you will find:

- **Demon Fighter > Generate > Placeholder Assets**: after you add or remove a model, this connects every model file to its body part. It also fills in anything missing. It is safe to run as often as you like.
- **Demon Fighter > Art > Validate Art Assets**: checks names, sizes and license entries and prints a report into the Console window (Window > General > Console) and into `TestResults\art-validation.txt`.

To test anything, open the scene `Assets/_Project/Scenes/Bootstrap.unity` (double-click it in the Project window) and press the Play button at the top. The main menu appears; click New Run.

---

## Step 2: the core blob model

The core blob is the body every demon has. It changes the look of the whole game at once, so it comes first.

### 2.1 Which tool and which plan

**Meshy** (https://www.meshy.ai) is the recommendation. It turns text into a textured 3D model, lets you set the triangle count, and exports FBX, the format our project imports without extras.

The plans as of October 2026:

| Plan | Price | Downloads | License |
|---|---|---|---|
| Free | 0 | You cannot download models made on the free plan | CC BY 4.0 (public, credit required) |
| Pro | 20 USD per month (first month 50 percent off) | Unlimited | Private, commercial use allowed |

So you need **Pro for one month**. Make all five models in that month, download everything, then cancel in the account settings. The models you made while subscribed stay yours to use.

Alternative: **Tripo** (https://www.tripo3d.ai). Its free plan is non-commercial and FBX export needs a paid plan too, so it costs about the same. Try it only if Meshy does not give you a good blob after many attempts.

Do not use the free plan of any tool for things you ship. Free outputs are public or non-commercial.

### 2.2 Create the account

1. Go to https://www.meshy.ai and click "Sign up". Use the email you want on invoices.
2. Choose Pro. Pay.
3. Do step 1.3: save the terms page and the invoice as PDF into `ArtSource\licenses\`.

### 2.3 Generate the blob

1. Open `docs/briefs/core-blob.md` in any text editor (or on GitHub). Read it once completely, it is short.
2. In Meshy, open the workspace and choose **Text to 3D**.
3. Copy the text under "Prompt words" from the brief into the prompt field.
4. If there is a field "Negative prompt", copy the "Negative" line into it.
5. Settings (the exact names change between Meshy versions; look for these):
   - Art style or quality: **Realistic**.
   - Topology: **Triangle**.
   - Target polycount: **8,000** for the core. If the generate screen has no such field, you set it later with Remesh (step 2.4).
   - Symmetry: **Off** for the blob (the brief wants it a bit uneven).
   - PBR or "Generate PBR maps": **On**. This gives the extra textures that make it look wet and lumpy.
6. Click Generate. Wait a minute or two.
7. Judge the result with the checklist from the brief:
   - One solid upright lump, no legs, no arms, no eyes.
   - No ground plate or pedestal underneath.
   - Front side recognizable: a mouth on the upper front, open a little, small teeth showing. Every blob can bite from birth; the big jaws come later as a mutation.
   - The flesh is clearly red in the preview, not near black. A dark texture renders as a black shape in the cave.
   - Looks like wet raw flesh, not plastic, not cartoon.
8. If it fails, generate again. Change one or two words of the prompt if the same mistake repeats, for example add "no base, no pedestal" if it keeps standing on a disc. Expect 3 to 6 tries. Each try costs credits; Pro has plenty for this.
9. When you have a good shape, start the texture step if Meshy did not texture it yet ("Texture" or "Refine"), with the same prompt and PBR on.
10. Copy the final prompt, the settings and the date into `ArtSource\prompts.md`.

### 2.4 Check the triangle count

1. Meshy shows the face or triangle count of the model in its details panel.
2. If it is above 8,000 for the core (5,000 for parts), open **Remesh**, choose Triangle, set the target to 8,000 (or 5,000), apply.
3. Look at it again. If Remesh made it ugly, try a slightly higher number. The validator will tell you if you are over budget.

### 2.5 Download

1. Click Download, choose format **FBX**.
2. You get a zip file. Save it into `ArtSource\models\` and give it a clear name, for example `core_blob_2026-10-12.zip`.
3. Unzip it into a new folder next to it. Inside you find one `.fbx` file and several `.png` files (the textures).

### 2.6 Put it into Unity

1. Rename the `.fbx` file to exactly **`BP_Core_Intact.fbx`**. The name tells the game which part it is and which damage state. Spelling and the underscores matter. Do not rename the `.png` files: the FBX finds its textures by their original names.
2. Open Unity with the project.
3. In Windows Explorer, copy the `.fbx` and all its `.png` files into `C:\Development\DemonFighter\Assets\_Project\Art\Models\Core\`.
4. Switch to Unity. It notices the new files and imports them; you see a progress bar. Our import rules set every import option for you.
5. In Unity's Project window, go to `Assets/_Project/Art/Models/Core`. Click the png whose name contains "normal". In the Inspector on the right, "Texture Type" should say **Normal map**. If it says "Default", change it to "Normal map" and click "Apply" at the bottom of the Inspector. (The import rules do this automatically when the file name contains "normal".)
6. Click **Demon Fighter > Generate > Placeholder Assets**. In the Console you see two lines:
   - `Bound 1 state mesh(es) from 1 file(s) to part.core`
   - `Switched .../BP_Core_Intact-....mat to DemonFighter/DemonSkin` (the material now gets blood)
7. Click **Demon Fighter > Art > Validate Art Assets**. Expected right now:
   - ERROR "no entry in docs/ASSET_LICENSES.md": you fix that in step 7.
   - WARNING "no Socket_ transforms": fine, the game uses default socket positions.
   - WARNING "no _Wounded mesh" and "no _Mangled mesh": fine for now (see 2.9).
   Any other error: see the troubleshooting table at the end.

### 2.7 Look at it in the game

1. Open `Assets/_Project/Scenes/Bootstrap.unity`, press Play, click New Run.
2. Every demon, you included, now wears the blob. The player keeps a teal tint, AI demons their tier color; the texture shows through.
3. Bite a blob: blood soaks into the skin where you hit.

### 2.8 If it looks wrong: the three fit knobs

1. Stop Play Mode.
2. In the Project window open `Assets/_Project/Content/BodyParts/BP_Core.asset` (click it once).
3. In the Inspector expand **Visual**, then **Meshes**. You see Intact, Wounded, Mangled, Stump, Material, Clip, and the three knobs:
   - **Scale**: size multiplier. For the core you rarely need it, because the game sizes the core to the body automatically.
   - **Offset**: moves the mesh. Values are in body units: 0.1 means one tenth of the body height.
   - **Euler**: turns the mesh, in degrees.
4. Common fixes for the core:
   - The mouth (front) points backward: set Euler Y to 180.
   - The blob lies on its side or face: set Euler X to 90 or -90. A Meshy export stands upright by itself, because the binder reads the upright turn Blender stores in the file (D-088).
   - It floats or sinks a little: Offset Y, small steps like 0.05.
5. Press Play again and look. Values you change on these assets stay after Play Mode ends (unlike things in a scene), so you can also tune while playing; existing demons keep the old fit, new ones and the mutation menu preview (Tab) use the new one.

### 2.9 Damage states: later, not now

The brief asks for Wounded and Mangled versions. Do not try them in your first round. The game already shows damage on every part: wounded parts darken, mangled ones darken further and turn red, and the skin shader paints blood where hits land.

Separate damage meshes are hard with AI tools: a second generation comes out with a different size and pose, and all states of a part share one Scale and Offset. When the five intact models are in, there are two ways forward, and both need a short session with Claude first:

- Texture-only damage (same mesh, bloodier texture, which Meshy's retexture can do): needs a small code change so each state can have its own material.
- Real damaged meshes: needs Blender to line them up. Claude can walk you through that when you get there.

Same for **stumps** (arm and legs): a stump is a damaged version of the same part, so it belongs to the later round.

---

## Step 3: the body parts

Same process as step 2, with these differences. Every part model goes into `Assets/_Project/Art/Models/BodyParts/`; each brief names its file, its pose, its prompt words and the values to set in Unity.

| Brief | File name | Pose in Meshy | Triangles |
|---|---|---|---|
| `jaws.md` | `BP_Jaws_Intact.fbx` | facing you | 5,000 |
| `eyes.md` | `BP_Eyes_Intact.fbx` | facing you | 3,000 |
| `arm.md` | `BP_Arm_Intact.fbx` | upright on the shoulder | 5,000 |
| `legs.md` | `BP_Legs_Intact.fbx` | standing on its feet | 5,000 |
| `tail.md` | `BP_Tail_Intact.fbx` | upright on the root | 5,000 |
| `hide-thick.md` | `BP_Hide_Thick_Intact.fbx` | lying flat, hollow side down | 5,000 |
| `plates.md` | `BP_Hide_Plates_Intact.fbx` | lying flat, hollow side down | 5,000 |
| `spines.md` | `BP_Spines_Intact.fbx` | lying flat, spines up | 5,000 |
| `elastic-tissue.md` | `BP_Hide_Elastic_Intact.fbx` | lying flat, hollow side down | 5,000 |

Settings in Meshy: the Symmetry setting each brief names. Everything else as in step 2.3. The briefs README (`docs/briefs/README.md`) explains the poses in one table.

### 3.1 Where the part sits

Each part grows out of a fixed spot of the blob, its socket: jaws on the upper front, arms on both flanks, legs underneath, hide on the back. `core-blob.md` lists them under "Sockets". The game puts the part's origin onto that spot and turns the part so it points away from the body. The second arm goes onto the other flank by itself.

The game takes the bottom center of each model, as it stood in Meshy, as the point where the part meets the body, wherever Meshy put the origin. So the pose in Meshy decides how a part attaches, and each brief names it: upright on the end that touches the body (arm, tail), facing you (jaws, eyes), standing on its feet (legs), or lying flat with the body side underneath (hide, plates, spines, elastic tissue). The prompt words in each brief already ask for the pose. You never need Blender for it. Legs are the one part the body rests on: the game lifts the blob until their feet touch the ground, so their Offset only says how far they reach out below it (D-094).

### 3.2 Fit it on the part asset

After Generate, select the part asset in `Assets/_Project/Content/BodyParts` (`BP_Arm.asset` for the arm) and set `Visual > Meshes`. Each brief lists its values under "In Unity".

- **Scale:** the part's length in body heights. The game scales every model to 1 on its longest side, so the arm gets 0.55.
- **Euler:** turns the part around the point where it meets the body. X tips the upright part over (90 points it out of the body), then Y swings it around the vertical (-90 points an arm forward). The arm brief has a table of values.
- **Offset:** moves the part along its socket (D-088). Positive Z pushes it out of the body, negative Z into it, Y moves it up. Use small steps, 0.02 to 0.05.
- The second arm takes every value mirrored, so you set them once.

### 3.3 Look at it in the game

1. Play and find a demon with the part. Stronger AI demons spawn with parts: Hunters carry an arm, Stalkers legs and eyes, Brutes an arm, jaws and thick hide. Or eat a few blobs and buy the part yourself in the mutation menu (Tab).
2. Tune while playing. Values on assets stay after Play Mode, and new demons and the menu preview use them at once. The brief has a table of common fixes.
3. If the validator says "the pivot lies inside the mesh", the part was not generated standing on the end that touches the body. Generate it again with that end at the bottom.

### 3.4 Order and speed

Do the jaws next, then arm, legs, hide. After the first one, each takes about 30 minutes including tries.

---

## Step 4: sound effects

### 4.1 What the game needs

The game has 18 sound effects. Each one is an asset in `Assets/_Project/Content/Audio/` named `AE_<Name>` that holds one or more sound clips. Right now each holds a synthesized placeholder.

| Asset | What happens in the game | Length | Search words | AI prompt (if you use ElevenLabs) |
|---|---|---|---|---|
| `AE_Bite` | jaws snap into flesh | 0.3 to 0.6 s | bite, chomp, teeth, flesh bite, gore crunch | a monstrous creature bites into raw flesh, wet crunch of teeth snapping shut, close up, gory |
| `AE_Claw` | claw slash | 0.3 to 0.5 s | claw swipe, slash flesh, rip | sharp claws slash through the air and rip into flesh, fast swipe with a wet tear |
| `AE_Grab` | grabbing a body | 0.3 to 0.6 s | grab flesh, squelch, wet grab | a heavy wet hand grabs slimy flesh, squelch and tight grip |
| `AE_Lunge` | body leaps forward | 0.4 to 0.7 s | heavy whoosh, creature lunge | a large creature lunges forward, heavy fast whoosh with a grunt of effort |
| `AE_TailSwing` | tail whip | 0.4 to 0.6 s | whip, heavy swing, whoosh crack | a thick muscular tail whips through the air and cracks |
| `AE_WetImpact` | any hit that lands | 0.2 to 0.4 s | flesh impact, body hit, meat punch, gore hit | a heavy blow hits raw meat, wet thud with a splatter |
| `AE_Footstep` | each step of every demon | 0.1 to 0.25 s | footstep gravel, heavy step dirt, creature step | a heavy bare clawed foot steps on ash and gravel, single step |
| `AE_Eat` | eating a corpse | 0.5 to 1 s | eating, chewing, gore eat, slurp | a monster greedily chews and tears raw meat, wet chewing and slurping |
| `AE_MutationStart` | a body starts to mutate | 1 to 2 s | flesh morph, transformation, bone stretch | flesh bubbling and stretching, bones cracking as a creature body mutates, wet and organic |
| `AE_MutationComplete` | the new part is done | 0.5 to 1 s | flesh pop, squelch | a final wet pop and crack as new flesh settles into place |
| `AE_Evolved` | evolution | 1.5 to 3 s | monster roar, transformation roar | a demon evolves, deep reverberant roar with bones cracking and flesh tearing |
| `AE_LevelUp` | you gain a level | 0.4 to 0.8 s | dark chime, low bell, horror stinger | short dark ominous chime, low metallic bell, game level up cue, no melody |
| `AE_ThreatRise` | threat level rises | 1 to 2 s | horror sting, deep boom, war horn | a distant deep horn and a low boom echoing through a huge cave, ominous warning |
| `AE_PlayerDeath` | you die | 2 to 3 s | creature death, monster dying | a monster dies in agony, gurgling last breath, heavy body collapses, cave echo |
| `AE_DemonDeath` | an enemy dies | 1 to 1.5 s | creature death squeal, monster dies | a demon creature shrieks and dies with a wet gurgle, body hits the ground |
| `AE_Sever` | a limb is torn off | 0.4 to 0.8 s | bone break, flesh rip, dismember, gore tear | a limb is torn off a body, loud bone snap and wet flesh rip |
| `AE_PartDestroyed` | eyes or hide destroyed | 0.3 to 0.6 s | crunch, squish, gore splat | flesh and cartilage crushed, wet crunch and squish |
| `AE_Burn` | a demon standing in lava or on a fissure | 0.5 to 1 s | sizzle, burning flesh, hiss, frying meat, fire crackle | raw flesh sizzling on glowing lava, loud hiss with crackling fat, short |

Tip: 2 to 4 slightly different clips per sound (for example three bites) make it sound much less repetitive. The game picks one at random each time and varies pitch and volume a little. Bigger demons automatically sound lower.

### 4.2 Where to get sounds (best first)

1. **Sonniss GDC Game Audio Bundle** (https://gdc.sonniss.com). Free, professional quality, commercial use, no credit needed. Download the 2026 bundle (about 7.5 GB, five zip parts); older yearly bundles are on the same site and have the same license. Unzip into `ArtSource\audio\sonniss\`. Search the folders in Windows Explorer for the search words in the table ("flesh", "gore", "squelch", "bone", "creature", "impact"). The license file is inside the download: copy it to `ArtSource\licenses\`.
2. **Freesound** (https://freesound.org). Huge, community-made. Make a free account (needed to download). After searching, click **Creative Commons 0** in the "licenses" column on the right: CC0 sounds need no credit and allow anything. Avoid "Attribution NonCommercial" (forbidden for a sold game). "Attribution" (CC BY) is allowed but you must credit the author; write it down in the license entry if you use one.
3. **Pixabay sound effects** (https://pixabay.com/sound-effects). Free, commercial use allowed, no credit needed. Quality varies.
4. **ElevenLabs Sound Effects** (https://elevenlabs.io/sound-effects). Makes a sound from your text (use the prompts from the table). The free plan is non-commercial, so you need the Starter plan (about 6 USD per month) for anything you ship. Good for things you cannot find, like a demon evolving. Generate several, download the best.

Never use: sounds ripped from YouTube or other games, sites without a clear license, anything marked "non-commercial" or "editorial only".

### 4.3 Prepare a sound in Audacity

For every sound:

1. Audacity: File > Open, pick the sound.
2. Find the part you want. Play with the space bar. Drag with the mouse over the part you want to keep.
3. Edit > Remove Special > **Trim Audio** (Ctrl+T). Everything else is removed.
4. Select everything (Ctrl+A). If the file is stereo (two waveforms): Tracks > Mix > **Mix Stereo Down to Mono**. Effects play in 3D, so mono is right.
5. Select the last 0.05 to 0.2 seconds: Effect > Fading > **Fade Out**. This avoids a click at the end.
6. Select everything: Effect > Volume and Compression > **Normalize**, peak -1.0 dB, OK. Now all sounds have a similar loudness.
7. File > Export Audio. Format **WAV (Microsoft) signed 16-bit**. Name it after the asset with a number: `S_Bite_01.wav`, `S_Bite_02.wav`, `S_Footstep_01.wav`. The `S_` at the start matters: the import rules treat `S_` files as short effects.
8. Save the export into `ArtSource\audio\ready\` first.

### 4.4 Put sounds into Unity

1. Create the folder `Assets\_Project\Audio\Effects\` in Windows Explorer (or right-click in Unity's Project window > Create > Folder).
2. Copy your `S_...wav` files into it. Unity imports them.
3. In the Project window open `Assets/_Project/Content/Audio/AE_Bite.asset` (type `AE_Bite` into the search field of the Project window to find it fast).
4. In the Inspector you see **Clips** with one element (the placeholder `S_Bite`). Drag your `S_Bite_01` from the Project window onto that element to replace it. To add more variants, click "+" under the list and drag the next clip into the new slot.
5. Adjust **Volume** if it is too loud or too quiet compared to the others (0 to 1).
6. Repeat for every sound.
7. Test: press Play, bite something.

The placeholder files in `Assets/_Project/Audio/Generated/` can stay. Nothing uses them once you replaced them in every asset.

---

## Step 5: the two ambient loops

| Asset field | What | Length | Search words | AI prompt |
|---|---|---|---|---|
| Cavern drone (`L_CavernDrone`) | the background hum of the cave | 30 to 60 s | cave ambience, underground room tone, dark drone | deep dark cave ambience, low rumbling drone, distant dripping water, faint wind, seamless loop |
| Lava loop (`L_Lava`) | bubbling near lava pools | 15 to 30 s | lava, magma bubbling, boiling mud | bubbling lava pool, thick magma bubbles popping, low fiery rumble, seamless loop |

1. Get them as in step 4.2. Many ambiences in Sonniss and on Freesound are labeled "loop" or "seamless"; prefer those. In ElevenLabs, switch on the loop option if it is offered.
2. Audacity: trim to 15 to 60 seconds, keep stereo, Normalize to -3 dB. If the end and start do not match, select the first 0.5 s, Fade In; select the last 0.5 s, Fade Out. A short dip at the loop point is fine for a drone.
3. Export as WAV named `L_CavernDrone.wav` and `L_Lava.wav` (the `L_` prefix makes Unity stream them instead of loading them fully).
4. Copy into `Assets\_Project\Audio\Ambient\`.
5. Open `Assets/_Project/Content/Biomes/BI_AshCavern.asset`. Under **Audio** drag the drone into **Ambient Loop** and the lava into **Lava Loop**.
6. Play: the drone is everywhere, the lava gets louder as you walk toward a lava pool.

---

## Step 6: music

### 6.1 What the game needs

Three instrumental tracks, 1.5 to 3 minutes each:

| Track | Plays when | Mood |
|---|---|---|
| Menu | main menu | dark, slow, ominous, inviting |
| Calm | in a run while nothing attacks you | tense, quiet, eerie, exploring a cave |
| Combat | in a run while you fight (and 5 s after) | aggressive, drums, relentless |

The game crossfades between Calm and Combat by itself.

### 6.2 Which tool

**Suno** (https://suno.com) is the recommendation: easy, good at dark instrumental music, and the Pro plan (about 10 USD per month) gives commercial use rights for songs you **download while subscribed**, 20 downloads per month (rule since 3 September 2026). The free plan is non-commercial. So: subscribe one month, make and download all three tracks (plus a spare or two), save the terms and invoice (step 1.3), cancel.

Not recommended for now: ElevenLabs Music. Its self-serve plans exclude "Studio Games", which the terms define as monetized games available on more than one platform. Steam-only might be fine, but it is unclear, so leave it unless ElevenLabs confirms in writing.

Note: Suno does not promise that its output never resembles existing music, and record labels have sued AI music companies. Keep the prompt, the date and the terms PDF; that is the best protection an indie developer has.

### 6.3 Make the tracks

1. Sign up at Suno, choose Pro, do step 1.3.
2. Click Create. Switch on **Instrumental** (no vocals). Use the custom or advanced mode where you can type a style description.
3. Style prompts to start with (change words freely):
   - Menu: `dark ambient horror soundtrack, slow ominous drones, deep choir pads, distant metallic scrapes, sparse low piano notes, underground cavern, instrumental, no vocals, 60 bpm`
   - Calm: `dark ambient exploration music, tense and quiet, low drones, subtle heartbeat pulse, eerie textures, cave atmosphere, minimal melody, instrumental, no vocals`
   - Combat: `aggressive dark industrial combat music, pounding tribal war drums, distorted low brass and cello ostinato, horror, relentless, 140 bpm, instrumental, no vocals`
4. Generate several versions of each. Listen in the game's mood: dark cave, gore. Pick one per track.
5. Download as **WAV** (best quality) while your subscription is active. Save into `ArtSource\music\` together with the prompt text and date in `prompts.md`.

### 6.4 Prepare the tracks in Audacity

1. Open the track.
2. Cut silence at the start: select it, press Delete.
3. Select the first 0.5 s: Effect > Fading > Fade In. Select the last 3 s: Fade Out. The game loops each track; the fade makes the restart smooth.
4. Select all: Effect > Volume and Compression > **Loudness Normalization**, "perceived loudness" -18 LUFS. All three tracks then sit at the same volume.
5. File > Export Audio, format **Ogg Vorbis**, quality 6. Names: `M_Menu.ogg`, `M_Calm.ogg`, `M_Combat.ogg` (the `M_` prefix makes Unity stream them).

### 6.5 Put the music into Unity

1. Copy the three files into `Assets\_Project\Audio\Music\`.
2. Open `Assets/_Project/Content/Audio/AudioCatalog.asset`. Under **Music** drag `M_Menu` into **Menu Track**.
3. Open `Assets/_Project/Content/Biomes/BI_AshCavern.asset`. Under **Audio** drag `M_Calm` into **Calm Track** and `M_Combat` into **Combat Track**.
4. Play: the menu track plays in the menu; in the run the calm track plays and fades into combat when something hits you.
5. Volume per category: the Settings menu (Audio tab) has Master, Effects, Ambient and Music sliders.

---

## Step 7: license entries

Every file you add needs an entry in `docs/ASSET_LICENSES.md` (the template is at its top). One entry can cover several files from the same source. The art validator reports an ERROR for each model without an entry; it looks for the file path or file name in the document.

Examples you can copy and adapt:

```
## Core blob model
- Source: Meshy (meshy.ai), Text to 3D, prompt and settings in ArtSource/prompts.md (2026-10-12)
- Date: 2026-10-12
- License: Meshy terms of service, Pro plan at generation time; PDF in ArtSource/licenses/2026-10-12_meshy_terms.pdf
- Commercial use: yes (paid plan)
- Attribution text (if required): none
- Files: Assets/_Project/Art/Models/Core/BP_Core_Intact.fbx and its textures in the same folder

## Sonniss GDC 2026 Game Audio Bundle
- Source: Sonniss, https://gdc.sonniss.com
- Date: 2026-10-14
- License: Sonniss GDC bundle license (royalty-free, commercial, no attribution); copy in ArtSource/licenses/
- Commercial use: yes
- Attribution text (if required): none
- Files: Assets/_Project/Audio/Effects/S_Bite_01.wav (from "<original file name>"), S_Sever_01.wav (from "<original file name>")

## Freesound sound "<title>" by <user>
- Source: https://freesound.org/people/<user>/sounds/<number>/
- Date: 2026-10-14
- License: CC0 1.0
- Commercial use: yes
- Attribution text (if required): none
- Files: Assets/_Project/Audio/Effects/S_Footstep_02.wav

## ElevenLabs sound effects
- Source: ElevenLabs Sound Effects, Starter plan, prompts in ArtSource/prompts.md
- Date: 2026-10-15
- License: ElevenLabs terms, paid plan with commercial license; PDF in ArtSource/licenses/
- Commercial use: yes (paid plan)
- Attribution text (if required): none on paid plans
- Files: Assets/_Project/Audio/Effects/S_Evolved_01.wav, S_MutationStart_01.wav

## Music: menu, calm and combat
- Source: Suno (suno.com), Pro plan, instrumental, prompts in ArtSource/prompts.md
- Date: 2026-10-16
- License: Suno terms (commercial rights for songs downloaded while subscribed); PDF in ArtSource/licenses/
- Commercial use: yes (downloaded on Pro)
- Attribution text (if required): none
- Files: Assets/_Project/Audio/Music/M_Menu.ogg, M_Calm.ogg, M_Combat.ogg
```

After adding entries, run **Demon Fighter > Art > Validate Art Assets** again: the license errors disappear.

### Steam and AI content

When you set up the Steam store page (milestone M6), Steam's content survey asks whether the game contains AI-generated content that players see or hear. With Meshy models and Suno music the answer is yes, and you describe what was generated. Valve's rules (clarified January 2026) are about generated content that ships in the game, not about tools used behind the scenes. Your license log is exactly the list you need to fill that in honestly.

---

## Step 8: commit

1. In Unity: File > Save Project, then close Unity (so it writes every file).
2. `git status`. You should see your new `.fbx`, `.png`, `.wav`, `.ogg`, the new `.meta` files, the extracted materials in `Materials` folders, the changed part, audio and biome assets, and `docs/ASSET_LICENSES.md`. Commit all of them together; `.meta` files belong to their asset.
3. Git LFS takes care of the big files automatically (fbx, png, wav, mp3, ogg are listed in `.gitattributes`).
4. `ArtSource` does not show up in `git status`. That is correct; it is backed up separately (step 1.1).
5. `ProjectSettings/QualitySettings.asset` should not show as changed: the project puts the graphics preset and vsync back automatically when you leave Play Mode. If it does show up and you did not change quality settings on purpose, undo it with `git restore ProjectSettings/QualitySettings.asset`.
6. Example commit message: `feat(art): core blob and jaws models from Meshy, first sound effects and music` with the license entries in the same commit.

---

## Troubleshooting

| What you see | Why | Fix |
|---|---|---|
| The part is still a capsule or a box | the binder did not find the file | check the exact file name (`BP_Jaws_Intact.fbx`), check the folder, run Generate again |
| Console: "the name does not follow BP_<Part>..." | name typo | rename the file in Unity's Project window (right-click > Rename), run Generate |
| Console: "no body part with id part.xyz" | the part name is wrong | use exactly Core, Jaws, Arm, Legs, Hide_Thick |
| The model is pink | the shader is missing or broken | run Generate; if it stays pink, tell Claude |
| The model is grey, no texture | the png files were not copied with the fbx | copy them into the same folder, then right-click the fbx > Reimport |
| Lighting on the model looks blotchy or inside out | the normal map is set as a color texture | click the normal png, Texture Type: Normal map, Apply |
| The part is invisible | Scale too small, or buried in the body | try Scale 1 and Offset Z 0.3 to find it |
| The part is huge | Scale too big | lower Scale (see 3.2) |
| The part points into the body | wrong direction | Euler Y 180 |
| The part points up or down | axis differs | Euler X 90 or -90 |
| Validator: over the triangle budget | too many triangles | Remesh in Meshy, download again |
| A sound is silent | not assigned or volume 0 | check the `AE_` asset's Clips and Volume; check the Settings menu sliders |
| A sound is far too loud | not normalized | Normalize in Audacity again, or lower Volume on the `AE_` asset |
| The music does not change in fights | tracks not assigned | check Calm Track and Combat Track on `BI_AshCavern.asset` |
| The music restart is abrupt | no fade at the end | Fade Out the last 3 s in Audacity and export again |

If something else goes wrong: copy the red lines from Unity's Console into the chat with Claude.

## Checklist

- [ ] `ArtSource` with models, audio, music, licenses and `prompts.md`; backup set up
- [ ] Audacity installed
- [ ] Meshy Pro for one month; terms and invoice saved as PDF
- [ ] `BP_Core_Intact.fbx` in the game and fitted
- [ ] The part models of step 3 (jaws, eyes, arm, legs, tail, thick hide, plates, spines, elastic tissue) in the game and fitted
- [ ] Meshy cancelled after the downloads
- [ ] 18 sound effects assigned in the `AE_` assets
- [ ] Cave drone and lava loop assigned on `BI_AshCavern.asset`
- [ ] Suno Pro for one month; terms and invoice saved; three tracks downloaded; Suno cancelled
- [ ] Menu, Calm and Combat tracks assigned
- [ ] One license entry per source in `docs/ASSET_LICENSES.md`; validator without errors
- [ ] Committed
