#nullable enable
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Food;
using DemonFighter.Simulation.Tests.Combat;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Commands
{
    public sealed class EatCommandTests
    {
        private const float Tolerance = 0.001f;
        private const float BiomassPerTick = 15f * 0.05f;

        [Test]
        public void Tick_HoldEatOnNearbyCorpse_BiomassFlowsEveryTick()
        {
            var scenario = new CombatScenario();
            FoodItem food = SpawnCorpseNear(scenario);

            HoldEat(scenario, food.Id, 2);

            scenario.Attacker.Biomass.Should().BeApproximately(2f * BiomassPerTick, Tolerance);
            food.BiomassRemaining.Should().BeApproximately(20f - 2f * BiomassPerTick, Tolerance);
            scenario.Consumed.Should().HaveCount(2);
            scenario.Attacker.IsEating.Should().BeTrue();
            scenario.Rejections.Should().BeEmpty();
        }

        [Test]
        public void Tick_EatUntilDepleted_RemovesTheFoodAndEndsTheMeal()
        {
            var scenario = new CombatScenario();
            FoodItem food = SpawnCorpseNear(scenario, biomass: 2f);

            HoldEat(scenario, food.Id, 3);

            scenario.Attacker.Biomass.Should().BeApproximately(2f, Tolerance);
            scenario.Attacker.BiomassEaten.Should().BeApproximately(2f, Tolerance);
            scenario.State.Food.Should().BeEmpty();
            scenario.Removed.Should().ContainSingle().Which.Reason.Should().Be(FoodRemovalReason.Eaten);
            scenario.Attacker.IsEating.Should().BeFalse();
        }

        [Test]
        public void Tick_ReleaseEat_EndsTheMealNextTick()
        {
            var scenario = new CombatScenario();
            FoodItem food = SpawnCorpseNear(scenario);
            HoldEat(scenario, food.Id, 1);

            scenario.Tick(1);

            scenario.Attacker.IsEating.Should().BeFalse();
            scenario.Consumed.Should().ContainSingle();
        }

        [Test]
        public void Tick_EatHigherTierFood_GainsTheTierBonus()
        {
            var scenario = new CombatScenario();
            FoodItem food = SpawnCorpseNear(scenario, sourceTier: 2);

            HoldEat(scenario, food.Id, 1);

            scenario.Attacker.Biomass.Should().BeApproximately(2f * BiomassPerTick, Tolerance);
            food.BiomassRemaining.Should().BeApproximately(20f - BiomassPerTick, Tolerance);
        }

        [Test]
        public void Tick_ElderEatsBlobFood_GainsVeryLittle()
        {
            var scenario = new CombatScenario(TestContent.Elder);
            FoodItem food = SpawnCorpseNear(scenario);

            HoldEat(scenario, food.Id, 1);

            scenario.Attacker.Biomass.Should().BeApproximately(0.1f * BiomassPerTick, Tolerance);
        }

        [Test]
        public void Tick_EatOutOfReach_IsRejected()
        {
            var scenario = new CombatScenario();
            FoodItem food = scenario.State.SpawnFood(FoodKind.Corpse, new Vector3(0f, 0f, 5f), 20f, scenario.Target.Id, 0);

            HoldEat(scenario, food.Id, 1);

            scenario.Rejections.Should().ContainSingle().Which.Reason.Should().Be(EatCommandHandler.OutOfReach);
            scenario.Attacker.IsEating.Should().BeFalse();
            scenario.Consumed.Should().BeEmpty();
        }

        [Test]
        public void Tick_EatUnknownFood_IsRejected()
        {
            var scenario = new CombatScenario();

            HoldEat(scenario, new FoodId(99), 1);

            scenario.Rejections.Should().ContainSingle().Which.Reason.Should().Be(EatCommandHandler.UnknownFood);
        }

        [Test]
        public void Tick_EatWhileStaggered_IsRejected()
        {
            var scenario = new CombatScenario();
            FoodItem food = SpawnCorpseNear(scenario);
            scenario.Attacker.Stagger(scenario.State.Tick + 5);

            HoldEat(scenario, food.Id, 1);

            scenario.Rejections.Should().ContainSingle().Which.Reason.Should().Be(EatCommandHandler.Staggered);
        }

        [Test]
        public void Tick_EatWhileUsingASkill_IsRejected()
        {
            var scenario = new CombatScenario();
            FoodItem food = SpawnCorpseNear(scenario);
            scenario.Submit(new UseSkillCommand(scenario.Attacker.Id, TestContent.BiteId));
            scenario.Tick(1);

            HoldEat(scenario, food.Id, 1);

            scenario.Rejections.Should().ContainSingle().Which.Reason.Should().Be(EatCommandHandler.Busy);
        }

        [Test]
        public void Tick_StartingASkillWhileEating_EndsTheMeal()
        {
            var scenario = new CombatScenario();
            FoodItem food = SpawnCorpseNear(scenario);
            HoldEat(scenario, food.Id, 1);

            scenario.Submit(new EatCommand(scenario.Attacker.Id, food.Id));
            scenario.Submit(new UseSkillCommand(scenario.Attacker.Id, TestContent.BiteId));
            scenario.Tick(1);

            scenario.Attacker.IsEating.Should().BeFalse();
            scenario.Consumed.Should().ContainSingle();
        }

        [Test]
        public void ApplyDamage_FromAnAttacker_InterruptsTheMeal()
        {
            var scenario = new CombatScenario();
            FoodItem food = SpawnCorpseNear(scenario);
            HoldEat(scenario, food.Id, 1);

            scenario.DamageSystem.ApplyDamage(scenario.Attacker, scenario.Attacker.Body.Core, 5f, DamageType.Pierce, scenario.Target.Id);

            scenario.Attacker.IsEating.Should().BeFalse();
        }

        [Test]
        public void Tick_BleedingWhileEating_DoesNotInterruptTheMeal()
        {
            var scenario = new CombatScenario();
            FoodItem food = SpawnCorpseNear(scenario);
            scenario.Attacker.Body.Core.StartBleeding(3f, 2f, DamageType.Pierce);

            HoldEat(scenario, food.Id, 2);

            scenario.Attacker.IsEating.Should().BeTrue();
            scenario.Consumed.Should().HaveCount(2);
        }

        [Test]
        public void ApplyDamage_KillsTheEater_EndsTheMeal()
        {
            var scenario = new CombatScenario();
            FoodItem food = SpawnCorpseNear(scenario);
            HoldEat(scenario, food.Id, 1);

            scenario.DamageSystem.ApplyDamage(scenario.Attacker, scenario.Attacker.Body.Core, 1000f, DamageType.Pierce, scenario.Target.Id);

            scenario.Attacker.IsEating.Should().BeFalse();
            scenario.Attacker.IsAlive.Should().BeFalse();
        }

        [Test]
        public void Tick_DeadEater_IsRejected()
        {
            var scenario = new CombatScenario();
            FoodItem food = SpawnCorpseNear(scenario);
            scenario.Attacker.Body.Core.ApplyDamage(1000f);

            HoldEat(scenario, food.Id, 1);

            scenario.Rejections.Should().ContainSingle().Which.Reason.Should().Be(EatCommandHandler.ActorDead);
        }

        private static FoodItem SpawnCorpseNear(CombatScenario scenario, float biomass = 20f, int sourceTier = 0)
        {
            return scenario.State.SpawnFood(FoodKind.Corpse, new Vector3(0f, 0f, 1f), biomass, scenario.Target.Id, sourceTier);
        }

        private static void HoldEat(CombatScenario scenario, FoodId food, int ticks)
        {
            for (int i = 0; i < ticks; i++)
            {
                scenario.Submit(new EatCommand(scenario.Attacker.Id, food));
                scenario.Tick(1);
            }
        }
    }
}
