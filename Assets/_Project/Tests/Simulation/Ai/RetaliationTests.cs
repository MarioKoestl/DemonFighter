#nullable enable
using System.Collections.Generic;
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Ai;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Ai
{
    public sealed class RetaliationTests
    {
        private static readonly GroundBounds Arena = GroundBounds.CenteredSquare(200f);

        [Test]
        public void Decide_VillagerBitten_HuntsTheAttackerAtOnce()
        {
            RunState state = new RunStateBuilder().Build();
            var ticker = new SimulationTicker(state, new SimulationEvents());
            Demon villager = new DemonBuilder().At(0f, 0f).SpawnInto(state);
            Demon attacker = new DemonBuilder().At(0f, 6f).SpawnInto(state);
            UtilityBrain brain = ticker.Ai.AddBrain(villager, TestArchetypes.Villager, Arena, null);
            ticker.Tick();
            AiGoal before = brain.CurrentGoal;

            ticker.Damage.ApplyDamage(villager, villager.Body.Core, 5f, DamageType.Pierce, attacker.Id);
            ticker.Tick();

            before.Should().NotBe(AiGoal.Hunt);
            brain.CurrentGoal.Should().Be(AiGoal.Hunt);
            brain.Prey.Should().BeSameAs(attacker);
            villager.Intent.IsMoving.Should().BeTrue();
        }

        [Test]
        public void Decide_PatrollingElderBittenByABlob_TurnsOnItDespiteTheRewardRule()
        {
            RunState state = new RunStateBuilder().Build();
            var ticker = new SimulationTicker(state, new SimulationEvents());
            Demon elder = new DemonBuilder().WithSpec(TestContent.Elder).At(0f, 0f).SpawnInto(state);
            Demon blob = new DemonBuilder().AsPlayer().At(0f, 10f).SpawnInto(state);
            var route = new List<Vector3> { new Vector3(0f, 0f, 60f), new Vector3(60f, 0f, 60f) };
            UtilityBrain brain = ticker.Ai.AddBrain(elder, TestArchetypes.Sentinel, Arena, route);
            ticker.Tick();
            AiGoal before = brain.CurrentGoal;

            ticker.Damage.ApplyDamage(elder, elder.Body.Core, 5f, DamageType.Pierce, blob.Id);
            ticker.Tick();

            before.Should().Be(AiGoal.Patrol);
            brain.CurrentGoal.Should().Be(AiGoal.Hunt);
            brain.Prey.Should().BeSameAs(blob);
        }

        [Test]
        public void Decide_PacifistBitten_DoesNotRetaliate()
        {
            RunState state = new RunStateBuilder().Build();
            var ticker = new SimulationTicker(state, new SimulationEvents());
            Demon wanderer = new DemonBuilder().At(0f, 0f).SpawnInto(state);
            Demon attacker = new DemonBuilder().At(0f, 6f).SpawnInto(state);
            UtilityBrain brain = ticker.Ai.AddBrain(wanderer, TestArchetypes.Wanderer, Arena, null);
            ticker.Tick();

            ticker.Damage.ApplyDamage(wanderer, wanderer.Body.Core, 5f, DamageType.Pierce, attacker.Id);
            ticker.Tick();

            brain.CurrentGoal.Should().Be(AiGoal.Wander);
            brain.Prey.Should().BeNull();
        }

        [Test]
        public void Decide_BittenWhileWeakNearTheFight_StillFlees()
        {
            RunState state = new RunStateBuilder().Build();
            var ticker = new SimulationTicker(state, new SimulationEvents());
            Demon coward = new DemonBuilder().At(0f, 0f).SpawnInto(state);
            Demon attacker = new DemonBuilder().At(0f, 3f).SpawnInto(state);
            UtilityBrain brain = ticker.Ai.AddBrain(coward, TestArchetypes.Coward, Arena, null);
            ticker.Tick();

            ticker.Damage.ApplyDamage(coward, coward.Body.Core, 45f, DamageType.Pierce, attacker.Id);
            ticker.Tick();

            brain.CurrentGoal.Should().Be(AiGoal.Flee);
        }
    }
}
