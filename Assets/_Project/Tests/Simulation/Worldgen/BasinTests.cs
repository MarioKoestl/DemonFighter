#nullable enable
using System;
using AwesomeAssertions;
using DemonFighter.Simulation.Worldgen;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Worldgen
{
    /// <summary>
    /// Regressions for D-086: a flat pool on a slope was partly buried under the ground, and the buried half still
    /// burned; then a basin dug below the surface left a moat around every pool. Every pool must lie above the ground
    /// it covers, and the ground must meet its edge all around.
    /// </summary>
    public sealed class BasinTests
    {
        private const float SurfaceLift = 0.05f;
        private const float Tolerance = 0.0001f;
        private const float EdgeDistance = 0.3f;
        private const float EdgeTolerance = 0.05f;

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(1000154155)]
        public void Generate_EveryPool_LiesAboveTheGroundItCovers(int seed)
        {
            WorldLayout world = new CavernWorldGenerator().Generate(seed, BiomeSpec.AshCavern);
            int pools = 0;

            foreach (FeaturePlacement feature in world.Features)
            {
                if (feature.Kind != FeatureKind.LavaPool && feature.Kind != FeatureKind.WaterPool)
                {
                    continue;
                }

                pools++;
                float radius = feature.Size.X * 0.5f;
                float surface = feature.Position.Y + SurfaceLift;
                for (float x = -radius; x <= radius; x += 0.5f)
                {
                    for (float z = -radius; z <= radius; z += 0.5f)
                    {
                        if (x * x + z * z > radius * radius)
                        {
                            continue;
                        }

                        float ground = world.Heightfield.SampleHeight(feature.Position.X + x, feature.Position.Z + z);
                        ground.Should().BeLessThan(surface, "the ground must never cover a " + feature.Kind + " (seed " + seed + ")");
                    }
                }
            }

            pools.Should().BePositive();
        }

        [Test]
        public void Generate_OtherFeatures_StandOnTheCarvedGround()
        {
            WorldLayout world = new CavernWorldGenerator().Generate(1000154155, BiomeSpec.AshCavern);

            foreach (FeaturePlacement feature in world.Features)
            {
                if (feature.Kind == FeatureKind.LavaPool || feature.Kind == FeatureKind.WaterPool)
                {
                    continue;
                }

                feature.Position.Y.Should().BeApproximately(world.Heightfield.SampleHeight(feature.Position.X, feature.Position.Z), Tolerance);
            }
        }

        // No moat and no wall: just outside its edge the ground lies at the surface of the pool, all around.
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(1000154155)]
        public void Generate_GroundRightOutsideEveryPool_MeetsItsSurface(int seed)
        {
            WorldLayout world = new CavernWorldGenerator().Generate(seed, BiomeSpec.AshCavern);

            foreach (FeaturePlacement feature in world.Features)
            {
                if (feature.Kind != FeatureKind.LavaPool && feature.Kind != FeatureKind.WaterPool)
                {
                    continue;
                }

                float radius = feature.Size.X * 0.5f + EdgeDistance;
                for (int i = 0; i < 48; i++)
                {
                    float angle = i * MathF.PI * 2f / 48f;
                    float ground = world.Heightfield.SampleHeight(feature.Position.X + (MathF.Sin(angle) * radius), feature.Position.Z + (MathF.Cos(angle) * radius));
                    ground.Should().BeApproximately(feature.Position.Y, EdgeTolerance, "the ground meets the edge of a " + feature.Kind + " (seed " + seed + ")");
                }

                world.Heightfield.SampleHeight(feature.Position.X, feature.Position.Z).Should().BeApproximately(feature.Position.Y, Tolerance, "the bed lies flat at the surface");
                Math.Abs(feature.Position.Y).Should().BeLessThan(BiomeSpec.AshCavern.HeightAmplitude * 1.5f + 0.01f);
            }
        }
    }
}
