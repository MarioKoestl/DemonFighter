# Architecture

How the Unity project is structured, where new code goes, and the constraints that keep it testable and open for multiplayer later. Read `UNITY_PRIMER.md` first if terms like MonoBehaviour, ScriptableObject or assembly definition are new.

## Principles

1. **Rules in the simulation, physics in Unity.** Game rules (stats, damage, Biomass, mutation, AI decisions, world generation) live in plain C# with no `UnityEngine` dependency. Movement, collisions, hit detection and rendering live in Unity components. The two talk through commands (in) and events (out).
2. **Deterministic given seed plus commands.** The simulation uses its own seeded random number generator and never reads the clock, the input devices or static mutable state. Replaying the same seed and the same command stream must produce the same result.
3. **Content is data.** ScriptableObject assets define parts, skills, mutations, archetypes and biomes. Behaviour that cannot be expressed as data lives in small strategy classes found by attribute, not by a hand-maintained list.
4. **Composition over inheritance.** A demon is a core plus parts plus skills. No `PlayerDemon : Demon` and no deep MonoBehaviour hierarchies.
5. **Thin MonoBehaviours.** Unity components adapt between Unity and the simulation. They do not contain game rules.
6. **No static singletons for game state.** One `GameServices` composition root created by the bootstrap scene, hand-wired, no DI container in v1. Dependencies are passed in, not fetched from statics.

## Assemblies and folders

Each assembly is a Unity assembly definition (`.asmdef`). Think of them as separate projects in a solution. References point downward only.

```
Assets/_Project/
  Scripts/
    Simulation/      DemonFighter.Simulation        no UnityEngine, no Unity packages
    Common/          DemonFighter.Common            Unity-side helpers without game rules (Log), see D-040
    Data/            DemonFighter.Data              ScriptableObject definitions, converts to Simulation specs
    Presentation/    DemonFighter.Presentation      views, animation, VFX, gore, camera, audio
    Input/           DemonFighter.Input             Unity Input System -> commands
    UI/              DemonFighter.UI                UI Toolkit screens and HUD
    App/             DemonFighter.App               bootstrap, composition root, game loop, scene flow
    Editor/          DemonFighter.Editor            editor-only tools (menu items, generators)
  Tests/
    Simulation/      DemonFighter.Simulation.Tests  EditMode, NUnit, fast
    PlayMode/        DemonFighter.PlayMode.Tests    PlayMode, needs scenes, slow
    Plugins/         test library DLLs: AwesomeAssertions, NSubstitute, Castle.Core (D-039)
  Analyzers/         Roslyn analyzer DLLs: Microsoft.Unity.Analyzers (D-039)
  Content/           ScriptableObject assets: BodyParts/, Skills/, Evolutions/, Demons/, Archetypes/, Biomes/, Catalog/
  Prefabs/           generated or hand-made prefabs
  Scenes/            Bootstrap.unity, MainMenu.unity, Run.unity
  Art/               Models/, Textures/, Materials/, Animations/, VFX/ (see ASSET_PIPELINE.md)
  Settings/          URP assets, Input Actions asset, quality settings
```

Reference graph:

```
App -> Presentation, Input, UI, Data, Simulation, Common
Presentation -> Data, Simulation, Common
Input -> Simulation, Common
UI -> Data, Simulation, Common
Data -> Simulation, Common
Common -> (UnityEngine only, no game rules)
Simulation -> (nothing from Unity)
Editor -> everything
Tests -> the assembly under test (+ Simulation)
```

`DemonFighter.Simulation.asmdef` has `noEngineReferences: true`. If something in Simulation seems to need Unity types (`Vector3`, `Mathf`), use `System.Numerics.Vector3` and `System.MathF` instead, or define a small struct. Conversions happen at the boundary in Presentation.

## The simulation

### Entities

