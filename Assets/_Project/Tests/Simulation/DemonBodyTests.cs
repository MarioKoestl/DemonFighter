#nullable enable
using AwesomeAssertions;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Skills;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Tests.Combat;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests
{
    public sealed class DemonBodyTests
    {
        private const float Tolerance = 0.001f;

        [Test]
        public void AttachPart_Arm_AddsStrengthAndClawWithoutSpendingPoints()
        {
            Demon demon = Spawn();

            demon.AttachPart(TestContent.Arm);

            demon.EffectiveStats().Get(StatIds.Strength).Should().Be(1);
            demon.Stats.Get(StatIds.Strength).Should().Be(0);
            demon.Derived.DamageMultiplier.Should().BeApproximately(1.05f, Tolerance);
            demon.Skills.Should().HaveCount(3);
            SkillInstance? claw = demon.FindSkill(TestContent.ClawId);
            claw.Should().NotBeNull();
            claw!.IsGrantedBy(demon.Body).Should().BeTrue();
        }

        [Test]
        public void TierForAndSizeFor_MatchTheDemonAfterAttaching()
        {
            Demon demon = Spawn();
            int tier = Demon.TierFor(demon.Spec, demon.Evolutions, demon.Body.InvestmentPoints + 4, TestContent.Tuning);
            float size = Demon.SizeFor(demon.Spec, tier, TestContent.Tuning);

            demon.AttachPart(TestContent.Arm);
            demon.AttachPart(TestContent.Arm);
            demon.AttachPart(TestContent.Legs);
            demon.AttachPart(TestContent.Jaws);

            tier.Should().Be(demon.Spec.Tier + 1);
            demon.Tier.Should().Be(tier);
            demon.SizeMeters.Should().BeApproximately(size, Tolerance);
        }

        [Test]
        public void HighestTier_FollowsTheTierUp()
        {
            Demon demon = Spawn();
            demon.HighestTier.Should().Be(demon.Spec.Tier);

            demon.AttachPart(TestContent.Arm);
            demon.AttachPart(TestContent.Arm);
            demon.AttachPart(TestContent.Legs);
            demon.AttachPart(TestContent.Jaws);

            demon.HighestTier.Should().Be(demon.Spec.Tier + 1);
            demon.HighestTier.Should().Be(demon.Tier);
        }

        [Test]
        public void SenseLevel_RisesWithEachPairOfEyes()
        {
            Demon demon = Spawn();
            demon.SenseLevel.Should().Be(0);

            demon.AttachPart(TestContent.Eyes);
            int withOne = demon.SenseLevel;
            demon.AttachPart(TestContent.Eyes);

            withOne.Should().Be(1);
            demon.SenseLevel.Should().Be(2);
        }

        [Test]
        public void ReachMultiplier_WithEyes_GrowsByThePerceptionBonus()
        {
            Demon demon = Spawn();
            demon.ReachMultiplier.Should().Be(1f);

            demon.AttachPart(TestContent.Eyes);

            demon.ReachMultiplier.Should().BeApproximately(1.3f, Tolerance);
        }

        [Test]
        public void AttachPart_FourParts_RaisesTheTierAndTheSize()
        {
            Demon demon = Spawn();

            demon.AttachPart(TestContent.Arm);
            demon.AttachPart(TestContent.Arm);
            demon.AttachPart(TestContent.Legs);
            int tierAtThree = demon.Tier;
            demon.AttachPart(TestContent.Tail);

            tierAtThree.Should().Be(0);
            demon.Body.InvestmentPoints.Should().Be(4);
            demon.Tier.Should().Be(1);
            demon.SizeMeters.Should().BeApproximately(1.8f, Tolerance);
        }

        [Test]
        public void AttachPart_Legs_RaisesTheTopSpeed()
        {
            Demon demon = Spawn();
            float before = demon.MaxSpeed;

            demon.AttachPart(TestContent.Legs);

            before.Should().BeApproximately(4f, Tolerance);
            demon.MaxSpeed.Should().BeApproximately(6f, Tolerance);
        }

        [Test]
        public void ApplyHit_WithJaws_BitesHarder()
        {
            var scenario = new CombatScenario();
            scenario.Attacker.AttachPart(TestContent.Jaws);

            scenario.HitCore(TestContent.Bite);

            scenario.DamageFrom(scenario.Attacker.Id).Should().ContainSingle().Which.Amount.Should().BeApproximately(15f, Tolerance);
        }

        [Test]
        public void RegrowPart_AfterSeveringTheArm_RestoresClawAtItsOldLevel()
        {
            Demon demon = Spawn();
            BodyPart arm = demon.AttachPart(TestContent.Arm);
            SkillInstance claw = demon.FindSkill(TestContent.ClawId)!;
            claw.GainXp(100f);
            arm.ApplyDamage(1000f);
            bool usableWhileLost = claw.IsGrantedBy(demon.Body);

            demon.RegrowPart(arm);

            usableWhileLost.Should().BeFalse();
            claw.IsGrantedBy(demon.Body).Should().BeTrue();
            claw.Level.Should().Be(2);
            demon.Derived.DamageMultiplier.Should().BeApproximately(1.05f, Tolerance);
        }

        [Test]
        public void UpgradePart_Arm_RaisesStrengthTwice()
        {
            Demon demon = Spawn();
            BodyPart arm = demon.AttachPart(TestContent.Arm);

            demon.UpgradePart(arm);

            demon.EffectiveStats().Get(StatIds.Strength).Should().Be(2);
            demon.Derived.DamageMultiplier.Should().BeApproximately(1.1f, Tolerance);
        }

        [Test]
        public void RecordEvolution_Once_RaisesTierAndSize()
        {
            Demon demon = Spawn();

            demon.RecordEvolution();

            demon.Evolutions.Should().Be(1);
            demon.Tier.Should().Be(1);
            demon.SizeMeters.Should().BeApproximately(1.8f, Tolerance);
        }

        [Test]
        public void Constructor_Elder_KeepsItsSpecTierAndSize()
        {
            Demon elder = new DemonBuilder().WithSpec(TestContent.Elder).SpawnInto(new RunStateBuilder().Build());

            elder.Tier.Should().Be(6);
            elder.SizeMeters.Should().BeApproximately(15f, Tolerance);
        }

        private static Demon Spawn()
        {
            return new DemonBuilder().AsPlayer().SpawnInto(new RunStateBuilder().Build());
        }
    }
}
