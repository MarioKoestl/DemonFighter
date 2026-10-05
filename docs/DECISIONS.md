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

### D-069: Threat rises half a level per minute, capped at ten

The threat level is the only clock of a run (D-016): `RunState.Threat` is simulated minutes times `ThreatPerMinute` (biome data, 0.5), capped at `ThreatMaxLevel` (10), so level 1 comes at two minutes and level 5 at ten. It is derived from the tick, needs no saved state and is zero until the world is attached. A `ThreatSystem` stage announces every whole level crossed as `ThreatLevelChanged`; the HUD shows a thin meter at the top. The spawn table, the blob cap, the respawn interval and the elder route read the level (D-070 onward). Rules out: threat driven by player actions or kills in v1.

### D-070: Spawns follow the threat: a table of kinds, a growing cap, faster respawns, elders drawn in

The biome holds a spawn table (D-016, D-069): entries of a demon kind, the whole threat level it appears from and a weight; among the entries the threat has unlocked, one is drawn by weight for every spawn. Stronger spawns are kinds born with a package: `DemonSpec` gains starting parts, Biomass, level and an evolution, applied at spawn without cost or transformation through the same code the Evolve command uses (`EvolutionPackage`). Ash Cavern: Blob always; Hunter (Arm, 30 Biomass, level 2) from threat 2; Stalker (Legs, Eyes, 40 Biomass, level 3) from 4; Brute (Arm, Jaws, Thick Hide, 60 Biomass, level 5, the first Brute evolution) from 6. The population cap grows by `BlobsPerThreatLevel` (1) per level and the respawn interval is divided by one plus `RespawnSpeedupPerThreat` (0.15) per level; elders never count toward the cap. Patrolling archetypes pull every route target toward the living player by `RoutePullPerThreat` (elder 0.1) per level, at most `RoutePullMax` (0.7), so the elder wanders closer as the run runs long while its reward rule still makes it ignore the small. The generator writes the three new demon assets and gives the old biome asset the table once. Rules out: hand-placed waves, and a run whose danger never changes.

### D-071: Three blob personalities drawn by weight; fleeing is back on

New blobs draw an archetype from the biome by weight (GAME_DESIGN, "AI demons"): Aggressive (weight 50: hunts most, flees below 15 percent health, wants Jaws, Arm and Legs, takes the Brute line, puts points into Strength), Cautious (30: eats and rests more, flees below 40, wants Thick Hide, Eyes and Legs, the Bulwark line, Constitution) and Scavenger (20: eats most, hunts little, flees below 50, wants Legs, Eyes and Jaws, the Stalker line, Agility). Archetypes are assets (`AR_*`) the biome asset lists with weights; the elder keeps its nested personality; the draw uses the run Rng, so a seed reproduces the mix. The preferences feed the AI mutation, evolution and stat goals (D-072). Fleeing, switched off in the M2 content for testing (D-050), is back on through these thresholds. Rules out: one personality for every blob.

### D-072: AI demons grow in calm moments by the preferences of their archetype

Every decision an AI brain first spends one unspent stat point into its preferred stat, or the next stat with room. When it is not hunting, eating or fleeing and has dealt or taken no damage for the combat window, it grows one step: a pending evolution is taken at once, choosing the offered option whose fit stat is the preferred one; otherwise it buys the first part of its preference list the rules allow, regrows a lost part, or takes the cheapest upgrade it can pay. The AI keeps the out-of-combat rule for itself although the player's is suspended (D-060), so no demon stops mid-fight to become invulnerable. The same commands and rules as the player's, so cost, level, sockets and unlocks bind the AI too and the views grow the parts by themselves. Rules out: scripted builds, and AI that cheats on cost or level.

### D-073: A run is saved as a plain snapshot the simulation writes and reads itself

`RunPersistence.Capture` turns a run into `RunSnapshot`, plain data with no behaviour and no Unity type: seed, ticks per second, tick, Rng state, id counters, the spawn timer, every demon (kind, pose, level, XP, Biomass, stamina, kills, evolutions, stats with unspent points and cap bonuses, unlocks, every part with HP, upgrade, lost flag and wound, every skill with level, XP and cooldown, the status timers, held and push state, what it eats, the name of its archetype) and every food item. `Restore` rebuilds the state, regenerates the world from the seed, re-adds demons and food, continues the Rng and the id counters, and gives living AI demons a fresh brain of their saved archetype. A skill use in progress, the movement intent and the goals of the brains are not saved, so a resumed run continues deterministically from the saved numbers while an AI may change its mind once. The App layer serializes the snapshot as JSON (D-026); the simulation never sees JSON. Rules out: saving Unity objects, and a format only one library can read.

