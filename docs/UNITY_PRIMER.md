# Unity Primer for a .NET Developer

Written for Mario. Everything here maps a Unity idea to something from ASP.NET or a .NET console app. Claude Code links here when it introduces a concept.

## The project

- A Unity project is a folder. `Assets/` holds everything you own, `Packages/manifest.json` is your `.csproj` package references, `ProjectSettings/` is configuration, `Library/` is a build cache you never commit.
- Unity generates a `.sln` and `.csproj` files for your IDE. They are output, not source. You regenerate them from the editor if they get out of sync.
- There is no `Program.cs`. The engine runs a loop and calls into your code. The entry point is the first scene in the build list (`Bootstrap.unity` here).

## Scenes, GameObjects, Components

- A **Scene** is a saved tree of GameObjects. Like a page in an app, or a saved object graph.
- A **GameObject** is an empty node with a transform (position, rotation, scale) and a list of components. It has no behaviour of its own.
- A **Component** gives a GameObject behaviour or data: a mesh, a collider, a camera, a light, or one of your scripts.
- Your scripts that live on GameObjects derive from **MonoBehaviour**. Think of a MonoBehaviour as a controller class the framework instantiates and calls at fixed points. You never `new` one; you add it to a GameObject.

Mapping: GameObject is the entity, components are the behaviours attached to it. Composition, not inheritance. The project keeps MonoBehaviours thin (adapters) and keeps the real logic in plain C# classes you can unit test.

## Lifecycle callbacks

Unity calls these on MonoBehaviours by name (no interface, no override keyword):

| Callback | When | Use for |
|---|---|---|
| `Awake` | once, when the object is created | cache references, set up fields |
| `OnEnable` / `OnDisable` | when enabled / disabled | subscribe / unsubscribe events |
| `Start` | once, before the first frame, after all `Awake`s | things that need other objects to exist |
| `Update` | every frame | input, visuals, anything frame-rate dependent |
| `FixedUpdate` | fixed interval (50 Hz default) | physics, and our simulation tick |
| `LateUpdate` | after all `Update`s | cameras following things |
| `OnDestroy` | when destroyed | cleanup |

`Update` runs at whatever frame rate the machine achieves, so anything time-based multiplies by `Time.deltaTime`. Our simulation does not run in `Update`; it runs on a fixed tick so it does not care about frame rate.

## Prefabs

A **Prefab** is a saved GameObject template, like a serialized object graph you can instantiate many times. Changing the prefab changes every instance that has not overridden that value. We generate most prefabs from editor scripts so they can be rebuilt.

## ScriptableObjects

A **ScriptableObject** is a C# class whose instances are saved as asset files in the project. It is configuration as data: a record you can edit in the Inspector and reference from other assets. We use them for every piece of content (body parts, skills, mutations). They are read-only at runtime in this project.

Mapping: `appsettings.json` sections, strongly typed, one file per entry, editable in a form.

## Serialization and the Inspector

Unity serializes public fields and private fields marked `[SerializeField]` into scenes, prefabs and assets, and shows them in the Inspector panel. Properties are not serialized. This is why Unity code has fields where you would write properties. We keep them private with `[SerializeField]` and expose read-only properties when needed.

## The null quirk

A destroyed Unity object is not a C# null. The managed wrapper still exists; Unity overrides `==` so `obj == null` returns true for destroyed objects, but `obj is null` and `obj?.Foo` do not know about that. Rule in this project: compare Unity objects with `== null`. Plain C# classes use the normal patterns.

## Assembly definitions

An `.asmdef` file turns a folder into its own assembly, with explicit references to other assemblies. Same as splitting a solution into projects. Benefits: enforced dependency direction (Simulation cannot reference Unity), faster compilation, and tests can target one assembly. Our layout is in `ARCHITECTURE.md`.

## Packages

Unity's Package Manager is NuGet for Unity. `Packages/manifest.json` lists them. We use the Input System (modern input, like a typed event source instead of polling), Cinemachine (camera behaviours), Test Framework (NUnit), UI Toolkit (UI with a UXML/USS model, similar to HTML/CSS) and Newtonsoft JSON.

## Rendering: URP

The Universal Render Pipeline is Unity's standard renderer for most projects. Materials use URP shaders (Lit, Unlit, Shader Graph). Post-processing (color grading, bloom, ambient occlusion) is configured with a Volume component in the scene. You do not need to understand the pipeline to write gameplay code; it matters when importing assets (use URP materials) and in M5.

