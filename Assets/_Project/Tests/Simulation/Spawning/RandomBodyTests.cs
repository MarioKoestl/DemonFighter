#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Spawning
{
    /// <summary>A kind born mutated, such as the elder: a random body from the run's seed, at its highest upgrades (D-095).</summary>
    public sealed class RandomBodyTests
    {
        private const float Tolerance = 0.001f;

        private static readonly DemonSpec Mutated = TestContent.Elder with { Id = "demon.mutated.test", Name = "Mutated", RandomPartChance = 0.75f, StartingPartsAtMaxUpgrade = true };

        [Test]
        public void SpawnDemon_SameSeed_GrowsTheSameBody()
        {
            Demon first = Spawn(Mutated, seed: 7);
            Demon second = Spawn(Mutated, seed: 7);

            Signature(second).Should().Be(Signature(first));
        }

        [Test]
        public void SpawnDemon_OtherSeeds_GrowOtherBodies()
        {
            var bodies = new HashSet<string>();
            for (int seed = 1; seed <= 20; seed++)
            {
                bodies.Add(Signature(Spawn(Mutated, seed)));
            }

            bodies.Count.Should().BeGreaterThan(3, "every run should meet a different elder");
        }

        // Head gets jaws and eyes before a second of either, both flanks get an arm, the tail needs no unlock here.
        [Test]
        public void SpawnDemon_CertainChance_FillsEverySocketAndRepeatsOnlyOnceEachPartIsOn()
        {
            Demon demon = Spawn(Mutated with { RandomPartChance = 1f }, seed: 3);

            List<string> ids = demon.Body.Parts.Where(p => !p.Spec.IsCore).Select(p => p.Spec.Id).ToList();
            ids.Should().HaveCount(7);
            ids.Should().ContainSingle(id => id == TestContent.JawsId);
            ids.Should().ContainSingle(id => id == TestContent.EyesId);
            ids.Count(id => id == TestContent.ArmId).Should().Be(2);
            ids.Should().ContainSingle(id => id == TestContent.LegsId);
            ids.Should().ContainSingle(id => id == TestContent.TailId, "parts that need an evolution count too");
            ids.Count(id => id == TestContent.HideId || id == TestContent.SpinesId).Should().Be(1);
        }

        [Test]
        public void SpawnDemon_AtMaxUpgrade_BornWithEveryPartAtItsHighestUpgradeAndFullHealth()
        {
            Demon demon = Spawn(Mutated with { RandomPartChance = 1f }, seed: 5);

            foreach (BodyPart part in demon.Body.Parts.Where(p => !p.Spec.IsCore))
            {
                part.UpgradeLevel.Should().Be(part.Spec.MaxUpgrade, part.Spec.Id + " is fully upgraded");
                part.Hp.Should().BeApproximately(part.MaxHp, Tolerance);
                part.MaxHp.Should().BeGreaterThan(part.Spec.MaxHp);
            }
        }

        // The fixed package goes on first; the random parts only take what it left free.
        [Test]
        public void SpawnDemon_WithStartingParts_FillsOnlyTheSocketsLeftFree()
        {
            DemonSpec spec = Mutated with { RandomPartChance = 1f, StartingPartIds = new[] { TestContent.SpinesId } };

            Demon demon = Spawn(spec, seed: 9);

            demon.Body.Parts.Where(p => p.Spec.Socket == SocketKind.Hide).Select(p => p.Spec.Id).Should().Equal(TestContent.SpinesId);
        }

        [Test]
        public void SpawnDemon_KindWithoutRandomParts_KeepsItsBareBody()
        {
            Demon demon = Spawn(TestContent.Elder, seed: 7);

            demon.Body.Parts.Should().HaveCount(1);
        }

        private static Demon Spawn(DemonSpec spec, int seed)
        {
            RunState state = new RunStateBuilder().WithSeed(seed).Build();
            return state.SpawnDemon(ControllerKind.Ai, spec, Vector3.Zero, 0f);
        }

        private static string Signature(Demon demon)
        {
            return string.Join(",", demon.Body.Parts.Select(p => p.Spec.Id + "+" + p.UpgradeLevel));
        }
    }
}
