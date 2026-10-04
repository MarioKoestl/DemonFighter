#nullable enable
using AwesomeAssertions;
using DemonFighter.Simulation.Commands;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Progression;
using DemonFighter.Simulation.Tests.Builders;
using DemonFighter.Simulation.Tests.Combat;
using DemonFighter.Simulation.Tests.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Progression
{
    public sealed class XpSystemTests
    {
        private const float Tolerance = 0.01f;

        [Test]
        public void Kill_BlobKillsBlob_GrantsTheBaseKillXp()
        {
            var scenario = new CombatScenario();

            scenario.DamageSystem.ApplyDamage(scenario.Target, scenario.Target.Body.Core, 1000f, DamageType.Pierce, scenario.Attacker.Id);
            scenario.Events.Flush();

            scenario.Attacker.Xp.Should().BeApproximately(50f, Tolerance);
            scenario.XpGains.Should().ContainSingle();
            scenario.XpGains[0].Source.Should().Be(XpSource.Kill);
            scenario.XpGains[0].Amount.Should().BeApproximately(50f, Tolerance);
            scenario.LevelUps.Should().BeEmpty();
        }

        [Test]
        public void Kill_ElderKillsBlob_GrantsVeryLittleXp()
        {
            var scenario = new CombatScenario(TestContent.Elder);

            scenario.DamageSystem.ApplyDamage(scenario.Target, scenario.Target.Body.Core, 1000f, DamageType.Pierce, scenario.Attacker.Id);

            scenario.Attacker.Xp.Should().BeApproximately(5f, Tolerance);
        }

        [Test]
        public void Kill_BlobKillsElder_GrantsBonusXpAndLevelsUp()
        {
            var scenario = new CombatScenario();
            Demon elder = new DemonBuilder().WithSpec(TestContent.Elder).At(0f, 2f).SpawnInto(scenario.State);

            scenario.DamageSystem.ApplyDamage(elder, elder.Body.Core, 100000f, DamageType.Pierce, scenario.Attacker.Id);
            scenario.Events.Flush();

            scenario.Attacker.Level.Should().Be(4);
            scenario.Attacker.Stats.UnspentPoints.Should().Be(9);
            scenario.LevelUps.Should().ContainSingle();
            scenario.LevelUps[0].NewLevel.Should().Be(4);
            scenario.LevelUps[0].UnspentStatPoints.Should().Be(9);
        }

        [Test]
        public void Kill_BySelfInflictedDamage_GrantsNothing()
        {
            var scenario = new CombatScenario();

            scenario.DamageSystem.ApplyDamage(scenario.Target, scenario.Target.Body.Core, 1000f, DamageType.Pierce, DemonId.None);
            scenario.Events.Flush();

            scenario.XpGains.Should().BeEmpty();
            scenario.Attacker.Xp.Should().Be(0f);
        }

        [Test]
        public void Tick_LandedBite_FeedsHalfTheSkillXpIntoCharacterXp()
        {
            var scenario = new CombatScenario();
            scenario.StartBiteAndReachActiveWindow();

            scenario.Submit(new ReportHitCommand(scenario.Attacker.Id, scenario.Target.Id, scenario.Target.Body.Core.Index));
            scenario.Tick(1);

            scenario.Attacker.Xp.Should().BeApproximately(5f, Tolerance);
            scenario.XpGains.Should().ContainSingle().Which.Source.Should().Be(XpSource.SkillUse);
        }
    }
}
