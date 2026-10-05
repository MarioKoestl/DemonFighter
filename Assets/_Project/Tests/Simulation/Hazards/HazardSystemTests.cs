#nullable enable
using System.Collections.Generic;
using AwesomeAssertions;
using DemonFighter.Simulation.Anatomy;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Hazards;
using DemonFighter.Simulation.Progression;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Hazards
{
    public sealed class HazardSystemTests
    {
        private const float Tolerance = 0.01f;

        private RunState _state = null!;
        private SimulationEvents _events = null!;
        private SimulationTicker _ticker = null!;
        private List<DamageApplied> _damage = null!;

        [SetUp]
        public void CreateRun()
        {
            _state = new RunStateBuilder().Build();
            _state.AttachWorld(HazardWorlds.Flat(_state.Seed));
            _events = new SimulationEvents();
            _ticker = new SimulationTicker(_state, _events);
            _damage = new List<DamageApplied>();
            _events.Subscribe<DamageApplied>(_damage.Add);
        }

        [Test]
        public void Tick_DemonInLava_BurnsAFifthOfItsCorePerSecond()
        {
            Demon demon = new DemonBuilder().At(HazardWorlds.LavaCenter.X, HazardWorlds.LavaCenter.Z).SpawnInto(_state);
            float max = demon.Body.Core.MaxHp;
            float regen = demon.Derived.RegenPerSecond;

            TickSeconds(1f);

            demon.Hazard.Should().Be(HazardKind.Lava);
            demon.Body.Core.Hp.Should().BeInRange(max * 0.8f - Tolerance, max * 0.8f + regen + Tolerance);
            _damage.Should().NotBeEmpty().And.OnlyContain(d => d.DamageType == DamageType.Fire && !d.Attacker.IsValid);
        }

        [Test]
        public void Tick_DemonInAFissure_BurnsSlowerThanInLava()
        {
            Demon demon = new DemonBuilder().At(HazardWorlds.FissureCenter.X, HazardWorlds.FissureCenter.Z).SpawnInto(_state);
            float max = demon.Body.Core.MaxHp;
            float regen = demon.Derived.RegenPerSecond;

            TickSeconds(1f);

            demon.Hazard.Should().Be(HazardKind.Fissure);
            demon.Body.Core.Hp.Should().BeInRange(max * 0.95f - Tolerance, max * 0.95f + regen + Tolerance);
        }

        [Test]
        public void Tick_DemonOnSafeGround_DoesNotBurn()
        {
            Demon demon = new DemonBuilder().At(-50f, -50f).SpawnInto(_state);

            TickSeconds(1f);

            demon.Hazard.Should().Be(HazardKind.None);
            demon.Body.Core.Hp.Should().Be(demon.Body.Core.MaxHp);
            _damage.Should().BeEmpty();
        }

        [Test]
        public void Tick_DemonWithLegsInLava_BurnsTheLegsFirst()
        {
            Demon demon = new DemonBuilder().At(HazardWorlds.LavaCenter.X, HazardWorlds.LavaCenter.Z).SpawnInto(_state);
            BodyPart legs = demon.AttachPart(TestContent.Legs);

            TickSeconds(1f);

            legs.Hp.Should().BeLessThan(legs.MaxHp);
            demon.Body.Core.Hp.Should().Be(demon.Body.Core.MaxHp);
        }

        [Test]
        public void Tick_LegsBurnedAway_TheFireMovesToTheCore()
        {
            Demon demon = new DemonBuilder().At(HazardWorlds.LavaCenter.X, HazardWorlds.LavaCenter.Z).SpawnInto(_state);
            BodyPart legs = demon.AttachPart(TestContent.Legs);

            TickSeconds(7f);

            legs.IsLost.Should().BeTrue();
            demon.Body.Core.Hp.Should().BeLessThan(demon.Body.Core.MaxHp);
        }

        [Test]
        public void Tick_Fire_LeavesNoBleedingWound()
        {
            Demon demon = new DemonBuilder().At(HazardWorlds.LavaCenter.X, HazardWorlds.LavaCenter.Z).SpawnInto(_state);

            TickSeconds(1f);

            demon.Body.Core.IsBleeding.Should().BeFalse();
        }

        [Test]
        public void Tick_StayingInLava_KillsWithoutAKillerAndTheSummarySaysLava()
        {
            Demon player = new DemonBuilder().AsPlayer().At(HazardWorlds.LavaCenter.X, HazardWorlds.LavaCenter.Z).SpawnInto(_state);
            var died = new List<DemonDied>();
            _events.Subscribe<DemonDied>(died.Add);

            TickSeconds(10f);

            player.IsAlive.Should().BeFalse();
            died.Should().ContainSingle().Which.Killer.IsValid.Should().BeFalse();
            RunSummary summary = RunSummary.From(_state, player, DemonId.None);
            summary.DeathHazard.Should().Be(HazardKind.Lava);
            summary.KillerName.Should().BeEmpty();
        }

        [Test]
        public void Tick_TransformingDemonInLava_IsSpared()
        {
            Demon demon = new DemonBuilder().At(HazardWorlds.LavaCenter.X, HazardWorlds.LavaCenter.Z).SpawnInto(_state);
            demon.StartTransformation(_state.Tick, _state.Tick + _state.Config.TicksFor(5f));

            TickSeconds(1f);

            demon.Body.Core.Hp.Should().Be(demon.Body.Core.MaxHp);
        }

        [Test]
        public void Tick_RunWithoutAWorld_BurnsNothing()
        {
            RunState state = new RunStateBuilder().Build();
            var ticker = new SimulationTicker(state, new SimulationEvents());
            Demon demon = new DemonBuilder().SpawnInto(state);

            for (int i = 0; i < 20; i++)
            {
                ticker.Tick();
            }

            demon.Hazard.Should().Be(HazardKind.None);
            demon.Body.Core.Hp.Should().Be(demon.Body.Core.MaxHp);
        }

        private void TickSeconds(float seconds)
        {
            int ticks = _state.Config.TicksFor(seconds);
            for (int i = 0; i < ticks; i++)
            {
                _ticker.Tick();
            }
        }
    }
}