### D-074: Save on quit, Continue in the menu, Esc pause panel (the App side of D-026)

`RunSaveService` keeps one slot, `run.json` under the persistent data path, written atomically as Newtonsoft JSON from the snapshot of D-073. Esc opens a pause panel with Resume and Save and Quit; Save and Quit writes the slot and returns to the menu, and closing the application mid-run writes it too (`Application.quitting`, which also fires when Play Mode stops in the editor). The main menu shows Continue while the slot holds a run; Continue loads the Run scene, rebuilds the run from the slot and empties it, so one save resumes once; death empties it as well. After a resume, dead demons whose corpse still lies there become corpses again, dead demons whose corpse was eaten lose their body, and severed parts lie where they fell. Esc while the mutation menu is open closes the menu instead. Rules out: several slots, save scumming, and saving after death.

### D-075: The death screen reads one summary record, and a no-op hook receives it

`RunSummary.From` reads the finished run once: seed, ticks and seconds survived, kills, Biomass eaten, level, highest and final tier, the evolutions taken by id, every part of the final body and the kind that dealt the final blow (empty when the player bled out). The death screen shows it, with the killer, the evolutions by name, the skills with their levels and the body with upgrades and conditions; `IMetaProgression.RecordRun` receives the same record and `NoMetaProgression` drops it, which is all v1 does (GAME_DESIGN, "Meta-progression"). The demon tracks `HighestTier` and `EvolutionIds` for this; both are saved with the run. Rules out: a death screen that computes its own numbers, and meta-progression wired into the simulation.

### D-076: First-run hints and a playtest settings box

Four hints appear once per installation, six seconds each at the top of the HUD: the first corpse under the crosshair (hold E), the first Biomass (Tab), the first stat points (C) and the first evolution ready (Tab, Evolve). PlayerPrefs remembers them (`HintMemory`). The main menu gets a Settings button with a small box: the offers toggle between the whole shop (O-001 A) and a random hand of three (B, `RandomOfferPolicy`), read when a run starts, and a button that shows the hints again. Play Mode tests reset the hints they may have marked. The real settings menu (graphics, audio, mouse) stays M5. Rules out: a tutorial level, and hints that return every run.

### D-077: A headless autoplay harness and a stress test pace the ecosystem

`AutoplayHarness` (editor tool, also `-executeMethod` in batch mode) plays seeded runs without a frame: the aggressive AI stands in for the player, the real content assets are loaded, and the report lists when and by whom the protagonist died, its tier, level, kills, parts and evolutions, the threat and the population at the end. Hits need no frame: `HeadlessHitResolver`, switched on only for the harness and tests, reports a hit on a random intact part of the nearest demon in reach and arc for every active skill use, through the same command the presenter sends. Three seeds for fifteen minutes is the default; the report lands in TestResults/autoplay.txt. `StressTests` ticks a run with 50 demons and 200 food items and asserts the average tick stays under 2 ms (ARCHITECTURE, "Performance notes"). Rules out: tuning by feel alone, and performance regressions nobody notices.

### D-078: A busier world: more blobs, faster and nearer spawns (Mario)

The first M4 playtest found too little going on. The Ash Cavern now starts 16 blobs spread over a 30 m cluster (was 10 in 15 m; the wider spread keeps the start from being one brawl), tops the population up every 6 seconds (was 12) between 25 and 55 m from the player (was 35 to 70), lets the cap grow by two per threat level (was one) and shortens the interval by 20 percent per level (was 15), so about 30 demons roam at threat 7. The stress test at 50 demons leaves room. The biome asset carries a content version and the generator rewrites the pacing numbers of an older asset once. Rules out: a world the player has to search for.

### D-079: Art pipeline conventions: briefs in docs, FBX, binding by name, socket anchors as data