```
RunState
  Seed, Time, ThreatLevel
  World (WorldLayout: terrain description, features, spawn points, elder routes)
  Demons: Dictionary<DemonId, Demon>
  Food: Dictionary<FoodId, FoodItem>          corpses, severed parts, bone piles
  Rng (seeded, owned by the run)

Demon
  Id (DemonId, a simulation id, never a Unity instance id)
  ControllerKind: Player | Ai
  Level, Xp, Biomass, Tier (derived from body investment and evolutions, D-054), SizeMeters (derived from the tier)
  BaseStats (Strength, Constitution, Agility; extensible as data; unspent points; caps raised by evolutions)
  DerivedStats (computed from the effective stats: base stats plus part bonuses; recomputed on change)
  Body
    Core (hp, the sockets with their capacities)
    Parts: List<BodyPart> (spec, socket, hp, condition, upgradeLevel; a lost part keeps its slot until it is regrown)
  Evolutions (count), UnlockedPartIds (from evolutions)
  Skills: List<SkillInstance> (spec, skillXp, skillLevel, cooldown; the perk follows from the level; frozen while the granting part is lost)
  Status: bleeding, stamina, eating target, transforming (mutation or evolution), held, pushed (knockback, dash), lastCombatTick, lastAttackedBy
  Position, Facing (mirrored from Unity each tick, see "Movement")

FoodItem
  Id, BiomassRemaining, DecayTime, Position, Source (corpse or part)
```

