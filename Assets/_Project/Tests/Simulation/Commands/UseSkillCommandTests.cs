#nullable enable
using AwesomeAssertions;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Tests.Combat;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Commands
{
    public sealed class UseSkillCommandTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void Tick_UseBite_SpendsStaminaAndAnnouncesTheActiveWindow()
        {
            var scenario = new CombatScenario();

            scenario.Submit(new UseSkillCommand(scenario.Attacker.Id, TestContent.BiteId));
            scenario.Tick(1);

            scenario.Attacker.Stamina.Should().BeApproximately(100f - 15f + 12f * 0.05f, Tolerance);
            scenario.Activations.Should().ContainSingle();
            scenario.Activations[0].SkillId.Should().Be(TestContent.BiteId);
            scenario.Activations[0].ActiveFromTick.Should().Be(3);
            scenario.Activations[0].ActiveUntilTick.Should().Be(6);
            scenario.Activations[0].EndTick.Should().Be(14);
            scenario.Attacker.CurrentSkillUse.Should().NotBeNull();
            scenario.Attacker.FindSkill(TestContent.BiteId)!.CooldownUntilTick.Should().Be(12);
        }

        [Test]
        public void Tick_UseBiteWhileBusy_IsRejected()
        {
            var scenario = new CombatScenario();
            scenario.Submit(new UseSkillCommand(scenario.Attacker.Id, TestContent.BiteId));
            scenario.Tick(1);

            scenario.Submit(new UseSkillCommand(scenario.Attacker.Id, TestContent.BiteId));
            scenario.Tick(1);

            scenario.Rejections.Should().ContainSingle();
            scenario.Rejections[0].Reason.Should().Be(UseSkillCommandHandler.Busy);
        }

        [Test]
        public void Tick_AfterRecovery_TheDemonCanBiteAgain()
        {
            var scenario = new CombatScenario();
            scenario.Submit(new UseSkillCommand(scenario.Attacker.Id, TestContent.BiteId));
            scenario.Tick(1);

            scenario.Tick(14);

            scenario.Attacker.CurrentSkillUse.Should().BeNull();
            scenario.Submit(new UseSkillCommand(scenario.Attacker.Id, TestContent.BiteId));
            scenario.Tick(1);
            scenario.Activations.Should().HaveCount(2);
        }

        [Test]
        public void Tick_UseBiteOnCooldown_IsRejected()
        {
            var scenario = new CombatScenario();
            scenario.Submit(new UseSkillCommand(scenario.Attacker.Id, TestContent.BiteId));
            scenario.Tick(1);
            scenario.Attacker.ClearSkillUse();

            scenario.Submit(new UseSkillCommand(scenario.Attacker.Id, TestContent.BiteId));
            scenario.Tick(1);

            scenario.Rejections.Should().ContainSingle();
            scenario.Rejections[0].Reason.Should().Be(UseSkillCommandHandler.Cooldown);
        }

        [Test]
        public void Tick_UseBiteWithoutStamina_IsRejected()
        {
            var scenario = new CombatScenario();
            scenario.Attacker.TrySpendStamina(95f);

            scenario.Submit(new UseSkillCommand(scenario.Attacker.Id, TestContent.BiteId));
            scenario.Tick(1);

            scenario.Rejections.Should().ContainSingle();
            scenario.Rejections[0].Reason.Should().Be(UseSkillCommandHandler.NoStamina);
            scenario.Activations.Should().BeEmpty();
        }

        [Test]
        public void Tick_UseUnknownSkill_IsRejected()
        {
            var scenario = new CombatScenario();

            scenario.Submit(new UseSkillCommand(scenario.Attacker.Id, "skill.nope"));
            scenario.Tick(1);

            scenario.Rejections.Should().ContainSingle();
            scenario.Rejections[0].Reason.Should().Be(UseSkillCommandHandler.UnknownSkill);
        }

        [Test]
        public void Tick_UseBiteWhileStaggered_IsRejected()
        {
            var scenario = new CombatScenario();
            scenario.Attacker.Stagger(scenario.State.Tick + 5);

            scenario.Submit(new UseSkillCommand(scenario.Attacker.Id, TestContent.BiteId));
            scenario.Tick(1);

            scenario.Rejections.Should().ContainSingle();
            scenario.Rejections[0].Reason.Should().Be(UseSkillCommandHandler.Staggered);
        }

        [Test]
        public void Tick_UseBiteWhenDead_IsRejected()
        {
            var scenario = new CombatScenario();
            scenario.Attacker.Body.Core.ApplyDamage(1000f);

            scenario.Submit(new UseSkillCommand(scenario.Attacker.Id, TestContent.BiteId));
            scenario.Tick(1);

            scenario.Rejections.Should().ContainSingle();
            scenario.Rejections[0].Reason.Should().Be(UseSkillCommandHandler.ActorDead);
        }

        [Test]
        public void Tick_AgileAttacker_StartsAndFinishesFaster()
        {
            DemonSpec agile = TestContent.Blob with { StartingStats = new[] { new StatValue(StatIds.Agility, 20) } };
            var scenario = new CombatScenario(agile);

            scenario.Submit(new UseSkillCommand(scenario.Attacker.Id, TestContent.BiteId));
            scenario.Tick(1);

            scenario.Activations[0].ActiveFromTick.Should().Be(2);
            scenario.Activations[0].EndTick.Should().BeLessThan(14);
        }

        [Test]
        public void Spawn_Blob_HasBiteFromItsCore()
        {
            var scenario = new CombatScenario();

            int skills = scenario.Attacker.Skills.Count;

            skills.Should().Be(1);
            scenario.Attacker.FindSkill(TestContent.BiteId).Should().NotBeNull();
            scenario.Attacker.FindSkill(TestContent.BiteId)!.GrantedByPartIndex.Should().Be(scenario.Attacker.Body.Core.Index);
        }
    }
}
