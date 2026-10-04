#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Ai;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Food;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Ai
{
    public sealed class UtilityBrainCombatTests
    {
        private static readonly GroundBounds Arena = GroundBounds.CenteredSquare(200f);

        [Test]
        public void Decide_HunterWithPreyInReach_FacesItAndBites()
        {
            RunState state = new RunStateBuilder().Build();
            var events = new SimulationEvents();
            var ticker = new SimulationTicker(state, events);
            var activations = new List<SkillActivated>();
            events.Subscribe<SkillActivated>(activations.Add);
            Demon hunter = new DemonBuilder().At(0f, 0f).FacingYaw(MathF.PI).SpawnInto(state);
            Demon prey = new DemonBuilder().AsPlayer().At(0f, 1.5f).SpawnInto(state);
            UtilityBrain brain = ticker.Ai.AddBrain(hunter, TestArchetypes.Hunter, Arena, null);

            ticker.Tick();
            ticker.Tick();

            brain.CurrentGoal.Should().Be(AiGoal.Hunt);
            brain.Prey.Should().BeSameAs(prey);
            hunter.Yaw.Should().BeApproximately(0f, 0.001f);
            hunter.Intent.IsMoving.Should().BeFalse();
            activations.Should().ContainSingle().Which.Actor.Should().Be(hunter.Id);
        }

        [Test]
        public void Decide_HunterWithPreyFarAway_WalksTowardIt()
        {
            RunState state = new RunStateBuilder().Build();
            var ticker = new SimulationTicker(state, new SimulationEvents());
            Demon hunter = new DemonBuilder().At(0f, 0f).SpawnInto(state);
            Demon prey = new DemonBuilder().AsPlayer().At(0f, 20f).SpawnInto(state);
            UtilityBrain brain = ticker.Ai.AddBrain(hunter, TestArchetypes.Hunter, Arena, null);

            for (int i = 0; i < 20; i++)
            {
                ticker.Tick();
            }

            brain.CurrentGoal.Should().Be(AiGoal.Hunt);
            hunter.Intent.IsMoving.Should().BeTrue();
            Vector3.Distance(hunter.Position, prey.Position).Should().BeLessThan(17f);
        }

        [Test]
        public void Decide_HunterIgnoresPreyTwoTiersAbove()
        {
            RunState state = new RunStateBuilder().Build();
            var ticker = new SimulationTicker(state, new SimulationEvents());
            Demon hunter = new DemonBuilder().At(0f, 0f).SpawnInto(state);
            new DemonBuilder().WithSpec(TestContent.Elder).At(0f, 5f).SpawnInto(state);
            UtilityBrain brain = ticker.Ai.AddBrain(hunter, TestArchetypes.Hunter, Arena, null);

            ticker.Tick();

            brain.CurrentGoal.Should().Be(AiGoal.Rest);
            brain.Prey.Should().BeNull();
        }

        [Test]
        public void Decide_ElderIgnoresBlobsUntilOneAttacksIt()
        {
            RunState state = new RunStateBuilder().Build();
            var ticker = new SimulationTicker(state, new SimulationEvents());
            Demon elder = new DemonBuilder().WithSpec(TestContent.Elder).At(0f, 0f).SpawnInto(state);
            Demon blob = new DemonBuilder().AsPlayer().At(0f, 10f).SpawnInto(state);
            UtilityBrain brain = ticker.Ai.AddBrain(elder, TestArchetypes.Hunter, Arena, null);
            ticker.Tick();
            AiGoal beforeAttack = brain.CurrentGoal;

            ticker.Damage.ApplyDamage(elder, elder.Body.Core, 1f, DamageType.Pierce, blob.Id);
            ticker.Tick();

            beforeAttack.Should().Be(AiGoal.Rest);
            brain.CurrentGoal.Should().Be(AiGoal.Hunt);
            brain.Prey.Should().BeSameAs(blob);
        }

        [Test]
        public void Decide_Hunter_PrefersTheWoundedPrey()
        {
            RunState state = new RunStateBuilder().Build();
            var ticker = new SimulationTicker(state, new SimulationEvents());
            Demon hunter = new DemonBuilder().At(0f, 0f).SpawnInto(state);
            new DemonBuilder().At(5f, 0f).SpawnInto(state);
            Demon wounded = new DemonBuilder().At(-5f, 0f).SpawnInto(state);
            wounded.Body.Core.ApplyDamage(40f);
            UtilityBrain brain = ticker.Ai.AddBrain(hunter, TestArchetypes.Hunter, Arena, null);

            ticker.Tick();

            brain.Prey.Should().BeSameAs(wounded);
        }

        [Test]
        public void Decide_EaterWithFoodInView_WalksToItAndKeepsEatingBetweenDecisions()
        {
            RunState state = new RunStateBuilder().Build();
            var events = new SimulationEvents();
            var ticker = new SimulationTicker(state, events);
            var consumed = new List<FoodConsumed>();
            events.Subscribe<FoodConsumed>(consumed.Add);
            Demon eater = new DemonBuilder().At(0f, 0f).SpawnInto(state);
            state.SpawnFood(FoodKind.Corpse, new Vector3(0f, 0f, 10f), 200f, new DemonId(42), 0);
            UtilityBrain brain = ticker.Ai.AddBrain(eater, TestArchetypes.EaterEvery(5), Arena, null);

            for (int i = 0; i < 80; i++)
            {
                ticker.Tick();
            }

            brain.CurrentGoal.Should().Be(AiGoal.Eat);
            eater.IsEating.Should().BeTrue();
            eater.Biomass.Should().BeGreaterThan(0f);
            int before = consumed.Count;
            for (int i = 0; i < 10; i++)
            {
                ticker.Tick();
            }

            (consumed.Count - before).Should().Be(10);
        }

        [Test]
        public void Decide_CowardAtLowHealthNearAFight_SprintsAway()
        {
            RunState state = new RunStateBuilder().Build();
            var ticker = new SimulationTicker(state, new SimulationEvents());
            Demon coward = new DemonBuilder().At(0f, 0f).SpawnInto(state);
            Demon attacker = new DemonBuilder().At(0f, 3f).SpawnInto(state);
            UtilityBrain brain = ticker.Ai.AddBrain(coward, TestArchetypes.Coward, Arena, null);
            ticker.Tick();

            ticker.Damage.ApplyDamage(coward, coward.Body.Core, 45f, DamageType.Pierce, attacker.Id);
            for (int i = 0; i < 5; i++)
            {
                ticker.Tick();
            }

            brain.CurrentGoal.Should().Be(AiGoal.Flee);
            brain.Threat.Should().BeSameAs(attacker);
            coward.Intent.Sprint.Should().BeTrue();
            Vector3.Distance(coward.Position, attacker.Position).Should().BeGreaterThan(3.5f);
        }

        [Test]
        public void Decide_CowardAtLowHealthWithNoFightNearby_DoesNotFlee()
        {
            RunState state = new RunStateBuilder().Build();
            var ticker = new SimulationTicker(state, new SimulationEvents());
            Demon coward = new DemonBuilder().At(0f, 0f).SpawnInto(state);
            UtilityBrain brain = ticker.Ai.AddBrain(coward, TestArchetypes.Coward, Arena, null);

            ticker.Damage.ApplyDamage(coward, coward.Body.Core, 45f, DamageType.Pierce, DemonId.None);
            ticker.Tick();

            brain.CurrentGoal.Should().Be(AiGoal.Wander);
            brain.Threat.Should().BeNull();
        }
    }
}
