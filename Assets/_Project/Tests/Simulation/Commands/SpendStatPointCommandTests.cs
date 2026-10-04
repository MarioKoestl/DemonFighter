#nullable enable
using AwesomeAssertions;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Tests.Combat;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Commands
{
    public sealed class SpendStatPointCommandTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void Tick_SpendOnStrength_RaisesTheStatAndTheDamage()
        {
            var scenario = new CombatScenario();
            scenario.Attacker.Stats.GrantPoints(2);

            scenario.Submit(new SpendStatPointCommand(scenario.Attacker.Id, StatIds.Strength));
            scenario.Tick(1);

            scenario.Attacker.Stats.Get(StatIds.Strength).Should().Be(1);
            scenario.Attacker.Stats.UnspentPoints.Should().Be(1);
            scenario.Attacker.Derived.DamageMultiplier.Should().BeApproximately(1.05f, Tolerance);
            scenario.StatSpends.Should().ContainSingle();
            scenario.StatSpends[0].Stat.Should().Be(StatIds.Strength);
            scenario.StatSpends[0].NewValue.Should().Be(1);
            scenario.StatSpends[0].UnspentStatPoints.Should().Be(1);
            scenario.Rejections.Should().BeEmpty();
        }

        [Test]
        public void Tick_SpendOnConstitution_RescalesTheCore()
        {
            var scenario = new CombatScenario();
            scenario.Attacker.Stats.GrantPoints(1);

            scenario.Submit(new SpendStatPointCommand(scenario.Attacker.Id, StatIds.Constitution));
            scenario.Tick(1);

            scenario.Attacker.Body.Core.MaxHp.Should().BeApproximately(70f, 0.01f);
        }

        [Test]
        public void Tick_SpendWithoutPoints_IsRejected()
        {
            var scenario = new CombatScenario();

            scenario.Submit(new SpendStatPointCommand(scenario.Attacker.Id, StatIds.Strength));
            scenario.Tick(1);

            scenario.Rejections.Should().ContainSingle().Which.Reason.Should().Be(SpendStatPointCommandHandler.NoPoints);
            scenario.Attacker.Stats.Get(StatIds.Strength).Should().Be(0);
        }

        [Test]
        public void Tick_SpendOnUnknownStat_IsRejected()
        {
            var scenario = new CombatScenario();
            scenario.Attacker.Stats.GrantPoints(1);

            scenario.Submit(new SpendStatPointCommand(scenario.Attacker.Id, new StatId("stat.nope")));
            scenario.Tick(1);

            scenario.Rejections.Should().ContainSingle().Which.Reason.Should().Be(SpendStatPointCommandHandler.UnknownStat);
            scenario.Attacker.Stats.UnspentPoints.Should().Be(1);
        }

        [Test]
        public void Tick_SpendWhenDead_IsRejected()
        {
            var scenario = new CombatScenario();
            scenario.Attacker.Stats.GrantPoints(1);
            scenario.Attacker.Body.Core.ApplyDamage(1000f);

            scenario.Submit(new SpendStatPointCommand(scenario.Attacker.Id, StatIds.Strength));
            scenario.Tick(1);

            scenario.Rejections.Should().ContainSingle().Which.Reason.Should().Be(SpendStatPointCommandHandler.ActorDead);
        }
    }
}
