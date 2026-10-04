#nullable enable
using AwesomeAssertions;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Food;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Combat
{
    public sealed class DamageSystemTests
    {
        private const float Tolerance = 0.001f;

        private static readonly SkillSpec Claw = TestContent.Bite with { Id = "skill.claw.test", Name = "Claw", DamageType = DamageType.Cut };

        private static readonly SkillSpec Slam = TestContent.Bite with
        {
            Id = "skill.slam.test",
            Name = "Slam",
            DamageType = DamageType.Blunt,
            BleedSeconds = 0f,
            StaggerSeconds = 0.4f,
        };

        [Test]
        public void ApplyHit_BiteOnABareCore_DealsTheBaseDamage()
        {
            var scenario = new CombatScenario();

            scenario.HitCore(TestContent.Bite);

            scenario.Target.Body.Core.Hp.Should().BeApproximately(48f, Tolerance);
            scenario.Damage.Should().ContainSingle();
            scenario.Damage[0].Amount.Should().BeApproximately(12f, Tolerance);
            scenario.Damage[0].Attacker.Should().Be(scenario.Attacker.Id);
            scenario.Damage[0].DamageType.Should().Be(DamageType.Pierce);
        }

        [Test]
        public void ApplyHit_StrongAttacker_ScalesByTheDamageMultiplier()
        {
            var scenario = new CombatScenario(TestContent.Elder);

            scenario.HitCore(TestContent.Bite);

            scenario.Damage[0].Amount.Should().BeApproximately(18f, Tolerance);
        }

        [Test]
        public void ApplyHit_SkillLevelThree_AddsTenPercent()
        {
            var scenario = new CombatScenario();

            scenario.HitCore(TestContent.Bite, skillLevel: 3);

            scenario.Damage[0].Amount.Should().BeApproximately(13.2f, Tolerance);
        }

        [Test]
        public void ApplyHit_CutAgainstThickHide_IsWeakened()
        {
            var scenario = new CombatScenario();
            scenario.Target.Body.Attach(TestContent.ThickHide);

            scenario.HitCore(Claw);

            scenario.Damage[0].Amount.Should().BeApproximately(7.2f, Tolerance);
        }

        [Test]
        public void ApplyHit_BluntAgainstThickHide_IsStrengthenedAndStaggers()
        {
            var scenario = new CombatScenario();
            scenario.Target.Body.Attach(TestContent.ThickHide);

            scenario.HitCore(Slam);

            scenario.Damage[0].Amount.Should().BeApproximately(18f, Tolerance);
            scenario.Target.IsStaggered(scenario.State.Tick).Should().BeTrue();
            scenario.Target.IsStaggered(scenario.State.Tick + 8).Should().BeFalse();
        }

        [Test]
        public void ApplyHit_CrossingTheWoundedThreshold_ReportsPartWoundedOnce()
        {
            var scenario = new CombatScenario();

            scenario.HitCore(TestContent.Bite);
            scenario.HitCore(TestContent.Bite);
            scenario.HitCore(TestContent.Bite);
            scenario.HitCore(TestContent.Bite);

            scenario.Target.Body.Core.Condition.Should().Be(PartCondition.Wounded);
            scenario.Wounded.Should().ContainSingle();
        }

        [Test]
        public void ApplyHit_Bite_StartsBleedingOnThePart()
        {
            var scenario = new CombatScenario();

            scenario.HitCore(TestContent.Bite);

            scenario.Target.Body.Core.IsBleeding.Should().BeTrue();
            scenario.Target.Body.Core.BleedSecondsLeft.Should().BeApproximately(3f, Tolerance);
            scenario.Target.Body.Core.BleedDamagePerSecond.Should().BeApproximately(2f, Tolerance);
        }

        [Test]
        public void ApplyHit_ArmToZero_SeversItIntoFood()
        {
            var scenario = new CombatScenario();
            BodyPart arm = scenario.Target.Body.Attach(TestContent.Arm);

            scenario.DamageSystem.ApplyHit(scenario.Attacker, scenario.Target, arm, TestContent.Bite, 1);
            scenario.DamageSystem.ApplyHit(scenario.Attacker, scenario.Target, arm, TestContent.Bite, 1);
            scenario.Events.Flush();

            arm.IsLost.Should().BeTrue();
            scenario.Severed.Should().ContainSingle();
            scenario.Severed[0].PartIndex.Should().Be(arm.Index);
            scenario.State.Food.Should().ContainSingle();
            scenario.State.Food[0].Kind.Should().Be(FoodKind.SeveredPart);
            scenario.State.Food[0].BiomassRemaining.Should().BeApproximately(5f, Tolerance);
            scenario.State.Food[0].Id.Should().Be(scenario.Severed[0].Food);
            scenario.FoodSpawns.Should().ContainSingle();
            scenario.Target.IsAlive.Should().BeTrue();
        }

        [Test]
        public void ApplyHit_HideToZero_DestroysItWithoutFood()
        {
            var scenario = new CombatScenario();
            BodyPart hide = scenario.Target.Body.Attach(TestContent.ThickHide);

            for (int i = 0; i < 5; i++)
            {
                scenario.DamageSystem.ApplyHit(scenario.Attacker, scenario.Target, hide, TestContent.Bite, 1);
            }

            scenario.Events.Flush();

            hide.IsLost.Should().BeTrue();
            scenario.Destroyed.Should().ContainSingle();
            scenario.State.Food.Should().BeEmpty();
        }

        [Test]
        public void ApplyHit_CoreToZero_KillsAndLeavesACorpse()
        {
            var scenario = new CombatScenario();

            for (int i = 0; i < 5; i++)
            {
                scenario.HitCore(TestContent.Bite);
            }

            scenario.Target.IsAlive.Should().BeFalse();
            scenario.Deaths.Should().ContainSingle();
            scenario.Deaths[0].Killer.Should().Be(scenario.Attacker.Id);
            scenario.Attacker.Kills.Should().Be(1);
            scenario.State.Food.Should().ContainSingle();
            scenario.State.Food[0].Kind.Should().Be(FoodKind.Corpse);
            scenario.State.Food[0].BiomassRemaining.Should().BeApproximately(20f, Tolerance);
            scenario.State.Food[0].Id.Should().Be(scenario.Deaths[0].Corpse);
            scenario.Target.Intent.IsMoving.Should().BeFalse();
        }

        [Test]
        public void ApplyHit_OnADeadTarget_ChangesNothing()
        {
            var scenario = new CombatScenario();
            for (int i = 0; i < 5; i++)
            {
                scenario.HitCore(TestContent.Bite);
            }

            int damageEvents = scenario.Damage.Count;
            scenario.HitCore(TestContent.Bite);

            scenario.Damage.Count.Should().Be(damageEvents);
            scenario.Deaths.Should().ContainSingle();
        }

        [Test]
        public void ApplyDamage_MarksBothSidesInCombatForTheWindow()
        {
            var scenario = new CombatScenario();
            int window = scenario.State.Config.TicksFor(TestContent.Tuning.InCombatSeconds);

            scenario.HitCore(TestContent.Bite);

            scenario.Target.IsInCombat(scenario.State.Tick, window).Should().BeTrue();
            scenario.Attacker.IsInCombat(scenario.State.Tick, window).Should().BeTrue();
            scenario.Attacker.IsInCombat(scenario.State.Tick + window, window).Should().BeFalse();
        }

        [Test]
        public void ApplyDamage_WithoutAttacker_ReportsNone()
        {
            var scenario = new CombatScenario();

            scenario.DamageSystem.ApplyDamage(scenario.Target, scenario.Target.Body.Core, 5f, DamageType.Cut, DemonId.None);
            scenario.Events.Flush();

            scenario.Damage[0].Attacker.Should().Be(DemonId.None);
            scenario.Target.LastCombatTick.Should().Be(scenario.State.Tick);
        }
    }
}
