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

            scenario.XpGains.Should().ContainSingle();
            scenario.XpGains[0].Source.Should().Be(XpSource.Kill);
            scenario.XpGains[0].Amount.Should().BeApproximately(100f, Tolerance);
            scenario.Attacker.Level.Should().Be(2, "one kill of an equal blob is a level at the start (D-090)");
            scenario.LevelUps.Should().ContainSingle();
        }

        // The higher the victim, the more it is worth: a level 5 blob gives double (D-090).
        [Test]
        public void Kill_VictimOfLevelFive_GrantsDoubleXp()
        {
            var scenario = new CombatScenario();
            while (scenario.Target.Level < 5)
            {
                scenario.Target.GainXp(TestContent.Tuning.LevelXpForNext(scenario.Target.Level), TestContent.Tuning);
            }

            scenario.DamageSystem.ApplyDamage(scenario.Target, scenario.Target.Body.Core, 100000f, DamageType.Pierce, scenario.Attacker.Id);
            scenario.Events.Flush();

            scenario.XpGains.Should().ContainSingle().Which.Amount.Should().BeApproximately(200f, Tolerance);
        }

        [Test]
        public void Kill_ElderKillsBlob_GrantsVeryLittleXp()
        {
            var scenario = new CombatScenario(TestContent.Elder);

            scenario.DamageSystem.ApplyDamage(scenario.Target, scenario.Target.Body.Core, 1000f, DamageType.Pierce, scenario.Attacker.Id);

            scenario.Attacker.Xp.Should().BeApproximately(10f, Tolerance);
        }

        [Test]
        public void Kill_BlobKillsElder_GrantsBonusXpAndLevelsUp()
        {
            var scenario = new CombatScenario();
            Demon elder = new DemonBuilder().WithSpec(TestContent.Elder).At(0f, 2f).SpawnInto(scenario.State);

            scenario.DamageSystem.ApplyDamage(elder, elder.Body.Core, 100000f, DamageType.Pierce, scenario.Attacker.Id);
            scenario.Events.Flush();

            // 100 x (tier 6 + 1) x the bonus of four for six tiers above = 2800 XP: level 5, just short of 6.
            scenario.Attacker.Level.Should().Be(5);
            scenario.Attacker.Stats.UnspentPoints.Should().Be(12);
            scenario.LevelUps.Should().ContainSingle();
            scenario.LevelUps[0].NewLevel.Should().Be(5);
            scenario.LevelUps[0].UnspentStatPoints.Should().Be(12);
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

        // Skill XP raises the skill and nothing else; character XP comes from kills only (D-090).
        [Test]
        public void Tick_LandedBite_GrantsSkillXpButNoCharacterXp()
        {
            var scenario = new CombatScenario();
            scenario.StartBiteAndReachActiveWindow();

            scenario.Submit(new ReportHitCommand(scenario.Attacker.Id, scenario.Target.Id, scenario.Target.Body.Core.Index));
            scenario.Tick(1);

            scenario.SkillXp.Should().ContainSingle();
            scenario.Attacker.Xp.Should().Be(0f);
            scenario.XpGains.Should().BeEmpty();
        }
    }
}
