#nullable enable
using AwesomeAssertions;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Skills;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Skills
{
    public sealed class SkillInstanceTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void Constructor_Always_StartsAtLevelOneWithFullCost()
        {
            var skill = new SkillInstance(TestContent.Bite, 0);

            int level = skill.Level;

            level.Should().Be(1);
            skill.DamageMultiplier.Should().BeApproximately(1f, Tolerance);
            skill.StaminaCost.Should().BeApproximately(15f, Tolerance);
            skill.IsOnCooldown(0).Should().BeFalse();
        }

        [Test]
        public void GainXp_BaseXp_ReachesLevelTwoAndCheapensTheSkill()
        {
            var skill = new SkillInstance(TestContent.Bite, 0);

            int gained = skill.GainXp(100f);

            gained.Should().Be(1);
            skill.Level.Should().Be(2);
            skill.Xp.Should().BeApproximately(0f, Tolerance);
            skill.DamageMultiplier.Should().BeApproximately(1.05f, Tolerance);
            skill.StaminaCost.Should().BeApproximately(15f * 0.97f, Tolerance);
        }

        [Test]
        public void GainXp_HugeAmount_StopsAtTheMaxLevel()
        {
            SkillSpec capped = TestContent.Bite with { LevelCurve = new SkillLevelCurve { MaxLevel = 3 } };
            var skill = new SkillInstance(capped, 0);

            skill.GainXp(100000f);

            skill.Level.Should().Be(3);
        }

        [Test]
        public void StaminaCost_ManyLevels_NeverDropsBelowTheFloor()
        {
            SkillSpec cheap = TestContent.Bite with { StaminaCostReductionPerLevel = 0.5f, LevelCurve = new SkillLevelCurve { BaseXp = 1f, Exponent = 0f } };
            var skill = new SkillInstance(cheap, 0);
            skill.GainXp(50f);

            float cost = skill.StaminaCost;

            cost.Should().BeApproximately(7.5f, Tolerance);
        }

        [Test]
        public void StartCooldown_Always_IsOnCooldownUntilTheTick()
        {
            var skill = new SkillInstance(TestContent.Bite, 0);

            skill.StartCooldown(12);

            skill.IsOnCooldown(11).Should().BeTrue();
            skill.IsOnCooldown(12).Should().BeFalse();
        }
    }
}