Specs are immutable `record` classes (C# 9) created from ScriptableObjects at load time:

```
BodyPartSpec (socket, HP, defense, granted skills, bonuses per upgrade level, cost and requirements),
SkillSpec (slot, timings, level scaling, one SkillPerkSpec),
EvolutionSpec (stage, bound stat gains, free stat points, cap bonuses, free parts, extra skills, unlocked parts, fit stat),
DemonSpec (kind, size, core, starting stats and the starting package of parts, Biomass, level and evolution),
ArchetypeSpec, BiomeSpec (world shape, spawn table, threat pace), SpawnEntry, StatSpec, CombatTuning
ContentCatalog (lookup by id for all of the above)
```

### Commands in, events out

Everything that changes the simulation goes through a command:

```csharp
public interface ICommand { DemonId Actor { get; } }

// C# 9: no record structs. Commands are readonly structs with explicit constructors.
public readonly struct MoveCommand : ICommand
{
    public DemonId Actor { get; }
    public Vec2 Direction { get; }
    public bool Sprint { get; }
    public MoveCommand(DemonId actor, Vec2 direction, bool sprint) { Actor = actor; Direction = direction; Sprint = sprint; }
}
// Same shape for:
// UseSkillCommand(actor, skill), ReportHitCommand(actor, target, part), EatCommand(actor, food),
// MutateCommand(actor, kind: Attach | Upgrade | Regrow, partId, partIndex), EvolveCommand(actor, evolution),
// SpendStatPointCommand(actor, stat)
```

Mutate (attach, upgrade, regrow) and Evolve are rejected while the actor is dead or transforming (the out-of-combat rule of D-014 is suspended, D-060), and when a cost, level, socket, unlock or requirement rule fails (`MutationRules`, `EvolutionRules`; the menu shows the same reasons). On success the change applies at once, the Biomass is spent and a transformation starts: the demon is invulnerable and ignores other commands for 2 seconds while the view morphs; mutations that start in the same tick share that one transformation (D-064). The rules live in the simulation so the AI and the player cannot bypass them; the offer policies (`IMutationOfferPolicy`) only read them.

The player's input and the AI both produce commands. The simulation does not know or care which is which.

Everything the outside world needs to show goes out as an event:

```csharp
CommandRejected(actor, command, reason)
DamageApplied(target, part, amount, damageType, attacker), PartWounded, PartSevered(demon, part, foodId), PartDestroyed
DemonDied(demon, killer), DemonSpawned(demon), DemonHeld(target, by, untilTick)
FoodSpawned(food), FoodConsumed(food, eater, biomass), FoodRemoved(food, reason)
SkillActivated(actor, skill, ...), SkillXpGained(demon, skill, amount), SkillLevelUp(demon, skill, newLevel)
XpGained, LevelUp(demon, level, statPoints), StatPointSpent(demon, stat)
MutationStarted(demon, kind, partIndex, partId, cost, untilTick), MutationCompleted(demon, ...), Evolved(demon, evolution, stage, untilTick)
ThreatLevelChanged(level)   (M4)
```

Events are structs published on a `SimulationEvents` bus. Presentation, UI and audio subscribe. Nothing subscribes from inside the simulation.

### Tick

The simulation advances in fixed steps (`SimulationTick`, 20 Hz for v1, configurable). One tick:

1. Apply queued commands (validated: does the actor exist and live, is it transforming, held, staggered or busy, is the cooldown over, is the stamina there; a hit report counts only inside the active window, within reach and arc). Skill XP and its character XP share are granted here and may raise a level. Mutate and Evolve apply here and start a transformation; a skill behaviour may hold or push a demon. Right after the commands, test mode refills Biomass and stat points of a demon in it (`TestModeSystem`, D-089); a paused menu runs the same refill after its commands.
2. Hold: a grabbed demon is pushed toward its spot beside the holder, or released when the hold ended or a side died (D-061).
3. Movement: integrate the demons that have no Unity body; expired knockback and dash velocities end.
4. Sprint: a sprinting demon drains stamina and earns Sprint XP.
5. Status effects: bleeding drains, passive regeneration heals, stamina refills. Then hazards: a demon standing in lava or a fissure burns (`HazardSystem`, D-086); the zones come from the layout (`RunState.Hazards`).
6. Skills: finished skill uses end.
7. Transformation: a transformation whose time is up ends (`MutationCompleted`).
8. Eating: Biomass flows for every demon that held Eat this tick.
9. Run AI decisions for AI demons (every N ticks per brain; held actions continue between decisions; dead, transforming and held demons are skipped). Walking directions bend around hazards and goals move out of them (D-086). Commands land in the next tick.
10. Threat and spawning: the threat level, derived from the tick, is announced when it crosses a whole level (D-069); then the Tier 0 population is topped up to the biome count, one demon per interval, out of sight of the player (D-053).
11. Decay food.
12. Flush events.

A `SimulationRunner` MonoBehaviour in the App assembly calls `Tick` from `FixedUpdate` with an accumulator, so the simulation rate is independent of the frame rate. While a menu pauses it, `ApplyPendingCommands` still applies queued commands without advancing time, so the Stats tab spends points through the same path as everything else.

### Movement, collision and hits

This is the deliberate exception to "rules in the simulation". Unity's `CharacterController` and physics move the bodies and detect hits:

- `MoveCommand` is consumed by the Presentation layer (the demon's view) which moves the Unity object. After the physics step, the view writes the resulting position and facing back into the simulation entity (`Demon.Position`). The simulation trusts this.
- Skill hit detection happens in Unity during the active ticks a `SkillActivated` event announces: `CombatPresenter` sweeps a sphere along the crosshair ray for the player, then falls back to a volume in front of the body (which is all AI uses), against `BodyPartView` trigger colliders on the `Demon` layer. The touch becomes a `ReportHitCommand`; the simulation accepts it only inside the window, once per use, within reach and arc, then applies the damage and emits events (D-046). Reach is the skill reach per meter of body size times `Demon.ReachMultiplier` (Eyes, D-058) in both places.
- Perception for AI is a distance scan of the run state inside the simulation (`Perception`, D-050), so AI tests need no Unity and every demon sees the same world. For runs without a frame (the autoplay harness, tests) `SimulationTicker.EnableHeadlessHits` lets a `HeadlessHitResolver` report hits itself (D-077).
- A grabbed demon is dragged by its holder: `HoldSystem` gives it a push toward the grab spot every tick, and the view moves it like any pushed body (D-061).

Why: writing our own 3D physics is out of scope, and Unity's physics also runs headless, so a future server could run the same code.

### AI

`DemonFighter.Simulation.Ai`:

- `UtilityBrain` picks a goal (`Hunt`, `Eat`, `Flee`, `Wander`, `Rest`, `Patrol`, `Mutate`, `Evolve`) by weighted chance from the weights of its `ArchetypeSpec`, which new blobs draw from the biome by weight (Aggressive, Cautious, Scavenger, D-071), scored by what `Perception` finds within the perception radius: prey at most one tier above, never two or more tiers below unless it attacked the demon, wounded or eating prey preferred, food by the reward factor (`CombatTuning.RewardFactor`). Low health near a fight overrides everything with `Flee`.
- The chosen goal produces commands: move (with a facing toward the prey when standing), a strike or a dash chosen from the skills the body grants when in reach, eat every tick while at food. A demon that was hit turns on its attacker (D-052). In calm moments it spends stat points, evolves and buys, regrows or upgrades parts by the preferences of its archetype, through the same commands as the player (D-072).
- AI decisions run every N ticks per demon (staggered), not every tick, to keep cost flat with 50+ demons; between decisions `Hold` only keeps a meal going.
- Elders use the same brain with a wider perception; the reward rule makes them ignore blobs until one bites them. Their route targets are pulled toward the player as the threat rises (D-070).

### World generation

`DemonFighter.Simulation.Worldgen`:

- `IWorldGenerator.Generate(seed, BiomeSpec) -> WorldLayout`
- `WorldLayout` is data only: heightfield, feature placements (rock, fissure, pool, bone pile), spawn clusters, elder routes.
- Pools lie in basins the generator carves into the heightfield (D-086), so a pool never hides under the ground; the burning zones of lava and fissures (`HazardMap`) are exactly the visible ones.
- `WorldBuilder` in Presentation turns a `WorldLayout` into terrain, meshes and colliders at run start.

## Content pipeline

1. A designer (Mario, or Claude Code on request) creates a ScriptableObject asset, for example `Content/BodyParts/BP_Arm_Basic.asset`, through the menu `Assets > Create > Demon Fighter > Body Part`.
2. The asset holds data (stats, cost, requirements, visual references) and, when the part needs custom behaviour, the string id of a behaviour class.
3. Behaviour classes implement a small interface and are tagged, for example `[SkillBehaviour("bite")] sealed class BiteBehaviour : ISkillBehaviour`. A registry scans assemblies once at startup. No hand-maintained list.
4. `ContentCatalog.asset` references all definitions. The editor menu `Demon Fighter > Rebuild Content Catalog` finds every definition asset and fills the catalog, so nobody edits it by hand.
5. At bootstrap, `ContentCatalogDefinition.Build()` converts definitions into immutable specs (`ToSpec()` on each definition) and builds the `ContentCatalog` used by the simulation. The placeholder generator creates the v1 assets once from `PlaceholderContent`; after that the assets are the truth and one-time migrations bring older assets up to the current fields.
6. Look data rides on the content assets without touching the specs: `PartMotion` and the mesh set on a part, `SkillMotion` on a skill (D-082). The simulation never sees them.
7. Art reaches the content by name (D-079): models under `Assets/_Project/Art/Models/` named `BP_<Part>[_<Variant>]_<State>` are bound to the mesh set and the socket anchors of their part asset by `ArtAssetBinder` (run by the generator, or `Demon Fighter > Art > Bind Art Assets`); `ArtImportRules` sets the import settings as files arrive; `ArtAssetValidator` checks the conventions, the budgets and the license log (`ASSET_PIPELINE.md`).

Rules: ScriptableObjects are never mutated at runtime. Specs are immutable records. The simulation only sees specs.

Addressables are not used in v1 (see `DECISIONS.md`). All content loads with the game.

## Presentation

- `DemonView` (MonoBehaviour): binds a `Demon` to a Unity object. Owns the `CharacterController` and the body part views, composes its body through `DemonFigure`, follows the size, the tier and the wounds of the demon, drives the `BodyAnimator` from the velocity the simulation decided and the attacks, hits and transformations the presenter reports, and becomes the corpse on death (flat, dark, on the `Food` layer, carrying the `FoodView`).
- `BodyAnimator`: the procedural motion (D-082), plain arithmetic over clocks the view advances: breathing, stretch and squash with speed, a gait bob, the bite swell and nod, the lunge stretch, the hit wobble and the transformation throb for the figure; stepping legs, swinging and striking limbs, snapping jaws and swaying tails for the parts, by the `PartMotion` of the part asset and the `SkillMotion` of the skill asset. Amplitudes come from `DemonViewSettings`. A part with a legacy clip on its mesh set plays that instead.
- `DemonFigure`: the transforms a body is composed of, shared by `DemonView` and `BodyPreviewRig`: the Body (the prefab capsule, or the bound core mesh fitted to the body height), the Placeholders (capsule-unit space where primitives of unbound parts hang as before) and the Rig (body-unit space where mesh parts hang on the socket anchors of the core, `SocketAnchors`). `PartVisuals` reads mesh sets and anchors from the part assets and creates the views; the figure tilts for the corpse pose.
- `BodyPartView`: one per part with a trigger collider on the `Demon` layer for hit detection. Shows the damage stage (`DamageStages`, D-080): a primitive darkens and shrinks when wounded or mangled and hides when lost; a bound mesh set swaps to its wounded, mangled or stump mesh, keeps its imported material and wears the owner color (player teal, AI tier) as a tint.
- `CombatPresenter`: detects hits for active skills and reports them, keeps the food under the crosshair for the eat prompt, highlights the body part under the crosshair and holds the Analyze lock for the HUD (D-065, D-066), and turns `DamageApplied`, `PartSevered`, `PartDestroyed`, `DemonDied`, `FoodRemoved`, `MutationStarted` and `Evolved` into gore (D-081): blood on the hit part through the skin shader (drying after 15 seconds, D-068), splats and corpse pools on the ground (`BloodDecalPool`, URP decal projectors), viscera bursts (`VisceraPool`, code-built meshes on a ballistic arc), fallen parts that keep the look of their view (`FoodView` with a rigidbody), corpses and the transformation throb. `GoreSettings` holds the tuning.
- Shader `DemonFighter/DemonSkin` (`Assets/_Project/Art/Shaders`): URP Lit with a blood mask in the forward pass, fed per renderer by `BodyPartView`; every demon material and every bound part material uses it.
- `BodyPreviewRig`: a stage far below the world on the Preview layer with its own camera, light and RenderTexture; composes a figure for the body the menu composes the same way the world does (`DemonFigure`), turns it, and switches fog off for its frames (D-063).
- `CameraRig`: Cinemachine, two virtual cameras (third-person default, first-person), toggled by an input event. Third-person uses the demon's own body with no culling of the player model. Camera distance, height and the `CharacterController` dimensions scale with the demon's `SizeMeters` and follow it when a mutation or evolution grows the body, so a 1 meter blob and a 15 meter elder use the same prefab.
- `WorldBuilder`: builds the world from `WorldLayout` at run start: terrain chunks, walls, features, the three-color ambient, the ceiling glow with soft shadows, fog, and the lava and fissure lights (D-083). `ShadowBudget` on the world root hands shadows to the point lights nearest the camera, `LightFlicker` lets lights breathe, `LavaFlow` drifts the crust of each pool through a property block.
- `GraphicsPresetCatalog` and `GraphicsQuality` (`Presentation/Rendering`): the Low, Medium and High pipeline assets and the switch between them (D-083); `RenderLookGenerator` (Editor) builds them and the cavern volume profile.
- `DemonView` adds an `LODGroup` with two levels to every body (parts dropped far away, the body culled farther still), rebuilt when a part grows (D-083).
- `RenderPipelineSetup` (Editor): adds the renderer features the views rely on (decals) to the URP renderer asset from code.
- `AudioDirector` (`Presentation/Audio`, D-084): turns the simulation events of a run into sound through `SoundPlayer` (pooled one-shots), meters footsteps, runs the `AmbientPlayer` (drone and lava loop) and picks the music cue for the `MusicPlayer` on the bootstrap object. Sounds are `AudioEventDefinition` assets referenced by skills, parts, the biome and the `AudioCatalog`; `AudioMix` holds the four volume knobs. Created and disposed by `RunController` with the run; the music player outlives it.

Presentation never changes simulation state directly. It sends commands or hit reports.

## App

- `Bootstrap.unity` is the first scene. It creates `GameServices` (content catalog, event bus, offer policies, save slot, meta hook, the audio catalog, the audio mix and the music player under the bootstrap object, the graphics presets and the `SettingsApplier`), reads `settings.json` through `GameSettings` and applies it (D-085), marks the services persistent, then loads `MainMenu`, where `SceneFlow` starts the menu track and fills the settings panel.
- `GameSettings` is the settings file `settings.json` in `Application.persistentDataPath` (D-085): graphics preset, resolution, fullscreen, vsync, four volumes, mouse sensitivity, invert Y and the playtest toggles (random offers, and test mode of D-089, which `RunController` sends into the run as a command), written on every change. `SettingsApplier` turns it into `SettingsValues` for the panels and pushes changes into `GraphicsQuality`, the screen, the `AudioMix` and, through `RunController`, the camera rig.
- `RunController` creates a `RunState` from a seed (or loads one from the save slot), builds the world, spawns views for every entity, starts the `SimulationRunner`, and tears everything down on run end.
- `RunSaveService`: on quit during a run, writes the `RunSnapshot` that `RunPersistence.Capture` produces (plain data, D-073) as Newtonsoft JSON to one slot under `Application.persistentDataPath`, and `RunPersistence.Restore` rebuilds the run from it. The world is not saved; it is regenerated from the seed. Resuming deletes the slot, death deletes the slot.
- On the death of the player `RunController` builds a `RunSummary`, hands it to `IMetaProgression` (a no-op in v1, D-075) and to the death screen.
- Scene flow: Bootstrap -> MainMenu -> Run -> (death) -> RunSummary (overlay) -> MainMenu or new Run; MainMenu -> Continue -> Run resumed from the slot; Run -> Esc -> Save and Quit -> MainMenu (D-074).

## Input

- One Input Actions asset (`Settings/DemonFighter.inputactions`) with action maps `Gameplay` and `Menu`.
- `PlayerInputAdapter` reads actions and produces commands for the demon of the player: move (with the sprint flag, facing the camera while attacking), one skill use per attack key pressed, resolved through the skill slot the key stands for (`SkillSlots.Find`: left mouse Primary, right mouse Secondary, Space Lunge, Q Tail Swing), eat while the key is held on the food Presentation found under the crosshair (`IPlayerAim`). It raises `MenuToggled(MenuTab)` for Tab and C, `AnalyzeRequested` for F and `PauseRequested` for Esc. Nothing else in the project reads input. No `Input.GetKey` anywhere.
- Adding gamepad later means adding bindings to the asset, no code changes.

## UI

- `MainMenuScreen` and `PausePanel` share the `SettingsPanel` (tabs Graphics, Audio, Controls, Playtest, D-085); both only raise requests (`NewRunRequested` with the parsed seed, `SettingsChanged` with the whole value set, `QuitRequested`) that `SceneFlow` and `RunController` act on. `MenuStyles` gives the menus one look in code.
- `HudScreen` (UI Toolkit, built in code) shows the run and hosts the `MutationMenu`, the `StatsPanel` inside it, the `PausePanel` and the `RunSummaryPanel`. The menu reads `RunState`, the offer policy and the rules every frame while open and only raises requests (`MutationsRequested` with every selected mutation, `EvolutionRequested`, `StatPointRequested`); `RunController` turns them into commands, pauses the runner while the menu is open and resumes it on confirm. The Mutate tab is a planner like Stats (D-064): it composes a preview `Body` (own parts plus selected offers) and hands it to an `IBodyPreview`, which the App layer implements over the `BodyPreviewRig`, so UI never references Presentation. The HUD names the aimed part under the crosshair and shows the analysis panel of a locked target through a `TargetFocus` callback composed the same way, revealing by the sense level of the player (D-066). First-run hints appear once per installation, remembered in PlayerPrefs through `HintMemory` (D-076). The UI never changes simulation state directly.

## Multiplayer readiness

Not building multiplayer. Both co-op and PvP should stay possible later, which means a server-authoritative model. Keeping that door open costs these rules:

- All state changes go through serializable commands; all outputs are events.
- No `Time.time`, `DateTime.Now`, `Random.Range` or `UnityEngine.Random` in the simulation. Use the run's `Rng` and the tick counter.
- Entity ids are simulation ids, assigned by the run, never Unity instance ids.
- No static mutable state. The composition root owns everything.
- Presentation can be destroyed and rebuilt from `RunState` plus the event stream at any time (this is also what makes a replay or spectator view possible).

## Testing

- `DemonFighter.Simulation.Tests` (EditMode): the bulk of tests. Create a `RunState` with a fixed seed, feed commands, assert on state and events. Must run in under a few seconds total.
- `DemonFighter.PlayMode.Tests`: a few integration tests that load `Run.unity` with a fixed seed and check that views match state. Slow; keep them few.
- `StressTests` keeps the 50 demon, 200 food tick under 2 ms; `AutoplayHarness` (Editor, menu or batch mode) plays seeded headless runs for tuning (D-077).
- `LookCaptureTests` (Play Mode, explicit, so only run by name) starts a run with a fixed seed and saves screenshots of the player view, a lava pool, an overview and a ground-level view into `TestResults/look` (D-087).
- Test builders: `DemonBuilder`, `RunStateBuilder` in the test assembly so tests read like design statements.
- `DemonFighter.Editor.Tests` (EditMode): the plain rules of the editor and presentation layers that need no scene: art naming, validation rules, damage stages, mesh set fallbacks, anchor and bounds math, the body animator, viscera meshes, painted textures, the shadow budget, and the generated render look assets (profile, presets, renderer feature).

## Performance notes (v1 scale)

- Target: 60 fps on an RTX 3060 class GPU at 1080p. v1 runs with about 10 demons; 50 demons and 200 food items is the stress target for M4 and later.
- Simulation tick must stay under 2 ms with 50 demons.
- No per-frame allocations in `DemonView`, `GoreSystem` or the AI. Pool decals, particles and food views.
- Mesh and LOD budgets are in `ASSET_PIPELINE.md`.
