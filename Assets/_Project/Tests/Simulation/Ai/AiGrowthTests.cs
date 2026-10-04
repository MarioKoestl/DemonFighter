#nullable enable
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using DemonFighter.Simulation.Ai;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Mutation;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Ai
{
    public sealed class AiGrowthTests
    {
        private const float Tolerance = 0.001f;
        private static readonly GroundBounds Arena = GroundBounds.CenteredSquare(100f);

        // A rester that wants Legs, then an Arm, grows toward the Stalker line and puts its points into Agility.
        private static readonly ArchetypeSpec Grower = TestArchetypes.Rester with
        {
            PreferredPartIds = new[] { TestContent.LegsId, TestContent.ArmId },
            PreferredEvolutionStat = StatIds.Agility,
            PreferredStat = StatIds.Agility,
        };

        [Test]
        public void Tick_IdleWithBiomass_BuysThePreferredPartsOneTransformationAtATime()
        {
            var run = new GrowthRun(Grower);
            run.Demon.GainBiomass(100f);

            run.Tick(2);
            bool transformingAfterFirst = run.Demon.IsTransforming(run.State.Tick);
            AiGoal goalAfterFirst = run.Brain.CurrentGoal;
            run.Tick(run.State.Config.TicksFor(TestContent.Tuning.TransformationSeconds) + 3);

            transformingAfterFirst.Should().BeTrue();
            goalAfterFirst.Should().Be(AiGoal.Mutate);
            run.Started.Should().HaveCount(2);
            run.Started[0].PartId.Should().Be(TestContent.LegsId);
            run.Started[1].PartId.Should().Be(TestContent.ArmId);
            run.Demon.Body.Parts.Select(p => p.Spec.Id).Should().Contain(TestContent.LegsId).And.Contain(TestContent.ArmId);
            run.Demon.Biomass.Should().BeApproximately(100f - TestContent.Legs.BiomassCost - TestContent.Arm.BiomassCost, Tolerance);
        }

        [Test]
        public void Tick_WhileInCombat_WaitsForTheCalmWindow()
        {
            var run = new GrowthRun(Grower);
            run.Demon.GainBiomass(100f);
            run.Ticker.Damage.ApplyDamage(run.Demon, run.Demon.Body.Core, 1f, DamageType.Pierce, run.Other.Id);

            run.Tick(3);
            int startedInCombat = run.Started.Count;
            run.Tick(run.State.Config.TicksFor(TestContent.Tuning.InCombatSeconds) + 2);

            startedInCombat.Should().Be(0);
            run.Started.Should().NotBeEmpty();
        }

        [Test]
        public void Tick_WithUnspentPoints_SpendsOnePerDecisionIntoThePreferredStat()
        {
            var run = new GrowthRun(Grower);
            run.Demon.Stats.GrantPoints(2);

            run.Tick(3);

            run.Demon.Stats.Get(StatIds.Agility).Should().Be(2);
            run.Demon.Stats.UnspentPoints.Should().Be(0);
        }

        [Test]
        public void Tick_WithAPendingEvolution_TakesTheLineOfItsPreferredStat()
        {
            var run = new GrowthRun(Grower);
            while (run.Demon.Level < 5)
            {
                run.Demon.GainXp(TestContent.Tuning.LevelXpForNext(run.Demon.Level), TestContent.Tuning);
            }

            run.Tick(2);

            run.Evolved.Should().ContainSingle().Which.EvolutionId.Should().Be(TestContent.Stalker1.Id);
            run.Demon.Evolutions.Should().Be(1);
            run.Brain.CurrentGoal.Should().Be(AiGoal.Evolve);
        }

        [Test]
        public void Tick_PreferredPartsOutOfReach_UpgradesTheCheapestOwnedPart()
        {
            ArchetypeSpec upgrader = TestArchetypes.Rester with { PreferredPartIds = new[] { TestContent.TailId } };
            var run = new GrowthRun(upgrader);
            run.Demon.AttachPart(TestContent.Arm);
            run.Demon.GainBiomass(100f);
            run.Demon.GainXp(TestContent.Tuning.LevelXpForNext(1), TestContent.Tuning);

            run.Tick(2);

            run.Started.Should().ContainSingle();
            run.Started[0].Kind.Should().Be(MutationKind.Upgrade);
            run.Demon.Body.Parts.Should().Contain(p => p.Spec.Id == TestContent.ArmId && p.UpgradeLevel == 1);
        }

        private sealed class GrowthRun
        {
            public GrowthRun(ArchetypeSpec archetype)
            {
                State = new RunStateBuilder().Build();
                Events = new SimulationEvents();
                Ticker = new SimulationTicker(State, Events);
                Demon = new DemonBuilder().At(0f, 0f).SpawnInto(State);
                Other = new DemonBuilder().At(0f, 40f).SpawnInto(State);
                Brain = Ticker.Ai.AddBrain(Demon, archetype, Arena, null);
                Events.Subscribe<MutationStarted>(Started.Add);
                Events.Subscribe<Evolved>(Evolved.Add);
            }

            public RunState State { get; }

            public SimulationEvents Events { get; }

            public SimulationTicker Ticker { get; }

            public Demon Demon { get; }

            public Demon Other { get; }

            public UtilityBrain Brain { get; }

            public List<MutationStarted> Started { get; } = new List<MutationStarted>();

            public List<Evolved> Evolved { get; } = new List<Evolved>();

            public void Tick(int times)
            {
                for (int i = 0; i < times; i++)
                {
                    Ticker.Tick();
                }
            }
        }
    }
}
