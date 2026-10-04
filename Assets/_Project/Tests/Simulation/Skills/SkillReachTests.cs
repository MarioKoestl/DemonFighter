#nullable enable
using AwesomeAssertions;
using DemonFighter.Simulation.Skills;
using DemonFighter.Simulation.Tests.Combat;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Skills
{
    public sealed class SkillReachTests
    {
        private const float Tolerance = 0.001f;

        [Test]
        public void Meters_BlobBitingABlob_IsReachPlusBodyRadiusPlusSlack()
        {
            var scenario = new CombatScenario();
            SkillInstance bite = scenario.Attacker.FindSkill(TestContent.BiteId)!;

            float reach = SkillReach.Meters(scenario.Attacker, scenario.Target, bite);

            float expected = TestContent.Bite.ReachPerMeter * 1.2f + 1.2f * SkillReach.TargetRadiusPerMeter + SkillReach.ToleranceMeters;
            reach.Should().BeApproximately(expected, Tolerance);
        }

        [Test]
        public void Meters_WithEyes_StretchesOnlyTheSkillReach()
        {
            var scenario = new CombatScenario();
            scenario.Attacker.AttachPart(TestContent.Eyes);
            SkillInstance bite = scenario.Attacker.FindSkill(TestContent.BiteId)!;

            float reach = SkillReach.Meters(scenario.Attacker, scenario.Target, bite);

            float expected = TestContent.Bite.ReachPerMeter * 1.2f * 1.3f + 1.2f * SkillReach.TargetRadiusPerMeter + SkillReach.ToleranceMeters;
            reach.Should().BeApproximately(expected, Tolerance);
        }
    }
}
