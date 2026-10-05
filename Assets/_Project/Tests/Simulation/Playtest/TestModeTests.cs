#nullable enable
using AwesomeAssertions;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Events;
using DemonFighter.Simulation.Hazards;
using DemonFighter.Simulation.Playtest;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Tests.Combat;
using DemonFighter.Simulation.Tests.Content;
using DemonFighter.Simulation.Tests.Hazards;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Playtest
{
    /// <summary>Test mode (D-089): no damage from anything, and Biomass and stat points kept at 1000.</summary>
    public sealed class TestModeTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void Tick_TestModeOn_FillsBiomassAndStatPointsTo1000()
        {
            var scenario = new CombatScenario();

            scenario.Submit(new SetTestModeCommand(scenario.Target.Id, true));
            scenario.Tick(1);

            scenario.Target.InTestMode.Should().BeTrue();
            scenario.Target.Biomass.Should().Be(1000f);
            scenario.Target.Stats.UnspentPoints.Should().Be(1000);
            scenario.Target.BiomassEaten.Should().Be(0f, "the run summary counts only what was eaten");
            scenario.Attacker.InTestMode.Should().BeFalse();
            scenario.Attacker.Biomass.Should().Be(0f);
        }

        [Test]
        public void Tick_SpentStatPoint_IsBackInTheSamePass()
        {
            var scenario = new CombatScenario();
            scenario.Submit(new SetTestModeCommand(scenario.Attacker.Id, true));
            scenario.Tick(1);

            scenario.Submit(new SpendStatPointCommand(scenario.Attacker.Id, StatIds.Strength));
            scenario.Tick(1);

            scenario.Attacker.Stats.Get(StatIds.Strength).Should().Be(1);
            scenario.Attacker.Stats.UnspentPoints.Should().Be(1000);
        }

        [Test]
        public void ApplyPendingCommands_InAPausedMenu_RefillsToo()
        {
            var scenario = new CombatScenario();

            scenario.Submit(new SetTestModeCommand(scenario.Attacker.Id, true));
            scenario.Ticker.ApplyPendingCommands();

            scenario.Attacker.Biomass.Should().Be(1000f);
            scenario.Attacker.Stats.UnspentPoints.Should().Be(1000);
        }

        [Test]
        public void Tick_MoreThan1000_IsKept()
        {
            var scenario = new CombatScenario();
            scenario.Target.GainBiomass(1500f);
            scenario.Target.Stats.GrantPoints(1200);

            scenario.Submit(new SetTestModeCommand(scenario.Target.Id, true));
            scenario.Tick(1);

            scenario.Target.Biomass.Should().Be(1500f);
            scenario.Target.Stats.UnspentPoints.Should().Be(1200);
        }

        [Test]
        public void ApplyHit_TargetInTestMode_TakesNoDamageAndDoesNotBleed()
        {
            var scenario = new CombatScenario();
            scenario.Submit(new SetTestModeCommand(scenario.Target.Id, true));
            scenario.Tick(1);

            scenario.HitCore(TestContent.Bite);
            scenario.Tick(20);

            scenario.Target.Body.Core.Hp.Should().Be(scenario.Target.Body.Core.MaxHp);
            scenario.Target.Body.Core.IsBleeding.Should().BeFalse();
            scenario.Damage.Should().BeEmpty();
            scenario.Deaths.Should().BeEmpty();
        }

        [Test]
        public void Tick_BleedingWhenTestModeStarts_DrainsNothing()
        {
            var scenario = new CombatScenario();
            scenario.HitCore(TestContent.Bite);
            float afterHit = scenario.Target.Body.Core.Hp;

            scenario.Submit(new SetTestModeCommand(scenario.Target.Id, true));
            scenario.Tick(20);

            scenario.Target.Body.Core.Hp.Should().BeGreaterThanOrEqualTo(afterHit, "regeneration may heal, bleeding may not drain");
        }

        [Test]
        public void ApplyHit_TestModeAttackerOnSpines_IsNotPricked()
        {
            var scenario = new CombatScenario();
            scenario.Target.AttachPart(TestContent.Spines);
            scenario.Submit(new SetTestModeCommand(scenario.Attacker.Id, true));
            scenario.Tick(1);

            scenario.HitCore(TestContent.Bite);

            scenario.Target.Body.Core.Hp.Should().BeApproximately(48f, Tolerance);
            scenario.Attacker.Body.Core.Hp.Should().Be(scenario.Attacker.Body.Core.MaxHp);
        }

        [Test]
        public void Tick_TestModeOff_TakesDamageAgain()
        {
            var scenario = new CombatScenario();
            scenario.Submit(new SetTestModeCommand(scenario.Target.Id, true));
            scenario.Tick(1);
            scenario.Submit(new SetTestModeCommand(scenario.Target.Id, false));
            scenario.Tick(1);

            scenario.HitCore(TestContent.Bite);

            scenario.Target.InTestMode.Should().BeFalse();
            scenario.Target.Body.Core.Hp.Should().BeApproximately(48f, Tolerance);
        }

        [Test]
        public void Tick_TestModeDemonInLava_DoesNotBurn()
        {
            RunState state = new RunStateBuilder().Build();
            state.AttachWorld(HazardWorlds.Flat(state.Seed));
            var ticker = new SimulationTicker(state, new SimulationEvents());
            Demon demon = new DemonBuilder().At(HazardWorlds.LavaCenter.X, HazardWorlds.LavaCenter.Z).SpawnInto(state);

            ticker.Commands.Submit(new SetTestModeCommand(demon.Id, true));
            for (int i = 0; i < state.Config.TicksFor(2f); i++)
            {
                ticker.Tick();
            }

            demon.Hazard.Should().Be(HazardKind.Lava);
            demon.Body.Core.Hp.Should().Be(demon.Body.Core.MaxHp);
            demon.IsAlive.Should().BeTrue();
        }

        [Test]
        public void Tick_UnknownActor_IsRejected()
        {
            var scenario = new CombatScenario();

            scenario.Submit(new SetTestModeCommand(new DemonId(999), true));
            scenario.Tick(1);

            scenario.Rejections.Should().ContainSingle().Which.Reason.Should().Be(SetTestModeCommandHandler.UnknownActor);
        }
    }
}
