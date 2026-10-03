# Game Design: Demon Fighter

Working title. Version 0.2, October 2026. Owner: Mario. Every rule in this document was confirmed by Mario unless it is marked **(default, open)**, which means Claude proposed it, Mario has not decided yet, and the entry lives under "Open" in `DECISIONS.md`.

## Pitch

You are born as a blind, limbless lump of demon flesh in a cavern world where everything else was born the same way, and everything else is hungry. Bite what you can reach. Eat what you kill. Spend the Biomass on a jaw, then an arm, then armor. Level up, evolve into something bigger, and climb the food chain before something bigger climbs it on top of you. When you die, the run is over.

Third-person by default so you watch your own body change; first-person on demand. Realistic and ugly: wounds stay, chunks go missing, and the thing you just killed is lunch.

Three ways to grow, all at once: **levels** (XP, base stats, evolutions), **mutations** (Biomass, body parts) and **skills** (get better at what you use).

## Inspirations and what we take from them

Inspiration for mechanics and feel only. We do not copy names, lore, creatures, text, art or designs from any of them. Everything in Demon Fighter is our own.

- **Chrysalis (RinoZ), demon stratum concept.** Creatures spawn weak, kill for experience, eat for Biomass, spend Biomass on mutations that change their body, and at level thresholds pick an evolution from several options that reshapes them. Weaker prey yields less to a stronger predator, so the big ones ignore the small ones. We take: Biomass as a currency earned by eating, visible mutation, evolution choices at thresholds, reward scaling by tier, an ecosystem where the player is not special.
- **Everything is Crab (Odd Dreams Digital).** Top-down evolution roguelite. Every evolution changes how the creature looks and plays, and the world is a living food chain. We take: every mutation must be visible on the body, specializations as evolution choices, and the world should feel alive without the player.
- **Classic roguelikes and roguelites.** Runs, permadeath, random worlds, build variety, short sessions.

## Tone and rating

Grim, bodily, realistic. No cartoon shading, no comedy gore. Damage is shown on the body: a bite removes a visible chunk, a severed arm is gone and lies on the ground, blood pools and dries. Expect a mature rating (PEGI 18 / ESRB M). This is a design decision, see `DECISIONS.md`.

Realism applies to the body and the violence. The world itself is fantastical: a demon cavern, not Earth.

## Design pillars

1. **Your body is your build.** No inventory, no equipment screen. Everything you can do comes from a body part you grew, a stat you raised or an evolution you chose. Lose the part, lose the ability.
2. **Everyone plays by the same rules.** AI demons spawn, eat, mutate, evolve and die exactly like the player. The player has no hidden bonuses. Difficulty comes from the ecosystem, not from scripted waves.
3. **Visible consequences.** Every stat change has a visible cause on a body. Every wound is drawn. A player should be able to judge an enemy's build by looking at it.
4. **Short, dense runs.** A run is 15 to 20 minutes for v1. Pressure rises over time. There is always a reason to act now.
5. **Extensible by data.** New body parts, skills, mutations, evolutions, demon archetypes and biomes are added as data assets, not by rewriting systems.

## Setting

An underground cavern stratum that looks almost like a surface at dusk: the ceiling glows (luminous rock, ash haze), so there is ambient light everywhere. Lava flows and glowing fissures add local light, heat and danger. The world ends at cavern walls. No sky, no weather, no day cycle in v1.

## Core loop

```
spawn as a Tier 0 blob
  -> perceive: find food, avoid threats
  -> fight: bite, claw, grab (whatever your body allows)
  -> kill: gain Experience (XP)
  -> eat the corpse: gain Biomass
  -> mutate (menu pauses, confirm out of combat): spend Biomass on body parts and upgrades
  -> level up: spend stat points; at thresholds, choose an evolution
  -> skills level up by use: every Bite makes Bite better
  -> stronger, bigger body, new skills, higher Tier
  -> bigger threats notice you, pressure rises
  -> die (permadeath) -> run summary -> new run at Level 1
```

## The three progression layers

Every demon, player or AI, grows on three independent layers. They feed each other but none replaces another.

| Layer | Fed by | What it changes | Where you spend it |
|---|---|---|---|
| **Level** | XP from kills and from skill use | Base stats (points you allocate), evolutions at level thresholds | Stats tab, Evolve tab |
| **Mutation** | Biomass from eating | Body parts, part upgrades, regrowing lost parts | Mutate tab |
| **Skill** | Using the skill (Skyrim style) | Skill level: effectiveness, cost, speed; perks at skill level thresholds | Nothing to spend; use it and it grows. Skills tab shows progress |

Resources:

