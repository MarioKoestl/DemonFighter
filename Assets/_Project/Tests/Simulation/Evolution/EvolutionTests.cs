#nullable enable
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Evolution;
using DemonFighter.Simulation.Mutation;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Evolution
{
    public sealed class EvolutionTests
    {
        private const float Tolerance = 0.001f;

        [Test]
        public void PendingStage_FollowsTheThresholdsAndTheEvolutionsTaken()
        {
            var run = new EvolutionRun();
            int atStart = EvolutionRules.PendingStage(run.Player, TestContent.Tuning);

            run.ReachLevel(5);
            int atFive = EvolutionRules.PendingStage(run.Player, TestContent.Tuning);
            run.Player.RecordEvolution();
            int afterFirst = EvolutionRules.PendingStage(run.Player, TestContent.Tuning);
            run.ReachLevel(10);
            int atTen = EvolutionRules.PendingStage(run.Player, TestContent.Tuning);
            run.Player.RecordEvolution();

            atStart.Should().Be(0);
            atFive.Should().Be(1);
            afterFirst.Should().Be(0);
            atTen.Should().Be(2);
            EvolutionRules.PendingStage(run.Player, TestContent.Tuning).Should().Be(0);
            EvolutionRules.NextThreshold(run.Player, TestContent.Tuning).Should().Be(0);
        }

        [Test]
        public void Options_AtStageOne_AreTheThreeLinesBestFitFirst()
        {
            var run = new EvolutionRun();
            run.ReachLevel(5);
            run.Player.Stats.GrantPoints(2);
            run.Player.Stats.TrySpendPoint(StatIds.Agility);
            run.Player.Stats.TrySpendPoint(StatIds.Agility);

            IReadOnlyList<EvolutionSpec> options = EvolutionRules.Options(run.State, run.Player);

            options.Should().HaveCount(3);
            options.Should().OnlyContain(o => o.Stage == 1);
            options[0].Id.Should().Be(TestContent.Stalker1.Id);
        }

        [Test]
        public void Options_WithNothingPending_AreEmpty()
        {
            var run = new EvolutionRun();

            EvolutionRules.Options(run.State, run.Player).Should().BeEmpty();
        }

        [Test]
        public void Tick_EvolveIntoBrute_GrantsThePackageGrowsAndTransforms()
        {
            var run = new EvolutionRun();
            run.ReachLevel(5);
            int pointsBefore = run.Player.Stats.UnspentPoints;

            run.Submit(new EvolveCommand(run.Player.Id, TestContent.Brute1.Id));
            run.Tick(1);

            run.Rejections.Should().BeEmpty();
            run.Evolved.Should().ContainSingle();
            run.Evolved[0].Stage.Should().Be(1);
            run.Player.Evolutions.Should().Be(1);
            run.Player.Tier.Should().Be(1);
            run.Player.SizeMeters.Should().BeApproximately(1.8f, Tolerance);
            run.Player.Stats.UnspentPoints.Should().Be(pointsBefore + TestContent.Brute1.StatPoints);
            run.Player.StatCap(StatIds.Strength).Should().Be(TestContent.Tuning.BaseStatCap + 5 + 10);
            run.Player.StatCap(StatIds.Agility).Should().Be(TestContent.Tuning.BaseStatCap);
            run.Player.IsUnlocked(TestContent.Tail).Should().BeTrue();
            run.Player.Body.Parts.Select(p => p.Spec.Id).Should().Contain(TestContent.HideId);
            run.Player.IsTransforming(run.State.Tick).Should().BeTrue();
            EvolutionRules.PendingStage(run.Player, TestContent.Tuning).Should().Be(0);
        }

        [Test]
        public void Tick_EvolveWithNothingPending_IsRejected()
        {
            var run = new EvolutionRun();

            run.Submit(new EvolveCommand(run.Player.Id, TestContent.Brute1.Id));
            run.Tick(1);

            run.Rejections.Should().ContainSingle().Which.Reason.Should().Be(EvolutionRules.NothingPending);
        }

        [Test]
        public void Tick_EvolveIntoAStageTwoLineAtStageOne_IsRejected()
        {
            var run = new EvolutionRun();
            run.ReachLevel(5);

            run.Submit(new EvolveCommand(run.Player.Id, TestContent.Brute2.Id));
            run.Tick(1);

            run.Rejections.Should().ContainSingle().Which.Reason.Should().Be(EvolutionRules.NothingPending);
        }

        [Test]
        public void Tick_EvolveInCombat_IsAllowed()
        {
            var run = new EvolutionRun();
            run.ReachLevel(5);
            run.Ticker.Damage.ApplyDamage(run.Player, run.Player.Body.Core, 1f, DamageType.Pierce, run.Other.Id);

            run.Submit(new EvolveCommand(run.Player.Id, TestContent.Brute1.Id));
            run.Tick(1);

            run.Rejections.Should().BeEmpty();
            run.Evolved.Should().ContainSingle();
        }

        [Test]
        public void Tick_UnknownEvolution_IsRejected()
        {
            var run = new EvolutionRun();
            run.ReachLevel(5);

            run.Submit(new EvolveCommand(run.Player.Id, "evolution.nope"));
            run.Tick(1);

            run.Rejections.Should().ContainSingle().Which.Reason.Should().Be(EvolveCommandHandler.UnknownEvolution);
        }

        [Test]
        public void Tick_SecondEvolutionWithAnExtraSkill_GrantsAUsableSkill()
        {
            var run = new EvolutionRun();
            run.ReachLevel(5);
            run.Submit(new EvolveCommand(run.Player.Id, TestContent.Brute1.Id));
            run.Tick(1);
            run.Tick(run.State.Config.TicksFor(TestContent.Tuning.TransformationSeconds));
            run.ReachLevel(10);

            run.Submit(new EvolveCommand(run.Player.Id, TestContent.Brute2.Id));
            run.Tick(1);

            run.Rejections.Should().BeEmpty();
            run.Player.Evolutions.Should().Be(2);
            run.Player.Tier.Should().Be(2);
            run.Player.FindSkill(TestContent.RoarId).Should().NotBeNull();
            run.Player.FindSkill(TestContent.RoarId)!.IsGrantedBy(run.Player.Body).Should().BeTrue();
        }

        [Test]
        public void Tick_SpendStatPointAtTheCap_IsRejectedUntilAnEvolutionRaisesIt()
        {
            var run = new EvolutionRun();
            run.Player.Stats.GrantPoints(TestContent.Tuning.BaseStatCap + 1);
            for (int i = 0; i < TestContent.Tuning.BaseStatCap; i++)
            {
                run.Player.Stats.TrySpendPoint(StatIds.Strength);
            }

            run.Submit(new SpendStatPointCommand(run.Player.Id, StatIds.Strength));
            run.Tick(1);

            run.Rejections.Should().ContainSingle().Which.Reason.Should().Be(SpendStatPointCommandHandler.AtCap);
            run.Player.Stats.Get(StatIds.Strength).Should().Be(TestContent.Tuning.BaseStatCap);
        }

        [Test]
        public void Tick_EvolveBrute_GivesTheBoundStrengthAndRaisesItsCap()
        {
            var run = new EvolutionRun();
            run.ReachLevel(5);
            int strengthBefore = run.Player.Stats.Get(StatIds.Strength);
            int capBefore = run.Player.StatCap(StatIds.Strength);
            float damageBefore = run.Player.Derived.DamageMultiplier;

            run.Submit(new EvolveCommand(run.Player.Id, TestContent.Brute1.Id));
            run.Tick(1);

            run.Rejections.Should().BeEmpty();
            run.Player.EvolutionIds.Should().ContainSingle().Which.Should().Be(TestContent.Brute1.Id);
            run.Player.Stats.Get(StatIds.Strength).Should().Be(strengthBefore + 10);
            run.Player.StatCap(StatIds.Strength).Should().Be(capBefore + 5 + 10);
            run.Player.Derived.DamageMultiplier.Should().BeGreaterThan(damageBefore);
        }

        private sealed class EvolutionRun
        {
            public EvolutionRun()
            {
                State = new RunStateBuilder().Build();
                Events = new SimulationEvents();
                Ticker = new SimulationTicker(State, Events);
                Player = new DemonBuilder().AsPlayer().At(0f, 0f).SpawnInto(State);
                Other = new DemonBuilder().At(0f, 3f).SpawnInto(State);
                Events.Subscribe<CommandRejected>(Rejections.Add);
                Events.Subscribe<Evolved>(Evolved.Add);
            }

            public RunState State { get; }

            public SimulationEvents Events { get; }

            public SimulationTicker Ticker { get; }

            public Demon Player { get; }

            public Demon Other { get; }

            public List<CommandRejected> Rejections { get; } = new List<CommandRejected>();

            public List<Evolved> Evolved { get; } = new List<Evolved>();

            public void ReachLevel(int level)
            {
                while (Player.Level < level)
                {
                    Player.GainXp(TestContent.Tuning.LevelXpForNext(Player.Level), TestContent.Tuning);
                }
            }

            public void Submit<TCommand>(in TCommand command)
                where TCommand : struct, ICommand
            {
                Ticker.Commands.Submit(in command);
            }

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
