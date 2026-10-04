# Decisions

Lightweight decision log. One entry per decision: what, why, what it rules out. Add entries at the bottom. Never edit a past entry; supersede it with a new one. Decisions marked "Mario" were made by Mario in design conversations; the rest are technical choices by Claude that Mario accepted.

## Accepted

### D-001: Unity 6 LTS with C#

Mario is a .NET developer. C# in Unity reuses that. Claude Code works well on C# text files, and poorly on Unreal Blueprints. Unreal would give better rendering out of the box but costs C++ and visual scripting. Godot 4 with C# was considered and rejected for weaker realistic 3D and a less mature Steam path. Rules out: switching engines later; the cost would be a rewrite.

### D-002: URP, not HDRP

HDRP renders more realistically but has a steeper learning curve, heavier editor performance and is harder to tune without an artist. URP in Unity 6 with decals, SSAO and post-processing gets the dark, wet, gory look far enough for a solo project on an RTX 30 class GPU. Rules out: HDRP-only features unless we accept a pipeline migration.

### D-003: Windows x64 only, keyboard and mouse only

One target platform and one input scheme for v1. The Input System setup keeps gamepad possible as a data change. Linux and Steam Deck are not planned.

### D-004: Singleplayer now; architecture kept open for both co-op and PvP later (Mario)

No networking in v1. The simulation is command-in, event-out, seeded and deterministic, with no static state and no direct input reads, so a server-authoritative model (needed for PvP, fine for co-op) can be added later. We do not pay for netcode, prediction or lobbies now.

### D-005: English everywhere in the repository and in the game (Mario)

Code, comments, commits, docs and the game's UI are English. No localization in v1. Chat with Mario is in German.

### D-006: Pure C# simulation, Unity for physics and rendering

Game rules live in an assembly with no `UnityEngine` reference so they can be unit tested in milliseconds. Movement, collision and hit detection stay in Unity because writing physics is out of scope. See `ARCHITECTURE.md`.

### D-007: Content as ScriptableObjects, behaviour found by attribute, no Addressables in v1

Parts, skills, mutations, evolutions, archetypes and biomes are assets. Custom behaviour classes are discovered by attribute scan at startup. Addressables add complexity we do not need while everything ships in one build.

### D-008: GitHub (private), Git LFS for imported binaries, Conventional Commits, PR workflow, squash merge

`main` is protected. Every change is a branch and a PR that Mario reviews. Binary assets inside `Assets/` go through LFS. Art source files stay outside the repository for now (D-021).

### D-009: Assets: primitives first, AI-generated 3D later, no Blender pipeline (Mario)

Nobody models by hand. Gameplay is built on primitives through M4. M5 introduces generated assets with the pipeline in `ASSET_PIPELINE.md`. Every asset gets a license entry.

### D-010: Gore and dismemberment are in scope; mature rating accepted (Mario)

Visible wounds, severed parts, blood and viscera are design pillars. The game will be rated for adults. Claude Code does not soften this.

### D-011: Fixed simulation tick at 20 Hz

Independent of frame rate, cheap enough for dozens of demons, fine-grained enough for melee. Configurable; revisit if attacks feel laggy.

### D-012: Two resources: XP for Level and stat points, Biomass for mutations (Mario)

XP from kills and skill use raises Level, grants stat points, gates mutations and triggers evolutions. Biomass from eating buys mutations and regrows lost parts, and is lost on death. Keeps "kill" and "eat" both meaningful.

### D-013: Camera: third-person default, first-person toggle, both from M1 (Mario)

Watching the own body change is the point of the game, so third-person is the start view. First-person stays as a toggle. Both views shape the feel; deciding late would mean re-tuning everything.

### D-014: Mutation menu pauses; confirming requires being out of combat; 2 second invulnerable transformation (Mario)

Browsing is always safe. Confirming needs 5 seconds without damage dealt or taken, then a 2 second transformation during which the demon is invulnerable and cannot act. Because it cannot be triggered in combat, it is not an escape move. Replaces an earlier proposal of a vulnerable transformation, which Mario rejected as bad for the loop.

### D-015: Setting: underground cavern with a glowing ceiling, lava and fissures as light sources, cavern walls as world bounds (Mario)

Looks like a surface at dusk while being underground. No sky, weather or day cycle in v1.

### D-016: Time pressure through rising threat only; no hunger mechanic (Mario)