Briefs live in `docs/briefs/` so they are versioned and reviewable; the heavy sources stay in the gitignored `ArtSource/`. Models are exported as FBX, because Unity imports FBX without a package and GLB would need glTFast (a dependency, rule 9). Art reaches the content by name, never by hand: `ArtAssetBinder` maps `BP_<Part>[_<Variant>][_<State>]` to `part.<part>[.<variant>]` and fills the mesh set of the part asset, `ArtImportRules` applies the import settings as files are imported, and `ArtAssetValidator` checks naming, budgets, damage states, sockets, scale, pivot and the license log. Socket positions are data on the core asset in body units (`SocketAnchorDefinition`, origin at the base, 1 is the body height): filled from the `Socket_` transforms of a core model when it has any, defaulted to the placeholder capsule otherwise, tunable in the Inspector, so a generated mesh without empties still works. Three fit knobs per part (scale, offset, rotation) replace re-exports. The import rules name extracted materials after their model, so two generated models whose materials share a name (Material_0) never share one material, and the binder switches the extracted material of a bound model to the skin shader (D-081). The game stays complete with placeholders: a part without a bound model is drawn as its primitive. Rules out: GLB imports, hand-dragged mesh references, a prefab variant per part to hold sockets.

### D-080: Visual damage stages are presentation only

The view shows four stages, intact, wounded, mangled and lost (a stump when the part has one), derived from the HP fraction of a part with thresholds in `DemonViewSettings`: wounded below 50 percent, mangled below 20. The simulation keeps Healthy, Wounded and Lost and its own wounded threshold; no rule changed. A mesh set carries one mesh per stage and falls back to the next healthier one; a primitive shrinks and darkens instead. Rules out: a fourth condition in the simulation, and gameplay that depends on the mangled look.

### D-081: Gore stage 2: decal projectors, a skin shader, code-born viscera, parts that fall as they looked

Blood on the ground is URP decal projectors (`BloodDecalPool`): splats under hits and bleeding, pools that spread under corpses over twenty seconds and grow with the Biomass the corpse holds, all wrapped over the terrain. A run keeps up to 256 splats and 64 pools and replaces the oldest when the cap is reached; the decal renderer feature is added to the PC renderer by `RenderPipelineSetup` from code. Blood on bodies is the skin shader `DemonFighter/DemonSkin`: URP Lit property for property (a Lit material becomes a skin material by swapping the shader) with a per-renderer blood amount, wound center and radius set through a MaterialPropertyBlock by `BodyPartView`, so the material stays SRP-batcher compatible; a hit soaks the part where it landed, bleeding seeps, and the blood dries over 15 seconds (D-068). Only the forward pass is ours; shadows, depth and depth normals are the URP passes, so lighting follows the pipeline version. Viscera (`VisceraPool`) are lumps and strands built from code, thrown on a plain ballistic arc without physics, resting on the ground found under the burst and shrinking away after 25 seconds; no new layer, no rigidbodies. A severed part falls as a copy of the view it had (mesh, material, tint) with a box collider and a rigidbody. The placeholder blood textures are painted by `TextureGenerator` into `Art/Textures/Decals/Generated/`; a hand-made file under the same name wins. Rules out: blood quads and spheres, physics-driven gibs, a second material set per demon for the bloody look.

### D-082: All body motion is procedural in M5; parts keep a clip slot for later

Mixamo clips are humanoid and do not fit a modular blob with parts on sockets, and generated meshes rarely deform well (ASSET_PIPELINE, "Animation"), so no clips are used in M5. `BodyAnimator` moves everything from the state the simulation already has: the figure breathes, stretches along its run and squashes across it, bobs with a gait whose cycle follows speed over stride, swells and nods on a bite, stretches on a lunge, shakes when hit and throbs through a transformation; legs step against each other, limbs swing with the gait and strike, jaws open and snap, tails sway and whip. Attack timing is the skill's windup, active and recovery, so the strike reads on the frames the simulation counts, for placeholders and real meshes alike. What moves how is data: `PartMotion` on the part asset (legs, limb, jaws, tail, none) and `SkillMotion` on the skill asset (bite, swipe, lunge, tail swing, none); the amplitudes live in `DemonViewSettings`. Every part keeps a clip slot on its mesh set: a legacy clip imported under `Art/Animations/` plays in a loop on that part and the animator leaves it alone. Rules out: Mixamo, Animator controllers and state machines in v1, and attack timing baked into clips.