- **Experience (XP)** from kills and from using skills. XP raises **Level**. Each level grants stat points. Level gates mutations (requirements) and triggers evolutions at thresholds.
- **Biomass** from eating corpses and severed parts. Biomass buys mutations and regrows lost parts. Biomass is lost on death. Your corpse is worth Biomass to whoever eats it.
- **Skill XP** per skill, from using that skill. Not a currency; it only raises that skill's level.

XP, Biomass and the active skills' levels are shown in the HUD at all times.

**Reward scaling:** XP and Biomass from a kill or a meal scale with the victim's tier relative to yours. Prey more than one tier below you gives very little. This is the rule that makes elders ignore the small, and it applies to the player too.

## The run

- **Start:** the player spawns as a Tier 0 blob with Bite, Crawl and Eat. A handful of other Tier 0 demons spawn in the same area at the same time (same rules, same starting body).
- **World:** procedurally generated from a seed, bounded by cavern walls. v1 is a single biome with terrain features (rock formations, lava, glowing fissures, pools, bone piles) that matter for line of sight and escape routes. More biomes later.
- **Pressure:** a run-wide **threat level** rises with time. Higher threat means more and stronger spawns, and elders wander closer. There is no hunger mechanic. This is what keeps a run at 15 to 20 minutes.
- **Elders:** one or two very high tier demons roam the world from the start. They ignore anything far below their tier (it is not worth the XP) unless attacked. The player should see one walking in the distance within the first minutes. They can be attacked and will fight back. Not a boss fight in v1, just a very bad idea.
- **End of run:** death. Run summary shows time survived, highest tier, evolutions taken, kills, Biomass eaten, final body. Other end conditions (reaching an exit, killing an elder) are later ideas.
- **Save and resume:** quitting mid-run saves the run. Resuming deletes the save. One slot. This is a convenience, not a safety net; death still ends the run.
- **Meta-progression:** none in v1. The code keeps a hook (`IMetaProgression`).
- **Seeds:** the player can enter a seed and the seed is shown so runs can be shared.

## The demon (player and AI)

One entity type, `Demon`, used by the player and by every AI. Differences are only in who sends the commands.

### Base stats

Three base stats in v1, raised by stat points on level up and by evolutions:

| Stat | Effect |
|---|---|
| Strength | Damage multiplier, grab power, how much Blunt damage body slams deal |
| Constitution | Core and part HP, passive regeneration rate, bleeding resistance |
| Agility | Movement speed, attack speed, stamina pool and regeneration |

Later: a mind stat for magic (Will or similar) when magic enters the game. The stat system must allow adding stats as data.

### Derived values

Health (core plus parts), Stamina, Speed, Perception (from sensory parts) and per-skill values (Bite force, Claw sharpness) are computed from base stats plus body parts. Tier (0 to N) is a derived label from total body investment and evolutions. Tier drives threat assessment, reward scaling and spawn tables.

### Size

Demons grow physically with tier. A Tier 0 blob is about 1 meter; elders are about 15 meters. Growth changes collision, camera distance, reach and how big a bite is. Every system must handle a body that can be 1 meter or 15 meters. **(default, open)** v1 player content reaches about Tier 3, roughly 3 meters; elder sizes exist as AI content only.

### The body: parts and sockets

A body is a tree of parts. The **Core** (the original blob) has sockets. A part plugs into a socket and can expose sockets of its own.

```
Core
 ├─ socket: Head        -> Jaws upgrades, Eyes, later Horns
 ├─ socket: Limb x2     -> Arm (Claw, Grab), later Blade Arm, Shield Arm
 ├─ socket: Locomotion  -> Legs (Sprint, Lunge), later Clawed Feet
 ├─ socket: Hide        -> Thick Hide, Plates, Elastic Tissue, Spines
 └─ socket: Tail        -> Tail (Blunt swing), later variants
```

Every part has: its own HP and condition (healthy, wounded, severed), stat contributions, skills it grants, a visual (mesh, attachment point, damage states), requirements (minimum Level, required parts, evolution, Biomass cost), and upgrade levels (+1 to +5).

A bought part stays yours. If it is severed, it is gone from your body until you choose to regrow it in the mutation menu, which costs Biomass. Regrowing is a manual decision because the player may prefer to spend that Biomass elsewhere. Later: healing magic as an alternative.

### Mutation

The mutation menu is the only shop. It is one screen with tabs: **Mutate**, **Evolve**, **Stats**, **Skills**. Tab opens it; the Stats and Skills tabs are also reachable directly with C.

Rules:

- Opening the menu pauses the game. Browsing and planning is always allowed.
- Confirming a mutation requires being **out of combat**: no damage dealt or taken in the last 5 seconds. The button is disabled otherwise and says why.
- After confirming, the transformation takes 2 seconds in the world. The demon is invulnerable and cannot act during it. Because it cannot be triggered in combat, this is not an escape move.
- **(default, open)** Offer policy: v1 lists every mutation you qualify for (shop). Roguelike-style random offers are implemented behind `IMutationOfferPolicy` and playtested in M4.

### Evolution

At level thresholds the demon evolves: a big change of form, inspired by Chrysalis evolutions and Everything is Crab specializations.

- The Evolve tab offers 3 evolution options generated from the demon's current body, stats, skills and archetype. Each option has a name, a description and a visual change to the core, and grants a **package**:
  - a pool of **stat points to allocate** (the pool differs per option, for example a Brute option grants more points than a Stalker option but the Stalker option raises the Agility cap)
  - one or more **free mutations** (parts or upgrades applied immediately, no Biomass)
  - one or more **extra skills** that no part grants (for example a roar, a burrow)
  - unlocked **part categories** (a Brute line unlocks heavy limbs and Plates, a Stalker line unlocks speed and sensory parts)
- Evolving is confirmed like a mutation (out of combat, short transformation) and grows the body a size step.
- AI demons evolve too, picking by archetype preference.
- **(default, open)** v1 has two evolution thresholds, Level 5 and Level 10, three options each. Whether an option's stat pool is fully free or partly bound to a stat is decided when the first evolutions are written in M3; default: fully free pool, option-specific size and caps.

### Skill levels

Skills get better by being used, like in Skyrim. There is nothing to buy.

- Each use of a skill grants skill XP to that skill (hits grant more than misses; harder targets grant more). Skill XP also feeds a share into character XP.
- Skill level raises the skill's numbers: damage or effect, stamina cost, cooldown, speed, reach. The curve is data on the `SkillSpec`.
- At skill level thresholds the skill gains a **perk** (data): Bite might gain a longer bleed, Claw a faster follow-up, Sprint a lower cost. Perks are defined per skill in the content catalog.
- Skill levels are per demon and per run. Losing the part that grants a skill freezes its level; regrowing the part restores the skill at the same level.
- The Skills tab (C) shows every skill, its level, progress to the next level and the perks taken.
- **(default, open)** v1 ships the level curve and one perk per skill at skill level 10. More perks later.

### Skills

Skills are granted by parts or by evolutions and improve with use (see "Skill levels"). Every skill except Crawl and Eat costs stamina. v1 skill set:

| Skill | Granted by | Damage type | Notes |
|---|---|---|---|
| Bite | Core jaws (from birth) | Pierce | Primary attack of a Tier 0 blob. Short range, tears a chunk, causes bleeding. |
| Crawl / Walk | Core / Legs | | Crawl is slow. Legs replace it with Walk. |
| Eat | Core | | Hold on a corpse or severed part to convert it to Biomass over time. Interruptible. Only dead things. |
| Claw | Arm | Cut | Faster than Bite, less damage, strong bleeding. |
| Grab | Arm | | Hold a smaller demon in place for a short time, then Bite or Claw it. |
| Lunge | Legs | Blunt | Short dash attack, knocks back. |
| Sprint | Legs | | Faster movement, burns stamina. |
| Tail Swing | Tail | Blunt | Wide arc, staggers. |

Magic, ranged attacks and auras are out of scope for v1. The skill system must allow them later (skills are data plus a behaviour class, see `ARCHITECTURE.md`).

### Damage model

- Attacks hit a **specific part**. The player aims at parts (an arm can be bitten off on purpose); AI picks targets by logic. The core can be hit from any side and has the highest HP.
- Three damage types: **Pierce** (Bite), **Cut** (Claw), **Blunt** (Lunge, Tail Swing, body slam).
- Three defense types on hide parts, rock-paper-scissors:

| Defense | vs Pierce | vs Cut | vs Blunt |
|---|---|---|---|
| Thick Hide | neutral | strong | weak |
| Plates | strong | strong | weak (cracks) |
| Elastic Tissue | neutral | weak | strong |

Exact numbers are tuning data, not design.

- **Bleeding:** Pierce and Cut wounds bleed for a few seconds, draining HP. Constitution shortens it.
- **Stagger:** Blunt hits interrupt actions and can knock back smaller demons.
- **Regeneration:** passive HP regeneration at all times, scaled by Constitution, improved by regeneration mutations and skills. Regeneration does not regrow severed parts.
- A part at 0 HP is **severed** (limbs, tail) or **destroyed** (hide, eyes). A core at 0 HP is death.
- Corpses and severed parts are **food**. Their Biomass value depends on tier and remaining mass, scaled by the eater's tier. Food decays after a while.