A run-wide threat level rises with time and drives spawns and elder behaviour. Hunger was considered and rejected.

### D-017: No jumping or climbing in v1; flying is a later idea (Mario)

The world generator must not produce terrain that requires jumping.

### D-018: Demons grow physically with tier: Tier 0 about 1 m, elders about 15 m (Mario)

Collision, camera distance, reach and bite size scale with the body. Every system must handle both ends of the range.

### D-019: Two progression layers: mutations (Biomass, continuous) and evolutions (level thresholds, choose from 2 to 3 options) (Mario)

Mutations grow and upgrade parts. Evolutions reshape the core, grant stat bonuses, unlock part categories and grow the body a size step. AI demons evolve too, by archetype preference. Inspired by Chrysalis evolutions and Everything is Crab specializations; names and content are our own.

### D-020: Three base stats in v1: Strength, Constitution, Agility; stat points on level up; a mind stat later with magic (Mario)

Our own names, not Chrysalis's. Stats are data so more can be added.

### D-021: Art source files are kept locally on Mario's PC, not in the repository (Mario)

GitHub Free has LFS quotas. Only imported assets under `Assets/` go into Git LFS. `ArtSource/` is gitignored. Revisit when quotas or backup become a problem.

### D-022: Damage types Pierce, Cut, Blunt; defense types Thick Hide, Plates, Elastic Tissue, as rock-paper-scissors (Mario)

Bite is Pierce, Claw is Cut, Lunge and Tail Swing are Blunt. The matrix is in `GAME_DESIGN.md`; numbers are tuning data.

### D-023: Bleeding and stamina are in v1; stamina costs every skill except Crawl and Eat, not walking (Mario)

### D-024: Passive HP regeneration always on, scaled by Constitution and improvable by mutations and skills; severed parts regrow only by an explicit Biomass purchase (Mario)

Keeps the Biomass decision with the player.

### D-025: Eating only corpses and severed parts, by holding a key, interruptible (Mario)

No feeding on living demons.

### D-026: Save on quit, resume once, one slot; death still ends the run and resets to Level 1 (Mario)

### D-027: Aim-based part targeting (Mario)

The player chooses which part to hit by aiming. If this proves too hard to make feel good, fall back to "nearest part in the attack arc" and record the change here.

### D-028: Reward scaling by tier difference; elders attackable but ignore the small (Mario)

Prey more than one tier below gives very little XP and Biomass. This is why big demons ignore small ones and why attacking an elder is the only way to get its attention. One or two elders roam from the start.

### D-029: About 10 demons alive at once in v1 for testing (Mario)

Higher counts are a later performance target, not a v1 requirement.

### D-030: No story, no tutorial level, no difficulty settings in v1; first-run hints and shareable seeds yes; music AI-generated (Mario)

Lore only through the environment. Difficulty comes from the seed and the ecosystem.

### D-031: No server, no Azure, no telemetry in v1 (Mario)

A singleplayer Steam game needs no backend; Steam provides cloud saves, achievements and leaderboards. A backend becomes a topic only with telemetry or multiplayer.

### D-032: Release path: local prototype first, then Steam Early Access (Mario)

Nothing in the architecture may assume a one-shot full release.

### D-033: No fixed time budget; Mario wants to learn Unity alongside the project (Mario)

Claude Code explains Unity concepts in PRs (one or two sentences, link to `UNITY_PRIMER.md`). Roadmap order matters more than dates.

### D-034: Test libraries: FluentAssertions 7.x or AwesomeAssertions, plus NSubstitute; free licenses only (Mario)

FluentAssertions 8 and newer carry a commercial license; 7.x is Apache 2.0, and the AwesomeAssertions fork keeps the same API under Apache 2.0. NSubstitute is BSD. Builders and hand-written fakes remain the first choice for simulation state; NSubstitute is for boundaries. No paid tooling without an explicit ok.

### D-035: Git is operated by Mario only; Claude Code never changes Git state (Mario)

No commit, push, branch, checkout, merge, stash, reset or tag from Claude Code. Read-only Git is fine. Claude Code hands over changed files and a proposed commit message; Mario reviews the diff, commits, pushes and opens the PR. The history is Mario's fallback when a session goes wrong, so half-finished states must never be committed by a tool.

### D-036: Code style and process choices (Mario accepted Claude's defaults)

