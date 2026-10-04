#nullable enable
using AwesomeAssertions;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Combat
{
    public sealed class StatusSystemTests
    {
        private const float Tolerance = 0.01f;

        [Test]
        public void Tick_BleedingPart_DrainsAtTheBleedRateNetOfRegeneration()
        {
            var scenario = new CombatScenario();
            scenario.HitCore(TestContent.Bite);

            scenario.Tick(20);

            scenario.Target.Body.Core.Hp.Should().BeApproximately(48f - 2f + 0.5f, Tolerance);
            scenario.Target.Body.Core.IsBleeding.Should().BeTrue();
        }

        [Test]
        public void Tick_PastTheBleedDuration_StopsDrainingAndKeepsRegenerating()
        {
            var scenario = new CombatScenario();
            scenario.HitCore(TestContent.Bite);

            scenario.Tick(80);

            scenario.Target.Body.Core.IsBleeding.Should().BeFalse();
            scenario.Target.Body.Core.Hp.Should().BeApproximately(48f - 6f + 2f, Tolerance);
        }

        [Test]
        public void Tick_BleedDamage_IsReportedWithoutAnAttacker()
        {
            var scenario = new CombatScenario();
            scenario.HitCore(TestContent.Bite);
            scenario.Damage.Clear();

            scenario.Tick(1);

            scenario.Damage.Should().NotBeEmpty();
            scenario.Damage[0].Attacker.Should().Be(DemonId.None);
            scenario.Damage[0].DamageType.Should().Be(DamageType.Pierce);
        }

        [Test]
        public void Tick_WoundedPartWithoutBleeding_RegeneratesTowardMax()
        {
            var scenario = new CombatScenario();
            scenario.DamageSystem.ApplyDamage(scenario.Target, scenario.Target.Body.Core, 10f, DamageType.Blunt, DemonId.None);

            scenario.Tick(40);

            scenario.Target.Body.Core.Hp.Should().BeApproximately(51f, Tolerance);
        }

        [Test]
        public void Tick_SpentStamina_Refills()
        {
            var scenario = new CombatScenario();
            scenario.Attacker.TrySpendStamina(50f);

            scenario.Tick(20);

            scenario.Attacker.Stamina.Should().BeApproximately(62f, Tolerance);
        }

        [Test]
        public void Tick_BleedingWithOneHpLeft_KillsWithoutAKiller()
        {
            var scenario = new CombatScenario();
            scenario.DamageSystem.ApplyDamage(scenario.Target, scenario.Target.Body.Core, 59f, DamageType.Blunt, DemonId.None);
            scenario.Target.Body.Core.StartBleeding(3f, 2f, DamageType.Cut);

            scenario.Tick(20);

            scenario.Target.IsAlive.Should().BeFalse();
            scenario.Deaths.Should().ContainSingle();
            scenario.Deaths[0].Killer.Should().Be(DemonId.None);
            scenario.State.Food.Should().ContainSingle();
        }

        [Test]
        public void Tick_DeadDemon_IsLeftAlone()
        {
            var scenario = new CombatScenario();
            for (int i = 0; i < 5; i++)
            {
                scenario.HitCore(TestContent.Bite);
            }

            int damageEvents = scenario.Damage.Count;
            scenario.Tick(40);

            scenario.Damage.Count.Should().Be(damageEvents);
            scenario.Target.Body.Core.Hp.Should().BeApproximately(0f, Tolerance);
        }
    }
}