### Visible damage and gore

Built in stages (see `ROADMAP.md` and `ASSET_PIPELINE.md`):

1. **Placeholder (M2):** parts are separate primitives. A wounded part changes color and shrinks; a severed part detaches and falls as a physics object. Blood is a flat decal.
2. **Chunk removal (M5):** each part mesh ships with pre-made damage states (intact, bitten, mangled) that swap on wound thresholds. Severing swaps to a stump mesh and spawns the detached part mesh.
3. **Later:** runtime mesh cutting, soft body viscera, persistent blood on terrain.

Whatever the stage: every wound must be visible on the body.

## AI demons

- Same entity, same rules, same mutation and evolution catalogs. AI demons start as Tier 0 blobs, except the elders.
- Decision making is a **utility AI** in the simulation layer: each demon scores goals (hunt, eat, flee, mutate, evolve, wander, rest) from perception and state, and acts through the same command interface as the player.
- Personality is data: an **archetype** asset sets goal weights (aggressive, cautious, scavenger) and preferred mutation and evolution paths, so AI builds differ without new code.
- AI demons avoid targets more than one tier above them, prefer wounded or eating targets, and flee when Health is low and a path away exists.
- Elders use the same AI with a very high tier; reward scaling makes them ignore the small.
- v1 target: about 10 demons alive at once for testing. Higher counts are a performance target for later milestones.

## World generation

- Seeded. Same seed, same world.
- v1: one biome, a bounded cavern of roughly 300 x 300 meters. Height variation, rock formations, lava pools and glowing fissures (light, damage on contact), shallow water pools, bone piles that hold a little free Biomass, cavern walls at the edge.
- Spawn points for Tier 0 demons are clustered so early encounters are guaranteed. Elder routes are generated as loops through the area.
- No jumping or climbing in v1; the generator must not create terrain that requires it. Flying is a later idea.
- Later: multiple biomes, more hazards, vertical structures.

## Camera and controls

- **Camera:** third-person (over the shoulder, slightly above) is the default, because watching your own body change is the point. First-person is a toggle. Both exist from M1. Camera distance scales with demon size.
- **Controls (keyboard and mouse, v1):**

| Input | Action |
|---|---|
| W A S D | Move |
| Mouse | Look / aim at parts |
| Left mouse | Primary attack (Bite, or Claw when arms exist) |
| Right mouse | Secondary (Grab when arms exist) |
| E (hold) | Eat |
| Shift | Sprint (needs Legs) |
| Space | Lunge (needs Legs) |
| Q | Tail Swing (needs Tail) |
| Tab | Mutation menu (pauses) |
| C | Stats and Skills (same menu, Stats tab) |
| V | Toggle third / first person |
| Esc | Pause menu |

Gamepad is not planned for v1. The Input System setup must not make it hard to add.

## UI

- **HUD:** Health (with a per-part indicator), Stamina, Biomass, Level and XP, Tier, active skills with cooldowns, bleeding indicator, threat level as a subtle meter, seed in a corner.
- **Mutation menu:** tabs Mutate, Evolve, Stats, Skills. Body silhouette with sockets, cost, requirements, preview of the stat change, regrow buttons for severed parts.
- **Death screen:** run summary.
- **Main menu:** New Run (with optional seed), Continue (if a save exists), Settings (graphics, audio, mouse sensitivity, invert Y), Quit.
- First-run hints: short on-screen prompts the first time a mechanic becomes relevant (first corpse, first Biomass, first level up). No tutorial level.
- English only. No localization in v1.

## Audio

Sound effects and ambient from the start of M5. Music is AI-generated; license terms are recorded like any other asset.

## Scope

### v1 (vertical slice, M0 to M4 in `ROADMAP.md`)

One cavern biome, procedural world, TP/FP camera, Bite/Claw/Grab/Lunge/Sprint/Tail Swing/Eat with skill levels and one perk each, three damage and three defense types, part-based damage with placeholder gore, stats and stat points, mutation menu with 8 to 10 parts and upgrades, two evolution thresholds with packages, AI demons that mutate and evolve, elders, threat escalation, save and resume, death and run summary. Placeholder visuals throughout.

### Toward Steam (M5 to M6)

Real assets, gore stage 2, audio and music, post-processing, main menu and settings, Steam integration, build pipeline, a playtest. Early Access is the intended release path; a local prototype comes first.

### Later (not scheduled)

Magic and a mind stat, ranged skills, multiple biomes, bosses, run end conditions beyond death, meta-progression, gamepad, achievements, flying, co-op and PvP multiplayer, mod support through the content system.