C# 9 only (no record structs, no file-scoped namespaces). Private fields `_camelCase` including serialized ones. `var` only when the type is obvious. Allman braces, 4 spaces, 120 columns, no regions, explicit access modifiers, `internal` by default with `InternalsVisibleTo` for tests. Nullable reference types and warnings-as-errors in the Simulation assembly only. Test names `Method_Condition_ExpectedResult`. Hand-wired composition root, no DI container in v1. Unity `Awaitable` over UniTask. Newtonsoft JSON. Microsoft.Unity.Analyzers, no StyleCop. `dotnet format` on the checklist, not as a hook. Conventional Commits with scope. Squash merge via PR; Mario reviews every change. Milestones in ROADMAP.md, bugs in GitHub Issues. Tags per milestone.

### D-037: Three progression layers: levels, mutations, skill levels; supersedes D-019 (Mario)

Levels (XP) grant allocatable stat points and, at thresholds, an evolution chosen from three options; each option grants a package of allocatable stat points plus free mutations and extra skills. Mutations (Biomass) grow and upgrade the body. Skills level by use, Skyrim style, with perks at skill level thresholds. All three apply to AI demons as well.

### D-038: Unity packages for M0: Cinemachine 3.1.7 and Newtonsoft JSON 3.2.2; unused template packages removed

Added `com.unity.cinemachine` 3.1.7 for the camera rig (D-013) and `com.unity.nuget.newtonsoft-json` 3.2.2 for run saves (D-026, D-036). Cinemachine brings `com.unity.splines` 2.0.0 and `com.unity.settings-manager` 1.0.3 as dependencies. Removed the template packages Visual Scripting, Multiplayer Center, Collab Proxy (Unity Version Control), Rider integration and Timeline: nothing in the design uses them, and fewer packages mean faster compiles and fewer surprises. Input System 1.20.0, Test Framework 1.6.0, uGUI 2.0.0, AI Navigation 2.0.14 and the built-in UI Toolkit module stay. Rules out: nothing; a removed package comes back by adding its name to `Packages/manifest.json`.

### D-039: Test and analyzer libraries as DLLs: AwesomeAssertions 9.6.0, NSubstitute 6.2.0, Castle.Core 5.2.1, Microsoft.Unity.Analyzers 1.28.0

Implements D-034. All four come from nuget.org and are committed as DLLs through Git LFS:

- `Assets/_Project/Tests/Plugins/AwesomeAssertions.dll`: AwesomeAssertions 9.6.0, netstandard2.1 build, Apache-2.0. The maintained fork of FluentAssertions 7 with the same API; since version 9 its root namespace is `AwesomeAssertions`, so tests write `using AwesomeAssertions;`. Chosen over FluentAssertions 7.2.0 because the 7.x line is frozen; both are free.
- `Assets/_Project/Tests/Plugins/NSubstitute.dll`: NSubstitute 6.2.0, netstandard2.0 build, BSD-3-Clause.
- `Assets/_Project/Tests/Plugins/Castle.Core.dll`: Castle.Core 5.2.1, netstandard2.1 build, Apache-2.0. Required by NSubstitute.
- `Assets/_Project/Analyzers/Microsoft.Unity.Analyzers.dll`: Microsoft.Unity.Analyzers 1.28.0, MIT. Built against Roslyn 3.11, so it loads in the Roslyn 4.3 compiler that Unity 6 ships.

Import settings are enforced by `PluginImportRules` (an `AssetPostprocessor` in the Editor assembly) on every import and can be re-applied with `Demon Fighter > Setup > Configure Plugin Imports`. Test DLLs are editor-only with Auto Reference off and are listed as precompiled references of the test assembly. Castle.Core has reference validation off because it references `System.Diagnostics.EventLog`, which Unity does not ship and NSubstitute never calls. The analyzer has every platform disabled and carries the `RoslynAnalyzer` label; loaded as a normal plugin it would inject its `UnityEngine` type stubs into every compilation. Upgrading a library means replacing the DLL and this entry. Rules out: FluentAssertions 8 or newer (commercial license) and OpenUPM wrappers (another registry to trust).

### D-040: `DemonFighter.Common` assembly for Unity-side helpers shared by all layers

