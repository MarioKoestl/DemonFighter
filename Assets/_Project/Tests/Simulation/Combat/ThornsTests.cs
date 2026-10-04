#nullable enable
using AwesomeAssertions;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Combat
{
    public sealed class ThornsTests
    {
        private const float Tolerance = 0.001f;

        [Test]
        public void ApplyHit_OnADemonWithSpines_PricksTheAttacker()
        {
            var scenario = new CombatScenario();
            scenario.Target.AttachPart(TestContent.Spines);

            scenario.HitCore(TestContent.Bite);

            scenario.DamageFrom(scenario.Attacker.Id).Should().ContainSingle().Which.Amount.Should().BeApproximately(12f, Tolerance);
            scenario.DamageFrom(scenario.Target.Id).Should().ContainSingle().Which.Amount.Should().BeApproximately(3.6f, Tolerance);
            scenario.Attacker.Body.Core.Hp.Should().BeApproximately(56.4f, Tolerance);
            scenario.Attacker.LastAttackedBy.Should().Be(scenario.Target.Id);
        }

        [Test]
        public void ApplyHit_BetweenTwoSpinyDemons_NeverChains()
        {
            var scenario = new CombatScenario();
            scenario.Attacker.AttachPart(TestContent.Spines);
            scenario.Target.AttachPart(TestContent.Spines);

            scenario.HitCore(TestContent.Bite);

            scenario.DamageFrom(scenario.Target.Id).Should().ContainSingle();
            scenario.Target.Body.Core.Hp.Should().BeApproximately(48f, Tolerance);
        }

        [Test]
        public void ApplyHit_WhenTheSpinesAreDestroyed_PricksNothing()
        {
            var scenario = new CombatScenario();
            BodyPart spines = scenario.Target.AttachPart(TestContent.Spines);
            spines.ApplyDamage(1000f);

            scenario.HitCore(TestContent.Bite);

            scenario.DamageFrom(scenario.Target.Id).Should().BeEmpty();
        }
    }
}
