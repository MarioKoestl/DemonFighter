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
