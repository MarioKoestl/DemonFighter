#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using DemonFighter.Simulation.Ai;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Worldgen;

namespace DemonFighter.Simulation.Spawning
{
    /// <summary>
    /// Keeps the arena populated and lets the threat shape it (D-053, D-070): while fewer AI demons live than the cap,
    /// which grows with the threat level, one more is born every interval, which shrinks with it, out of sight of the
    /// player and clear of features. Its kind comes from the spawn table of the biome: among the entries whose
    /// threshold the threat has reached, one is drawn by weight, so stronger kinds join as the run goes on. Elders
    /// never count toward the cap.
    /// </summary>
    internal sealed class SpawnSystem
    {
        private const int PlacementAttempts = 12;
        private const float FeatureClearanceMeters = 2f;

        private readonly SimulationEvents _events;
        private readonly AiSystem _ai;
        private long _nextSpawnTick = -1;

        /// <summary>Tick of the next spawn attempt; -1 before the first; saved with the run (D-073).</summary>
        public long NextSpawnTick => _nextSpawnTick;

        public SpawnSystem(SimulationEvents events, AiSystem ai)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _ai = ai ?? throw new ArgumentNullException(nameof(ai));
        }

        /// <summary>Continues the spawn timer of a saved run.</summary>
        internal void RestoreNextSpawnTick(long tick)
        {
            _nextSpawnTick = tick;
        }

        /// <summary>How many AI demons below the elder the biome wants alive at this threat level.</summary>
        public static int PopulationCap(BiomeSpec biome, int threatLevel)
        {
            return biome.InitialBlobs + threatLevel * biome.BlobsPerThreatLevel;
        }

        /// <summary>Seconds between spawns at this threat level; the interval shrinks as the threat rises.</summary>
        public static float RespawnSeconds(BiomeSpec biome, int threatLevel)
        {
            return biome.RespawnSeconds / (1f + biome.RespawnSpeedupPerThreat * threatLevel);
        }

        /// <summary>Spawns at most one demon per interval; nothing before the world is attached or when disabled.</summary>
        public void Advance(RunState state)
        {
            WorldLayout? world = state.World;
            if (world == null || world.Biome.RespawnSeconds <= 0f)
            {
                return;
            }

            int threat = state.ThreatLevel;
            int interval = Math.Max(1, state.Config.TicksFor(RespawnSeconds(world.Biome, threat)));
            if (_nextSpawnTick < 0)
            {
                _nextSpawnTick = state.Tick + interval;
                return;
            }

            if (state.Tick < _nextSpawnTick)
            {
                return;
            }

            if (CountLivingPopulation(state, world.Biome) >= PopulationCap(world.Biome, threat))
            {
                _nextSpawnTick = state.Tick + interval;
                return;
            }

            if (!TryFindSpawnPosition(state, world, out Vector3 position))
            {
                // No clear spot this tick; the next tick tries again with fresh rolls.
                return;
            }

            DemonSpec kind = PickKind(state, world.Biome, threat);
            float yaw = state.Rng.NextFloat(0f, MathF.PI * 2f);
            Demon demon = state.SpawnDemon(ControllerKind.Ai, kind, position, yaw);
            _ai.AddBrain(demon, world.Biome.PickBlobArchetype(state.Rng), world.Bounds, null);
            _events.Publish(new DemonSpawned(demon.Id));
            _nextSpawnTick = state.Tick + interval;
        }

        // Weighted draw among the table entries the threat has unlocked; an empty table spawns the blob of the biome.
        private static DemonSpec PickKind(RunState state, BiomeSpec biome, int threat)
        {
            IReadOnlyList<SpawnEntry> table = biome.SpawnTable;
            float total = 0f;
            for (int i = 0; i < table.Count; i++)
            {
                if (table[i].MinThreat <= threat)
                {
                    total += table[i].Weight;
                }
            }

            if (total <= 0f)
            {
                return biome.BlobDemon;
            }

            float roll = state.Rng.NextFloat(0f, total);
            DemonSpec last = biome.BlobDemon;
            for (int i = 0; i < table.Count; i++)
            {
                if (table[i].MinThreat > threat)
                {
                    continue;
                }

                last = table[i].Demon;
                if (roll < table[i].Weight)
                {
                    return last;
                }

                roll -= table[i].Weight;
            }

            return last;
        }

        private static int CountLivingPopulation(RunState state, BiomeSpec biome)
        {
            int count = 0;
            IReadOnlyList<Demon> demons = state.Demons;
            for (int i = 0; i < demons.Count; i++)
            {
                Demon demon = demons[i];
                if (demon.Controller == ControllerKind.Ai && demon.IsAlive && !string.Equals(demon.Spec.Id, biome.ElderDemon.Id, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        // A ring around the player, inside the bounds and clear of features; the player stays the anchor even dead.
        private static bool TryFindSpawnPosition(RunState state, WorldLayout world, out Vector3 position)
        {
            Vector3 anchor = world.Bounds.Center;
            IReadOnlyList<Demon> demons = state.Demons;
            for (int i = 0; i < demons.Count; i++)
            {
                if (demons[i].Controller == ControllerKind.Player)
                {
                    anchor = demons[i].Position;
                    break;
                }
            }

            BiomeSpec biome = world.Biome;
            for (int attempt = 0; attempt < PlacementAttempts; attempt++)
            {
                float angle = state.Rng.NextFloat(0f, MathF.PI * 2f);
                float distance = state.Rng.NextFloat(biome.RespawnMinDistance, biome.RespawnMaxDistance);
                var candidate = new Vector3(anchor.X + MathF.Sin(angle) * distance, 0f, anchor.Z + MathF.Cos(angle) * distance);
                if (!world.Bounds.Contains(candidate) || IsBlocked(world, candidate))
                {
                    continue;
                }

                candidate.Y = world.Heightfield.SampleHeight(candidate.X, candidate.Z);
                position = candidate;
                return true;
            }

            position = default;
            return false;
        }

        private static bool IsBlocked(WorldLayout world, Vector3 candidate)
        {
            IReadOnlyList<FeaturePlacement> features = world.Features;
            for (int i = 0; i < features.Count; i++)
            {
                FeaturePlacement feature = features[i];
                float dx = feature.Position.X - candidate.X;
                float dz = feature.Position.Z - candidate.Z;
                float clearance = feature.FootprintRadius + FeatureClearanceMeters;
                if (dx * dx + dz * dz < clearance * clearance)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
