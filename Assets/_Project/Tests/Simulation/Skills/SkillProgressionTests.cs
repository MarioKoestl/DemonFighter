#nullable enable
using AwesomeAssertions;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Skills;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Tests.Combat;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Skills
{
    public sealed class SkillProgressionTests
    {
        private const float Tolerance = 0.001f;

        [Test]
        public void Perk_ArrivesAtItsLevelAndNotBefore()
        {
            var bite = new SkillInstance(TestContent.Bite);
            RaiseTo(bite, 9);
            bool atNine = bite.HasPerk;

            RaiseTo(bite, 10);

            atNine.Should().BeFalse();
            bite.HasPerk.Should().BeTrue();
            bite.Perk!.Name.Should().Be("Deep Bite");
            bite.XpForNextLevel.Should().BeApproximately(TestContent.Bite.LevelCurve.XpForNextLevel(10), Tolerance);
        }

        [Test]
        public void ApplyHit_BiteWithPerk_BleedsHalfAgainAsLong()
        {
            var scenario = new CombatScenario();

            scenario.HitCore(TestContent.Bite, 10);

            scenario.Target.Body.Core.BleedSecondsLeft.Should().BeApproximately(4.5f, Tolerance);
        }

        [Test]
        public void Numbers_AtLevelSix_ScaleByTheSpecRates()
        {
            var bite = new SkillInstance(TestContent.Bite);
            RaiseTo(bite, 6);

            bite.CooldownSeconds.Should().BeApproximately(0.54f, Tolerance);
            bite.ReachPerMeter.Should().BeApproximately(1.47f, Tolerance);
            bite.WindupSeconds.Should().BeApproximately(0.15f / 1.1f, Tolerance);
            bite.DamageMultiplier.Should().BeApproximately(1.25f, Tolerance);
        }

        [Test]
        public void StaminaCostPerSecond_SprintWithPerk_IsCheaper()
        {
            var sprint = new SkillInstance(TestContent.Sprint);
            RaiseTo(sprint, 10);

            sprint.StaminaCostPerSecond.Should().BeApproximately(15f * 0.73f * 0.7f, Tolerance);
        }

        [Test]
        public void Grab_WithPerk_HoldsLonger()
        {
            var scenario = new CombatScenario(TestContent.Blob with { SizeMeters = 2f });
            scenario.Attacker.AttachPart(TestContent.Arm);
            RaiseTo(scenario.Attacker.FindSkill(TestContent.GrabId)!, 10);
            scenario.StartSkillAndReachActiveWindow(TestContent.GrabId);

            scenario.Submit(new ReportHitCommand(scenario.Attacker.Id, scenario.Target.Id, scenario.Target.Body.Core.Index));
            scenario.Tick(1);

            long hitTick = scenario.State.Tick - 1;
            scenario.Held.Should().ContainSingle().Which.UntilTick.Should().Be(hitTick + scenario.State.Config.TicksFor(2f));
        }

        [Test]
        public void Claw_WithPerk_RecoversInFewerTicks()
        {
            var slow = new CombatScenario();
            slow.Attacker.AttachPart(TestContent.Arm);
            var fast = new CombatScenario();
            fast.Attacker.AttachPart(TestContent.Arm);
            RaiseTo(fast.Attacker.FindSkill(TestContent.ClawId)!, 10);

            slow.Submit(new UseSkillCommand(slow.Attacker.Id, TestContent.ClawId));
            slow.Tick(1);
            fast.Submit(new UseSkillCommand(fast.Attacker.Id, TestContent.ClawId));
            fast.Tick(1);

            long slowRecovery = slow.Activations[0].EndTick - slow.Activations[0].ActiveUntilTick - 1;
            long fastRecovery = fast.Activations[0].EndTick - fast.Activations[0].ActiveUntilTick - 1;
            slowRecovery.Should().Be(5);
            fastRecovery.Should().Be(4);
        }

        [Test]
        public void Hit_OnAMuchHigherTier_GrantsMoreSkillXp()
        {
            var scenario = new CombatScenario();
            Demon elder = new DemonBuilder().WithSpec(TestContent.Elder).At(0f, 2f).SpawnInto(scenario.State);
            scenario.StartBiteAndReachActiveWindow();

            scenario.Submit(new ReportHitCommand(scenario.Attacker.Id, elder.Id, elder.Body.Core.Index));
            scenario.Tick(1);

            scenario.SkillXp.Should().ContainSingle().Which.Amount.Should().BeApproximately(40f, Tolerance);
        }

        [Test]
        public void Hit_OnPreyFarBelow_GrantsLittleSkillXp()
        {
            var scenario = new CombatScenario(TestContent.Elder);
            scenario.StartBiteAndReachActiveWindow();

            scenario.Submit(new ReportHitCommand(scenario.Attacker.Id, scenario.Target.Id, scenario.Target.Body.Core.Index));
            scenario.Tick(1);

            scenario.SkillXp.Should().ContainSingle().Which.Amount.Should().BeApproximately(1f, Tolerance);
        }

        private static void RaiseTo(SkillInstance skill, int level)
        {
            while (skill.Level < level)
            {
                skill.GainXp(skill.XpForNextLevel);
            }
        }
    }
}
