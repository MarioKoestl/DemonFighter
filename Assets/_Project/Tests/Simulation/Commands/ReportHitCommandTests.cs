#nullable enable
using System;
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Tests.Combat;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Commands
{
    public sealed class ReportHitCommandTests
    {
        private const float Tolerance = 0.01f;

        [Test]
        public void Tick_HitInTheActiveWindow_DamagesTheTargetAndGrantsSkillXp()
        {
            var scenario = new CombatScenario();
            scenario.StartBiteAndReachActiveWindow();

            scenario.Submit(new ReportHitCommand(scenario.Attacker.Id, scenario.Target.Id, scenario.Target.Body.Core.Index));
            scenario.Tick(1);

            scenario.DamageFrom(scenario.Attacker.Id).Should().ContainSingle().Which.Amount.Should().BeApproximately(12f, Tolerance);
            scenario.Target.Body.Core.Hp.Should().BeLessThan(49f);
            scenario.SkillXp.Should().ContainSingle();
            scenario.SkillXp[0].Amount.Should().BeApproximately(10f, Tolerance);
            scenario.Attacker.FindSkill(TestContent.BiteId)!.Xp.Should().BeApproximately(10f, Tolerance);
            scenario.Rejections.Should().BeEmpty();
        }

        [Test]
        public void Tick_SecondReportInTheSameUse_IsRejected()
        {
            var scenario = new CombatScenario();
            scenario.StartBiteAndReachActiveWindow();
            scenario.Submit(new ReportHitCommand(scenario.Attacker.Id, scenario.Target.Id, 0));
            scenario.Tick(1);

            scenario.Submit(new ReportHitCommand(scenario.Attacker.Id, scenario.Target.Id, 0));
            scenario.Tick(1);

            scenario.DamageFrom(scenario.Attacker.Id).Should().ContainSingle();
            scenario.Rejections.Should().ContainSingle();
            scenario.Rejections[0].Reason.Should().Be(ReportHitCommandHandler.AlreadyHit);
        }

        [Test]
        public void Tick_ReportDuringWindup_IsRejected()
        {
            var scenario = new CombatScenario();
            scenario.Submit(new UseSkillCommand(scenario.Attacker.Id, TestContent.BiteId));
            scenario.Tick(1);

            scenario.Submit(new ReportHitCommand(scenario.Attacker.Id, scenario.Target.Id, 0));
            scenario.Tick(1);

            scenario.Rejections.Should().ContainSingle();
            scenario.Rejections[0].Reason.Should().Be(ReportHitCommandHandler.NoActiveSkill);
        }

        [Test]
        public void Tick_ReportWithoutAnySkillUse_IsRejected()
        {
            var scenario = new CombatScenario();

            scenario.Submit(new ReportHitCommand(scenario.Attacker.Id, scenario.Target.Id, 0));
            scenario.Tick(1);

            scenario.Rejections.Should().ContainSingle();
            scenario.Rejections[0].Reason.Should().Be(ReportHitCommandHandler.NoActiveSkill);
        }

        [Test]
        public void Tick_TargetOutOfReach_IsRejected()
        {
            var scenario = new CombatScenario();
            scenario.Target.SetPose(new Vector3(0f, 0f, 10f), 0f);
            scenario.StartBiteAndReachActiveWindow();

            scenario.Submit(new ReportHitCommand(scenario.Attacker.Id, scenario.Target.Id, 0));
            scenario.Tick(1);

            scenario.Rejections.Should().ContainSingle();
            scenario.Rejections[0].Reason.Should().Be(ReportHitCommandHandler.OutOfReach);
        }

        [Test]
        public void Tick_TargetBehindTheAttacker_IsRejected()
        {
            var scenario = new CombatScenario();
            scenario.Target.SetPose(new Vector3(0f, 0f, -2f), 0f);
            scenario.StartBiteAndReachActiveWindow();

            scenario.Submit(new ReportHitCommand(scenario.Attacker.Id, scenario.Target.Id, 0));
            scenario.Tick(1);

            scenario.Rejections.Should().ContainSingle();
            scenario.Rejections[0].Reason.Should().Be(ReportHitCommandHandler.OutsideArc);
        }

        [Test]
        public void Tick_HitOnALostPart_IsRejected()
        {
            var scenario = new CombatScenario();
            BodyPart arm = scenario.Target.Body.AddPart(TestContent.Arm, 1f);
            arm.ApplyDamage(1000f);
            scenario.StartBiteAndReachActiveWindow();

            scenario.Submit(new ReportHitCommand(scenario.Attacker.Id, scenario.Target.Id, arm.Index));
            scenario.Tick(1);

            scenario.Rejections.Should().ContainSingle();
            scenario.Rejections[0].Reason.Should().Be(ReportHitCommandHandler.PartLost);
        }

        [Test]
        public void Tick_HitOnADeadTarget_IsRejected()
        {
            var scenario = new CombatScenario();
            scenario.Target.Body.Core.ApplyDamage(1000f);
            scenario.StartBiteAndReachActiveWindow();

            scenario.Submit(new ReportHitCommand(scenario.Attacker.Id, scenario.Target.Id, 0));
            scenario.Tick(1);

            scenario.Rejections.Should().ContainSingle();
            scenario.Rejections[0].Reason.Should().Be(ReportHitCommandHandler.TargetDead);
        }

        [Test]
        public void Tick_SelfReport_IsRejected()
        {
            var scenario = new CombatScenario();
            scenario.StartBiteAndReachActiveWindow();

            scenario.Submit(new ReportHitCommand(scenario.Attacker.Id, scenario.Attacker.Id, 0));
            scenario.Tick(1);

            scenario.Rejections.Should().ContainSingle();
            scenario.Rejections[0].Reason.Should().Be(ReportHitCommandHandler.SelfTarget);
        }

        [Test]
        public void Tick_IntentFacingAtTheTarget_CountsWhileTheBodyStillTurns()
        {
            var scenario = new CombatScenario();
            scenario.Attacker.SetPose(scenario.Attacker.Position, MathF.PI);
            scenario.Attacker.AttachBody();
            scenario.Submit(new MoveCommand(scenario.Attacker.Id, Vector2.Zero, sprint: false, facing: new Vector2(0f, 1f)));
            scenario.StartBiteAndReachActiveWindow();

            scenario.Submit(new ReportHitCommand(scenario.Attacker.Id, scenario.Target.Id, 0));
            scenario.Tick(1);

            scenario.Rejections.Should().BeEmpty();
            scenario.DamageFrom(scenario.Attacker.Id).Should().ContainSingle();
        }

        [Test]
        public void Tick_TenLandedBites_LevelTheSkillUp()
        {
            var scenario = new CombatScenario();
            DemonSpec sturdy = TestContent.Blob with { StartingStats = new[] { new StatValue(StatIds.Constitution, 30) } };
            Demon sturdyTarget = new DemonBuilder().WithSpec(sturdy).At(0f, 2f).SpawnInto(scenario.State);

            for (int i = 0; i < 10; i++)
            {
                scenario.StartBiteAndReachActiveWindow();
                scenario.Submit(new ReportHitCommand(scenario.Attacker.Id, sturdyTarget.Id, sturdyTarget.Body.Core.Index));
                scenario.Tick(1);
                while (scenario.Attacker.CurrentSkillUse != null || scenario.Attacker.FindSkill(TestContent.BiteId)!.IsOnCooldown(scenario.State.Tick))
                {
                    scenario.Tick(1);
                }
            }

            scenario.SkillLevelUps.Should().ContainSingle();
            scenario.SkillLevelUps[0].NewLevel.Should().Be(2);
            scenario.Attacker.FindSkill(TestContent.BiteId)!.Level.Should().Be(2);
            scenario.DamageFrom(scenario.Attacker.Id).Should().HaveCount(10);
        }
    }
}
