#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;

namespace DemonFighter.Simulation.Worldgen
{
    /// <summary>
    /// A generated world as data only: ground heights, where things stand, where demons start and where the elder
    /// walks (ARCHITECTURE, "World generation"). Rebuilt from the seed, never saved.
    /// </summary>
    public sealed class WorldLayout
    {
        public WorldLayout(
            int seed,
            BiomeSpec biome,
            GroundBounds bounds,
            Heightfield heightfield,
            IReadOnlyList<FeaturePlacement> features,
            IReadOnlyList<Vector3> spawnPoints,
            IReadOnlyList<Vector3> elderRoute)
        {
            Seed = seed;
            Biome = biome ?? throw new ArgumentNullException(nameof(biome));
            Bounds = bounds;
            Heightfield = heightfield ?? throw new ArgumentNullException(nameof(heightfield));
            Features = features ?? throw new ArgumentNullException(nameof(features));
            SpawnPoints = spawnPoints ?? throw new ArgumentNullException(nameof(spawnPoints));
            ElderRoute = elderRoute ?? throw new ArgumentNullException(nameof(elderRoute));
            if (spawnPoints.Count == 0)
            {
                throw new ArgumentException("A world needs at least the player's spawn point.", nameof(spawnPoints));
            }

            if (elderRoute.Count < 3)
            {
                throw new ArgumentException("An elder route needs at least three waypoints.", nameof(elderRoute));
            }
        }

        /// <summary>The seed this world was generated from.</summary>
        public int Seed { get; }

        /// <summary>The biome that shaped it.</summary>
        public BiomeSpec Biome { get; }

        /// <summary>Walkable ground inside the walls.</summary>
        public GroundBounds Bounds { get; }

        /// <summary>Ground heights.</summary>
        public Heightfield Heightfield { get; }

        /// <summary>Rocks, fissures, pools and bone piles.</summary>
        public IReadOnlyList<FeaturePlacement> Features { get; }

        /// <summary>Start positions on the ground; the first one is the player's.</summary>
        public IReadOnlyList<Vector3> SpawnPoints { get; }

        /// <summary>Closed loop of waypoints on the ground; the elder starts at the first one.</summary>
        public IReadOnlyList<Vector3> ElderRoute { get; }

        /// <summary>Where the player starts.</summary>
        public Vector3 PlayerSpawn => SpawnPoints[0];

        /// <summary>Where the elder starts.</summary>
        public Vector3 ElderSpawn => ElderRoute[0];
    }
}
