# Blender guide

Blender is the tool for rigging and animating the demons (D-100). Not for modeling: the shapes still come from Meshy. This guide teaches exactly what this project needs, in the order you need it. Every chapter ends with a task on our own demon and a "done when", so you learn on the real thing.

Who does what: Claude Code writes Blender Python scripts for the repetitive work (building the skeleton, placing and binding the parts, exporting to Unity). You do what needs eyes: checking the result, fixing weights, posing and timing the animations.

Where files go:

- Blender files (`.blend`): `ArtSource\blender\`. Git ignores `ArtSource`, so back it up like your other source files (ASSET_GUIDE.md, 1.1). These files hold your animation work; losing them hurts.
- Scripts: `Tools\Blender\` in the repository. Claude Code writes them; you never need to edit them.
- Exports for the game: under `Assets\_Project\Art\`. Each chapter says where.

---

## Chapter 1: Install Blender and find your way around

You learn: install Blender, the screen, moving the view, selecting and changing things, the axes of this project. About an hour.

### 1.1 Install

Pick one:

- **In Claude Code:** type `! winget install --id BlenderFoundation.Blender -e` and press Enter. Windows may ask for permission once.
- **By hand:** open blender.org, Download, the Windows installer, run it with the defaults.

Take the current release (4.x or newer). Then tell Claude Code where `blender.exe` landed, usually `C:\Program Files\Blender Foundation\Blender <version>\blender.exe`. It goes into `.claude/settings.local.json` as `BLENDER_EXE`, next to `UNITY_EDITOR`, so the scripts can run Blender without opening it.

### 1.2 First settings

Open Blender, then Edit > Preferences:

1. **Save & Load:** tick Auto Save and set it to every 2 minutes.
2. **Input:** if your mouse has no middle button, tick "Emulate 3 Button Mouse" (then Alt + left button acts as the middle button). If your keyboard has no number pad, tick "Emulate Numpad" (then the number row acts as the number pad).
3. **Interface:** raise Resolution Scale if the text is too small.
4. Close the window; preferences save themselves (or click the menu at the bottom left, Save Preferences).

### 1.3 The screen

- **3D Viewport**, the big area in the middle: the scene. Like Unity's Scene view.
- **Outliner**, top right: every object in the file as a tree. Like Unity's Hierarchy.
- **Properties**, bottom right: the settings of the selected object, in tabs down its left edge. Like Unity's Inspector.
- **Timeline**, at the bottom: frames and the play button. You need it from chapter 4 on.

A new file holds a cube, a camera and a light. Hover the mouse over the 3D Viewport, press A (select all) and X, then Delete. Shortcuts act on the area under the mouse, so keep it over the viewport.

### 1.4 Moving the view

- **Orbit:** hold the middle mouse button and drag.
- **Pan:** Shift + middle button drag.
- **Zoom:** mouse wheel.
- **Views:** number pad 1 front, 3 right side, 7 top; Ctrl + the same number for the opposite side. Number pad 5 switches between perspective and flat (orthographic) views.
- **Frame:** number pad . (period) zooms to the selected object; Home shows everything.
- Without a number pad: the small axis gizmo at the top right of the viewport; click its X, Y and Z dots.

### 1.5 Selecting and changing

- Left click selects, Shift + left click adds to the selection. A selects all, Alt + A selects nothing.
- **G** moves, **R** rotates, **S** scales. Then type X, Y or Z to lock to that axis, or a number for an exact value (S, 2 doubles the size). Left click or Enter confirms, right click or Esc cancels.
- Ctrl + Z undoes, Ctrl + Shift + Z redoes.
- **N** opens the side panel; its Item tab shows Location, Rotation, Scale and Dimensions as numbers, like Transform in Unity's Inspector.
- **Tab** switches between Object Mode (whole objects) and Edit Mode (points and faces). You will rarely need Edit Mode; if the screen looks strange, press Tab again.

### 1.6 Axes and units in this project

- Blender: **Z is up**, a character faces **-Y** (the front view, number pad 1, looks at its face), and its left side is +X. One unit is one meter.
- Unity: Y is up and +Z is forward. The export converts between the two; the scripts set those options, so you never have to.
- Our demons are built **one unit tall**, in "body units"; the game scales them to their size, 1 meter to 15 meters.
- Bones on the left side end in `.L`, on the right in `.R`. Blender mirrors poses between them by these names.

### 1.7 Task: look at your blob

1. File > Import > FBX (.fbx). Pick `Assets\_Project\Art\Models\Core\BP_Core_Intact.fbx` and click Import FBX.
2. Nothing to see? Press Home. Generated files often come in centimeters, so the blob can be huge or tiny.
3. Look at it from the front, the side and the top. Find the mouth: that is the front.
4. Press N and read Dimensions in the Item tab. Note the height; chapter 2 brings it to one unit.
5. At the top right of the viewport, click the third of the four small spheres (Material Preview) to see its texture.
6. File > Save As, into `ArtSource\blender\`, named `practice.blend`.

**Done when:** you can orbit around the blob, frame it, read its size, and `practice.blend` is saved.

### What comes next

- **Chapter 2, the master skeleton:** a script builds the skeleton inside your blob. You learn Pose Mode, turn bones and watch the blob bend.
- **Chapter 3, weights:** how much each bone moves which part of the skin, and how to fix it where the automatic result tears or dents.
- **Chapter 4, your first animation:** an idle loop with key poses, easing in the graph editor, and playing it in a loop.
- **Chapter 5, into the game:** the export and how the game picks it up.