## Meshes, importers and the asset pipeline

A `MeshFilter` holds the geometry, a `MeshRenderer` draws it with a `Material`. Swapping `MeshFilter.sharedMesh` is how a body part changes its damage state; nothing else about the object changes. A `MaterialPropertyBlock` overrides a few shader values (we use `_BaseColor`) for one renderer without copying the material, which is how the owner tint, the wound darkening and the aim highlight work on a shared material.

An FBX file imports as a model: a prefab-like asset with child objects, and the meshes and materials inside it as sub-assets. The `ModelImporter` and the `TextureImporter` hold the import settings you would otherwise click in the Inspector. An `AssetPostprocessor` is a hook that runs while an asset is imported, like a build step: `ArtImportRules` sets the importer fields for everything under `Assets/_Project/Art/`, so a dropped file comes in right. Editor code reads imported models with `AssetDatabase.LoadAssetAtPath<GameObject>` and walks their children like any hierarchy. `Mesh.bounds` is the axis-aligned box of a mesh in its own space; after a rotation the box around the eight transformed corners is the honest bound, which is what `MeshBounds.Transform` computes. Unity ignores files and folders whose names start with a dot, so a `.gitkeep` keeps an empty art folder in Git without becoming an asset.

Mapping: an import pipeline with per-file-type rules, like MSBuild item metadata, with the rules written in code instead of clicked.

## Decals, renderer features and shader passes

A **Decal Projector** is a box that projects a material onto whatever surfaces lie inside it; URP draws it in a renderer feature ("Decals") that has to be on the renderer asset, which `RenderPipelineSetup` adds from code. We use projectors for blood on the floor because they wrap over slopes and steps where a flat quad would float or clip. A **renderer feature** is a plug-in pass on the URP renderer, like middleware in a request pipeline; SSAO and decals are the two we use.

A **Volume** is a scene object that holds a **Volume Profile**, the list of post-processing overrides (tonemapping, bloom, vignette and so on) a camera blends in; our scenes carry one global volume with the cavern profile the generator built. **Quality presets** in this project are whole URP pipeline assets: switching `QualitySettings.renderPipeline` swaps render scale, shadows and renderer features in one go, which is simpler to reason about than Unity's quality levels with their per-level overrides.

A **shader** has one or more passes, each a small program for one purpose: the forward pass shades the surface, the shadow caster writes depth into shadow maps, the depth and depth-normals passes feed SSAO and decals. Our skin shader keeps the URP Lit passes and replaces only the forward one, including the URP source files, so a pipeline update updates the lighting for free. Values set per renderer through a MaterialPropertyBlock reach uniforms that are not material properties; that is how blood is per body part without a material copy.

## Animation without clips

Unity animates with clips (keyframed curves) played by an `Animator` (state machine) or the older `Animation` component (legacy clips, plain play and loop). We use neither for the bodies: `BodyAnimator` computes rotations and scales from the simulation state every frame and writes them to the transforms, the same way a shader computes a color from inputs. The upside is that every demon of every size moves right without a rig; the downside is that nothing can be hand-tuned keyframe by keyframe. Parts that do get a hand-made clip use the legacy `Animation` component, because it needs no controller asset.

Mapping: a clip is recorded data, procedural animation is a pure function of state; think a stored report versus a computed view.

## Physics

- `Rigidbody` makes an object move under physics. `Collider` gives it a shape for collisions. A `CharacterController` is a special capsule for characters that moves by code and handles slopes and steps without being thrown around by physics.
- Physics runs in `FixedUpdate`. Querying "what is near me" is `Physics.OverlapSphere` and friends. Layers control what collides with what.
- We use Unity physics for movement and hits, and feed the results into the simulation. See `ARCHITECTURE.md`, "Movement, collision and hits".
- A collider marked `isTrigger` is a shape that queries and rays can find but that blocks nothing. Body parts use triggers on the `Demon` layer, so a sphere cast finds the part under the crosshair without the parts bumping into each other. Layers are plain project settings (Edit > Project Settings > Tags and Layers); our generator adds `Demon` and `Food`.
- A `Rigidbody` falls asleep once it rests; `IsSleeping()` is how a severed part knows it has landed and can report its position.

## UI Toolkit

