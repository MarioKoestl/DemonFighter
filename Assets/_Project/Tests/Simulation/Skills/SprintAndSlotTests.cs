#nullable enable
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Skills;
using DemonFighter.Simulation.Tests.Combat;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Skills
{
    public sealed class SprintAndSlotTests
    {
        private const float Tolerance = 0.01f;

        [Test]
        public void Sprint_WithoutLegs_IsWalkingSpeed()
        {
            var scenario = new CombatScenario();

            scenario.Submit(new MoveCommand(scenario.Attacker.Id, new Vector2(0f, 1f), sprint: true));
            scenario.Tick(1);

            scenario.Attacker.Intent.Sprint.Should().BeTrue();
            scenario.Attacker.CanSprint.Should().BeFalse();
            scenario.Attacker.IsSprinting.Should().BeFalse();
            scenario.Attacker.MaxSpeed.Should().BeApproximately(4f, Tolerance);
            scenario.Attacker.Stamina.Should().BeApproximately(100f, Tolerance);
        }

        [Test]
        public void Sprint_WithLegs_IsFasterAndDrainsStaminaAndEarnsSkillXp()
        {
            var scenario = new CombatScenario();
            scenario.Attacker.AttachPart(TestContent.Legs);

            for (int i = 0; i < 20; i++)
            {
                scenario.Submit(new MoveCommand(scenario.Attacker.Id, new Vector2(0f, 1f), sprint: true));
                scenario.Tick(1);
            }

            scenario.Attacker.IsSprinting.Should().BeTrue();
            scenario.Attacker.MaxSpeed.Should().BeApproximately(4f * 1.6f * 1.5f, Tolerance);
            scenario.Attacker.Stamina.Should().BeApproximately(100f - 15f + 12f, 0.5f);
            scenario.Attacker.FindSkill(TestContent.SprintId)!.Xp.Should().BeApproximately(2f, Tolerance);
            scenario.SkillXp.Should().HaveCount(2);
        }

        [Test]
        public void Sprint_WithoutStamina_FallsBackToWalking()
        {
            var scenario = new CombatScenario();
            scenario.Attacker.AttachPart(TestContent.Legs);
            scenario.Submit(new MoveCommand(scenario.Attacker.Id, new Vector2(0f, 1f), sprint: true));
            scenario.Tick(1);

            scenario.Attacker.TrySpendStamina(scenario.Attacker.Stamina - 0.5f);

            scenario.Attacker.IsSprinting.Should().BeFalse();
            scenario.Attacker.MaxSpeed.Should().BeApproximately(6f, Tolerance);
        }

        [Test]
        public void UseSkill_PassiveSprint_IsRejected()
        {
            var scenario = new CombatScenario();
            scenario.Attacker.AttachPart(TestContent.Legs);

            scenario.Submit(new UseSkillCommand(scenario.Attacker.Id, TestContent.SprintId));
            scenario.Tick(1);

            scenario.Rejections.Should().ContainSingle().Which.Reason.Should().Be(UseSkillCommandHandler.Passive);
        }

        [Test]
        public void Find_PrimarySlot_IsBiteUntilAnArmBringsClaw()
        {
            var scenario = new CombatScenario();
            Demon demon = scenario.Attacker;
            SkillInstance? before = SkillSlots.Find(demon, SkillSlot.Primary);

            BodyPart arm = demon.AttachPart(TestContent.Arm);
            SkillInstance? withArm = SkillSlots.Find(demon, SkillSlot.Primary);
            SkillInstance? secondary = SkillSlots.Find(demon, SkillSlot.Secondary);
            arm.ApplyDamage(1000f);
            SkillInstance? afterLoss = SkillSlots.Find(demon, SkillSlot.Primary);

            before!.Spec.Id.Should().Be(TestContent.BiteId);
            withArm!.Spec.Id.Should().Be(TestContent.ClawId);
            secondary!.Spec.Id.Should().Be(TestContent.GrabId);
            afterLoss!.Spec.Id.Should().Be(TestContent.BiteId);
            SkillSlots.Find(demon, SkillSlot.Secondary).Should().BeNull();
        }

        [Test]
        public void Registry_Validate_SkipsPassiveSkills()
        {
            var registry = new SkillBehaviourRegistry();

            System.Action act = () => registry.Validate(TestContent.Catalog());

            act.Should().NotThrow();
        }
    }
}
