#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AwesomeAssertions;
using DemonFighter.Simulation.Ai;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Persistence;
using DemonFighter.Simulation.Tests.Ai;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Tests.Content;
using DemonFighter.Simulation.Worldgen;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Persistence
{
    public sealed class RunPersistenceTests
    {
        private const float Tolerance = 0.001f;
        private static readonly BiomeSpec Biome = BiomeSpec.AshCavern;

        [Test]
        public void CaptureRestoreCapture_GivesTheSameSnapshot()
        {
            var run = new SavedRun(Biome.BlobArchetype);
            run.Player.GainBiomass(50f);
            run.Player.AttachPart(TestContent.Arm);
            run.Player.Stats.GrantPoints(2);
            run.Player.Stats.TrySpendPoint(StatIds.Agility);
            run.Ticker.Damage.ApplyDamage(run.Blob, run.Blob.Body.Core, 10f, DamageType.Cut, run.Player.Id);
            run.Ticker.Damage.ApplyDamage(run.Victim, run.Victim.Body.Core, 100000f, DamageType.Pierce, run.Player.Id);
            run.Tick(30);

            RunSnapshot first = RunPersistence.Capture(run.State, run.Ticker);
            SimulationTicker restored = RunPersistence.Restore(first, run.State.Catalog, Biome, new CavernWorldGenerator(), new SimulationEvents());
            RunSnapshot second = RunPersistence.Capture(restored.State, restored);

            run.State.Food.Should().NotBeEmpty();
            second.Should().BeEquivalentTo(first);
            restored.State.Tick.Should().Be(run.State.Tick);
            restored.State.Demons.Should().HaveCount(run.State.Demons.Count);
            restored.State.Food.Should().HaveCount(run.State.Food.Count);
            restored.State.World.Should().NotBeNull();
        }

        [Test]
        public void Restore_ContinuesTheRandomSequenceAndTheIds()
        {
            var run = new SavedRun(null);
            run.Tick(5);
            RunSnapshot snapshot = RunPersistence.Capture(run.State, run.Ticker);

            SimulationTicker restored = RunPersistence.Restore(snapshot, run.State.Catalog, Biome, new CavernWorldGenerator(), new SimulationEvents());

            restored.State.Rng.NextUInt64().Should().Be(run.State.Rng.NextUInt64());
            restored.State.DemonIds.Next().Should().Be(run.State.DemonIds.Next());
            restored.State.FoodIds.LastIssued.Should().Be(run.State.FoodIds.LastIssued);
            restored.Spawning.NextSpawnTick.Should().Be(run.Ticker.Spawning.NextSpawnTick);
        }

        [Test]
        public void Restore_ThenTheSameCommands_TicksInLockstep()
        {
            var run = new SavedRun(null);
            Move(run.Ticker, run.Player, 20);
            RunSnapshot snapshot = RunPersistence.Capture(run.State, run.Ticker);
            SimulationTicker restored = RunPersistence.Restore(snapshot, run.State.Catalog, Biome, new CavernWorldGenerator(), new SimulationEvents());
            restored.State.TryGetDemon(run.Player.Id, out Demon? twin).Should().BeTrue();

            Move(run.Ticker, run.Player, 20);
            Move(restored, twin!, 20);

            twin!.Position.X.Should().BeApproximately(run.Player.Position.X, Tolerance);
            twin.Position.Z.Should().BeApproximately(run.Player.Position.Z, Tolerance);
            twin.Stamina.Should().BeApproximately(run.Player.Stamina, Tolerance);
            twin.Body.TotalHp.Should().BeApproximately(run.Player.Body.TotalHp, Tolerance);
            restored.State.Tick.Should().Be(run.State.Tick);
        }

        [Test]
        public void Restore_GivesLivingAiDemonsABrainOfTheirArchetypeAndTheDeadNone()
        {
            var run = new SavedRun(TestArchetypes.Wanderer);
            run.Ticker.Damage.ApplyDamage(run.Victim, run.Victim.Body.Core, 100000f, DamageType.Pierce, run.Player.Id);
            run.Tick(2);
            RunSnapshot snapshot = RunPersistence.Capture(run.State, run.Ticker);

            SimulationTicker restored = RunPersistence.Restore(snapshot, run.State.Catalog, Biome, new CavernWorldGenerator(), new SimulationEvents());

            snapshot.Demons.Single(d => d.Id == run.Blob.Id.Value).ArchetypeName.Should().Be(TestArchetypes.Wanderer.Name);
            snapshot.Demons.Single(d => d.Id == run.Player.Id.Value).ArchetypeName.Should().BeEmpty();
            restored.Ai.Brains.Should().ContainSingle();
            restored.Ai.Brains[0].Demon.Id.Should().Be(run.Blob.Id);
            restored.Ai.Brains[0].Archetype.Should().BeSameAs(Biome.BlobArchetype);
            restored.State.TryGetDemon(run.Victim.Id, out Demon? corpse).Should().BeTrue();
            corpse!.IsAlive.Should().BeFalse();
        }

        [Test]
        public void Restore_KeepsTheBodyTheSkillsAndTheProgress()
        {
            var run = new SavedRun(null);
            run.Player.GainBiomass(80f);
            run.Player.AttachPart(TestContent.Legs);
            while (run.Player.Level < 3)
            {
                run.Player.GainXp(TestContent.Tuning.LevelXpForNext(run.Player.Level), TestContent.Tuning);
            }

            run.Ticker.Damage.ApplyDamage(run.Player, run.Player.Body.Core, 7f, DamageType.Pierce, run.Blob.Id);
            run.Tick(3);
            RunSnapshot snapshot = RunPersistence.Capture(run.State, run.Ticker);

            SimulationTicker restored = RunPersistence.Restore(snapshot, run.State.Catalog, Biome, new CavernWorldGenerator(), new SimulationEvents());
            restored.State.TryGetDemon(run.Player.Id, out Demon? twin).Should().BeTrue();

            twin!.Level.Should().Be(3);
            twin.Stats.UnspentPoints.Should().Be(run.Player.Stats.UnspentPoints);
            twin.Biomass.Should().BeApproximately(80f, Tolerance);
            twin.Body.Parts.Select(p => p.Spec.Id).Should().Equal(run.Player.Body.Parts.Select(p => p.Spec.Id));
            twin.Body.Core.Hp.Should().BeApproximately(run.Player.Body.Core.Hp, Tolerance);
            twin.Skills.Select(s => s.Spec.Id).Should().Equal(run.Player.Skills.Select(s => s.Spec.Id));
            twin.SizeMeters.Should().BeApproximately(run.Player.SizeMeters, Tolerance);
            twin.LastAttackedBy.Should().Be(run.Blob.Id);
            twin.CanSprint.Should().BeTrue();
        }

        [Test]
        public void Restore_WithAnotherLayoutVersion_Throws()
        {
            var run = new SavedRun(null);
            RunSnapshot snapshot = RunPersistence.Capture(run.State, run.Ticker);
            snapshot.Version = RunSnapshot.CurrentVersion + 1;

            Action act = () => RunPersistence.Restore(snapshot, run.State.Catalog, Biome, new CavernWorldGenerator(), new SimulationEvents());

            act.Should().Throw<InvalidOperationException>();
        }

        private static void Move(SimulationTicker ticker, Demon demon, int ticks)
        {
            for (int i = 0; i < ticks; i++)
            {
                ticker.Commands.Submit(new MoveCommand(demon.Id, new Vector2(1f, 0f), sprint: false));
                ticker.Tick();
            }
        }

        private sealed class SavedRun
        {
            public SavedRun(ArchetypeSpec? brain)
            {
                State = new RunStateBuilder().Build();
                State.AttachWorld(new CavernWorldGenerator().Generate(State.Seed, Biome));
                Events = new SimulationEvents();
                Ticker = new SimulationTicker(State, Events);
                Player = new DemonBuilder().AsPlayer().At(0f, 0f).SpawnInto(State);
                Blob = new DemonBuilder().At(0f, 3f).SpawnInto(State);
                Victim = new DemonBuilder().At(5f, 5f).SpawnInto(State);
                if (brain != null)
                {
                    Ticker.Ai.AddBrain(Blob, brain, State.World!.Bounds, null);
                }
            }

            public RunState State { get; }

            public SimulationEvents Events { get; }

            public SimulationTicker Ticker { get; }

            public Demon Player { get; }

            public Demon Blob { get; }

            public Demon Victim { get; }

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