### D-083: The URP look is generated: a cavern volume profile, three preset assets, a shadow budget, painted surfaces, two-level LODs

`RenderLookGenerator` builds the post-processing profile `VP_Cavern` once (ACES tonemapping, a warm dark grade with a touch less saturation, bloom for lava and embers, vignette, light film grain, a hint of chromatic aberration, no motion blur) and the scenes carry a global Volume with it; the cameras render post-processing with SMAA. Graphics presets are three pipeline assets, not Unity quality levels: the PC assets stay High, `URP_Low` and `URP_Medium` are copies with their own renderer copies (so ambient occlusion can be off on Low without touching the PC renderer), lower render scale, shorter and softer shadows, fewer cascades; `GraphicsPresets` lists them, High is the default, and `GraphicsQuality` switches the active pipeline (the settings menu wires it, M5 item 6). Lighting: three-color ambient so undersides stay dark, soft shadows from the ceiling glow, and a `ShadowBudget` that lets only the four point lights nearest the camera cast shadows, re-picked twice a second; lava and fissure lights flicker with smooth noise, lava crusts drift. Surfaces: `TextureGenerator` paints tileable base color and normal maps for ground, rock, wall and lava (plus a lava emission map) from periodic noise into `Art/Textures/Surfaces/Generated/`; a hand-made PNG under the same name wins. Level of detail without extra meshes: every demon gets an `LODGroup` with two levels, parts dropped below 4 percent of the screen height and the body culled below 0.6 percent, rebuilt when a part grows. Rules out: Unity quality levels as the preset mechanism, per-material shader variants for the look, hand-authored lighting in the scene files.

### D-084: Audio is data-driven events over a pooled player, with synthesized placeholders and no mixer asset

A sound is an `AudioEventDefinition` asset: clips to pick from, volume and pitch with their variance, 3D or in the head, distances, a cooldown. Skills carry their use sound, parts the sound of losing them, the biome its cavern drone, lava loop and calm and combat tracks, and `AudioCatalog` holds the rest (footsteps, eating, mutation, evolution, level and threat cues, deaths, the fallbacks, the menu track). `AudioDirector` subscribes to the simulation events of a run and plays them through `SoundPlayer` (a pool of 24 sources, oldest cut when full), meters footsteps from the real movement of every body with a pitch by size, keeps the drone in both ears and the lava loop on the pool nearest the player (`AmbientPlayer`), and asks `MusicPlayer` for calm or combat by the player's combat window; the player's own sounds play in the head, everyone else's at the body. `MusicPlayer` crossfades two looping sources on the persistent bootstrap object, so the menu track survives scene loads. Volumes are four knobs in `AudioMix` (master, effects, ambient, music) read at play time and on change; no AudioMixer asset, because one cannot be built from code (M6 may revisit). Until recorded or generated clips exist, `AudioSynth` writes deterministic placeholder WAVs (effects, two loops, three music loops) that `AudioImportRules` imports as Vorbis, effects mono and decompressed, loops and music streaming; a file dropped under the same name wins, and the music placeholders are temporary by design (D-030). Rules out: an AudioMixer asset in v1, sounds wired in code by skill name, music that restarts on every scene.

### D-085: Menus share one settings panel; settings live in settings.json and apply at boot

The main menu is New Run with an optional seed (a number is used as is, any other text becomes a seed by a stable hash, so a word shared in chat gives the same cavern everywhere), Continue while a save exists, Settings and Quit. The pause menu behind Esc is Resume, Settings, Save and Quit back to the menu, and Quit to Desktop, which saves the run through the quitting hook (D-074). Both open the same `SettingsPanel` with four tabs: Graphics (preset from the catalog of D-083, resolution, fullscreen, vsync), Audio (master, effects, ambient, music), Controls (mouse sensitivity as a multiplier on the tuned look, invert Y, the key list) and Playtest (the offers toggle of O-001, the hint reset). The panel edits plain `SettingsValues` and raises the whole set on every change; `SettingsApplier` writes it into `settings.json` next to the save (Newtonsoft, atomic write, defaults when missing or unreadable) and pushes it into the pipeline, the screen, the audio mix and the camera rig, and does the same once at boot. The old PlayerPrefs offers toggle of D-076 is carried over on the first load and deleted; first-run hint memory stays in PlayerPrefs because it is memory, not a setting. In the editor, Unity writes a preset or vsync switch into the project's quality settings; `PlayModeQualityGuard` remembers both when Play Mode starts and restores them when it ends, so testing presets never leaves a change to commit. Rules out: PlayerPrefs for settings, a settings menu per screen, bindings UI in v1 (keyboard and mouse only, D-030).

