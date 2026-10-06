#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Worldgen;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Worldgen
{
    public sealed class CavernWorldGeneratorTests
    {
        private const int Seed = 4242;

        /// <summary>A CharacterController climbs 45 degrees; the floor stays far below that.</summary>
        private const float MaxWalkableSlope = 0.7f;

        private static readonly BiomeSpec Biome = BiomeSpec.AshCavern;

        [Test]
        public void Generate_SameSeed_ProducesIdenticalLayouts()
        {
            WorldLayout first = new CavernWorldGenerator().Generate(Seed, Biome);
            WorldLayout second = new CavernWorldGenerator().Generate(Seed, Biome);

            second.Heightfield.Heights.Should().Equal(first.Heightfield.Heights);
            second.Features.Should().Equal(first.Features);
            second.SpawnPoints.Should().Equal(first.SpawnPoints);
            second.ElderRoute.Should().Equal(first.ElderRoute);
        }

        [Test]
        public void Generate_DifferentSeeds_ProduceDifferentLayouts()
        {
            WorldLayout first = new CavernWorldGenerator().Generate(Seed, Biome);
            WorldLayout second = new CavernWorldGenerator().Generate(Seed + 1, Biome);

            second.Heightfield.Heights.Should().NotEqual(first.Heightfield.Heights);
            second.Features.Should().NotEqual(first.Features);
        }

        [Test]
        public void Generate_AshCavern_PlacesTheFeatureCountsTheBiomeAsksFor()
        {
            WorldLayout layout = new CavernWorldGenerator().Generate(Seed, Biome);

            Count(layout, FeatureKind.Rock).Should().BeInRange(Biome.RockCountMin, Biome.RockCountMax);
            Count(layout, FeatureKind.Fissure).Should().BeInRange(Biome.FissureCountMin, Biome.FissureCountMax);
            Count(layout, FeatureKind.LavaPool).Should().Be(Biome.LavaPoolCount);
            Count(layout, FeatureKind.WaterPool).Should().Be(Biome.WaterPoolCount);
            Count(layout, FeatureKind.BonePile).Should().BeInRange(Biome.BonePileCountMin, Biome.BonePileCountMax);
        }

        [Test]
        public void Generate_AshCavern_KeepsEveryFeatureInsideTheWalkableBounds()
        {
            WorldLayout layout = new CavernWorldGenerator().Generate(Seed, Biome);

            foreach (FeaturePlacement feature in layout.Features)
            {
                layout.Bounds.Contains(feature.Position).Should().BeTrue();
            }
        }

        [Test]
        public void Generate_AshCavern_NoSlopeNeedsJumping()
        {
            WorldLayout layout = new CavernWorldGenerator().Generate(Seed, Biome);

            float slope = layout.Heightfield.MaxSlopeTangent();

            slope.Should().BeLessThan(MaxWalkableSlope);
            slope.Should().BeGreaterThan(0f);
        }

        [Test]
        public void Generate_AshCavern_HeightsStayWithinTheAmplitude()
        {
            WorldLayout layout = new CavernWorldGenerator().Generate(Seed, Biome);
            // Pool beds and shores lie at ground heights the terrain already had (D-086).
            float limit = Biome.HeightAmplitude * 1.5f + 0.001f;

            foreach (float height in layout.Heightfield.Heights)
            {
                Math.Abs(height).Should().BeLessThanOrEqualTo(limit);
            }
        }

        [Test]
        public void Generate_AshCavern_SpawnsPlayerAndBlobsOnClearGround()
        {
            WorldLayout layout = new CavernWorldGenerator().Generate(Seed, Biome);

            layout.SpawnPoints.Count.Should().Be(Biome.InitialBlobs + 1);
            foreach (Vector3 spawn in layout.SpawnPoints)
            {
                layout.Bounds.Contains(spawn).Should().BeTrue();
                GroundDistance(spawn, layout.PlayerSpawn).Should().BeLessThanOrEqualTo(Biome.SpawnClusterRadius + 0.001f);
                foreach (FeaturePlacement feature in layout.Features)
                {
                    GroundDistance(spawn, feature.Position).Should().BeGreaterThan(feature.FootprintRadius);
                }
            }
        }

        [Test]
        public void Generate_AshCavern_SpawnHeightsSitOnTheTerrain()
        {
            WorldLayout layout = new CavernWorldGenerator().Generate(Seed, Biome);

            foreach (Vector3 spawn in layout.SpawnPoints)
            {
                spawn.Y.Should().BeApproximately(layout.Heightfield.SampleHeight(spawn.X, spawn.Z), 0.0001f);
            }
        }

        [Test]
        public void Generate_AshCavern_ElderRouteIsALoopInsideTheBoundsClearOfFeatures()
        {
            WorldLayout layout = new CavernWorldGenerator().Generate(Seed, Biome);

            layout.ElderRoute.Count.Should().Be(Biome.ElderRouteWaypoints);
            foreach (Vector3 waypoint in layout.ElderRoute)
            {
                layout.Bounds.Contains(waypoint).Should().BeTrue();
            }

            foreach (FeaturePlacement feature in layout.Features)
            {
                CavernWorldGenerator.DistanceToLoop(feature.Position, layout.ElderRoute)
                    .Should().BeGreaterThanOrEqualTo(Biome.ElderRouteClearance + feature.FootprintRadius - 0.001f);
            }
        }

        [Test]
        public void Generate_AshCavern_ElderStartsNearTheSpawnButNotOnIt()
        {
            WorldLayout layout = new CavernWorldGenerator().Generate(Seed, Biome);

            float distance = GroundDistance(layout.ElderSpawn, layout.PlayerSpawn);

            distance.Should().BeGreaterThanOrEqualTo(Biome.ElderMinSpawnDistance);
            distance.Should().BeLessThan(Biome.ElderRouteRadius + Biome.ElderRouteJitter + Biome.SizeMeters * 0.2f);
        }

        [Test]
        public void Generate_ManySeeds_AlwaysMeetsTheFeatureCounts()
        {
            var generator = new CavernWorldGenerator();

            for (int seed = 1; seed <= 20; seed++)
            {
                WorldLayout layout = generator.Generate(seed, Biome);
                Count(layout, FeatureKind.Rock).Should().BeInRange(Biome.RockCountMin, Biome.RockCountMax, "seed " + seed);
                Count(layout, FeatureKind.LavaPool).Should().Be(Biome.LavaPoolCount, "seed " + seed);
                layout.SpawnPoints.Count.Should().Be(Biome.InitialBlobs + 1, "seed " + seed);
            }
        }

        [Test]
        public void Generate_InvalidBiome_Throws()
        {
            BiomeSpec broken = Biome with { RockCountMin = 10, RockCountMax = 5 };

            Action act = () => new CavernWorldGenerator().Generate(Seed, broken);

            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void DistanceToLoop_PointBesideASegment_IsThePerpendicularDistance()
        {
            var loop = new List<Vector3> { new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f), new Vector3(10f, 0f, 10f), new Vector3(0f, 0f, 10f) };

            float distance = CavernWorldGenerator.DistanceToLoop(new Vector3(5f, 0f, 3f), loop);

            distance.Should().BeApproximately(3f, 0.0001f);
        }

        private static int Count(WorldLayout layout, FeatureKind kind)
        {
            int count = 0;
            foreach (FeaturePlacement feature in layout.Features)
            {
                if (feature.Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        private static float GroundDistance(Vector3 a, Vector3 b)
        {
            return new Vector2(a.X - b.X, a.Z - b.Z).Length();
        }
    }
}