The `Log` helper from CODING_GUIDELINES must be reachable from Presentation, Input, UI and App, and no assembly in the original reference graph is referenced by all four. `DemonFighter.Common` holds Unity-side infrastructure without game rules (logging now, small shared adapters later) and is referenced by those four assemblies and by Editor. Simulation does not reference it, so the pure C# rule (D-006) is untouched. ARCHITECTURE.md lists it. Rules out: infrastructure types in Data, and a static logger inside the simulation.

### D-041: Terrain is a generated chunked mesh, not Unity Terrain (resolves O-003)

The cavern floor comes from `CavernWorldGenerator` as a heightfield and becomes 30-cell mesh chunks with mesh colliders in `WorldBuilder`. Fully code-driven and deterministic, no terrain data or texture assets while the art is primitives, per-chunk culling for free, and a future server runs the same code headless. Unity Terrain would add LOD and painting we do not need yet and texture layers we do not have. Rules out: nothing for M5; a terrain-based builder could replace the mesh builder behind the same layout data.

### D-042: Simulation vectors are System.Numerics

`System.Numerics.Vector2` and `Vector3` from the .NET Standard 2.1 profile Unity ships, converted at the boundary by `SimulationVectors` in Presentation. Axes match Unity (X east, Y up, Z north) and the yaw is radians clockwise from north, so conversions are plain copies. Rules out: own vector structs and `UnityEngine.Vector3` inside the simulation.

### D-043: Content and placeholder assets are generated, starting with the biome

`BiomeDefinition` is the first ScriptableObject content type (id `biome.ash.cavern`), converted to an immutable `BiomeSpec` at bootstrap and validated in `OnValidate`. `Demon Fighter > Generate > Placeholder Assets` creates the URP materials, the `PlaceholderPalette`, the world, demon view and camera settings assets, the biome asset and the `P_Demon` prefab, and updates them in place on re-run; `Generate > Scenes` wires them into the scenes, so no reference is set by hand. Rules out: materials created in code at runtime (shader stripping in player builds) and hand-made assets for the primitive stage.

### D-044: Bodies are moved by their views; the simulation integrates only demons without a body

A `MoveCommand` sets the demon's intent. `DemonView` moves the `CharacterController` from that intent every frame and writes the resulting pose back; the simulation integrates positions only for demons without a body (tests, a headless server). Facing follows the movement direction in third person and the camera in first person. One mover per demon, no double movement, and the collision result is what the AI sees. Rules out: a simulation prediction that Unity then overrides every frame.

### D-045: Elders patrol; mouse look is routed through the input adapter

The utility brain has a `Patrol` goal for route following next to `Wander` and `Rest`; the elder archetype weights it highest, the blob archetype never picks it. `PlayerInputAdapter` reads the mouse and forwards deltas and the view toggle to `CameraRig` through `ICameraControl`, and WASD is camera-relative through `IHeadingProvider`; Cinemachine's own input component stays unused, so one class reads input. Rules out: Cinemachine reading actions directly.

### D-046: Hits are detected in Unity and judged by the simulation

A `UseSkillCommand` starts a skill; the simulation opens its active window (windup, active ticks and recovery are data on `SkillSpec`, shortened by Agility) and announces it with `SkillActivated`. Presentation detects the touch during the active ticks: the player with a sphere sweep along the crosshair ray from the camera, AI with a forward sweep from the eyes, both against body part trigger colliders on the `Demon` layer. The touch goes in as a `ReportHitCommand`, which the simulation accepts only inside the window (one tick of grace), once per use, for a living part within reach and arc; the facing of the move intent counts as the aim. The skill behaviour then applies the hit. Rules out: damage computed in Unity, and hits the view can force on the rules.

### D-047: Skills are data plus attribute-found behaviours, validated at run start

`SkillSpec` holds the damage type, the numbers and the timings; `BehaviourId` names a class tagged `[SkillBehaviour]` that implements `ISkillBehaviour`. `SkillBehaviourRegistry` scans the Simulation assembly once and `SimulationTicker` validates every catalog skill against it when a run starts, so an unknown behaviour fails before the first tick. Bite, Claw and Tail Swing share `MeleeStrikeBehaviour` (`melee-strike`). Per demon, a `SkillInstance` carries level, XP and cooldown and is usable only while the part that grants it is attached. Rules out: a switch over skill ids, and behaviour lookups at hit time.

### D-048: Eating is a held command; reward scaling is one formula on the tuning

