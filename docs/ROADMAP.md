# Roadmap

Milestones are ordered. Each has acceptance criteria that Mario can check in Play Mode or that a test proves. A milestone is done when every box is ticked and `main` reflects it.

## Current milestone

**M5: Look and feel.** M4 is merged. The code side of M5 is done on branch `feat/m5-look-and-feel` (2026-10-05): the art pipeline with import rules, a binder that maps `BP_<Part>_<State>` models to part assets by name, socket anchors as data, a validator and five briefs in `docs/briefs/` (D-079); four visual damage stages and mesh sets with stumps (D-080); gore stage 2 with URP decal projectors, corpse pools, code-born viscera, parts that fall as they looked and the `DemonFighter/DemonSkin` shader that soaks bodies in blood (D-081); procedural motion for bodies and parts with a clip slot per part (D-082); the render look with a generated volume profile, three graphics presets as pipeline assets, a shadow budget, flickering lights, painted surface textures and two-level LODs (D-083); data-driven audio over a pooled player with synthesized placeholder clips, ambient and lava loops and crossfading music (D-084); the main menu with a seed field and Quit, the pause menu, the shared settings panel and `settings.json` (D-085); 565 EditMode and 5 PlayMode tests. Open and Mario's: generate the first meshes from the briefs and drop them in, source real clips and the three music tracks with license entries, tune the look in Play Mode. Next: M6 Steam prep.

(Claude Code: update this section when a milestone completes. Do not rewrite other sections without asking.)

## M0: Project setup

Goal: a Unity project in the repository that opens without errors, has the layer structure from `ARCHITECTURE.md`, and runs an empty bootstrap.

Done by Mario, in Unity Hub (cannot be done from the repository):

- [x] Install Unity Hub, the latest Unity 6 LTS, with "Windows Build Support (IL2CPP)" module
- [x] Create the project with the **Universal 3D** template, name `DemonFighter`, in a temp folder, then move `Assets`, `Packages` and `ProjectSettings` to the repository root `C:\Development\DemonFighter` (steps in README)
- [x] Edit > Project Settings > Editor: Asset Serialization = Force Text, Version Control = Visible Meta Files (both are the defaults, confirm)
- [x] Commit the fresh project (`chore(project): create Unity project`)

Done by Claude Code:

- [x] Add packages to `Packages/manifest.json`: Input System, Cinemachine, Test Framework, UI Toolkit (built in), Newtonsoft JSON (`com.unity.nuget.newtonsoft-json`)
- [x] Test libraries: FluentAssertions 7.x or AwesomeAssertions, and NSubstitute, as DLLs under `Assets/_Project/Tests/Plugins` or as UPM packages; both free licenses, exact source and version recorded in `DECISIONS.md`
- [x] Microsoft.Unity.Analyzers installed as a Roslyn analyzer in the project
- [x] Create the folder structure and all assembly definitions from `ARCHITECTURE.md`, with `noEngineReferences` on Simulation and warnings as errors via `csc.rsp`
- [x] Add `Bootstrap.unity`, `MainMenu.unity`, `Run.unity` through an editor script (`Demon Fighter > Generate > Scenes`), and set the build scene list
- [x] `GameServices` composition root, `SimulationRunner` with a fixed tick and an empty `RunState`
- [x] `Log` helper, `Rng` wrapper, `DemonId` and friends, `SimulationEvents` bus
- [x] One EditMode test that creates a `RunState` with a seed and ticks it 100 times
- [x] Input Actions asset with the `Gameplay` and `Menu` maps and the bindings from `GAME_DESIGN.md`
- [x] `.editorconfig`, `.gitignore`, `.gitattributes` (LFS) in place; `git lfs install` documented in README; `dotnet format` runs clean
- [x] Project settings: Active Input Handling = Input System only, URP asset assigned, quality level "PC"

Acceptance:

- [x] Project opens with zero errors and zero warnings in the Console
- [x] `dotnet build DemonFighter.slnx` passes
- [x] EditMode test passes in the Test Runner
- [x] Pressing Play in `Bootstrap.unity` loads `MainMenu.unity` and shows a "New Run" button that loads an empty `Run.unity`

