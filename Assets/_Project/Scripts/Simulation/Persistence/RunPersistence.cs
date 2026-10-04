#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using DemonFighter.Simulation.Ai;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Food;
using DemonFighter.Simulation.Skills;
using DemonFighter.Simulation.Worldgen;

namespace DemonFighter.Simulation.Persistence
{
    /// <summary>
    /// Saves and resumes a run inside the simulation (D-026, D-073): <see cref="Capture"/> copies everything the rules
    /// keep into a <see cref="RunSnapshot"/>, <see cref="Restore"/> rebuilds the state from one, regenerates the world
    /// from the seed and gives living AI demons a fresh brain of their saved archetype. A skill use in progress, the
    /// movement intent and the goals of the brains are not saved. The App layer turns the snapshot into a file.
    /// </summary>
    public static class RunPersistence
    {
        /// <summary>Copies the run into plain data; the run itself is untouched.</summary>
        public static RunSnapshot Capture(RunState state, SimulationTicker ticker)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (ticker == null)
            {
                throw new ArgumentNullException(nameof(ticker));
            }

            var archetypes = new Dictionary<DemonId, string>();
            IReadOnlyList<UtilityBrain> brains = ticker.Ai.Brains;
            for (int i = 0; i < brains.Count; i++)
            {
                archetypes[brains[i].Demon.Id] = brains[i].Archetype.Name;
            }

            var snapshot = new RunSnapshot
            {
                Seed = state.Seed,
                TicksPerSecond = state.Config.TicksPerSecond,
                Tick = state.Tick,
                RngState = state.Rng.State,
                LastDemonId = state.DemonIds.LastIssued,
                LastFoodId = state.FoodIds.LastIssued,
                NextSpawnTick = ticker.Spawning.NextSpawnTick,
            };

            IReadOnlyList<Demon> demons = state.Demons;
            for (int i = 0; i < demons.Count; i++)
            {
                archetypes.TryGetValue(demons[i].Id, out string? archetype);
                snapshot.Demons.Add(CaptureDemon(demons[i], archetype ?? string.Empty, state.Catalog.Tuning));
            }

            IReadOnlyList<FoodItem> food = state.Food;
            for (int i = 0; i < food.Count; i++)
            {
                FoodItem item = food[i];
                snapshot.Food.Add(new FoodSnapshot
                {
                    Id = item.Id.Value,
                    Kind = (int)item.Kind,
                    X = item.Position.X,
                    Y = item.Position.Y,
                    Z = item.Position.Z,
                    BiomassRemaining = item.BiomassRemaining,
                    Source = item.Source.Value,
                    SourceTier = item.SourceTier,
                    DecayTicksLeft = item.DecayTicksLeft,
                });
            }

            return snapshot;
        }

        /// <summary>
        /// Rebuilds a run from a snapshot: the state with its clock, random source and id counters, the world from the
        /// seed, every demon and food item, and a ticker whose spawn timer and brains continue where the save left off.
        /// </summary>
        public static SimulationTicker Restore(RunSnapshot snapshot, ContentCatalog catalog, BiomeSpec biome, IWorldGenerator generator, SimulationEvents events)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (biome == null)
            {
                throw new ArgumentNullException(nameof(biome));
            }

            if (generator == null)
            {
                throw new ArgumentNullException(nameof(generator));
            }

            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            if (snapshot.Version != RunSnapshot.CurrentVersion)
            {
                throw new InvalidOperationException("The save has layout version " + snapshot.Version + "; this build reads version " + RunSnapshot.CurrentVersion + ".");
            }

            var config = new SimulationConfig(snapshot.TicksPerSecond);
            var state = new RunState(snapshot.Seed, config, catalog, snapshot.RngState, snapshot.LastDemonId, snapshot.LastFoodId, snapshot.Tick);
            WorldLayout world = generator.Generate(snapshot.Seed, biome);
            state.AttachWorld(world);

            for (int i = 0; i < snapshot.Demons.Count; i++)
            {
                DemonSnapshot saved = snapshot.Demons[i];
                DemonSpec spec = catalog.GetDemon(saved.SpecId);
                Demon demon = state.RestoreDemon(new DemonId(saved.Id), (ControllerKind)saved.Controller, spec, new Vector3(saved.X, saved.Y, saved.Z), saved.Yaw);
                demon.Restore(saved);
            }

            for (int i = 0; i < snapshot.Food.Count; i++)
            {
                FoodSnapshot saved = snapshot.Food[i];
                if (saved.DecayTicksLeft <= 0 || saved.BiomassRemaining < 0f)
                {
                    continue;
                }

                state.RestoreFood(new FoodId(saved.Id), (FoodKind)saved.Kind, new Vector3(saved.X, saved.Y, saved.Z), saved.BiomassRemaining, new DemonId(saved.Source), saved.SourceTier, saved.DecayTicksLeft);
            }

