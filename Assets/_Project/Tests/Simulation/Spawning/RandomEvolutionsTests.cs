#nullable enable
using System.Collections.Generic;
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Spawning
{
    /// <summary>A kind born evolved, such as the elder: both stages of a random line, drawn from the run's seed (D-095).</summary>
    public sealed class RandomEvolutionsTests
    {
        private const float Tolerance = 0.001f;

        private static readonly DemonSpec Evolved = TestContent.Elder with { Id = "demon.evolved.test", Name = "Evolved", Tier = 4, SizeMeters = 7.5f, RandomEvolutionStages = 2 };

        [Test]
        public void SpawnDemon_BornEvolved_TakesBothStagesOfOneLineAndWalksTwoTiersUp()
        {
            Demon demon = Spawn(Evolved, seed: 7);

            demon.Evolutions.Should().Be(2);
            demon.Tier.Should().Be(6);
            demon.Level.Should().Be(1, "every evolution starts its tier at level 1 (D-091)");
            demon.SizeMeters.Should().BeApproximately(15f, Tolerance, "two tiers double the base size of 7.5 m");
            LineOf(demon.EvolutionIds[1]).Should().Be(LineOf(demon.EvolutionIds[0]), "the second stage stays with the line of the first");
        }

        [Test]
        public void SpawnDemon_BornEvolved_GetsTheWholePackageOfEachStage()
        {
            Demon demon = Spawn(Evolved, seed: 7);
            int points = 0;
            foreach (string id in demon.EvolutionIds)
            {
                points += TestContent.Catalog().GetEvolution(id).StatPoints;
            }

            demon.Stats.UnspentPoints.Should().Be(points);
        }

        [Test]
        public void SpawnDemon_SameSeed_TakesTheSameLine()
        {
            Spawn(Evolved, seed: 11).EvolutionIds.Should().Equal(Spawn(Evolved, seed: 11).EvolutionIds);
        }

        [Test]
        public void SpawnDemon_OtherSeeds_TakeEveryLine()
        {
            var lines = new HashSet<string>();
            for (int seed = 1; seed <= 30; seed++)
            {
                lines.Add(LineOf(Spawn(Evolved, seed).EvolutionIds[0]));
            }

            lines.Should().HaveCount(3, "Brute, Stalker and Bulwark all turn up over thirty runs");
        }

        // Stages the content does not have are skipped rather than failing the spawn.
        [Test]
        public void SpawnDemon_MoreStagesThanTheContentHas_StopsAtTheLastStage()
        {
            Demon demon = Spawn(Evolved with { RandomEvolutionStages = 5 }, seed: 3);

            demon.Evolutions.Should().Be(2);
        }

        private static Demon Spawn(DemonSpec spec, int seed)
        {
            RunState state = new RunStateBuilder().WithSeed(seed).Build();
            return state.SpawnDemon(ControllerKind.Ai, spec, Vector3.Zero, 0f);
        }

        // The test content names a line by its first word: evolution.brute.2 belongs to brute.
        private static string LineOf(string evolutionId)
        {
            return evolutionId.Split('.')[1];
        }
    }
}