## M1: Walking skeleton (be in a 3D world and see other demons)

Goal: the first thing Mario asked for. A seeded world you can walk through, with AI demons moving around in it, from both camera views.

- [x] `IWorldGenerator` produces a `WorldLayout` from a seed: heightfield with gentle variation and no terrain that needs jumping, cavern walls at the bounds, 40 to 80 rock features, 5 to 10 glowing fissures, 2 lava pools, 3 water pools, bone piles, a spawn cluster, one elder route loop
- [x] `WorldBuilder` builds terrain (Unity Terrain or a generated mesh, Claude Code decides and records it in DECISIONS.md), places primitive features with colliders, and sets up the glowing-ceiling ambient light plus emissive lava and fissures as the setting's light sources
- [x] Player demon as a capsule with `CharacterController`, WASD, mouse look, sprint placeholder (no stamina yet)
- [x] Cinemachine rig with third-person (default) and first-person cameras; V toggles; third-person shows the player capsule; camera distance reads the demon's size step
- [x] About 10 AI demons (capsules, different color) with a `UtilityBrain` that only knows `Wander` and `Rest`, moving through the same `MoveCommand` path as the player
- [x] One elder demon (large capsule, distinct color) walking its route, visible from spawn
- [x] HUD with placeholder Health, Stamina, Biomass, Level, Tier, seed
- [x] Seed shown on screen; same seed produces the same world (test)
- [x] EditMode tests: world generation is deterministic; AI wander produces commands; commands move entities

Acceptance:

- [ ] Walk for two minutes without falling through the world or getting stuck on a feature
- [ ] Switch cameras at any time without a visible jump
- [ ] AI demons move around and do not walk through rocks
- [ ] The elder is visible in the distance from the spawn cluster within 30 seconds
- [ ] 60 fps on Mario's machine at 1080p

## M2: Combat, death and eating

Goal: bite something, see it bleed, kill it, eat it.

- [x] Body model: core plus parts with HP and condition; base stats Strength, Constitution, Agility; derived stats computed from base stats and body (tests)
- [x] Damage model: Pierce, Cut, Blunt against Thick Hide, Plates, Elastic Tissue; bleeding; stagger; passive regeneration; part wounded, severed, destroyed; core death (tests)
- [x] Bite skill: hitbox during active frames, aim-based part targeting (the part under the crosshair within the arc), `HitReport` into the simulation, damage applied, events out; stamina cost; Bite gains skill XP per hit (the first skill with a level)
- [x] Placeholder gore stage 1: wounded parts change color and shrink, severed parts detach as physics objects, blood decal on the ground and on the body
- [x] Death: demon view becomes a corpse `FoodItem`; severed parts are food; food decays
- [x] Eat: hold E on food, Biomass flows over time, interruptible, shown in HUD
- [x] XP for kills with reward scaling by tier difference (tests); Level up grants stat points; Stats tab (C) to spend them
- [x] AI goals `Hunt`, `Flee`, `Eat` added to the brain, with the one-tier-above avoidance rule (tests)
- [x] Player death: freeze, run summary overlay, back to menu

Acceptance:

- [ ] Bite a demon: a chunk visibly changes, blood appears, it bleeds for a few seconds
- [ ] Kill a demon, eat it, Biomass rises in the HUD
- [ ] AI demons hunt each other and the player, and flee when low
- [ ] Die and see the run summary

## M3: Mutation

Goal: spend Biomass, grow a body, see it, use it.

