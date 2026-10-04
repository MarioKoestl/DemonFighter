#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using DemonFighter.Simulation.Ai;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Worldgen;

namespace DemonFighter.Simulation.Spawning
{
    /// <summary>
    /// Keeps the arena populated (D-053): while fewer Tier 0 demons live than the biome asks for, one more is born
    /// every <see cref="BiomeSpec.RespawnSeconds"/>, out of sight of the player and clear of features. The threat
    /// level that scales spawns over time is M4; this is the floor under it.
    /// </summary>
    internal sealed class SpawnSystem
    {
        private const int PlacementAttempts = 12;
        private const float FeatureClearanceMeters = 2f;

        private readonly SimulationEvents _events;
        private readonly AiSystem _ai;
        private long _nextSpawnTick = -1;

        public SpawnSystem(SimulationEvents events, AiSystem ai)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _ai = ai ?? throw new ArgumentNullException(nameof(ai));
        }

        /// <summary>Spawns at most one demon per interval; nothing before the world is attached or when disabled.</summary>
        public void Advance(RunState state)
        {
            WorldLayout? world = state.World;
            if (world == null || world.Biome.RespawnSeconds <= 0f)
            {
                return;
            }

            int interval = Math.Max(1, state.Config.TicksFor(world.Biome.RespawnSeconds));
            if (_nextSpawnTick < 0)
            {
                _nextSpawnTick = state.Tick + interval;
                return;
            }

            if (state.Tick < _nextSpawnTick)
            {
                return;
            }

            if (CountLivingBlobs(state, world.Biome) >= world.Biome.InitialBlobs)
            {
                _nextSpawnTick = state.Tick + interval;
                return;
            }

            if (!TryFindSpawnPosition(state, world, out Vector3 position))
            {
                // No clear spot this tick; the next tick tries again with fresh rolls.
                return;
            }

            float yaw = state.Rng.NextFloat(0f, MathF.PI * 2f);
            Demon blob = state.SpawnDemon(ControllerKind.Ai, world.Biome.BlobDemon, position, yaw);
            _ai.AddBrain(blob, world.Biome.BlobArchetype, world.Bounds, null);
            _events.Publish(new DemonSpawned(blob.Id));
            _nextSpawnTick = state.Tick + interval;
        }

        private static int CountLivingBlobs(RunState state, BiomeSpec biome)
        {
            int count = 0;
            IReadOnlyList<Demon> demons = state.Demons;
            for (int i = 0; i < demons.Count; i++)
            {
                Demon demon = demons[i];
                if (demon.Controller == ControllerKind.Ai && demon.IsAlive && string.Equals(demon.Spec.Id, biome.BlobDemon.Id, StringComparison.Ordinal))
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
