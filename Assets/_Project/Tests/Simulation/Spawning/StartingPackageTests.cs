#nullable enable
using System.Linq;
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Spawning
{
    public sealed class StartingPackageTests
    {
        private const float Tolerance = 0.001f;

        private static readonly DemonSpec Brute = TestContent.Blob with
        {
            Id = "demon.brute.test",
            Name = "Brute",
            StartingPartIds = new[] { TestContent.ArmId },
            StartingBiomass = 30f,
            StartingLevel = 2,
            StartingEvolutionId = TestContent.Brute1.Id,
        };

        [Test]
        public void SpawnDemon_WithAStartingPackage_IsBornGrownWithoutCostOrTransformation()
        {
            RunState state = new RunStateBuilder().Build();

            Demon brute = state.SpawnDemon(ControllerKind.Ai, Brute, Vector3.Zero, 0f);

            brute.Body.Parts.Select(p => p.Spec.Id).Should().Contain(TestContent.ArmId).And.Contain(TestContent.HideId);
            brute.Biomass.Should().BeApproximately(30f, Tolerance);
            brute.Level.Should().Be(1, "its starting evolution started Tier 1 at level 1 (D-091)");
            brute.Stats.UnspentPoints.Should().Be(TestContent.Tuning.StatPointsPerLevel + TestContent.Brute1.StatPoints);
            brute.Evolutions.Should().Be(1);
            brute.Stats.Get(StatIds.Strength).Should().Be(10);
            brute.Tier.Should().Be(Brute.Tier + 1);
            brute.SizeMeters.Should().BeApproximately(Brute.SizeMeters * 1.5f, Tolerance);
            brute.IsTransforming(state.Tick).Should().BeFalse();
        }

        [Test]
        public void SpawnDemon_PlainBlob_StartsAsBefore()
        {
            RunState state = new RunStateBuilder().Build();

            Demon blob = state.SpawnDemon(ControllerKind.Ai, TestContent.Blob, Vector3.Zero, 0f);

            blob.Body.Parts.Should().HaveCount(1);
            blob.Biomass.Should().Be(0f);
            blob.Level.Should().Be(1);
            blob.Evolutions.Should().Be(0);
        }

        [Test]
        public void SpawnDemon_StartingPartWithoutASocket_IsSkipped()
        {
            DemonSpec crowded = TestContent.Blob with { Id = "demon.crowded.test", Name = "Crowded", StartingPartIds = new[] { TestContent.LegsId, TestContent.LegsId } };
            RunState state = new RunStateBuilder().Build();

            Demon demon = state.SpawnDemon(ControllerKind.Ai, crowded, Vector3.Zero, 0f);

            demon.Body.Parts.Count(p => p.Spec.Id == TestContent.LegsId).Should().Be(1);
        }
    }
}
