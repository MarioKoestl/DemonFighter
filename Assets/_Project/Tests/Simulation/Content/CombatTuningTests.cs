#nullable enable
using System;
using AwesomeAssertions;
using DemonFighter.Simulation.Content;
using NUnit.Framework;

namespace DemonFighter.Simulation.Tests.Content
{
    public sealed class CombatTuningTests
    {
        private const float Tolerance = 0.0001f;
        private static readonly CombatTuning Tuning = new CombatTuning();

        [TestCase(DamageType.Pierce, DefenseType.ThickHide, 1f)]
        [TestCase(DamageType.Cut, DefenseType.ThickHide, 0.6f)]
        [TestCase(DamageType.Blunt, DefenseType.ThickHide, 1.5f)]
        [TestCase(DamageType.Pierce, DefenseType.Plates, 0.6f)]
        [TestCase(DamageType.Cut, DefenseType.Plates, 0.6f)]
        [TestCase(DamageType.Blunt, DefenseType.Plates, 1.5f)]
        [TestCase(DamageType.Pierce, DefenseType.ElasticTissue, 1f)]
        [TestCase(DamageType.Cut, DefenseType.ElasticTissue, 1.5f)]
        [TestCase(DamageType.Blunt, DefenseType.ElasticTissue, 0.6f)]
        [TestCase(DamageType.Pierce, DefenseType.None, 1f)]
        [TestCase(DamageType.Blunt, DefenseType.None, 1f)]
        public void Multiplier_DesignMatrix_MatchesGameDesign(DamageType damage, DefenseType defense, float expected)
        {
            float multiplier = Tuning.Multiplier(damage, defense);

            multiplier.Should().BeApproximately(expected, Tolerance);
        }

        [TestCase(0, 0, 1, 100f)]
        [TestCase(0, 0, 5, 200f)]
        [TestCase(0, 1, 1, 300f)]
        [TestCase(0, 1, 3, 450f)]
        [TestCase(3, 0, 1, 10f)]
        public void KillXp_GrowsWithTheVictimsTierAndLevel(int killerTier, int victimTier, int victimLevel, float expected)
        {
            Tuning.KillXp(killerTier, victimTier, victimLevel).Should().BeApproximately(expected, Tolerance);
        }

        [Test]
        public void LevelXpForNext_TierTwo_CostsThreeTimesAsMuch()
        {
            Tuning.LevelXpForNext(3, 2).Should().BeApproximately(Tuning.LevelXpForNext(3) * 3f, 0.01f);
        }

        [Test]
        public void LevelXpForNext_Level1_IsTheBase()
        {
            float xp = Tuning.LevelXpForNext(1);

            xp.Should().BeApproximately(100f, Tolerance);
        }

        [Test]
        public void LevelXpForNext_Level4_IsBaseTimesEight()
        {
            float xp = Tuning.LevelXpForNext(4);

            xp.Should().BeApproximately(800f, 0.01f);
        }

        [TestCase(0, 0, 1f)]
        [TestCase(0, 1, 1.5f)]
        [TestCase(0, 2, 2f)]
        [TestCase(1, 0, 1f)]
        [TestCase(2, 0, 0.1f)]
        [TestCase(6, 0, 0.1f)]
        public void RewardFactor_TierGap_MatchesGameDesign(int receiverTier, int sourceTier, float expected)
        {
            float factor = Tuning.RewardFactor(receiverTier, sourceTier);

            factor.Should().BeApproximately(expected, Tolerance);
        }

        [Test]
        public void Validate_Defaults_Pass()
        {
            Action act = () => Tuning.Validate();

            act.Should().NotThrow();
        }

        [Test]
        public void Validate_StrongBelowNeutral_Throws()
        {
            CombatTuning broken = Tuning with { StrongMultiplier = 0.9f };

            Action act = () => broken.Validate();

            act.Should().Throw<ContentException>();
        }

        [Test]
        public void Stats_Default_ListsTheThreeV1Stats()
        {
            var ids = new StatId[Tuning.Stats.Count];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = Tuning.Stats[i].Id;
            }

            ids.Should().Equal(StatIds.Strength, StatIds.Constitution, StatIds.Agility);
        }
    }
}