            var ticker = new SimulationTicker(state, events);
            ticker.Spawning.RestoreNextSpawnTick(snapshot.NextSpawnTick);
            for (int i = 0; i < snapshot.Demons.Count; i++)
            {
                DemonSnapshot saved = snapshot.Demons[i];
                if ((ControllerKind)saved.Controller != ControllerKind.Ai || !state.TryGetDemon(new DemonId(saved.Id), out Demon? demon) || !demon.IsAlive)
                {
                    continue;
                }

                bool elder = string.Equals(demon.Spec.Id, biome.ElderDemon.Id, StringComparison.Ordinal);
                ticker.Ai.AddBrain(demon, ResolveArchetype(biome, saved.ArchetypeName), world.Bounds, elder ? world.ElderRoute : null);
            }

            return ticker;
        }

        // A saved name is matched against the personalities the biome knows; an unknown one gets the plain blob personality.
        private static ArchetypeSpec ResolveArchetype(BiomeSpec biome, string name)
        {
            if (string.Equals(name, biome.ElderArchetype.Name, StringComparison.Ordinal))
            {
                return biome.ElderArchetype;
            }

            IReadOnlyList<ArchetypeChoice> choices = biome.BlobArchetypes;
            for (int i = 0; i < choices.Count; i++)
            {
                if (string.Equals(name, choices[i].Archetype.Name, StringComparison.Ordinal))
                {
                    return choices[i].Archetype;
                }
            }

            return biome.BlobArchetype;
        }

        private static DemonSnapshot CaptureDemon(Demon demon, string archetypeName, CombatTuning tuning)
        {
            var saved = new DemonSnapshot
            {
                Id = demon.Id.Value,
                Controller = (int)demon.Controller,
                SpecId = demon.Spec.Id,
                ArchetypeName = archetypeName,
                X = demon.Position.X,
                Y = demon.Position.Y,
                Z = demon.Position.Z,
                Yaw = demon.Yaw,
                Level = demon.Level,
                Xp = demon.Xp,
                Biomass = demon.Biomass,
                BiomassEaten = demon.BiomassEaten,
                Stamina = demon.Stamina,
                Kills = demon.Kills,
                Evolutions = demon.Evolutions,
                HighestTier = demon.HighestTier,
                UnspentPoints = demon.Stats.UnspentPoints,
                StaggeredUntilTick = demon.StaggeredUntilTick,
                LastCombatTick = demon.LastCombatTick,
                LastAttackedBy = demon.LastAttackedBy.Value,
                LastAttackedTick = demon.LastAttackedTick,
                TransformingUntilTick = demon.TransformingUntilTick,
                TransformationStartedTick = demon.TransformationStartedTick,
                HeldUntilTick = demon.HeldUntilTick,
                HeldBy = demon.HeldBy.Value,
                HeldOffsetX = demon.HeldOffset.X,
                HeldOffsetY = demon.HeldOffset.Y,
                ExternalVelocityX = demon.ExternalVelocity.X,
                ExternalVelocityY = demon.ExternalVelocity.Y,
                ExternalVelocityUntilTick = demon.ExternalVelocityUntilTick,
                EatingFoodId = demon.EatingFoodId.Value,
                EatRequestTick = demon.EatRequestTick,
                SprintXpBuffer = demon.SprintXpBuffer,
            };

            IReadOnlyList<StatId> ids = demon.Stats.Ids;
            for (int i = 0; i < ids.Count; i++)
            {
                saved.Stats.Add(new StatValueSnapshot { StatId = ids[i].Value, Value = demon.Stats.Get(ids[i]) });
                int capBonus = demon.StatCap(ids[i]) - tuning.BaseStatCap;
                if (capBonus != 0)
                {
                    saved.CapBonuses.Add(new StatValueSnapshot { StatId = ids[i].Value, Value = capBonus });
                }
            }

            foreach (string partId in demon.UnlockedPartIds)
            {
                saved.UnlockedPartIds.Add(partId);
            }

            saved.EvolutionIds.AddRange(demon.EvolutionIds);

            IReadOnlyList<BodyPart> parts = demon.Body.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                BodyPart part = parts[i];
                saved.Parts.Add(new PartSnapshot
                {
                    SpecId = part.Spec.Id,
                    UpgradeLevel = part.UpgradeLevel,
                    MaxHp = part.MaxHp,
                    Hp = part.Hp,
                    IsLost = part.IsLost,
                    BleedSecondsLeft = part.BleedSecondsLeft,
                    BleedDamagePerSecond = part.BleedDamagePerSecond,
                    BleedType = (int)part.BleedType,
                });
            }

            IReadOnlyList<SkillInstance> skills = demon.Skills;
            for (int i = 0; i < skills.Count; i++)
            {
                SkillInstance skill = skills[i];
                saved.Skills.Add(new SkillSnapshot
                {
                    SpecId = skill.Spec.Id,
                    GrantedByEvolution = skill.GrantedByEvolution,
                    Level = skill.Level,
                    Xp = skill.Xp,
                    CooldownUntilTick = skill.CooldownUntilTick,
                });
            }

            return saved;
        }
    }
}