### D-086: Lava and fissures burn on contact; AI demons walk around them (Mario)

GAME_DESIGN always said lava and glowing fissures do damage on contact; now they do. `HazardMap` turns every lava pool (a disc) and fissure (a turned rectangle) of the layout into a zone on the ground plane, built when the run gets its world. `HazardSystem` runs after the status effects each tick: a demon whose feet stand in a zone burns, lava 20 percent and fissures 5 percent of the touching part's health per second. The fire eats the legs first while they last, then the core, ignores armor, opens no bleeding wound, gives nobody the kill and spares a transforming body; the new `DamageType.Fire` exists only for the world. The numbers are on the biome asset (`Hazards` header). The demon remembers the hazard it stands in (`Demon.Hazard`, not saved); the HUD warns "BURNING" and the death screen says "Burned in lava". AI demons steer around hazards (`HazardMap.Steer`, a lookahead of 1.5 body sizes, a margin of 1.5 m plus the body radius), move goals out of them and ignore food lying in them; the player is free to walk in. Presentation: burning parts char and cool off over 12 seconds, sparks fly from a burning body, embers rise over every lava pool, and a sizzle sound plays. The burning area must be exactly the visible one: the first playtest burned meters ahead of the lava, because a flat pool on sloped ground lay up to half under the terrain (measured 35 to 47 percent hidden). The generator now carves a basin under every pool: the surface sits at the lowest ground it covers, the floor and a shore of three meters drop below it, and a bank rises at a walkable slope of 0.45 until it meets the terrain; features around are set onto the carved ground. Fissures are drawn as a glowing strip that follows the ground instead of a flat box. Rules out: heat damage at a distance, AI that dies in lava by walking into it, and burning ground that does not look like lava.

### D-087: A brighter cavern: more light from above (Mario)

The world was too dark. The ceiling glow is now a warm directional light at 1.4 instead of 0.35, leaning 28 degrees from straight down so shapes read, with softer shadows; the ambient light is brighter and lifts the undersides more; the fog is a lighter ash haze and its color is also the camera background, so the open cave above glows instead of being black. Lava radiates harder: brighter emission, lights at 12 with a longer reach, embers over the surface. The values live on `WorldBuildSettings`; an older asset gets them once through a lighting version. Only two point lights cast shadows at a time; each face gets 512 pixels in an atlas of at least 2048, so the twelve faces fit without URP shrinking them. The first test showed the new values never arrived because nobody ran the generator; `GeneratedAssetsGuard` now reruns it once by itself whenever the generator version stamped into the palette is older than the code. A Play Mode capture (`LookCaptureTests`, explicit) renders the player view, a lava pool and an overview into `TestResults/look` for judging the look without the editor. Rules out: volumetric light shafts (not in URP) and a hand-placed ceiling mesh.

