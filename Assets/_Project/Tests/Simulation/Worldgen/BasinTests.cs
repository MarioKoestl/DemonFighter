#nullable enable
using System;
using AwesomeAssertions;
using DemonFighter.Simulation.Worldgen;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Worldgen
{
    /// <summary>
    /// Regression for D-086: a flat pool on a slope was partly buried under the ground, and the buried part still
    /// burned. Every pool must now lie in a basin, its whole surface above the ground it covers.
    /// </summary>
    public sealed class BasinTests
    {
        private const float SurfaceLift = 0.05f;
        private const float Tolerance = 0.0001f;

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

        [Test]
        public void Generate_PoolSurface_SitsAtTheLowestGroundOfItsDisc()
        {
            WorldLayout world = new CavernWorldGenerator().Generate(2, BiomeSpec.AshCavern);

            foreach (FeaturePlacement feature in world.Features)
            {
                if (feature.Kind != FeatureKind.LavaPool)
                {
                    continue;
                }

                // The floor right under the surface is a hand's width below it, not a cliff.
                float floor = world.Heightfield.SampleHeight(feature.Position.X, feature.Position.Z);
                (feature.Position.Y - floor).Should().BeInRange(0.2f, 0.4f);
                Math.Abs(feature.Position.Y).Should().BeLessThan(BiomeSpec.AshCavern.HeightAmplitude * 1.5f + 0.01f);
            }
        }
    }
}