`EatCommand` is sent every tick the key is held (AI brains re-send it between decisions). The eating stage moves `EatBiomassPerSecond` out of the food and into the eater, scaled by `CombatTuning.RewardFactor(receiverTier, sourceTier)`: full at equal tier or one below, plus 50 percent per tier the source stands above, 10 percent two or more tiers below. The same factor scales kill XP. A tick without the command, a started skill or a hit from another demon ends the meal; bleeding does not. Food rots away after `FoodDecaySeconds` in the decay stage. Rules out: an eat toggle the player could leave on, and separate scaling rules for XP and Biomass.

### D-049: Kill XP, a skill XP share and stat points through commands

A kill grants `KillXpBase x (victim tier + 1) x reward factor` to the killer; half of every skill XP gain (`CharacterXpPerSkillXp`, tuning) also becomes character XP, as GAME_DESIGN asks. `XpSystem.Grant` is the only place XP is handed out and publishes `XpGained` and `LevelUp`. Stat points are spent with `SpendStatPointCommand`, so the Stats tab and a future AI mutation brain use the same path; `SimulationTicker.ApplyPendingCommands` applies queued commands without advancing time while the run is paused. Rules out: UI code writing stats directly.

### D-050: AI perception is distance-based in the simulation; Hunt, Eat and Flee join the brain

Perception is a pure scan of the run state within the `PerceptionRadius` of the archetype, so AI stays deterministic and testable without Unity; the `IPerceptionProvider` idea from the architecture draft is dropped. Prey more than one tier above is never hunted; prey two or more tiers below only when it attacked the demon recently (`Demon.LastAttackedBy`), which is how elders ignore blobs until bitten. Wounded and eating prey score higher. Idle goals are interrupted the moment prey or food comes into view. A demon below `FleeHealthFraction` with a fight nearby sprints away from the nearest fighter; the M2 content sets that threshold to 0 for both archetypes because fights are hard to test when prey runs (Mario), a later milestone turns it on. Standing attackers face their prey through `MovementIntent.Facing`, which the view turns toward. Rules out: Unity overlap queries feeding the brains, and omniscient AI.

### D-051: Gore stage 1, corpses as food views, menus pause the runner

Each body part is a `BodyPartView` with a trigger collider on the `Demon` layer; wounded parts darken and shrink, lost parts vanish. The view of a dead demon becomes the corpse: it lies flat on the `Food` layer with the corpse material and carries the `FoodView`; severed parts are rigidbody spheres that write their resting position back into the simulation. `BloodDecalPool` reuses a fixed number of ground quads and body blobs. The two layers are created by the placeholder asset generator through the TagManager asset. The Stats panel and the death screen pause `SimulationRunner`, which then only applies queued menu commands. Rules out: a separate corpse prefab, and per-hit allocations for blood.
### D-052: Attacked demons retaliate, whatever the attacker is worth (Mario)

A hit makes the attacker the prey of its victim on the next decision, overriding wandering, resting, patrolling and eating, but not fleeing; the reward discount for prey far below does not apply to a provoked demon, so an elder bitten by a blob turns on it as GAME_DESIGN asks. Archetypes with a hunt weight of zero never retaliate. The demon remembers who hit it and when (`LastAttackedBy`, `LastAttackedTick`); the memory expires with the combat window. Rules out: a bitten blob that shrugs and rests, and an elder that ignores the player chewing on its leg.

### D-053: The Tier 0 population is topped up during a run (Mario)

A spawn stage spawns one Tier 0 demon per `RespawnSeconds` (biome data, 12 s) while fewer live than `InitialBlobs`, between `RespawnMinDistance` and `RespawnMaxDistance` from the player, inside the bounds and clear of features, with a brain and a `DemonSpawned` event the App layer answers with a body. Pulled forward from M4 because the arena emptied within minutes; the threat level that scales spawns over time stays an M4 item. Rules out: a run that ends because nothing is left to fight.

### D-054: Tier and size follow body investment and evolutions

Tier is the spawn tier plus the evolutions taken plus one per `TierInvestmentStep` (4) investment points, where every part beyond the core, lost or not, counts one point and every upgrade level one more. Size is the spawn size grown by `SizeStepPerTier` (50 percent of the spawn size) per tier gained, so a 1.2 m blob with four parts stands 1.8 m tall and an evolved one 2.4 m. The view, the `CharacterController` and the camera framing read the size; the HUD shows tier and size. Rules out: tiers as hand-assigned content labels, and parts that change the tier without changing the body.