- [x] Content: 8 to 10 body parts (Jaws upgrade, Arm, second Arm, Legs, Thick Hide, Plates, Elastic Tissue, Eyes, Spines, Tail) with upgrade levels, as ScriptableObjects, loaded through the catalog (tests on specs and requirements)
- [x] Skills Claw, Grab, Lunge, Sprint, Tail Swing as data plus behaviour classes found by attribute
- [x] Skill progression: skill XP per use, level curve on `SkillSpec`, numbers scale with level, one perk per skill at level 10, levels freeze when the granting part is severed; Skills tab shows it (tests)
- [x] Mutation menu (UI Toolkit, tabs Mutate / Evolve / Stats / Skills): pauses the game, shows the body silhouette with sockets, lists eligible mutations with cost and preview (Option A); regrow buttons for severed parts; `IMutationOfferPolicy` with the shop implementation and a stubbed offers implementation
- [x] Confirming requires being out of combat (5 seconds without damage dealt or taken); then a 2 second transformation, invulnerable and unable to act (tests on the rule). The combat rule is suspended in the playtest (D-060)
- [x] Evolution: two thresholds (Level 5, Level 10), three generated options each from `EvolutionSpec` assets; evolving reshapes the core placeholder, grants a package (stat point pool with caps, free mutations, extra skills, unlocked part categories) and grows the size step (tests)
- [x] Body part views attach primitives to sockets and show them in third-person
- [x] Tier derived from body investment and evolutions; size step scales the view, the `CharacterController` and the camera; shown in HUD and on AI demons

Acceptance:

- [ ] Buy an Arm, open third-person, see it, use Claw and Grab
- [ ] Buy Legs, move faster, Sprint and Lunge work, stamina drains
- [ ] Lose the Arm in a fight, Claw and Grab vanish from the HUD, the arm lies on the ground as food

## M4: A living ecosystem (end of the vertical slice)

Goal: a complete 15 to 20 minute run where the world evolves without the player.

- [x] AI demons mutate and evolve using archetype preferences; three archetypes (aggressive, cautious, scavenger)
- [x] Threat level over time: spawn tables, elder proximity, stronger spawns
- [x] Elders fight back when attacked and ignore low tiers otherwise
- [ ] Run tuning: a competent player survives 15 to 20 minutes, a careless one dies in 5 (the autoplay harness of D-077 is in place; the first playtests set the numbers)
- [x] Save on quit and resume (one slot, deleted on resume and on death); Continue button in the main menu
- [x] Death screen with full summary; `IMetaProgression` hook with a no-op implementation
- [x] First-run hints (first corpse, first Biomass, first level up, first evolution)
- [x] Playtest Option B (random offers) behind a setting; decide, record in `DECISIONS.md` (the setting is in place; the decision follows the playtest)
- [x] Stress test: 50 demons, 200 food, simulation tick under 2 ms (test with timing assertion)

Acceptance:

- [ ] Play three runs; each feels different because the ecosystem went differently
- [ ] Watch for five minutes without moving; the world keeps changing

## M5: Look and feel

- [ ] First real assets through the pipeline in `ASSET_PIPELINE.md`: core blob, jaws, arm, legs, hide, with damage states (pipeline, binder, validator and briefs done, D-079; the meshes are Mario's, see `docs/briefs/`)
- [x] Gore stage 2: mesh damage states, stump meshes, viscera, blood accumulation (D-080, D-081)
- [x] Animation: procedural movement for the blob, Mixamo or hand-made clips for limbed bodies (procedural for everything, clip slot per part, D-082)
- [x] URP look: post-processing volume (color grading, bloom, vignette, SSAO), fog, lighting pass on the world (D-083)
- [x] Audio: bites, wet impacts, footsteps, cavern ambient, lava, mutation and evolution; AI-generated music with a license entry (synthesized placeholders in place, D-084; the real clips, the music and their license entries are Mario's)
- [x] Main menu, settings menu, pause menu (D-085)

## M6: Steam prep

- [ ] Steamworks integration (library choice recorded in `DECISIONS.md`), app id, overlay
- [ ] Build pipeline: Build Profile for Windows x64, IL2CPP, a `Demon Fighter > Build` menu, versioning
- [ ] Settings persisted; crash and error logging to a file
- [ ] Store page assets, rating questionnaire, first closed playtest

## Backlog (not scheduled)

Magic and ranged skills, biomes, bosses, run end conditions beyond death, meta-progression, gamepad, achievements, cloud saves (Azure only if a server-side feature appears), multiplayer exploration, mod support through the content system, Unity MCP for Claude Code.
