#nullable enable
using System.Collections.Generic;
using System.Numerics;
using DemonFighter.Simulation.Worldgen;

namespace DemonFighter.Simulation.Tests.Hazards
{
    /// <summary>Hand-made flat worlds with hazards exactly where a test wants them.</summary>
    internal static class HazardWorlds
    {
        public const float Side = 200f;
        public const float LavaRadius = 8f;
        public const float FissureWidth = 2f;
        public const float FissureLength = 16f;

        /// <summary>One lava pool at the origin and one fissure, running north to south, centered at x = 40.</summary>
        public static readonly Vector3 LavaCenter = Vector3.Zero;

        public static readonly Vector3 FissureCenter = new Vector3(40f, 0f, 0f);

        public static WorldLayout Flat(int seed, BiomeSpec? biome = null)
        {
            const int vertices = 11;
            float cell = Side / (vertices - 1);
            var heights = new float[vertices * vertices];
            var features = new List<FeaturePlacement>
            {
                new FeaturePlacement(FeatureKind.LavaPool, LavaCenter, 0f, new Vector3(LavaRadius * 2f, 0.4f, LavaRadius * 2f)),
                new FeaturePlacement(FeatureKind.Fissure, FissureCenter, 0f, new Vector3(FissureWidth, 0.3f, FissureLength)),
            };
            var route = new[] { new Vector3(-60f, 0f, -60f), new Vector3(60f, 0f, -60f), new Vector3(0f, 0f, 60f) };
            return new WorldLayout(
                seed,
                biome ?? BiomeSpec.AshCavern,
                GroundBounds.CenteredSquare(Side),
                new Heightfield(vertices, vertices, cell, -Side * 0.5f, -Side * 0.5f, heights),
                features,
                new[] { new Vector3(-50f, 0f, -50f) },
                route);
        }
    }
}