### D-055: Mutation costs, requirements, sockets and the menu flow (resolves O-001 for v1)

The core has sockets with capacities (Head 2, Limb 2, Locomotion 1, Hide 1, Tail 1); a part occupies one slot of its socket kind and a lost part keeps its slot until it is regrown. Costs in Biomass: the base cost of the part (Eyes 20, Jaws 25, Arm 30, Legs 40, hides and Tail 35), a further copy of a part the body already holds costs `RepeatCostMultiplier` (1.5) times that, an upgrade costs `UpgradeCostFraction` (one half) of the base per level reached, a regrow `RegrowCostFraction` (one half) of the base. Requirements: a character level per part (`MinLevel`, for example Legs 2, Tail 4) and per repeat copy (`RepeatMinLevel`, second Arm 3), upgrade level n needs character level 2n (`CharacterLevelPerUpgradeLevel`), parts can require other parts, and Plates, Elastic Tissue, Spines and Tail need an evolution unlock. Upgrades go to +5 (the core cannot be upgraded); a level adds `PartHpPerUpgradeLevel` (15 percent) of the base HP and one more helping of the stat and skill bonuses of the part. The menu: Tab opens the mutation menu on Mutate, C on Stats, any menu key closes it; opening pauses the run; a confirmed mutation or evolution closes the menu, resumes the run and the command applies on the next tick; Apply in the Stats tab spends through commands while paused and keeps the menu open. The shop (`ShopOfferPolicy`) lists every attach, upgrade and regrow with cost and the reason a confirm is disabled; `RandomOfferPolicy` offers a hand of three for the M4 playtest. Rules out: free sockets without limits, upgrades past +5, and a menu that confirms while the world keeps moving.

### D-056: The v1 skill set, its slots and passive Sprint

One key per slot, resolved through `SkillSlots.Find` from the skills the attached parts grant, highest priority wins: left mouse Primary (Bite, Claw with priority 1 replaces it), right mouse Secondary (Grab), Space Lunge, Q Tail Swing, Shift Sprint. Claw: 8 Cut, 10 stamina, 0.1 s windup, 0.15 s active, 0.25 s recovery, 0.4 s cooldown, 4 s of bleeding at 3 per second. Grab: 20 stamina, 4 s cooldown, holds a smaller target for 1.5 s; a held demon ignores its own commands and the AI skips it. Lunge: 10 Blunt, 25 stamina, 3 s cooldown, a 4 m dash during the active window, 2 m of knockback over `KnockbackSeconds` (0.3 s) scaled by the size ratio, 0.5 s of stagger. Tail Swing: 14 Blunt in a 150 degree arc, 20 stamina, 2 s cooldown, 0.6 s of stagger. Sprint is a passive skill granted by Legs: while the sprint flag of the move command is set and at least one stamina is left it drains 15 stamina per second and earns 2 skill XP per second; Legs also add 50 percent movement speed. Skill XP for hits is scaled by the reward factor of the target tier; half of every skill XP gain is character XP (D-049). Rules out: a key that does nothing because a skill is bound to it by name, and sprinting without legs.

### D-057: Skill progression numbers and one perk per skill (resolves O-011)

Per skill level above one: damage plus `DamageBonusPerLevel` (5 percent), stamina cost minus 3 percent down to half, cooldown minus 2 percent down to half, windup, active and recovery 2 percent faster, reach plus 1 percent; the curve is 100 XP times the level to the power of 1.5, max level 20. At level 10 every skill gains its one perk as data on the spec (`SkillPerkSpec`, multipliers only): Deep Bite (bleed lasts half again as long), Quick Claw (recovery a quarter shorter), Iron Grip (hold 2 s), Long Leap (dash a quarter farther), Tireless (sprint costs 30 percent less), Heavy Tail (stagger half again as long). Levels freeze while the granting part is lost and return with it; a skill an evolution grants has no part to lose. Rules out: perks as code branches, and perk trees before playtests ask for them.

### D-058: Spines return damage; Eyes extend perception and reach

