#nullable enable
using System;
using AwesomeAssertions;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Progression;
using DemonFighter.Simulation.Tests.Combat;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Progression
{
    public sealed class RunSummaryTests
    {
        private const float Tolerance = 0.001f;

        [Test]
        public void From_ReadsTheRunAndNamesTheKiller()
        {
            var scenario = new CombatScenario();
            scenario.Target.AttachPart(TestContent.Arm);
            scenario.Ticker.Damage.ApplyDamage(scenario.Target, scenario.Target.Body.Core, 100000f, DamageType.Pierce, scenario.Attacker.Id);
            scenario.Tick(10);

            RunSummary summary = RunSummary.From(scenario.State, scenario.Target, scenario.Attacker.Id);

            summary.Seed.Should().Be(scenario.State.Seed);
            summary.TicksSurvived.Should().Be(scenario.State.Tick);
            summary.SecondsSurvived.Should().BeApproximately(scenario.State.Time, Tolerance);
            summary.KillerSpecId.Should().Be(scenario.Attacker.Spec.Id);
            summary.KillerName.Should().Be(scenario.Attacker.Spec.Name);
            summary.Level.Should().Be(1);
            summary.HighestTier.Should().Be(scenario.Target.Tier);
            summary.FinalTier.Should().Be(scenario.Target.Tier);
            summary.PartIds.Should().Equal(TestContent.CoreId, TestContent.ArmId);
            summary.EvolutionIds.Should().BeEmpty();
        }

        [Test]
        public void From_WithoutAKiller_LeavesTheKillerEmpty()
        {
            var scenario = new CombatScenario();

            RunSummary summary = RunSummary.From(scenario.State, scenario.Attacker, DemonId.None);

            summary.KillerSpecId.Should().BeEmpty();
            summary.KillerName.Should().BeEmpty();
        }

        [Test]
        public void NoMetaProgression_RecordRun_AcceptsEveryRunAndKeepsNothing()
        {
            var scenario = new CombatScenario();
            RunSummary summary = RunSummary.From(scenario.State, scenario.Attacker, DemonId.None);
            var meta = new NoMetaProgression();

            Action act = () => meta.RecordRun(summary);

            act.Should().NotThrow();
        }
    }
}