The HUD and menus are built in code from `VisualElement`, `Label` and `Button` objects that live under a `UIDocument` component with a `PanelSettings` asset. Styles are set on `element.style` (positions, colors, `display` to show or hide). Elements must be created in `Awake` or `OnEnable`, never in a field initializer, because Unity constructs MonoBehaviours while it deserializes a scene and forbids UI creation then. An element with `pickingMode = PickingMode.Ignore` lets mouse clicks pass through, which the HUD overlay needs and the mutation menu does not. `Button.SetEnabled(false)` greys a button out and swallows its clicks, a `ScrollView` scrolls content that does not fit, and `resolvedStyle` holds what the layout actually computed, which is what Play Mode tests assert on; `style` is only what the code asked for. A `VisualElement` can show a `RenderTexture` as its background image, which is how the mutation menu shows what a second camera sees of the body (D-063).

## Input System

Actions (`Move`, `Look`, `Attack`) are defined in an asset and bound to devices. Code reads actions, not keys. This is what makes adding a gamepad a data change later. Only `PlayerInputAdapter` reads them.

## Audio

An `AudioSource` component plays an `AudioClip`; an `AudioListener` on the camera hears. With `spatialBlend` 1 the source sits in the world and falls off with distance, with 0 it plays flat in both ears. There is no event system of its own, so we built a small one: `SoundPlayer` keeps a pool of sources and sets clip, volume, pitch and position per play, `MusicPlayer` crossfades two looping sources. Unity's `AudioMixer` asset would give us volume groups, but it cannot be created from code, so the four knobs live in a plain class and every player reads them. Import settings (`AudioImporter`) decide compression and whether a clip streams or sits decompressed in memory.

Mapping: an `AudioClip` is a sample buffer, an `AudioSource` a channel strip you configure per play.

## UI Toolkit controls

Besides buttons and labels, UI Toolkit ships the usual form controls: `Slider`, `Toggle`, `DropdownField`, `TextField`. Each has a `value`, raises a `ChangeEvent` through `RegisterValueChangedCallback` when the player moves it, and offers `SetValueWithoutNotify` for filling it from code without firing the event; the settings panel uses the latter when it shows the file and the former when the player edits. Elements are found by `name` with `Q<T>("name")`, which is also how the Play Mode tests click them by sending a `NavigationSubmitEvent`.

## Saving and settings

`Application.persistentDataPath` is the per-user folder for save files; on Windows it is under `AppData\LocalLow\<Company>\<Product>`. `PlayerPrefs` is a small key-value store (the registry on Windows) for flags and settings such as the first-run hints and the offers toggle. `Application.quitting` fires when the player quits and also when Play Mode stops in the editor, which is why a run saves itself then (D-074). A DLL from a package, such as Newtonsoft.Json, is referenced from an assembly definition through `overrideReferences` plus `precompiledReferences`, not through the references list.

## Play Mode and the Test Runner

- Pressing Play runs the game inside the editor. Changes made to scene objects while playing are discarded on stop (a classic trap).
- Window > General > Test Runner lists EditMode tests (run in the editor process, no scene, fast) and PlayMode tests (run inside a playing scene, slow). Our simulation tests are EditMode and run like any NUnit suite.
- The Console window shows logs, warnings, errors and compile errors. Red entries with a file path are compile errors; double click opens the file.

## Async

Unity 6 has `Awaitable` (await-able engine operations) which fits `async`/`await` as you know it. Coroutines (`IEnumerator` with `yield return`) are the older model; we use them only when an API forces it.

## Git with Unity

- `.meta` files: every asset has one with a GUID. References between assets use that GUID. If a `.meta` goes missing, references break. Always commit them with the asset.
- `Library/` is never committed. First open after clone takes a while because Unity rebuilds it.
- Scene and prefab merges are painful. We keep scenes small and generate what we can, so merges stay rare.
- Binaries (models, textures, audio) go through Git LFS, configured in `.gitattributes`.

## Where to look things up

- Unity Manual and Scripting API: https://docs.unity3d.com/
- URP docs: https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest
- Input System docs: https://docs.unity3d.com/Packages/com.unity.inputsystem@latest
- Cinemachine docs: https://docs.unity3d.com/Packages/com.unity.cinemachine@latest
- Unity Test Framework: https://docs.unity3d.com/Packages/com.unity.test-framework@latest