Spines (Hide socket, unlock) deal `ReturnDamageFraction` (30 percent) of every melee hit taken back to the attacker as plain Pierce on its core; the returned damage never returns again, so two spined demons cannot chain. Eyes (Head socket, 20 Biomass) add `PerceptionBonus` (30 percent) to the perception radius of an AI demon and, through `Demon.ReachMultiplier`, 30 percent to the reach of every hit and of eating for any demon; Presentation uses the same factor for its aim and eat scans, so what the crosshair accepts the simulation accepts. The Spines mechanic was offered as a decision and accepted; GAME_DESIGN names Eyes without an effect, and this is it. Rules out: thorns that scale with armor or chain, and sensory parts without a gameplay effect.

### D-059: Evolution lines, thresholds and packages in v1 (resolves O-009)

Thresholds at character level 5 and 10 (`EvolutionLevels`); each stage offers the three lines, ordered by how many points the demon has in the fit stat of the line: Brute (Strength; 6 then 8 stat points, Strength cap +5 per stage, unlocks Plates and Tail, Thick Hide free, later Jaws free), Stalker (Agility; 4 then 6 points, Agility cap +5, unlocks Spines, Legs free, later Eyes free), Bulwark (Constitution; 5 then 7 points, Constitution cap +5, unlocks Plates and Elastic Tissue, Elastic Tissue free, later Spines unlocked). Stat caps start at `BaseStatCap` (10) and only evolutions raise them; a free part is attached only when a socket is free and otherwise skipped. Evolving follows the calm rule and the 2 second transformation of D-014, raises the tier by one and with it the size (D-054). AI demons do not evolve or mutate yet; that is the M4 ecosystem work. Rules out: evolution options generated at random, and caps that mutations can raise.

### D-060: The out-of-combat rule for mutating and evolving is suspended (Mario)

Opening the mutation menu and finding every button disabled because a fight had just happened felt wrong in the first M3 playtest. Until a better rule is found, `MutationRules.CanMutateNow` asks only for a living demon that is not transforming; the menu pause and the 2 second invulnerable transformation of D-014 stay. Known consequence: a mutation started in a fight buys 2 seconds of invulnerability, which D-014 wanted to rule out; the replacement rule (candidates: a vulnerable transformation, a Biomass surcharge in combat, a cooldown after taking damage) has to close that. Supersedes the combat clause of D-014; `InCombatSeconds` stays for the AI.

### D-061: A grabbed demon is dragged along by its holder (Mario)

Grab rooted its target; Mario wants to take it with him. The hold stores the grab spot in the frame of the holder, pulled in until the two bodies touch, and `HoldSystem` pushes the held demon toward that spot every tick, at most 12 m/s, until the hold ends, either side dies or the holder is gone. The held demon still ignores its own commands and the AI skips it. Rules out: a hold that only roots the target, and a holder slowed by what it drags (not for now).

### D-062: Playtest leveling pace

The tuning asset now asks 30 XP times the level to the power of 1.2 for the next level (was 100 and 1.5): level 5 after about 370 XP, roughly five Tier 0 kills with their bites, level 10 after about 1900 XP. The `CombatTuning` record keeps the old numbers as the test baseline; the asset is the truth for the game and the `CombatTuningDefinition` defaults match the asset. Revisit with the threat level in M4.

### D-063: The body preview is a second camera on a stage below the world

The Mutate tab shows the own body as a live render: `BodyPreviewRig` builds the prefab capsule and the same part primitives the world uses from a body the menu composes (own parts plus the offers toggled to Preview), on a stage 500 m below the world on a Preview layer only its own camera sees, into a RenderTexture the menu shows as a background image; fog is switched off for the frames of that camera. Chosen over a UI-drawn silhouette because it reuses the part visuals one to one and shows the real result of a purchase, including the size step. Rules out: a second copy of the visual data for the menu.

### D-064: Mutations are selected together and applied at once (Mario)

Like the Stats tab, the Mutate tab is a planner: offers are selected, the preview shows them on the body, the footer sums the cost against the Biomass, and one Apply sends every selected mutation as its own `MutateCommand` in the same tick, new parts first. The menu refuses a selection the Biomass or the free sockets could not carry; the simulation still validates every command. Mutations that start in the same tick share one transformation: `MutationRules.CanMutateNow` lets a mutation join a transformation that began this very tick, so a batch reshapes the body once for 2 seconds. Rules out: a batch command type, and one transformation per bought part.

### D-065: Grab holds anything no bigger than you; the aim marks the part under the crosshair (Mario)