Second pass, after the capture still averaged a brightness of about 10 of 255. Four things stacked up. The painted ground, rock and wall textures were near black (an albedo of 1 to 2 percent, darker than charcoal), so the light had nothing to show; they are now ash and rock gray at about a tenth of the light reflected, repainted once. The light and ambient were so orange that the green channel, which carries most of what the eye reads as brightness, stayed near zero; the ceiling light is now a warm white (1, 0.9, 0.8) at 1.6, and the ambient a dusty warm gray with a stronger fill (intensity 2.5, now a linear multiplier like a light's). ACES tonemapping maps mid-gray to about a tenth and crushes everything below; the post exposure rises from +0.2 to +0.8 EV and the contrast drops from 15 to 8. The haze is lighter, (0.45, 0.32, 0.25). Separately, the owner tint over an imported texture multiplied the player's teal into Mario's red core texture and left it nearly black; the tint blend for textured parts drops from 0.6 to 0.3. The lava texture repeated in a visible grid of 30 cm plates; it now tiles 0.12 times per meter instead of 0.4, so the plates are about a meter. The larger plates showed what the small ones hid: the lava, rock and wall textures had a seam at every tile edge, because their noise was scaled but still wrapped after eight cells; each noise now wraps after exactly the cells it spans, and the painted surfaces are repainted once. The lighting version is 3 and the generator version 6, so the editor applies all of it once by itself. `LookCaptureTests` logs the mean brightness of each picture.

### D-088: Part fit for Meshy models: an import fix under the knobs, offsets along the socket

Meshy exports through Blender's FBX writer. The mesh stands upright along Blender's Z axis, 1 unit on its longest side, with the origin at the bottom center, and the file's single object carries a -90 degree turn and a scale of 100 (centimeters). Unity folds a single-object file into the model's top object, and the binder measured meshes relative to that object, so it saw neither the turn nor the scale. The core lay on its side until Mario set its Euler knob to -90, and an arm would have come in at a hundredth of its size. The binder now reads the mesh in the file's own space, top object included, and stores that turn and unit scale on every bind as a hidden import fix on the part asset. The Scale, Offset and Euler knobs apply on top and start neutral: Scale 1 shows a part at the size it had in the tool, in body heights, and Euler 0 shows it upright as the tool showed it. A set bound before keeps its look: the core's -90 moves out of the Euler knob by itself. The Offset of a part now turns with its socket anchor, so +Z pushes any part out of the body and both arms move alike; added in body space, it had pushed one arm into the body. The validator's pivot rule changes from "the mesh lies along +Z from the pivot" to "the pivot lies on the surface of the mesh bounds": that holds for an arm standing on its shoulder, jaws reaching forward, legs hanging down and a hide slab, and it still catches a pivot in the middle of the mesh. The core and arm briefs describe the Meshy way: generate a part standing upright on the end that touches the body. Rules out: re-exporting every model through Blender to fix pivots and axes.

Second step, with Mario's first arm. Meshy keeps no convention for origin and size: the core came out 1 unit tall with its origin at the bottom, the arm 1.9 units tall with its origin in the middle. So the import fix also scales the model so its longest side is one unit, and moves the pivot to the bottom center of the model as it stood in the tool. Scale is now always the longest side in body heights, and the end a part stands on always meets the anchor. The validator's shape checks (plausible height, pivot on the surface) go, because the binder now fixes what they warned about. A copy on the left flank of a part that mirrors its second copy (the arm) takes the knobs mirrored across the middle of the body: the rotation's turns about Y and Z and the Offset's X change sign. Euler (90, -90, 0) now points both arms forward; before, the left arm was the right one turned around and pointed backward. The left copy is also a true mirror image in shape, not two right hands: the view builds a mirrored copy of each of its meshes once (X flipped, windings turned), so body part models import with Read/Write on. Negative scale was ruled out, because box colliders do not support it. The arm brief has a table of Euler values.

### D-089: Test mode for playtesting: invincible, 1000 Biomass and stat points (Mario)

Testing parts, mutations and evolutions needed a way past dying and saving up. The Playtest tab of the settings has a Test mode toggle, stored in `settings.json` like the offers toggle. The simulation owns it: `RunController` sends `SetTestModeCommand` for the player when a run starts or resumes and whenever the toggle changes, and the runner applies it even in a paused menu. A demon in test mode takes no damage at all: `DamageSystem` skips hits (no damage, no bleeding, no stagger), thorns, bleeding and hazards alike. `TestModeSystem` runs right after the commands of each pass, in a tick and in a paused menu, and raises the demon's Biomass and unspent stat points back to at least 1000, so whatever a mutation or a stat point spent is back at once; the refill counts neither as eaten nor as earned, so the run summary stays honest. The HUD shows "TEST MODE: invincible". The flag is not saved; a run saved in test mode keeps its Biomass and points, and the setting decides again on resume. Rules out: cheats in UI code that change the run behind the simulation's back, and test-only stats of 1000 instead of points to spend (the stat caps stay).

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
