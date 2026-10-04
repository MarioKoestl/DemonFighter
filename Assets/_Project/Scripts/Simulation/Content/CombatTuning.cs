#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DemonFighter.Simulation.Content
{
    /// <summary>
    /// Every number of the combat, growth and eating rules in one immutable record (D-022, D-023, D-024, D-028,
    /// D-037). The design fixes which matchups are strong or weak; this record only says by how much. Created from
    /// the CombatTuning asset; the defaults are the v1 proposal.
    /// </summary>
    public sealed record CombatTuning
    {
        /// <summary>Damage factor when the attack is strong against the armor.</summary>
        public float StrongMultiplier { get; init; } = 1.5f;

        /// <summary>Damage factor when the armor is strong against the attack.</summary>
        public float WeakMultiplier { get; init; } = 0.6f;

        public float NeutralMultiplier { get; init; } = 1f;

        /// <summary>A part below this fraction of its HP counts as wounded.</summary>
        public float WoundedThreshold { get; init; } = 0.5f;

        /// <summary>Extra part HP per point of Constitution, as a fraction of the part's base HP.</summary>
        public float HpPerConstitution { get; init; } = 10f / 60f;

        /// <summary>Extra damage per point of Strength, as a fraction.</summary>
        public float DamagePerStrength { get; init; } = 0.05f;

        /// <summary>Extra move speed per point of Agility, as a fraction.</summary>
        public float SpeedPerAgility { get; init; } = 0.03f;

        /// <summary>Skill timing reduction per point of Agility, as a fraction.</summary>
        public float AttackSpeedPerAgility { get; init; } = 0.03f;

        public float StaminaBase { get; init; } = 100f;

        public float StaminaPerAgility { get; init; } = 10f;

        public float StaminaRegenPerSecond { get; init; } = 12f;

        /// <summary>Passive HP regeneration per second for every part (D-024).</summary>
        public float RegenBasePerSecond { get; init; } = 0.5f;

        public float RegenPerConstitution { get; init; } = 0.25f;

        /// <summary>Bleed duration reduction per point of Constitution, as a fraction, floored by the minimum.</summary>
        public float BleedReductionPerConstitution { get; init; } = 0.05f;

        public float MinBleedFraction { get; init; } = 0.2f;

        /// <summary>Seconds without dealing or taking damage before a demon is out of combat (D-014).</summary>
        public float InCombatSeconds { get; init; } = 5f;

        /// <summary>XP for a kill at equal tier.</summary>
        public float KillXpBase { get; init; } = 50f;

        /// <summary>Reward bonus per tier the victim stands above the killer, as a fraction.</summary>
        public float RewardBonusPerTierAbove { get; init; } = 0.5f;

        /// <summary>Reward factor for prey two or more tiers below (D-028).</summary>
        public float RewardFactorFarBelow { get; init; } = 0.1f;

        /// <summary>XP from level 1 to level 2.</summary>
        public float LevelXpBase { get; init; } = 100f;

        public float LevelXpExponent { get; init; } = 1.5f;

        public int StatPointsPerLevel { get; init; } = 3;

        /// <summary>Share of every skill XP gain that also becomes character XP (GAME_DESIGN, "Skill levels").</summary>
        public float CharacterXpPerSkillXp { get; init; } = 0.5f;

        /// <summary>Biomass moved from food to eater per second of eating.</summary>
        public float EatBiomassPerSecond { get; init; } = 15f;

        /// <summary>Reach for eating in meters per meter of body size.</summary>
        public float EatReachPerMeter { get; init; } = 1.5f;

        /// <summary>Biomass of a corpse per tier of the dead demon plus one.</summary>
        public float CorpseBiomassPerTier { get; init; } = 20f;

        public float FoodDecaySeconds { get; init; } = 90f;

        /// <summary>Body investment points (parts and upgrade levels) per tier gained (D-054).</summary>
        public int TierInvestmentStep { get; init; } = 4;

        /// <summary>Fraction of the spawn size a demon grows per tier gained (D-018).</summary>
        public float SizeStepPerTier { get; init; } = 0.5f;

        /// <summary>Fraction of base HP a part gains per upgrade level.</summary>
        public float PartHpPerUpgradeLevel { get; init; } = 0.15f;

        /// <summary>Seconds a demon transforms after confirming a mutation or evolution, invulnerable and unable to act (D-014).</summary>
        public float TransformationSeconds { get; init; } = 2f;

        /// <summary>Cost factor for a further copy of a part the body already holds.</summary>
        public float RepeatCostMultiplier { get; init; } = 1.5f;

        /// <summary>Fraction of the base cost one upgrade level costs, times the level reached.</summary>
        public float UpgradeCostFraction { get; init; } = 0.5f;

        /// <summary>Fraction of the base cost regrowing a lost part costs.</summary>
        public float RegrowCostFraction { get; init; } = 0.5f;

        /// <summary>Character levels needed per upgrade level: +2 needs level 4.</summary>
        public int CharacterLevelPerUpgradeLevel { get; init; } = 2;

        /// <summary>Seconds a knockback keeps pushing; the knockback distance of the skill is covered in this time.</summary>
        public float KnockbackSeconds { get; init; } = 0.3f;

        /// <summary>Character levels at which an evolution is offered, ascending (GAME_DESIGN, "Evolution": 5 and 10 in v1).</summary>
        public IReadOnlyList<int> EvolutionLevels { get; init; } = new[] { 5, 10 };

        /// <summary>Points a stat can hold before evolutions raise its cap.</summary>
        public int BaseStatCap { get; init; } = 10;

        /// <summary>Perception bonus per sense level, what a demon learns about a target it aims at (D-066); one pair of Eyes is one level.</summary>
        public float PerceptionPerSenseLevel { get; init; } = 0.3f;

        /// <summary>The base stats of v1 in display order.</summary>
        public IReadOnlyList<StatSpec> Stats { get; init; } = new[]
        {
            new StatSpec(StatIds.Strength, "Strength", "Damage, grab power and body slams."),
            new StatSpec(StatIds.Constitution, "Constitution", "Core and part HP, regeneration, bleeding resistance."),
            new StatSpec(StatIds.Agility, "Agility", "Movement speed, attack speed, stamina."),
        };

        /// <summary>
        /// The rock-paper-scissors from GAME_DESIGN "Damage model": Thick Hide shrugs off Cut and fears Blunt, Plates
        /// shrug off Pierce and Cut and crack under Blunt, Elastic Tissue swallows Blunt and tears under Cut.
        /// </summary>
        public float Multiplier(DamageType damage, DefenseType defense)
        {
            switch (defense)
            {
                case DefenseType.None:
                    return NeutralMultiplier;
                case DefenseType.ThickHide:
                    return damage == DamageType.Cut ? WeakMultiplier : damage == DamageType.Blunt ? StrongMultiplier : NeutralMultiplier;
                case DefenseType.Plates:
                    return damage == DamageType.Blunt ? StrongMultiplier : WeakMultiplier;
                case DefenseType.ElasticTissue:
                    return damage == DamageType.Cut ? StrongMultiplier : damage == DamageType.Blunt ? WeakMultiplier : NeutralMultiplier;
                default:
                    throw new ArgumentOutOfRangeException(nameof(defense), defense, "Unknown defense type.");
            }
        }

        /// <summary>XP needed to advance from the given character level to the next one.</summary>
        public float LevelXpForNext(int currentLevel)
        {
            if (currentLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(currentLevel), currentLevel, "Levels start at 1.");
            }

            return LevelXpBase * MathF.Pow(currentLevel, LevelXpExponent);
        }

        /// <summary>
        /// Share of the XP of a kill or the Biomass of a meal a demon gets, from the tier gap (GAME_DESIGN, "Reward
        /// scaling"): full at equal tier or one below, a bonus per tier above, very little for prey two or more tiers below.
        /// </summary>
        public float RewardFactor(int receiverTier, int sourceTier)
        {
            int gap = sourceTier - receiverTier;
            if (gap <= -2)
            {
                return RewardFactorFarBelow;
            }

            return 1f + RewardBonusPerTierAbove * Math.Max(0, gap);
        }

        /// <summary>Throws with the first content error found.</summary>
        public void Validate()
        {
            Require(WeakMultiplier > 0f && WeakMultiplier <= NeutralMultiplier, "WeakMultiplier must be in (0, Neutral].");
            Require(StrongMultiplier >= NeutralMultiplier, "StrongMultiplier must be at least Neutral.");
            Require(NeutralMultiplier > 0f, "NeutralMultiplier must be positive.");
            Require(WoundedThreshold > 0f && WoundedThreshold < 1f, "WoundedThreshold must be in (0, 1).");
            Require(HpPerConstitution >= 0f && DamagePerStrength >= 0f && SpeedPerAgility >= 0f && AttackSpeedPerAgility >= 0f, "Stat effects are never negative.");
            Require(AttackSpeedPerAgility < 1f, "AttackSpeedPerAgility must stay below 1.");
            Require(StaminaBase > 0f && StaminaPerAgility >= 0f && StaminaRegenPerSecond >= 0f, "Stamina values must be positive or zero where allowed.");
            Require(RegenBasePerSecond >= 0f && RegenPerConstitution >= 0f, "Regeneration is never negative.");
            Require(BleedReductionPerConstitution >= 0f && MinBleedFraction > 0f && MinBleedFraction <= 1f, "Bleed resistance values are out of range.");
            Require(InCombatSeconds >= 0f, "InCombatSeconds is never negative.");
            Require(KillXpBase >= 0f && RewardBonusPerTierAbove >= 0f, "Reward values are never negative.");
            Require(RewardFactorFarBelow >= 0f && RewardFactorFarBelow <= 1f, "RewardFactorFarBelow must be in [0, 1].");
            Require(LevelXpBase > 0f && LevelXpExponent >= 0f && StatPointsPerLevel >= 0, "Level values are out of range.");
            Require(CharacterXpPerSkillXp >= 0f, "CharacterXpPerSkillXp is never negative.");
            Require(EatBiomassPerSecond > 0f && EatReachPerMeter > 0f && CorpseBiomassPerTier >= 0f && FoodDecaySeconds > 0f, "Food values are out of range.");
            Require(TierInvestmentStep >= 1 && SizeStepPerTier >= 0f && PartHpPerUpgradeLevel >= 0f, "Body values are out of range.");
            Require(TransformationSeconds >= 0f && RepeatCostMultiplier >= 1f && UpgradeCostFraction >= 0f && RegrowCostFraction >= 0f && CharacterLevelPerUpgradeLevel >= 0, "Mutation values are out of range.");
            Require(KnockbackSeconds >= 0f, "KnockbackSeconds is never negative.");
            Require(EvolutionLevels != null && BaseStatCap >= 1, "Evolution values are out of range.");
            for (int i = 0; i < EvolutionLevels.Count; i++)
            {
                Require(EvolutionLevels[i] >= 1 && (i == 0 || EvolutionLevels[i] > EvolutionLevels[i - 1]), "EvolutionLevels must be positive and ascending.");
            }
            Require(Stats != null && Stats.Count > 0, "At least one stat is required.");
        }

        private static void Require([DoesNotReturnIf(false)] bool condition, string message)
        {
            if (!condition)
            {
                throw new ContentException("Combat tuning: " + message);
            }
        }
    }
}
