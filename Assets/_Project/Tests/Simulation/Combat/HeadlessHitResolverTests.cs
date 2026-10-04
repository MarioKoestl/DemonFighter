#nullable enable
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Ai;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Tests.Ai;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Combat
{
    public sealed class HeadlessHitResolverTests
    {
        private static readonly GroundBounds Arena = GroundBounds.CenteredSquare(100f);

        [Test]
        public void Tick_WithHeadlessHits_ABiteInReachLandsWithoutAPresenter()
        {
            var scenario = new CombatScenario();
            scenario.Ticker.EnableHeadlessHits();

            scenario.Submit(new UseSkillCommand(scenario.Attacker.Id, TestContent.BiteId));
            scenario.Tick(12);

            scenario.DamageFrom(scenario.Attacker.Id).Should().NotBeEmpty();
            scenario.Target.Body.TotalHp.Should().BeLessThan(scenario.Target.Body.TotalMaxHp);
        }

        [Test]
        public void Tick_WithoutHeadlessHits_NothingLands()
        {
            var scenario = new CombatScenario();

            scenario.Submit(new UseSkillCommand(scenario.Attacker.Id, TestContent.BiteId));
            scenario.Tick(12);

            scenario.DamageFrom(scenario.Attacker.Id).Should().BeEmpty();
        }

        [Test]
        public void Tick_TargetBehindTheAttacker_IsNotReported()
        {
            var scenario = new CombatScenario();
            scenario.Ticker.EnableHeadlessHits();
            scenario.Target.SetPose(new Vector3(0f, 0f, -1.5f), 0f);

            scenario.Submit(new UseSkillCommand(scenario.Attacker.Id, TestContent.BiteId));
            scenario.Tick(12);

            scenario.DamageFrom(scenario.Attacker.Id).Should().BeEmpty();
            scenario.Rejections.Should().BeEmpty();
        }

        [Test]
        public void Tick_HunterAgainstARester_KillsItHeadless()
        {
            RunState state = new RunStateBuilder().Build();
            var events = new SimulationEvents();
            var ticker = new SimulationTicker(state, events);
            ticker.EnableHeadlessHits();
            Demon hunter = new DemonBuilder().At(0f, 0f).SpawnInto(state);
            Demon rester = new DemonBuilder().At(0f, 6f).SpawnInto(state);
            ticker.Ai.AddBrain(hunter, TestArchetypes.Hunter, Arena, null);
            ticker.Ai.AddBrain(rester, TestArchetypes.Rester, Arena, null);
            int died = 0;
            events.Subscribe<DemonDied>(evt => died += evt.Demon == rester.Id ? 1 : 0);

            for (int i = 0; i < 20 * 60 && rester.IsAlive; i++)
            {
                ticker.Tick();
            }

            rester.IsAlive.Should().BeFalse();
            died.Should().Be(1);
            hunter.Kills.Should().Be(1);
            state.Food.Should().NotBeEmpty();
        }
    }
}