Grab refused demons of equal size, so a blob could never grab a blob and the skill looked dead in the playtest; now only a bigger demon shrugs it off. The part under the crosshair of the player, within eight body heights (stretched by the senses), is tinted gold on the body and named under the crosshair together with the kind and tier of its owner, and "out of reach" when the primary attack would not land from here, so aiming a bite at an arm, a leg or the core is a visible choice (D-027). Hit reports and the marker read the same reach rule (`SkillReach`). A hit that deals no damage, such as a grab, draws no blood. Rules out: a grab that needs a size advantage, and aiming blind.

### D-066: Analyze key and sense levels (Mario)

F locks the demon under the crosshair; a panel then shows what the senses of the player reveal, and the lock drops when the target dies or F is pressed again. The sense level is the perception bonus of the body divided by `PerceptionPerSenseLevel` (0.3, so one pair of Eyes is one level): level 0 shows the parts and visible wounds, level 1 adds the health of every part, bleeding and the skills the parts grant with their levels, level 2 adds level, stats, evolutions and the Biomass the corpse would hold. This is what Eyes and later sensory parts are for beyond reach and AI perception. Rules out: a target panel that shows everything to everyone.

### D-067: Evolution packages give bound stat gains, not a free pool (Mario)

An evolution gives a specific number of points to specific stats (`StatBonuses`): Brute +10 Strength, Stalker +10 Agility, Bulwark +10 Constitution at stage one; stage two adds +10 of the line stat and +5 of a second one (Brute Constitution, Stalker Strength, Bulwark Agility). A bound gain raises the stat and its cap by the same amount, so it always fits; the separate cap bonus of +5 stays as headroom for level points. The free pool (`StatPoints`) stays in the data at zero. The free parts and unlocks remain the special mutations of each package; Bulwark two now gives Legs for free. The six evolution assets are rewritten once by the generator (content version 2). Supersedes the free-pool default of O-009 and D-059. Rules out: evolutions that only hand out points to spend.

### D-068: Body blood dries (Mario)

Blood blobs stuck to a body stayed until the pool reused them, so a healed demon still wore every old hit. A body blob now lives 15 seconds, shrinks away over the last third of that and goes back to the pool; ground pools stay until the pool reuses them, the trail of a fight. Look only, nothing in the simulation. Rules out: blood that outlives the wound.

## Open

Each open item has a proposed default. Work proceeds with the default until Mario decides.

### O-001: Mutation offers: shop (A) or three random offers (B)

Default: A for v1. Implemented behind `IMutationOfferPolicy`; B gets a playtest in M4. Resolved in M3 for v1: A, see D-055; B exists as `RandomOfferPolicy` for the M4 playtest.

### O-002: Run end conditions beyond death

Default: none in v1. Candidates: reach an exit, kill an elder, survive N minutes. Decide after M4 playtests.

### O-003: Terrain technology: Unity Terrain vs generated mesh

Default: Claude Code picks in M1 based on what the world generator needs and records the choice here. Resolved in M1: generated chunked mesh, see D-041.

### O-004: Steam library: Steamworks.NET vs Facepunch.Steamworks

Default: decide in M6.

### O-005: CI on GitHub Actions for Unity builds and tests

Default: not in M0. Unity in CI needs license activation (GameCI is the usual route). Add when local test runs become the bottleneck.

### O-006: Unity MCP server for Claude Code

Default: not yet. Revisit when manual editor round trips slow things down.

### O-007: Company and product name for Steam, and the final game title

Default: "Demon Fighter" as working title. Mario decides before M6.

### O-008: v1 size range for the player

Default: player content reaches about Tier 3, roughly 3 meters. Elder sizes exist as AI content only in v1.

### O-009: Evolution thresholds, option count and package shape in v1

Default: Level 5 and Level 10, three options each; each option's stat pool is fully free to allocate, with an option-specific size and caps. The evolution lines (for example Brute, Stalker), their packages and names are Mario's call when the content is written in M3. Resolved in M3: Brute, Stalker and Bulwark, see D-059; their stat gains became bound in the playtest, see D-067.

### O-011: Skill perks in v1

Default: one perk per skill at skill level 10, defined as data on the `SkillSpec`. More perks and branching later. Resolved in M3: see D-057.

### O-010: Chrysalis demon evolution lines as inspiration

Mario mentioned demon types from the books (murder, chaos, war, massacre and others). Claude could not find a reliable spoiler-safe source. If Mario lists them from memory, they inform the direction of our evolution lines; names stay our own.
