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
  Content/           ScriptableObject assets: BodyParts/, Skills/, Mutations/, Archetypes/, Biomes/, Catalog/
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
  Level, Xp, UnspentStatPoints, Biomass, Tier (derived), SizeStep (derived from tier and evolutions)
  BaseStats (Strength, Constitution, Agility; extensible as data)
  DerivedStats (computed from base stats + Core + parts + evolutions, cached, recomputed on change)
  Body
    Core (hp, sockets, evolution line)
    Parts: List<BodyPart> (spec, socket, hp, condition, upgradeLevel, child sockets)
    SeveredParts: List<BodyPartSpecId> (regrowable on request)
  Evolutions: List<EvolutionSpecId> (taken, in order)
  Skills: List<SkillInstance> (spec, skillXp, skillLevel, perksTaken, cooldown, frozen when the granting part is severed)
  Status: bleeding, stamina, eating target, transforming (mutation or evolution), lastCombatTick
  Position, Facing (mirrored from Unity each tick, see "Movement")

FoodItem
  Id, BiomassRemaining, DecayTime, Position, Source (corpse or part)
```

Specs are immutable `record` classes (C# 9) created from ScriptableObjects at load time:

```
BodyPartSpec, SkillSpec (with level curve and PerkSpecs), MutationSpec,
EvolutionSpec (stat point pool, caps, granted mutations, granted skills, unlocked part categories),
ArchetypeSpec, BiomeSpec, StatSpec
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
// UseSkillCommand(actor, skill, target), EatCommand(actor, food), MutateCommand(actor, mutation),
// RegrowPartCommand(actor, part), EvolveCommand(actor, evolution), SpendStatPointCommand(actor, stat)
```

Mutate, RegrowPart and Evolve are rejected while the actor is in combat (`lastCombatTick` within the out-of-combat window, 5 seconds). On success they start a transformation: the demon is invulnerable and ignores other commands for 2 seconds, then the change applies. The rule lives in the simulation so the AI and the player cannot bypass it.

The player's input and the AI both produce commands. The simulation does not know or care which is which.

Everything the outside world needs to show goes out as an event:

```csharp
DamageApplied(target, part, amount, damageType, attacker)
PartWounded(demon, part), PartSevered(demon, part, foodId)
DemonDied(demon, killer), FoodSpawned(food), FoodConsumed(food, eater, biomass)
TransformationStarted(demon), MutationApplied(demon, mutation), PartRegrown(demon, part)
EvolutionApplied(demon, evolution, newSizeStep), LevelUp(demon, statPointsGranted), StatPointSpent(demon, stat)
SkillXpGained(demon, skill, amount), SkillLevelUp(demon, skill, newLevel), SkillPerkUnlocked(demon, skill, perk)
ThreatLevelChanged(level)
```

Events are structs published on a `SimulationEvents` bus. Presentation, UI and audio subscribe. Nothing subscribes from inside the simulation.

### Tick

The simulation advances in fixed steps (`SimulationTick`, 20 Hz for v1, configurable). One tick:

1. Apply queued commands (validated: does the actor exist and live, is it staggered or busy, is the cooldown over, is the stamina there; a hit report counts only inside the active window, within reach and arc). Skill XP and its character XP share are granted here and may raise a level.
2. Movement: integrate the demons that have no Unity body.
3. Status effects: bleeding drains, passive regeneration heals, stamina refills; finished skill uses end.
4. Eating: Biomass flows for every demon that held Eat this tick.
5. Run AI decisions for AI demons (every N ticks per brain; held actions continue between decisions). Commands land in the next tick.
6. Spawning: top the Tier 0 population up to the biome count, one demon per interval, out of sight of the player (D-053); the threat level that scales it comes in M4.
7. Decay food.
8. Flush events.

A `SimulationRunner` MonoBehaviour in the App assembly calls `Tick` from `FixedUpdate` with an accumulator, so the simulation rate is independent of the frame rate.

### Movement, collision and hits

This is the deliberate exception to "rules in the simulation". Unity's `CharacterController` and physics move the bodies and detect hits:

- `MoveCommand` is consumed by the Presentation layer (the demon's view) which moves the Unity object. After the physics step, the view writes the resulting position and facing back into the simulation entity (`Demon.Position`). The simulation trusts this.
- Skill hit detection happens in Unity during the active ticks a `SkillActivated` event announces: `CombatPresenter` sweeps a sphere along the crosshair ray for the player and along the facing for AI, against `BodyPartView` trigger colliders on the `Demon` layer. The touch becomes a `ReportHitCommand`; the simulation accepts it only inside the window, once per use, within reach and arc, then applies the damage and emits events (D-046).
- Perception for AI is a distance scan of the run state inside the simulation (`Perception`, D-050), so AI tests need no Unity and every demon sees the same world.

Why: writing our own 3D physics is out of scope, and Unity's physics also runs headless, so a future server could run the same code.

### AI

`DemonFighter.Simulation.Ai`:

- `UtilityBrain` picks a goal (`Hunt`, `Eat`, `Flee`, `Wander`, `Rest`, `Patrol`; `Mutate` and `Evolve` join in M3 and M4) by weighted chance from the `ArchetypeSpec` weights, scored by what `Perception` finds within the perception radius: prey at most one tier above, never two or more tiers below unless it attacked the demon, wounded or eating prey preferred, food by the reward factor (`CombatTuning.RewardFactor`). Low health near a fight overrides everything with `Flee`.
- The chosen goal produces commands: move (with a facing toward the prey when standing), skill use when in reach, eat every tick while at food.
- AI decisions run every N ticks per demon (staggered), not every tick, to keep cost flat with 50+ demons; between decisions `Hold` only keeps a meal going.
- Elders use the same brain with a wider perception; the reward rule makes them ignore blobs until one bites them.

### World generation

`DemonFighter.Simulation.Worldgen`:

- `IWorldGenerator.Generate(seed, BiomeSpec) -> WorldLayout`
- `WorldLayout` is data only: heightfield, feature placements (rock, fissure, pool, bone pile), spawn clusters, elder routes.
- `WorldBuilder` in Presentation turns a `WorldLayout` into terrain, meshes and colliders at run start.

## Content pipeline

1. A designer (Mario, or Claude Code on request) creates a ScriptableObject asset, for example `Content/BodyParts/BP_Arm_Basic.asset`, through the menu `Assets > Create > Demon Fighter > Body Part`.
2. The asset holds data (stats, cost, requirements, visual references) and, when the part needs custom behaviour, the string id of a behaviour class.
3. Behaviour classes implement a small interface and are tagged, for example `[SkillBehaviour("bite")] sealed class BiteBehaviour : ISkillBehaviour`. A registry scans assemblies once at startup. No hand-maintained list.
4. `ContentCatalog.asset` references all definitions. The editor menu `Demon Fighter > Rebuild Content Catalog` finds every definition asset and fills the catalog, so nobody edits it by hand.
5. At bootstrap, `ContentLoader` converts definitions into immutable specs and builds the `ContentCatalog` used by the simulation.

Rules: ScriptableObjects are never mutated at runtime. Specs are immutable records. The simulation only sees specs.

Addressables are not used in v1 (see `DECISIONS.md`). All content loads with the game.

## Presentation

- `DemonView` (MonoBehaviour): binds a `Demon` to a Unity object. Owns the `CharacterController` and the body part views, plays the attack pulse and becomes the corpse on death (flat, dark, on the `Food` layer, carrying the `FoodView`).
- `BodyPartView`: one per part with a trigger collider on the `Demon` layer for hit detection. Shows the condition (wounded darker and smaller, lost hidden); damage state meshes come in M5.
- `CombatPresenter`: detects hits for active skills and reports them, and turns `DamageApplied`, `PartSevered`, `PartDestroyed`, `DemonDied` and `FoodRemoved` into blood (`BloodDecalPool`), fallen parts (`FoodView` with a rigidbody) and corpses.
- `CameraRig`: Cinemachine, two virtual cameras (third-person default, first-person), toggled by an input event. Third-person uses the demon's own body with no culling of the player model. Camera distance, height and the `CharacterController` dimensions scale with the demon's `SizeStep`, so a 1 meter blob and a 15 meter elder use the same prefab.
- `WorldBuilder`: builds the world from `WorldLayout` at run start.
- `AudioDirector`: later.

Presentation never changes simulation state directly. It sends commands or hit reports.

## App

- `Bootstrap.unity` is the first scene. It creates `GameServices` (content catalog, settings, event bus, run factory), marks it persistent, then loads `MainMenu`.
- `RunController` creates a `RunState` from a seed (or loads one from the save slot), builds the world, spawns views for every entity, starts the `SimulationRunner`, and tears everything down on run end.
- `RunSaveService`: on quit during a run, serializes the whole `RunState` (Newtonsoft JSON) to one slot under `Application.persistentDataPath`. The world is not saved; it is regenerated from the seed. Resuming deletes the slot, death deletes the slot. Every type inside `RunState` must therefore be serializable without Unity references.
- Scene flow: Bootstrap -> MainMenu -> Run -> (death) -> RunSummary (overlay) -> MainMenu or new Run.

## Input

- One Input Actions asset (`Settings/DemonFighter.inputactions`) with action maps `Gameplay` and `Menu`.
- `PlayerInputAdapter` reads actions and produces commands for the demon of the player: move (facing the camera while attacking), skill use per attack press, eat while the key is held on the food Presentation found under the crosshair (`IPlayerAim`). It raises the stats menu toggle. Nothing else in the project reads input. No `Input.GetKey` anywhere.
- Adding gamepad later means adding bindings to the asset, no code changes.

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
- Test builders: `DemonBuilder`, `RunStateBuilder` in the test assembly so tests read like design statements.

## Performance notes (v1 scale)

- Target: 60 fps on an RTX 3060 class GPU at 1080p. v1 runs with about 10 demons; 50 demons and 200 food items is the stress target for M4 and later.
- Simulation tick must stay under 2 ms with 50 demons.
- No per-frame allocations in `DemonView`, `GoreSystem` or the AI. Pool decals, particles and food views.
- Mesh and LOD budgets are in `ASSET_PIPELINE.md`.
